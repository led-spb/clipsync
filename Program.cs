using System.Runtime.InteropServices;

namespace clipsync;


internal static class NativeMethods
{
    public const int WM_CLIPBOARDUPDATE = 0x031D;
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AddClipboardFormatListener(IntPtr hwnd);
}

public class Program
{

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new SyncForm());
    }
}