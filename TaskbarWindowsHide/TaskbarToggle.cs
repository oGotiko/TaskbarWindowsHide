using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

internal class TaskbarToggle
{
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    private const int WM_HOTKEY = 0x0312;

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;

    private const uint VK_T = 0x54;

    private const int HOTKEY_ID = 1;

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(
        string? lpClassName,
        string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(
        IntPtr hWnd,
        int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(
        IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    A simple program that adds a shortcut to toggle the visibility of the taskbar.
    
        [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(
        IntPtr hWnd,
        int id);

    [DllImport("user32.dll")]
    private static extern int GetMessage(
        out MSG lpMsg,
        IntPtr hWnd,
        uint wMsgFilterMin,
        uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(
        ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(
        ref MSG lpMsg);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public UIntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    private static void Main()
    {
        AddToStartup();

        if (!RegisterHotKey(
                IntPtr.Zero,
                HOTKEY_ID,
                MOD_CONTROL | MOD_ALT,
                VK_T))
        {
            return;
        }

        try
        {
            while (GetMessage(
                out MSG msg,
                IntPtr.Zero,
                0,
                0) != 0)
            {
                if (msg.message == WM_HOTKEY &&
                    msg.wParam.ToUInt32() == HOTKEY_ID)
                {
                    ToggleTaskbar();
                }

                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }
        finally
        {
            UnregisterHotKey(
                IntPtr.Zero,
                HOTKEY_ID);
        }
    }

    private static void ToggleTaskbar()
    {
        IntPtr taskbar = FindWindow(
            "Shell_TrayWnd",
            null);

        if (taskbar == IntPtr.Zero)
            return;

        bool visible = IsWindowVisible(taskbar);

        ShowWindow(
            taskbar,
            visible ? SW_HIDE : SW_SHOW);
    }

    private static void AddToStartup()
    {
        string? executablePath = Environment.ProcessPath;

        if (string.IsNullOrEmpty(executablePath))
            return;

        using RegistryKey? key =
            Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run",
                writable: true);

        key?.SetValue(
            "TaskbarToggle",
            executablePath);
    }
}

