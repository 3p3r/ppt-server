using System.Drawing.Imaging;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System;
using System.Runtime.InteropServices;

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

    public uint SlideNumber
    {
        get
        {
            int[] offsets = { 0x004FBADC, 0x730, 0x730, 0x7D0, 0x6E8, 0x218 };
            return (uint)X86MultiPointerReader.Resolve(RendererProcess, offsets).ToInt32();
        }
    }

    private bool GetRenderWindowHandle()
    {
        const string class_name = "screenClass";

        RenderWindowHwnd = IntPtr.Zero;

        User32.EnumWindows((hwnd, lParam) =>
        {
            uint pid;
            User32.GetWindowThreadProcessId(hwnd, out pid);

            const int nChars = 1024;
            var Buff2 = new System.Text.StringBuilder(nChars);
            if (User32.GetClassName(hwnd, Buff2, nChars) > 0 &&
                Buff2.ToString().Equals(class_name) &&
                RendererProcess.Id == pid)
            {
                RenderWindowHwnd = hwnd;
            }

            return true;
        },
        0);

        return RenderWindowHwnd != IntPtr.Zero;
    }

    public void Render(ref UnityEngine.Texture2D texture)
    {
        if (RenderWindowHwnd == IntPtr.Zero &&
            !GetRenderWindowHandle())
            return;

        if (!texture)
            return;

        RECT rc;
        User32.GetWindowRect(RenderWindowHwnd, out rc);

        if (texture.width != rc.Width ||
            texture.height != rc.Height)
            texture.Resize(rc.Width, rc.Height, UnityEngine.TextureFormat.ARGB32, false);

        using (Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb))
        using (Graphics gfx = Graphics.FromImage(bmp))
        {
            IntPtr hdc = gfx.GetHdc();

            if (User32.PrintWindow(RenderWindowHwnd, hdc, User32.PrintWindowFlags.PW_ALL))
            {
                gfx.ReleaseHdc(hdc);
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                int length = data.Stride * data.Height;

                byte[] bytes = new byte[length];
                Marshal.Copy(data.Scan0, bytes, 0, length);

                texture.LoadRawTextureData(bytes);
                texture.Apply();

                bmp.UnlockBits(data);
            }
            else
            {
                gfx.ReleaseHdc(hdc);
            }
        }
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
