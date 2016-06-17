namespace iStreamU
{
    using System;
    using System.Runtime.InteropServices;

    using AppSrc        = System.IntPtr;
    using GPointer      = System.IntPtr;
    using GstBuffer     = System.IntPtr;
    using GstEelement   = System.IntPtr;

    /// <summary>
    /// A utility class which exposes used GStreamer Core Library API
    /// <see cref="https://gstreamer.freedesktop.org/data/doc/gstreamer/head/gstreamer/html/libgstreamer.html"/>
    /// </summary>
    public static class GStreamer
    {
        /// <summary>
        /// Version struct of GStreamer
        /// <see cref="https://gstreamer.freedesktop.org/data/doc/gstreamer/head/gstreamer/html/gstreamer-GstVersion.html"/>
        /// </summary>
        public struct Version
        {
            public uint Major;
            public uint Minor;
            public uint Patch;
            public uint Nano;

            public override string ToString()
            {
                return string.Format("{0}.{1}.{2}.{3}",
                    Major, Minor, Patch, Nano);
            }
        }

        /// <summary>
        /// According to GStreamer docs, only GST_STATE_CHANGE_FAILURE is a true failure
        /// <see cref="https://gstreamer.freedesktop.org/data/doc/gstreamer/head/gstreamer/html/GstElement.html#GstStateChangeReturn"/>
        /// </summary>
        public enum StateChangeReturn : int
        {
            GST_STATE_CHANGE_FAILURE    = 0,
            GST_STATE_CHANGE_SUCCESS    = 1,
            GST_STATE_CHANGE_ASYNC      = 2,
            GST_STATE_CHANGE_NO_PREROLL = 3
        }

        /// <summary>
        /// We only use PLAYING state since we broadcast a stream
        /// <see cref="https://gstreamer.freedesktop.org/data/doc/gstreamer/head/gstreamer/html/GstElement.html#GstState"/>
        /// </summary>
        public enum State : int
        {
            GST_STATE_VOID_PENDING  = 0,
            GST_STATE_NULL          = 1,
            GST_STATE_READY         = 2,
            GST_STATE_PAUSED        = 3,
            GST_STATE_PLAYING       = 4
        }

        /// <summary>
        /// In case of streaming, our "push data" call should always return FLOW_OK, otherwise it's failed
        /// <see cref="https://gstreamer.freedesktop.org/data/doc/gstreamer/head/gstreamer/html/GstPad.html#GstFlowReturn"/>
        /// </summary>
        [Flags] public enum FlowReturn : int
        {
            GST_FLOW_OK             = 0,
            GST_FLOW_NOT_LINKED     = -1,
            GST_FLOW_FLUSHING       = -2,
            GST_FLOW_EOS            = -3,
            GST_FLOW_NOT_NEGOTIATED = -4,
            GST_FLOW_ERROR          = -5,
            GST_FLOW_NOT_SUPPORTED  = -6
        }

        /// <summary>
        /// Note that flags are not hard coded int the original header, this is evaluated with GStreamer 1.8.0
        /// <see cref="https://gstreamer.freedesktop.org/data/doc/gstreamer/head/gstreamer/html/GstMemory.html#GstMemoryFlags"/>
        /// </summary>
        [Flags] public enum MemoryFlags : int
        {
            GST_MEMORY_FLAG_NONE        = 0,
            GST_MEMORY_FLAG_READONLY    = 2,
            GST_MEMORY_FLAG_NO_SHARE    = 16
        }

        /// <summary>
        /// AppSrc's need-data action signature. This is called when AppSrc needs data to be pushed
        /// <see cref="https://gstreamer.freedesktop.org/data/doc/gstreamer/head/gst-plugins-base-libs/html/gst-plugins-base-libs-appsrc.html#GstAppSrcCallbacks"/>
        /// </summary>
        /// <param name="appsrc">AppSrc instance pointer requesting data</param>
        /// <param name="size">"is just a hint and when it is set to -1, any number of bytes can be pushed into appsrc"</param>
        /// <param name="data">user data pointer (it's null in our case always)</param>
        public delegate void NeedDataDelegate(AppSrc appsrc, uint size, GPointer data);

        /// <summary>
        /// Answers true if GStreamer is initialized. Safe to call when DLL is not present.
        /// Answers false if DLL is not present or if GStreamer is not initialized.
        /// </summary>
        public static bool IsInitialized
        {
            get
            {
                try { return GstNativeMethods.gst_is_initialized(); }
                catch (DllNotFoundException) { return false; }
            }
        }

        /// <summary>
        /// Attempts to initialize GStreamer. Safe to call when DLL is not present.
        /// </summary>
        /// <returns>true if initialized or false if (not initialized or DLL is not found)</returns>
        public static bool Initialize()
        {
            try { return GstNativeMethods.gst_init_check(GPointer.Zero, GPointer.Zero, GPointer.Zero); }
            catch(DllNotFoundException) { return false; }
        }

        /// <summary>
        /// Returns the version of GStreamer being used.
        /// Throws if DLL is not present.
        /// </summary>
        /// <returns>Version of the GStreamer library</returns>
        public static Version GetVersion()
        {
            Version version = new Version();

            GstNativeMethods.gst_version(
                ref version.Major,
                ref version.Minor,
                ref version.Patch,
                ref version.Nano);

            return version;
        }

        /// <summary>
        /// wrapper for gst_parse_launch
        /// </summary>
        /// <param name="pipeline">pipeline desription (in GstLaunch in syntax)</param>
        /// <returns>a pointer to the pipeline if successful</returns>
        public static GstEelement ParseLaunch(string pipeline)
        {
            return GstNativeMethods.gst_parse_launch(pipeline, GPointer.Zero);
        }

        /// <summary>
        /// Decreases ref count of a GStreamer object
        /// </summary>
        /// <param name="element">GStreamer object</param>
        public static void ObjectUnref(GstEelement element)
        {
            if (element == GstEelement.Zero)
                return;

            GstNativeMethods.gst_object_unref(element);
        }

        /// <summary>
        /// Looks up a child element in a GstBin by name
        /// </summary>
        /// <param name="bin">parent bin</param>
        /// <param name="name">child element</param>
        /// <returns>child element on success. Use ObjectUnref when you are done.</returns>
        public static GstEelement BinGetByName(GstEelement bin, string name)
        {
            if (bin == GstEelement.Zero)
                return GstEelement.Zero;

            return GstNativeMethods.gst_bin_get_by_name(bin, name);
        }

        /// <summary>
        /// Changes state of a GStreamer element
        /// </summary>
        /// <param name="element">element which its state needs to be changed</param>
        /// <param name="state">new state the element should transition into</param>
        /// <returns></returns>
        public static StateChangeReturn ElementSetState(GstEelement element, State state)
        {
            if (element == GstEelement.Zero)
                return StateChangeReturn.GST_STATE_CHANGE_FAILURE;

            return GstNativeMethods.gst_element_set_state(element, state);
        }

        /// <summary>
        /// Pushes a buffer into AppSrc when it "needs data"
        /// </summary>
        /// <param name="appsrc">AppSrc requesting a buffer</param>
        /// <param name="buffer">buffer to be pushed into AppSrc</param>
        /// <returns>status of the buffer push (FLOW_OK on success)</returns>
        public static FlowReturn AppSrcPushBuffer(AppSrc appsrc, GstBuffer buffer)
        {
            if (appsrc == AppSrc.Zero || buffer == GstBuffer.Zero)
                return FlowReturn.GST_FLOW_ERROR;

            return GstNativeMethods.gst_app_src_push_buffer(appsrc, buffer);
        }

        /// <summary>
        /// Wraps the passed byte array in a GStreamer buffer object
        /// </summary>
        /// <param name="data">C# byte pointer to be wrapped</param>
        /// <returns>GstBuffer pointer wrapping data argument</returns>
        public static GstBuffer BufferNewWrapped(byte[] data)
        {
            ulong size = (ulong)data.Length;
            return GstNativeMethods.gst_buffer_new_wrapped_full(
                MemoryFlags.GST_MEMORY_FLAG_READONLY | MemoryFlags.GST_MEMORY_FLAG_NO_SHARE,
                data, size, 0, size, GPointer.Zero, GPointer.Zero);
        }
    }

    internal sealed class GstNativeMethods
    {
        const string GstCoreDllName = "libgstreamer-1.0-0.dll";
        const string GstAppDllName  = "libgstapp-1.0-0.dll";

        [DllImport(GstCoreDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool gst_is_initialized();

        [DllImport(GstCoreDllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool gst_init_check(GPointer argc, GPointer argv, GPointer error);

        [DllImport(GstCoreDllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void gst_version(ref uint major, ref uint minor, ref uint micro, ref uint nano);

        [DllImport(GstCoreDllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern GstEelement gst_parse_launch(string pipeline_description, GPointer error);

        [DllImport(GstCoreDllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void gst_object_unref(IntPtr element);

        [DllImport(GstCoreDllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern GstEelement gst_bin_get_by_name(IntPtr bin, string name);

        [DllImport(GstCoreDllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern GStreamer.StateChangeReturn gst_element_set_state(GstEelement element, GStreamer.State state);

        [DllImport(GstCoreDllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern GstBuffer gst_buffer_new_wrapped_full(GStreamer.MemoryFlags flags, byte[] data, ulong maxsize, ulong offset, ulong size, GPointer user_data, GPointer notify);

        [DllImport(GstAppDllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern GStreamer.FlowReturn gst_app_src_push_buffer(AppSrc appsrc, GstBuffer buffer);
    }
}
