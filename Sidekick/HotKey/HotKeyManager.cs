using System.Diagnostics;
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

    // Modifiers
    private const uint MOD_NONE = 0x0000;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;

    private const int WM_HOTKEY = 0x0312;

    private static IntPtr _windowHandle;
    private static HwndSource? _source;
    private static int _currentId = 9000;
    
    private static Dictionary<int, Action> _hotkeyActions = new();
    private static List<int> _registeredIds = new();
    
    public static void Initialize(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _windowHandle = helper.EnsureHandle();
        if (_windowHandle == IntPtr.Zero) {
            Debug.WriteLine("WARNING: Window handle not available during HotKeyManager Initialize.");
            // Consider alternative initialization timing if this occurs
            return;
        }

        _source = HwndSource.FromHwnd(_windowHandle);
        if (_source == null) {
            Debug.WriteLine("WARNING: HwndSource is null during HotKeyManager Initialize.");
            return;
        }
        _source.AddHook(HwndHook);
        Debug.WriteLine("HotKeyManager Initialized and Hook Added.");
    }
    
    public static int RegisterHotKey(Key key, ModifierKeys modifiers, Action hotkeyPressedAction)
    {
        if (_source == null || _windowHandle == IntPtr.Zero)
        {
            Debug.WriteLine($"ERROR: HotKeyManager not initialized. Cannot register {modifiers}+{key}.");
            return 0; 
        }

        _currentId++; 
        var idToRegister = _currentId;

        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        var fsModifiers = MapModifierKeys(modifiers);

        if (!RegisterHotKey(_windowHandle, idToRegister, fsModifiers, vk))
        {
            Debug.WriteLine($"Hotkey registration failed for ID {idToRegister} ({modifiers}+{key}). Maybe conflict?");
           
            return 0; 
        }

        // Registration successful, store action and ID
        _hotkeyActions[idToRegister] = hotkeyPressedAction;
        _registeredIds.Add(idToRegister);
        Debug.WriteLine($"Hotkey registered: ID={idToRegister}, Key={key}, Modifiers={modifiers}");
        return idToRegister;
    }

    private static void UnregisterHotKey(int id)
    {
         UnregisterHotKeyInternal(id);
    }

    private static void UnregisterHotKeyInternal(int id)
    {
        if (id == 0 || _windowHandle == IntPtr.Zero) return;

        if (_registeredIds.Contains(id))
        {
            try
            {
                if (!UnregisterHotKey(_windowHandle, id))
                {
                    Debug.WriteLine($"Warning: Failed to unregister hotkey ID {id}.");
                }
                else
                {
                    Debug.WriteLine($"Hotkey unregistered: ID={id}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception during UnregisterHotKey ID {id}: {ex.Message}");
            } // Catch potential errors

            _hotkeyActions.Remove(id);
            _registeredIds.Remove(id);
        }
    }
    
    public static void UnregisterAllHotkeys()
    {
        Debug.WriteLine($"Unregistering all hotkeys ({_registeredIds.Count})...");
        
        List<int> idsToUnregister = new List<int>(_registeredIds);
        foreach (int id in idsToUnregister)
        {
            UnregisterHotKey(id);
        }
        
        _hotkeyActions.Clear(); 
        _registeredIds.Clear();
        Debug.WriteLine($"All hotkeys unregistered.");
    }
    
    //Handle Key Press
    private static IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32(); 
            Debug.WriteLine($"WM_HOTKEY received for ID: {id}");
            
            if (_hotkeyActions.TryGetValue(id, out Action? action))
            {
                Debug.WriteLine($"Action found for ID {id}. Invoking...");
                action?.Invoke();
                handled = true;
            } else {
                Debug.WriteLine($"No action found for ID {id}.");
            }
        }
        return IntPtr.Zero;
    }

    private static uint MapModifierKeys(ModifierKeys modifiers)
    {
         var fsModifiers = MOD_NONE;
         if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) fsModifiers |= MOD_ALT;
         if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control) fsModifiers |= MOD_CONTROL;
         if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) fsModifiers |= MOD_SHIFT;
         if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows) fsModifiers |= MOD_WIN;
         return fsModifiers;
    }
     
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
    
    public static void Dispose()
    {
        Debug.WriteLine("Disposing HotKeyManager...");
        _source?.RemoveHook(HwndHook);
        _source = null;
        UnregisterAllHotkeys(); // Ensure all are unregistered
        _windowHandle = IntPtr.Zero;
        Debug.WriteLine("HotKeyManager disposed.");
    }
}