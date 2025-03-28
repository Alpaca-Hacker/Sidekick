using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Sidekick;

public static class HotKeyManager
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int HOTKEY_ID = 9000; // Unique ID

    // Modifiers (adjust if necessary)
    private const uint MOD_NONE = 0x0000;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;

    private const int WM_HOTKEY = 0x0312;

    private static IntPtr _windowHandle;
    private static HwndSource _source;
    private static Action _hotkeyPressedAction;
    private static bool _isRegistered = false;

    public static bool RegisterHotKey(Window window, Key key, ModifierKeys modifiers, Action hotkeyPressedAction)
    {
        if (_isRegistered) 
        {
            UnregisterHotKeyInternal();
        }

        _hotkeyPressedAction = hotkeyPressedAction;
        var helper = new WindowInteropHelper(window);
         
        _windowHandle = helper.EnsureHandle();
        if (_windowHandle == IntPtr.Zero)
        {
             System.Diagnostics.Debug.WriteLine("Warning: Window handle not available for hotkey registration yet.");
        }

        _source = HwndSource.FromHwnd(_windowHandle);
        if (_source == null)
        {
             System.Diagnostics.Debug.WriteLine("Warning: HwndSource is null.");
             return false; // Cannot hook messages
        }
        _source.AddHook(HwndHook);

        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        uint fsModifiers = MapModifierKeys(modifiers);

        if (!RegisterHotKey(_windowHandle, HOTKEY_ID, fsModifiers, vk))
        {
            System.Diagnostics.Debug.WriteLine($"Hotkey registration failed. Modifiers: {fsModifiers}, Key: {vk}");
            // Consider throwing an exception or logging error
            _source.RemoveHook(HwndHook); // Clean up hook if registration fails
            _source = null;
            return false;
        }
        _isRegistered = true;
        return true;
    }

    public static void UnregisterHotKey()
    {
         UnregisterHotKeyInternal();
    }

    private static void UnregisterHotKeyInternal()
    {
        if (!_isRegistered) return;

        _source?.RemoveHook(HwndHook);
        _source = null;
        UnregisterHotKey(_windowHandle, HOTKEY_ID);
        _isRegistered = false;
        _hotkeyPressedAction = null; // Clear the action
         System.Diagnostics.Debug.WriteLine("Hotkey unregistered.");
    }

    private static IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            _hotkeyPressedAction?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

     private static uint MapModifierKeys(ModifierKeys modifiers)
    {
         uint fsModifiers = MOD_NONE;
         if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) fsModifiers |= MOD_ALT;
         if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control) fsModifiers |= MOD_CONTROL;
         if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) fsModifiers |= MOD_SHIFT;
         if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows) fsModifiers |= MOD_WIN;
         return fsModifiers;
    }

     // Optional: Helper to parse from string config (not used in this minimal example yet)
    public static bool TryParseHotkey(string keyStr, string modifiersStr, out Key key, out ModifierKeys modifiers)
    {
        key = Key.None;
        modifiers = ModifierKeys.None;

        if (!Enum.TryParse<Key>(keyStr, true, out key)) return false;

        if (!string.IsNullOrEmpty(modifiersStr))
        {
            var parts = modifiersStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (Enum.TryParse<ModifierKeys>(part, true, out var mod))
                {
                    modifiers |= mod;
                }
            }
        }
        return true;
    }
}