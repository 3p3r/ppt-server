namespace Helios
{
    using System;
    using System.Runtime.InteropServices;

    public static class Kernel32
    {
        const string DllName = "kernel32.dll";

        [DllImport(DllName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, uint dwSize, out IntPtr lpNumberOfBytesRead);
    }
}
