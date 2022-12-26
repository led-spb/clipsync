using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace clipsync;

public partial class SyncForm : Form
{
    private WebDavHelper webDavHelper;
    private Task? monitorTask;
    private Uri syncUrl;

    private String clipboardData = "";

    private bool isSyncActive {get => monitorTask != null && !monitorTask.IsCompleted; }
    private AppSettings appSettings;
    public SyncForm()
    {
        InitializeComponent();
        appSettings = new AppSettings();

        NativeMethods.AddClipboardFormatListener(Handle);
        actionStopped();

        syncUrl = new Uri("http://localhost");
        webDavHelper = new WebDavHelper(new HttpClient()){period = 3000};
        webDavHelper.OnChanged += onRemoteClipboardChange;
        webDavHelper.OnError += onSyncError;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE && Clipboard.ContainsText())
        {
            if(clipboardData != Clipboard.GetText() && isSyncActive ){
                clipboardData = Clipboard.GetText();
                logTextBox.Text = clipboardData;

                var encrypted = EncryptHelper.encryptAsync(clipboardData, textBoxPassword.Text).Result;
                webDavHelper.SaveData(syncUrl, new ByteArrayContent(encrypted)).ConfigureAwait(false);
            }
        }
        base.WndProc(ref m);
    }

    private async void onRemoteClipboardChange(HttpResponseMessage response){
        byte[] data = await response.Content.ReadAsByteArrayAsync();
        if( data.Length<16 ) return;
        data = await EncryptHelper.decryptAsync(data, textBoxPassword.Text);
        using(MemoryStream stream = new MemoryStream(data))
        using(StreamReader reader = new StreamReader(stream)){
            clipboardData = reader.ReadToEnd();
            if( clipboardData != null && clipboardData != String.Empty){
                logTextBox.Text = clipboardData;
                Clipboard.SetText(clipboardData);
            }
        }            
    }
    private void onSyncError(HttpResponseMessage response){
        webDavHelper.Stop();
        actionStopped(response.ReasonPhrase==null? "Unknown error" : response.ReasonPhrase.ToString());
    }

    private HttpClient buildHttpClient(Uri syncUrl){
        var baseUrl = new UriBuilder(syncUrl){Path="/"}.Uri;
        var credentials = new NetworkCredential{
                UserName = textBoxUsername.Text,
                Password = textBoxPassword.Text,
                Domain = textBoxDomain.Text
        };

        var credentialsCache = new CredentialCache();
        credentialsCache.Add(baseUrl, "NTLM", credentials);
        var handler = new HttpClientHandler() { Credentials = credentialsCache, PreAuthenticate = true };
        return new HttpClient(handler);
    }

    private void BtnStart_Click(object sender, EventArgs e)
    {
        if( isSyncActive )
            return;

        syncUrl = new Uri(textBoxURL.Text);
        var httpClient = buildHttpClient(syncUrl);

        webDavHelper.httpClient = buildHttpClient(syncUrl);
        monitorTask = webDavHelper.StartAsync(syncUrl).ContinueWith(onMonitorStopped);
        actionStarted();
    }

    private void btnStop_Click(object sender, EventArgs e)
    {
        if( !isSyncActive )
            return;

        this.webDavHelper.Stop();
        actionStopped();
    }

    private void onMonitorStopped(Task<WebDavHelper> task){
        if(task.Exception != null){
            actionStopped(task.Exception.Message);
        }else{
            actionStopped();
        }
    }

    private void actionStarted(){
        btnStart.Enabled = false;
        btnStop.Enabled = true;
        groupBox1.Enabled = false; 
        syncStatusLabel.Text = "started";
        logTextBox.Text = String.Empty;
        detailStatusLabel.Text = "...";
    }

    private void actionStopped(){
        actionStopped(String.Empty);
    }

    private void actionStopped(String reason){
        btnStop.Enabled = false;
        btnStart.Enabled = true;
        groupBox1.Enabled = true; 
        syncStatusLabel.Text = "stopped";
        if( reason != String.Empty){
            logTextBox.Text = reason;
            detailStatusLabel.Text = reason;
        }
    }

    private void SyncForm_Load(object sender, EventArgs e)
    {
        this.textBoxURL.DataBindings.Add(
            new Binding("Text", appSettings, "syncUrl")
        );

        this.textBoxUsername.DataBindings.Add(
            new Binding("Text", appSettings, "userName")
        );

        this.textBoxDomain.DataBindings.Add(
            new Binding("Text", appSettings, "domain")
        );
    }

    private void SyncForm_Resize(object sender, EventArgs e){
        if (this.WindowState == FormWindowState.Minimized)  
        {  
            Hide();
            notifyIcon.Visible = true;
        }          
    }
    
    private void notifyIcon_Click(object sender, EventArgs e){
        Show();  
        this.WindowState = FormWindowState.Normal;  
        notifyIcon.Visible = false;        
    }
    private void SyncForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        this.appSettings.Save();
    }
}
