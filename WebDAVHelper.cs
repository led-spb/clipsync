using System.Net;

namespace clipsync;


public class WebDavHelper {
    public delegate void MonitorEvent(HttpResponseMessage response);

    public HttpClient httpClient{get; set;}
    public event MonitorEvent OnChanged = delegate {};
    public event MonitorEvent OnError = delegate {};
    public int period { get; set; } = 1000;
    private bool stopFlag = false;
    private CancellationTokenSource cancelTokenSource;

    public WebDavHelper(HttpClient httpClient){
        this.httpClient = httpClient;
        this.cancelTokenSource = new CancellationTokenSource();
    }

    public async Task<WebDavHelper> StartAsync(Uri uri)
    {
        this.cancelTokenSource = new CancellationTokenSource();

        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(period));
        try{
            await SaveData(uri, new StringContent(""));
            httpClient.DefaultRequestHeaders.IfModifiedSince = DateTime.UtcNow;
            stopFlag = false;
            while (!stopFlag){
                await timer.WaitForNextTickAsync(cancelTokenSource.Token);

                var response = await httpClient.GetAsync(uri, cancelTokenSource.Token);
                if (response.IsSuccessStatusCode)
                {
                    httpClient.DefaultRequestHeaders.IfModifiedSince = DateTime.UtcNow;
                    OnChanged(response);
                }else if( response.StatusCode != HttpStatusCode.NotModified ) {
                    OnError(response);
                }
            }
        }catch(HttpRequestException httpError){
            Console.WriteLine(httpError.ToString());
            throw httpError;
        }catch(TaskCanceledException){            
        }
        return this;
    }

    public async Task<WebDavHelper> SaveData(Uri uri, HttpContent content){
        try{
            var response = await httpClient.PutAsync(uri, content, cancelTokenSource.Token);
            httpClient.DefaultRequestHeaders.IfModifiedSince = DateTime.UtcNow;
            if( !response.IsSuccessStatusCode ){
                throw new HttpRequestException(response.ReasonPhrase ==null ? "Unknown error" : response.ReasonPhrase.ToString());
            }
        }catch(HttpRequestException httpError){
            Console.WriteLine(httpError.ToString());
            throw httpError;                
        }
        return this;
    }

    public void Stop(){
        stopFlag = true;
        cancelTokenSource.Cancel();
    } 
}
