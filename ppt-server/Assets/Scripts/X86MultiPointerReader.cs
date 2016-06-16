using System;
using System.Diagnostics;

/// <summary>
/// This is a multi-level pointer reader, designed to work with
/// ONLY x86 processes. Offsets can be obtained by Cheat Engine
/// </summary>
public static class X86MultiPointerReader
{
    /// <summary>
    /// Reads 4 bytes (32 bit integer from a given address in the Handle)
    /// </summary>
    /// <param name="Handle">Start of the module (usually result of OpenProcess)</param>
    /// <param name="Address">Offset address into the module base</param>
    /// <returns>4 read bytes of the foreign memory</returns>
    private static byte[] ReadFourBytes(IntPtr Handle, IntPtr Address)
    {
        IntPtr ptrBytesRead;
        byte[] buffer = new byte[4];
        Kernel32.ReadProcessMemory(Handle, Address, buffer, (uint)buffer.Length, out ptrBytesRead);
        return buffer;
    }

    /// <summary>
    /// Same as ReadFourBytes, but casts its return value to integer
    /// </summary>
    /// <param name="Handle"><see cref="ReadFourBytes"/></param>
    /// <param name="Address"><see cref="ReadFourBytes"/></param>
    /// <returns><see cref="ReadFourBytes"/></returns>
    private static int ReadInt32(IntPtr Handle, IntPtr Address)
    {
        return BitConverter.ToInt32(ReadFourBytes(Handle, Address), 0);
    }

    /// <summary>
    /// Resolves a multi-level pointer. If you are using Cheat Engine,
    /// After you lock down your static pointer in CE's pointer scanner:
    /// The first offset is always what comes after "name.exe + OFFSET"
    /// other offsets are CE's off0, off1, ..., off4 in order
    /// </summary>
    /// <param name="proc">Process to read a multi level pointer from</param>
    /// <param name="offsets">different levels of the requested pointer</param>
    /// <returns>resolved pointer (last level)</returns>
    public static IntPtr Resolve(Process proc, int[] offsets)
    {
        IntPtr base_addr = proc.MainModule.BaseAddress;
        IntPtr temp_addr = base_addr;

        for (int i = 0; i < offsets.Length; ++i)
        {
            IntPtr level_addr = new IntPtr(temp_addr.ToInt32() + offsets[i]);
            temp_addr = (IntPtr)ReadInt32(proc.Handle, level_addr);
        }

        return temp_addr;
    }
}
