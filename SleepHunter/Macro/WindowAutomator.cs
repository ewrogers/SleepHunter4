using System;
using System.Windows;
using System.Windows.Input;

using SleepHunter.Win32;

namespace SleepHunter.Macro
{
    internal static class WindowAutomator
    {
        public static readonly byte VK_SHIFT = 0x10;
        public static readonly byte VK_ESCAPE = 0x1B;
        public static readonly byte VK_SPACE = 0x20;
        public static readonly byte VK_TILDE = 0xC0;

        public static readonly uint MK_LBUTTON = 0x1;
        public static readonly uint MK_RBUTTON = 0x2;
        public static readonly uint MK_MBUTTON = 0x10;
        public static readonly uint MK_XBUTTON1 = 0x20;
        public static readonly uint MK_XUBTTON2 = 0x40;

        enum KeyboardCommand : uint
        {
            HotKey = 0x312,
            KeyDown = 0x100,
            KeyUp = 0x101,
            Char = 0x102,
            DeadChar = 0x103,
            SysKeyDown = 0x014,
            SysKeyUp = 0x105,
            SysDeadChar = 0x107
        }

        enum MouseCommand : uint
        {
            MouseMove = 0x200,
            LeftButtonDown = 0x201,
            LeftButtonUp = 0x202,
            LeftButtonDoubleClick = 203,
            RightButtonDown = 0x204,
            RightButtonUp = 0x205,
            RightButtonDoubleClick = 0x206,
            MiddleButtonDown = 0x207,
            MiddleButtonUp = 0x208,
            MiddleButtonDoubleClick = 0x209
        }

        enum ControlCommand : uint
        {
            WM_CLOSE = 0x10
        }

        private sealed class KeyParameter
        {
            private readonly short repeatCount;
            private readonly byte scanCode;
            private readonly bool isExtendedKey;
            private readonly bool contextCode;
            private readonly bool previousState;
            private readonly bool transitionState;

            public KeyParameter(uint lParam)
            {
                repeatCount = (short)(lParam & 0xFFFF);
                scanCode = (byte)((lParam >> 16) & 0xFF);
                isExtendedKey = (lParam & (1 << 24)) != 0;
                contextCode = (lParam & (1 << 29)) != 0;
                previousState = (lParam & (1 << 30)) != 0;
                transitionState = (lParam & (1 << 31)) != 0;
            }

            public KeyParameter(short repeatCount, byte scanCode, bool isExtendedKey = false, bool contextCode = false, bool previousState = false, bool transitionState = true)
            {
                this.repeatCount = repeatCount;
                this.scanCode = scanCode;
                this.isExtendedKey = isExtendedKey;
                this.contextCode = contextCode;
                this.previousState = previousState;
                this.transitionState = transitionState;
            }

            public UIntPtr ToLParam()
            {
                long lParam = repeatCount;

                lParam |= ((uint)scanCode << 16);

                if (isExtendedKey)
                    lParam |= (1u << 24);

                if (contextCode)
                    lParam |= (1u << 29);

                if (previousState)
                    lParam |= (1u << 30);

                if (transitionState)
                    lParam |= (1u << 31);

                return new UIntPtr((uint)lParam);
            }
        }

        #region Keyboard Actions
        public static void SendKeyDown(IntPtr windowHandle, char key)
        {
            var virtualKey = GetVirtualKey(key, out var _);
            SendKeyDown(windowHandle, virtualKey);
        }

        public static void SendKeyChar(IntPtr windowHandle, char key)
        {
            var virtualKey = GetVirtualKey(key, out var _);
            SendKeyChar(windowHandle, virtualKey);
        }

        public static void SendKeyUp(IntPtr windowHandle, char key)
        {
            var virtualKey = GetVirtualKey(key, out var _);
            SendKeyUp(windowHandle, virtualKey);
        }

        public static void SendKeyDown(IntPtr windowHandle, byte virtualKey)
        {
            var scanCode = GetScanCode(virtualKey);
            var keyParameter = new KeyParameter(1, scanCode, false, false, false, false);

            NativeMethods.PostMessage(windowHandle, (uint)KeyboardCommand.KeyDown, new UIntPtr(virtualKey), keyParameter.ToLParam());
        }

        public static void SendKeyChar(IntPtr windowHandle, byte virtualKey)
        {

        }

        public static void SendKeyUp(IntPtr windowHandle, byte virtualKey)
        {
            var scanCode = GetScanCode(virtualKey);
            var keyParameter = new KeyParameter(1, scanCode, false, false, true, true);

            NativeMethods.PostMessage(windowHandle, (uint)KeyboardCommand.KeyUp, new UIntPtr(virtualKey), keyParameter.ToLParam());
        }

        public static void SendShiftKeyDown(IntPtr windowHandle)
        {
            var virtualKey = VK_SHIFT;
            var scanCode = GetScanCode(virtualKey);
            var keyParameter = new KeyParameter(1, scanCode, false, false, false, false);

            NativeMethods.PostMessage(windowHandle, (uint)KeyboardCommand.KeyDown, new UIntPtr(virtualKey), keyParameter.ToLParam());
        }

        public static void SendShiftKeyUp(IntPtr windowHandle)
        {
            var virtualKey = VK_SHIFT;
            var scanCode = GetScanCode(virtualKey);
            var keyParameter = new KeyParameter(1, scanCode, false, false, true, true);

            NativeMethods.PostMessage(windowHandle, (uint)KeyboardCommand.KeyUp, new UIntPtr(virtualKey), keyParameter.ToLParam());
        }

        public static void SendKeystroke(IntPtr windowHandle, char key, bool includeCharMessage = false)
        {
            SendKeyDown(windowHandle, key);

            if (includeCharMessage)
                SendKeyChar(windowHandle, key);

            SendKeyUp(windowHandle, key);
        }

        public static void SendKeystroke(IntPtr windowHandle, byte virtualKey, bool includeCharMessage = false)
        {
            SendKeyDown(windowHandle, virtualKey);

            if (includeCharMessage)
                SendKeyChar(windowHandle, virtualKey);

            SendKeyUp(windowHandle, virtualKey);
        }
        #endregion

        #region Mouse Actions
        public static void SendMouseMove(IntPtr windowHandle, int x, int y)
        {
            NativeMethods.PostMessage(windowHandle, (uint)MouseCommand.MouseMove, UIntPtr.Zero, new UIntPtr(MakeXYParameter(new Point(x, y))));
        }

        public static void SendMouseDown(IntPtr windowHandle, MouseButton mouseButton, int x = 0, int y = 0)
        {
            var xyParam = MakeXYParameter(new Point(x, y));

            if (mouseButton.HasFlag(MouseButton.Left))
                NativeMethods.PostMessage(windowHandle, (uint)MouseCommand.LeftButtonDown, new UIntPtr(MK_LBUTTON), new UIntPtr(xyParam));

            if (mouseButton.HasFlag(MouseButton.Middle))
                NativeMethods.PostMessage(windowHandle, (uint)MouseCommand.MiddleButtonDown, new UIntPtr(MK_MBUTTON), new UIntPtr(xyParam));

            if (mouseButton.HasFlag(MouseButton.Right))
                NativeMethods.PostMessage(windowHandle, (uint)MouseCommand.RightButtonDown, new UIntPtr(MK_RBUTTON), new UIntPtr(xyParam));
        }

        public static void SendMouseUp(IntPtr windowHandle, MouseButton mouseButton, int x = 0, int y = 0)
        {
            var xyParam = MakeXYParameter(new Point(x, y));

            if (mouseButton.HasFlag(MouseButton.Left))
                NativeMethods.PostMessage(windowHandle, (uint)MouseCommand.LeftButtonUp, new UIntPtr(MK_LBUTTON), new UIntPtr(xyParam));

            if (mouseButton.HasFlag(MouseButton.Middle))
                NativeMethods.PostMessage(windowHandle, (uint)MouseCommand.MiddleButtonUp, new UIntPtr(MK_MBUTTON), new UIntPtr(xyParam));

            if (mouseButton.HasFlag(MouseButton.Right))
                NativeMethods.PostMessage(windowHandle, (uint)MouseCommand.RightButtonUp, new UIntPtr(MK_RBUTTON), new UIntPtr(xyParam));
        }

        public static void SendMouseClick(IntPtr windowHandle, MouseButton mouseButton, int x, int y, bool moveFirst = true)
        {
            if (moveFirst)
                SendMouseMove(windowHandle, x, y);

            SendMouseDown(windowHandle, mouseButton, x, y);
            SendMouseUp(windowHandle, mouseButton, x, y);
        }
        #endregion

        #region Control Actions
        public static void SendCloseWindow(IntPtr windowHandle)
        {
            NativeMethods.PostMessage(windowHandle, (uint)ControlCommand.WM_CLOSE, UIntPtr.Zero, UIntPtr.Zero);
        }
        #endregion

        #region Helper Methods
        static uint MakeXYParameter(Point pt)
        {
            var parameter = (uint)pt.X;
            parameter |= ((uint)pt.Y << 16);

            return parameter;
        }

        static byte GetScanCode(char c)
        {
            var vkey = GetVirtualKey(c);
            var scanCode = NativeMethods.MapVirtualKey(vkey, VirtualKeyMapMode.VirtualToScanCode);

            return (byte)scanCode;
        }

        static byte GetScanCode(byte virtualKey)
        {
            var scanCode = NativeMethods.MapVirtualKey(virtualKey, VirtualKeyMapMode.VirtualToScanCode);

            return (byte)scanCode;
        }

        static byte GetVirtualKey(char c)
        {
            return GetVirtualKey(c, out var _);
        }

        static byte GetVirtualKey(char c, out ModifierKeys modifiers)
        {
            var keyScan = NativeMethods.VkKeyScan(c);

            var vkey = (byte)keyScan;
            byte modifierScan = (byte)(keyScan >> 8);

            modifiers = (ModifierKeys)modifierScan;
            return vkey;
        }
        #endregion
    }
}
