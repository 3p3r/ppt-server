namespace iStreamU
{
    using System;
    using System.Net;
    using System.Runtime.InteropServices;

    public sealed class ByteStreamer : IDisposable
    {
        private GCHandle        m_NeedDataCallbackHandle;
        private IntPtr          m_Pipeline  = IntPtr.Zero;
        private IntPtr          m_AppSrc    = IntPtr.Zero;

        /// <summary>
        /// Flag indicating if AppSrc is starving for data
        /// </summary>
        public bool             NeedData { get; private set; }

        /// <summary>
        /// Answers true if this instance is already disposed
        /// </summary>
        public bool             Disposed { get; private set; }

        /// <summary>
        /// Network options. Pass this to ByteStreamer constructor to configure
        /// its network sink. If "TransportType.All" is passed, both a UDP and
        /// TCP sink will be created.
        /// </summary>
        public class NetworkOptions
        {
            public TransportType    StreamType  = TransportType.Udp;
            public string           Address     = IPAddress.Loopback.ToString();
            public short            Port        = 10000;
        }

        /// <summary>
        /// Attempts to construct a GStreamer pipeline that streams an ARGB C# byte array
        /// encoded with JPEG and multiplexed into HTTP multi part frames.
        /// THROWS if it cannot construct the pipeline.
        /// Receiver can be (in case of UDP transport):
        /// gst-launch-1.0 udpsrc address=<host> port=<port> ! decodebin ! autovideosink
        /// </summary>
        /// <param name="width">width of the input image buffer</param>
        /// <param name="height">height of the input image buffer</param>
        /// <param name="netopts">network options to launch the ByteStreamer instance with</param>
        public ByteStreamer(int width, int height, NetworkOptions netopts = null)
        {
            Disposed = false;

            if (!GStreamer.IsInitialized &&
                !GStreamer.Initialize())
                throw new ExternalException("Unable to initialize GStreamer.");

            if (netopts == null)
                netopts = new NetworkOptions();

            string appsrc_name = "AppSrc";
            string appsrc_sink = string.Empty;
            string appsrc_opts = "is-live=true do-timestamp=true";
            string appsrc_caps = string.Format("video/x-raw,format=ARGB,framerate=0/1,width={0},height={1}", width, height);

            if (netopts.StreamType == TransportType.Udp)
                appsrc_sink = string.Format("udpsink host={0} port={1}",
                    netopts.Address, netopts.Port);
            else
            if (netopts.StreamType == TransportType.Tcp)
                appsrc_sink = string.Format("tcpserversink host={0} port={1}",
                    netopts.Address, netopts.Port);
            else
            if (netopts.StreamType == TransportType.All)
                appsrc_sink = string.Format("tee name=t ! queue ! tcpserversink host={0} port={1} t. ! queue ! udpsink host={0} port={1}",
                    netopts.Address, netopts.Port);

            string[] pipeline_elements = new string[]
            {
                string.Format("appsrc name=\"{0}\" caps=\"{1}\" {2}", appsrc_name, appsrc_caps, appsrc_opts),
                "videoconvert",
                "video/x-raw,format=I420",
                "jpegenc quality=75",
                appsrc_sink
            };

            string pipeline_description = string.Join(" ! ", pipeline_elements);

            m_Pipeline = GStreamer.ParseLaunch(pipeline_description);

            if (m_Pipeline == IntPtr.Zero)
                throw new ExternalException("Unable to launch the pipeline.");

            m_AppSrc = GStreamer.BinGetByName(m_Pipeline, appsrc_name);

            if (m_AppSrc == IntPtr.Zero)
                throw new ExternalException("Unable to obtain pipeline's AppSrc.");

            var need_data_cb = new GStreamer.NeedDataDelegate((appsrc, size, data) => { NeedData = true; });
            m_NeedDataCallbackHandle = GCHandle.Alloc(need_data_cb, GCHandleType.Pinned);

            if (!GSignal.AppSrcConnect(m_AppSrc, need_data_cb))
                throw new ExternalException("Failed to connect the need-data action signal.");

            if (GStreamer.ElementSetState(m_Pipeline, GStreamer.State.GST_STATE_PLAYING)
                == GStreamer.StateChangeReturn.GST_STATE_CHANGE_FAILURE)
                throw new ExternalException("Pipeline state change failed.");
        }

        /// <summary>
        /// Attempts to push data into AppSrc. Pair this with NeedData check.
        /// THROWS if it cannot push buffers.
        /// </summary>
        /// <param name="data">image data to be pushed. width and height of it is passed into constructor</param>
        /// <returns>success on push success</returns>
        public bool PushBuffer(byte[] data)
        {
            if (m_AppSrc == IntPtr.Zero)
                throw new InvalidOperationException("AppSrc is not present.");

            if (NeedData)
            {
                NeedData = false;

                IntPtr buffer = GStreamer.BufferNewWrapped(data);

                if (buffer == IntPtr.Zero)
                    throw new InvalidOperationException("Unable to wrap byte array.");

                return GStreamer.AppSrcPushBuffer(m_AppSrc, buffer)
                    == GStreamer.FlowReturn.GST_FLOW_OK;
            }

            return false;
        }

        #region IDisposable Support
        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (m_AppSrc != IntPtr.Zero)
                {
                    GStreamer.ObjectUnref(m_AppSrc);
                    m_AppSrc = IntPtr.Zero;
                }

                if (m_Pipeline != IntPtr.Zero)
                {
                    GStreamer.ElementSetState(m_Pipeline, GStreamer.State.GST_STATE_NULL);
                    GStreamer.ObjectUnref(m_Pipeline);
                    m_Pipeline = IntPtr.Zero;
                }

                if (disposing)
                {
                    if (m_NeedDataCallbackHandle.IsAllocated)
                        m_NeedDataCallbackHandle.Free();
                }

                Disposed = true;
            }
        }

        ~ByteStreamer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
