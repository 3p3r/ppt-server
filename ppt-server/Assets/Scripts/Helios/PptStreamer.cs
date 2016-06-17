namespace Helios
{
    using System;
    using iStreamU;
    using System.Threading;

    public sealed class PptStreamer : IDisposable
    {
        private bool            quit;
        private bool            disposed;
        private byte[]          slidePixels;
        private PptView         pptView;
        private ByteStreamer    byteStreamer;
        private Thread          streamThread;
        private TimeSpan        streamDelay;

        public struct LaunchOptions
        {
            public string   SlideShowPath;
            public string   StreamAddress;
            public short    StreamPort;
            public uint     StartSlide;
            public int      StreamWidth;
            public int      StreamHeight;
        }

        public PptStreamer(LaunchOptions opts)
        {
            byteStreamer = new ByteStreamer(opts.StreamWidth, opts.StreamHeight, opts.StreamAddress, opts.StreamPort);
            slidePixels = new byte[opts.StreamWidth * opts.StreamHeight * 4];
            pptView = new PptView(opts.SlideShowPath, opts.StartSlide);
            disposed = false;
            quit = false;

            streamDelay = TimeSpan.FromMilliseconds(50);
            streamThread = new Thread(new ThreadStart(StreamRoutine));
            streamThread.Start();
        }

        void StreamRoutine()
        {
            while(!quit)
            {
                int width = 0, height = 0;
                if (pptView.Render(ref slidePixels, ref width, ref height) &&
                    byteStreamer.NeedData)
                    byteStreamer.PushBuffer(slidePixels);

                Thread.Sleep(streamDelay);
            }
        }

        #region IDisposable Support
        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    quit = true;
                    streamThread.Join(streamDelay);

                    byteStreamer.Dispose();
                    pptView.Dispose();
                }

                streamThread = null;
                pptView = null;
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
