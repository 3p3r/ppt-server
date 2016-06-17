namespace Helios
{
    using System;
    using System.IO;
    using System.Drawing;
    using System.Diagnostics;
    using System.Drawing.Imaging;
    using System.Runtime.InteropServices;

    public sealed class PptView : IDisposable
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

        /// <summary>
        /// Pixel holder for the last time you called Render
        /// </summary>
        private byte[] lastRenderedPixels;

        /// <summary>
        /// Called when this instance is disposed.
        /// NOTE: Can be called from a different thread!
        /// </summary>
        public Action OnDispose;

        /// <summary>
        /// Opens pptview.exe to render a slide show
        /// </summary>
        /// <param name="presentation_path">path to .ppt or .pptx slideshow</param>
        /// <param name="start_slide">starting slide opened in pptview, if exceeds max slide count, it opens from the beginning</param>
        public PptView(string presentation_path, uint start_slide = 1)
        {
            if (!File.Exists(presentation_path))
                throw new ArgumentException("presentation file does not exist.");

            if (!Directory.Exists(RootPath))
                throw new InvalidOperationException("root path does not exist.");

            if (!File.Exists(BinaryPath))
                throw new InvalidOperationException("binary path does not exist.");

            PresentationPath = presentation_path;
            lastRenderedPixels = new byte[0];

            RendererProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    Arguments = string.Format("/F /S /N{0} \"{1}\"", start_slide, presentation_path),
                    FileName = BinaryPath
                },
                EnableRaisingEvents = true
            };

            RendererProcess.Exited += (sender, ev) => { Dispose(); };

            RendererProcess.Start();
            RendererProcess.WaitForInputIdle();

            Disposed = false;
        }

        /// <summary>
        /// Returns the current slide number or 0 on error
        /// </summary>
        public uint SlideNumber
        {
            get
            {
                if (Disposed)
                    return 0;

                int[] offsets = { 0x004F8434, 0x24, 0x40, 0x10, 0xC, 0xD4 };
                return (uint)X86MultiPointerReader.Resolve(RendererProcess, offsets).ToInt32();
            }
        }

        /// <summary>
        /// Steps the presentation forward (similar to right arrow key in MS PowerPoint)
        /// </summary>
        public void NextStep()
        {
            if (Disposed)
                return;

            ActivateWindow();
            // 1775 is the wParam sniffed with Spy++
            User32.SendMessage(RenderWindowHwnd, User32.WindowMessages.WM_COMMAND, (IntPtr)1775, IntPtr.Zero);
        }

        /// <summary>
        /// Steps the presentation backward (similar to left arrow key in MS PowerPoint)
        /// </summary>
        public void PreviousStep()
        {
            if (Disposed)
                return;

            ActivateWindow();
            // 1774 is the wParam sniffed with Spy++
            User32.SendMessage(RenderWindowHwnd, User32.WindowMessages.WM_COMMAND, (IntPtr)1774, IntPtr.Zero);
        }

        public void ActivateWindow()
        {
            if (Disposed)
                return;

            if (User32.GetForegroundWindow() !=
                RenderWindowHwnd)
            {
                User32.SwitchToThisWindow(RenderWindowHwnd, true);
                User32.SetForegroundWindow(RenderWindowHwnd);

                // NOTE: the following is taken from Phoenix' code
                // after calling it, the window will never leave
                // foreground. Call it only if the above are not working.
                /*User32.SetWindowPos(
                    RenderWindowHwnd,
                    User32.HWND_TOPMOST,
                    0, 0, 0, 0,
                    User32.SetWindowPosFlags.SWP_NOSIZE |
                    User32.SetWindowPosFlags.SWP_NOMOVE |
                    User32.SetWindowPosFlags.SWP_SHOWWINDOW);*/
            }
        }

        /// <summary>
        /// Attempts to obtain window handle of pptview.exe in its full screen mode
        /// Process.MainWindowHandle won't work when in full screen mode
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Render the current slide of pptview.exe into a Unity Texture2D
        /// NOTE: this method will resize the texture if it's not the correct size/format
        /// </summary>
        /// <param name="texture">texture to receive pptview.exe's window pixels</param>
        public void Render(ref UnityEngine.Texture2D texture)
        {
            int width = 0, height = 0;
            if (!Render(ref lastRenderedPixels, ref width, ref height))
                return;

            if (texture.width != width ||
                texture.height != height ||
                texture.format != UnityEngine.TextureFormat.BGRA32)
                texture.Resize(width, height, UnityEngine.TextureFormat.BGRA32, false);

            texture.LoadRawTextureData(lastRenderedPixels);
            texture.Apply();
        }

        /// <summary>
        /// Render the current slide of pptview.exe into a C# byte array
        /// Output format is Windows' ARGB (equivalent to Unity's BGRA)
        /// NOTE: this method will resize the array if it's not the correct length
        /// </summary>
        /// <param name="pixels">byte array to receive pptview.exe's window pixels</param>
        /// <param name="width">width of the rendered slide</param>
        /// <param name="height">height of the rendered slide</param>
        /// <returns></returns>
        public bool Render(ref byte[] pixels, ref int width, ref int height)
        {
            if (Disposed ||
                (RenderWindowHwnd == IntPtr.Zero &&
                !GetRenderWindowHandle()))
                return false;

            RECT rc;
            if (!User32.GetWindowRect(RenderWindowHwnd, out rc))
                return false;

            try
            {
                using (Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb))
                using (Graphics gfx = Graphics.FromImage(bmp))
                {
                    IntPtr hdc = gfx.GetHdc();

                    if (User32.PrintWindow(RenderWindowHwnd, hdc, User32.PrintWindowFlags.PW_ALL))
                    {
                        gfx.ReleaseHdc(hdc);
                        Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                        BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);

                        int length = data.Stride * data.Height;

                        if (pixels.Length != length)
                            Array.Resize(ref pixels, length);

                        Marshal.Copy(data.Scan0, pixels, 0, length);

                        height = rc.Height;
                        width = rc.Width;

                        bmp.UnlockBits(data);
                        return true;
                    }
                    else
                    {
                        gfx.ReleaseHdc(hdc);
                        return false;
                    }
                }
            }
            catch (ArgumentException)
            {
                // this can happen if Disposed is
                // called while render is in process

                return false;
            }
        }

        #region IDisposable Support
        public bool Disposed { get; private set; }

        private void Dispose(bool disposing)
        {
            lock (this)
            {
                if (!Disposed)
                {
                    Disposed = true;

                    if (disposing)
                    {
                        if (RendererProcess != null)
                        {
                            RendererProcess.Refresh();

                            if (!RendererProcess.HasExited)
                                RendererProcess.CloseMainWindow();

                            RendererProcess.Dispose();
                        }

                        if (OnDispose != null)
                            OnDispose.Invoke();
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
