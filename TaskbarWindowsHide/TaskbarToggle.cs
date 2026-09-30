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

    private const uint EVENT_OBJECT_SHOW = 0x8002;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    private static bool _isHidden = false;
    private static IntPtr _hookHandle = IntPtr.Zero;

    private delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime);

    private static readonly WinEventDelegate EventCallback = OnWindowShow;

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

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

        if (!RegisterHotKey(IntPtr.Zero, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_T))
            return;

        _hookHandle = SetWinEventHook(
            EVENT_OBJECT_SHOW,
            EVENT_OBJECT_SHOW,
            IntPtr.Zero,
            EventCallback,
            0,
            0,
            WINEVENT_OUTOFCONTEXT);

        try
        {
            while (GetMessage(out MSG msg, IntPtr.Zero, 0, 0) != 0)
            {
                if (msg.message == WM_HOTKEY && msg.wParam.ToUInt32() == HOTKEY_ID)
                {
                    ToggleTaskbar();
                }

                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }
        finally
        {
            if (_hookHandle != IntPtr.Zero)
                UnhookWinEvent(_hookHandle);

            UnregisterHotKey(IntPtr.Zero, HOTKEY_ID);
        }
    }

    private static void ToggleTaskbar()
    {
        _isHidden = !_isHidden;
        ApplyTaskbarState();
    }

    private static void ApplyTaskbarState()
    {
        IntPtr mainTaskbar = FindWindow("Shell_TrayWnd", null);
        IntPtr secondaryTaskbar = FindWindow("Shell_SecondaryTrayWnd", null);
        IntPtr startButton = FindWindow("Button", "Start");

        int command = _isHidden ? SW_HIDE : SW_SHOW;

        if (mainTaskbar != IntPtr.Zero)
            ShowWindow(mainTaskbar, command);

        if (secondaryTaskbar != IntPtr.Zero)
            ShowWindow(secondaryTaskbar, command);

        if (startButton != IntPtr.Zero)
            ShowWindow(startButton, command);
    }

    private static void OnWindowShow(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        if (!_isHidden)
            return;

        IntPtr mainTaskbar = FindWindow("Shell_TrayWnd", null);
        IntPtr secondaryTaskbar = FindWindow("Shell_SecondaryTrayWnd", null);

        if (hwnd == mainTaskbar || hwnd == secondaryTaskbar)
        {
            ApplyTaskbarState();
        }
    }

    private static void AddToStartup()
    {
        string? executablePath = Environment.ProcessPath;

        if (string.IsNullOrEmpty(executablePath))
            return;

        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            writable: true);

        key?.SetValue("TaskbarToggle", executablePath);
    }
}