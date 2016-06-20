namespace Helios
{
    using System;
    using iStreamU;
    using System.Threading;

    public sealed class PptStreamer : IDisposable
    {
        public bool                     Quit { get; private set; }
        public bool                     Disposed { get; private set; }
        private byte[]                  SlidePixels;
        private readonly Thread         StreamThread;
        private readonly TimeSpan       StreamDelay;
        private readonly LaunchOptions  LaunchOpts;

        public struct LaunchOptions
        {
            public string   RootPath;
            public string   SlideShowPath;
            public string   StreamAddress;
            public short    StreamPort;
            public uint     StartSlide;
            public int      StreamWidth;
            public int      StreamHeight;
        }

        public PptStreamer(LaunchOptions opts)
        {
            System.Diagnostics.Debug.WriteLine("PptStreamer");
            LaunchOpts = opts;

            Disposed = false;
            Quit = false;

            StreamDelay = TimeSpan.FromMilliseconds(50);
            StreamThread = new Thread(new ThreadStart(StreamRoutine));
            StreamThread.Start();
        }

        void StreamRoutine()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("StreamRoutine");
                using (var byteStreamer = new ByteStreamer(
                    LaunchOpts.StreamWidth,
                    LaunchOpts.StreamHeight))
                using (var pptView = new PptView(
                    LaunchOpts.RootPath,
                    LaunchOpts.SlideShowPath,
                    LaunchOpts.StartSlide))
                {
                    System.Diagnostics.Debug.WriteLine("Loop");
                    while (!Quit)
                    {
                        int width = 0, height = 0;
                        if (pptView.Render(ref SlidePixels, ref width, ref height) &&
                            byteStreamer.NeedData)
                            byteStreamer.PushBuffer(SlidePixels);
                        System.Diagnostics.Debug.WriteLine("Render");
                        Thread.Sleep(StreamDelay);
                    }
                }
            }
            catch(Exception)
            {
                System.Diagnostics.Debug.WriteLine("done");
                Quit = true;
                return;
            }
        }

        #region IDisposable Support
        void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    Quit = true;
                    StreamThread.Join(StreamDelay);
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
}
