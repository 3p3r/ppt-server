namespace iStreamU
{
    using System.Runtime.InteropServices;

    using AppSrc    = System.IntPtr;
    using GPointer  = System.IntPtr;

    /// <summary>
    /// A utility class which exposes used GSignal API
    /// </summary>
    public static class GSignal
    {
        /// <summary>
        /// GConnectFlags, note that G_CONNECT_NONE is added here.
        /// </summary>
        public enum ConnectFlags : int
        {
            G_CONNECT_NONE      = 0,
            G_CONNECT_AFTER     = 1 << 0,
            G_CONNECT_SWAPPED   = 1 << 1
        }

        /// <summary>
        /// Used to connect a C# delegate to AppSrc's need-data action signal
        /// </summary>
        /// <param name="appsrc">AppSrc instance whose need-data signal needs to be changed</param>
        /// <param name="callback">need-data C# callback, might be called in a separate thread!</param>
        /// <returns></returns>
        public static bool AppSrcConnect(AppSrc appsrc, GStreamer.NeedDataDelegate callback)
        {
            if (appsrc == AppSrc.Zero)
                return false;

            return GsignalNativeMethods.g_signal_connect_data(appsrc, "need-data", callback, GPointer.Zero, GPointer.Zero, ConnectFlags.G_CONNECT_NONE) > 0;
        }
    }

    internal sealed class GsignalNativeMethods
    {
        const string DllName = "libgobject-2.0-0.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong g_signal_connect_data(GPointer instance, string detailed_signal, GStreamer.NeedDataDelegate c_handler, GPointer data, GPointer destroy_data, GSignal.ConnectFlags connect_flags);
    }
}
