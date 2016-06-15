using System.Drawing.Imaging;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System;

public class PptView : IDisposable
{
    /// <summary>
    /// Returns path to the root directory of pptview.exe
    /// </summary>
    public static string RootPath
    {
        get { return Path.Combine(UnityEngine.Application.streamingAssetsPath, "pptview"); }
    }

    /// <summary>
    /// Returns path to the pptview.exe executable, located in RootPath
    /// </summary>
    public static string BinaryPath
    {
        get { return Path.Combine(RootPath, "PPTVIEW.EXE"); }
    }

    /// <summary>
    /// Path to currently open presentation
    /// </summary>
    public readonly string PresentationPath;

    /// <summary>
    /// pptview.exe's process which is rendering the current presentation
    /// </summary>
    public readonly Process RendererProcess;

    /// <summary>
    /// HWND to pptview.exe's render window
    /// </summary>
    public IntPtr RenderWindowHwnd { get; private set; }

    public PptView(string presentation_path, uint start_slide = 1)
    {
        if (!File.Exists(presentation_path))
            throw new ArgumentException("presentation file does not exist.");

        if (!Directory.Exists(RootPath))
            throw new InvalidOperationException("root path does not exist.");

        if (!File.Exists(BinaryPath))
            throw new InvalidOperationException("binary path does not exist.");

        PresentationPath = presentation_path;

        RendererProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                Arguments = string.Format("/FSN{0} \"{1}\"", start_slide, presentation_path),
                FileName = BinaryPath
            },
            EnableRaisingEvents = true
        };

        RendererProcess.Start();
        RendererProcess.WaitForInputIdle();

        Disposed = false;
    }

    private bool GetRenderWindowHandle()
    {
        RenderWindowHwnd = IntPtr.Zero;

        User32.EnumWindows((hwnd, lParam) =>
        {
            uint pid;
            User32.GetWindowThreadProcessId(hwnd, out pid);

            const int nChars = 1024;
            var Buff2 = new System.Text.StringBuilder(nChars);
            if (User32.GetClassName(hwnd, Buff2, nChars) > 0 &&
                Buff2.ToString() == "screenClass")
            {
                if (RendererProcess.Id == pid)
                    RenderWindowHwnd = hwnd;
            }

            return true;
        },
        0);

        return RenderWindowHwnd != IntPtr.Zero;
    }

    public byte[] GetScreenshot()
    {
        byte[] pixels = new byte[0];

        if (RenderWindowHwnd == IntPtr.Zero &&
            !GetRenderWindowHandle())
            return pixels;

        RECT rc;
        User32.GetWindowRect(RenderWindowHwnd, out rc);

        using (Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb))
        using (Graphics gfxBmp = Graphics.FromImage(bmp))
        {
            IntPtr hdcBitmap = gfxBmp.GetHdc();
            User32.PrintWindow(RenderWindowHwnd, hdcBitmap, 0);
            gfxBmp.ReleaseHdc(hdcBitmap);

            using (MemoryStream stream = new MemoryStream())
            {
                bmp.Save(stream, ImageFormat.Jpeg);
                stream.Close();
                pixels = stream.ToArray();
            }
        }

        return pixels;
    }

    #region IDisposable Support
    public bool Disposed { get; private set; }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            if (disposing)
            {
                if (RendererProcess != null)
                {
                    RendererProcess.CloseMainWindow();
                    RendererProcess.Dispose();
                }
            }

            Disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }
    #endregion
}
