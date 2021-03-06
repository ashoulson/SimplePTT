﻿using System;
using System.Linq;
using System.Windows.Interop;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Windows;
using System.Diagnostics;

namespace SimplePTT
{
  public class KeyEventArgs : EventArgs
  {
    public Key Key { get { return this.key; } }
    private readonly Key key;

    public KeyEventArgs(Key key)
    {
      this.key = key;
    }
  }

  public class KeyboardHook : IDisposable
  {
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    private delegate IntPtr LowLevelKeyboardProc(
      int nCode,
      IntPtr wParam,
      IntPtr lParam);

    public event EventHandler<KeyEventArgs> KeyDown;
    public event EventHandler<KeyEventArgs> KeyUp;

    private LowLevelKeyboardProc keyboardProc;
    private IntPtr hookId;

    public KeyboardHook()
    {
      this.keyboardProc = this.HookCallback;
      this.hookId = this.SetHook(this.keyboardProc);
    }

    public void Dispose()
    {
      KeyboardHook.UnhookWindowsHookEx(this.hookId);
    }

    private void OnKeyDown(Key key)
    {
      if (this.KeyDown != null)
        this.KeyDown.Invoke(null, new KeyEventArgs(key));
    }

    private void OnKeyUp(Key key)
    {
      if (this.KeyUp != null)
        this.KeyUp.Invoke(null, new KeyEventArgs(key));
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
      using (Process curProcess = Process.GetCurrentProcess())
      {
        using (ProcessModule curModule = curProcess.MainModule)
        {
          return SetWindowsHookEx(
            WH_KEYBOARD_LL, 
            proc, 
            GetModuleHandle(curModule.ModuleName), 
            0);
        }
      }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
      if (nCode >= 0)
      {
        bool keyDown = (wParam == (IntPtr)WM_KEYDOWN);
        bool keyUp = (wParam == (IntPtr)WM_KEYUP);

        if (wParam == (IntPtr)WM_KEYDOWN)
        {
          OnKeyDown(KeyInterop.KeyFromVirtualKey(Marshal.ReadInt32(lParam)));
        }
        else if (wParam == (IntPtr)WM_KEYUP)
        {
          OnKeyUp(KeyInterop.KeyFromVirtualKey(Marshal.ReadInt32(lParam)));
        }
      }

      return CallNextHookEx(this.hookId, nCode, wParam, lParam);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
      int idHook, 
      LowLevelKeyboardProc lpfn, 
      IntPtr hMod, 
      uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(
      IntPtr hhk, 
      int nCode, 
      IntPtr wParam, 
      IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
  }
}
