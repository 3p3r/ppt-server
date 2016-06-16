using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    private int _Left;
    private int _Top;
    private int _Right;
    private int _Bottom;

    public int X
    {
        get { return _Left; }
        set { _Left = value; }
    }
    public int Y
    {
        get { return _Top; }
        set { _Top = value; }
    }
    public int Left
    {
        get { return _Left; }
        set { _Left = value; }
    }
    public int Top
    {
        get { return _Top; }
        set { _Top = value; }
    }
    public int Right
    {
        get { return _Right; }
        set { _Right = value; }
    }
    public int Bottom
    {
        get { return _Bottom; }
        set { _Bottom = value; }
    }
    public int Height
    {
        get { return _Bottom - _Top; }
        set { _Bottom = value + _Top; }
    }
    public int Width
    {
        get { return _Right - _Left; }
        set { _Right = value + _Left; }
    }
}

public static class User32
{
    const string DllName = "user32.dll";

    public delegate bool EnumWindowsCallback(IntPtr hwnd, int lParam);

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    public enum PrintWindowFlags : uint
    {
        PW_ALL          = 0x00000000,
        PW_CLIENTONLY   = 0x00000001
    }

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, PrintWindowFlags nFlags);

    [DllImport(DllName)]
    public static extern int EnumWindows(EnumWindowsCallback cb, int lPar);

    [DllImport(DllName)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport(DllName, SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);
}
