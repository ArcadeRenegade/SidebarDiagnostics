using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Forms;

namespace SidebarDiagnostics.Windows
{
    internal static class NativeMethods
    {
        [DllImport("User32.dll")]
        internal static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("User32.dll")]
        internal static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        internal static extern int RegisterWindowMessage(string msg);

        [DllImport("Shell32.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern UIntPtr SHAppBarMessage(int dwMessage, ref AppBarWindow.APPBARDATA pData);
    }

    public static class ClickThroughWindow
    {
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int GWL_EXSTYLE = (-20);

        public static void SetClickThrough(Window window)
        {
            IntPtr _hwnd = new WindowInteropHelper(window).Handle;
            int _style = NativeMethods.GetWindowLong(_hwnd, GWL_EXSTYLE);

            NativeMethods.SetWindowLong(_hwnd, GWL_EXSTYLE, _style | WS_EX_TRANSPARENT);
        }
    }

    public static class AppBarWindow
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public int uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        private enum AppBarMsg : int
        {
            ABM_NEW,
            ABM_REMOVE,
            ABM_QUERYPOS,
            ABM_SETPOS,
            ABM_GETSTATE,
            ABM_GETTASKBARPOS,
            ABM_ACTIVATE,
            ABM_GETAUTOHIDEBAR,
            ABM_SETAUTOHIDEBAR,
            ABM_WINDOWPOSCHANGED,
            ABM_SETSTATE
        }

        private enum AppBarNotify : int
        {
            ABN_STATECHANGE,
            ABN_POSCHANGED,
            ABN_FULLSCREENAPP,
            ABN_WINDOWARRANGE
        }

        public static void SetAppBar(Window window, Screen screen, DockEdge edge)
        {
            RegisterInfo _regInfo = GetRegisterInfo(window, screen);

            _regInfo.Edge = edge;

            APPBARDATA _appBarData = new APPBARDATA();
            _appBarData.cbSize = Marshal.SizeOf(_appBarData);
            _appBarData.hWnd = new WindowInteropHelper(window).Handle;

            if (edge == DockEdge.None)
            {
                if (_regInfo.IsRegistered)
                {
                    NativeMethods.SHAppBarMessage((int)AppBarMsg.ABM_REMOVE, ref _appBarData);
                    _regInfo.IsRegistered = false;
                }

                return;
            }

            if (!_regInfo.IsRegistered)
            {
                _regInfo.IsRegistered = true;
                _regInfo.CallbackID = NativeMethods.RegisterWindowMessage("AppBarMessage");
                _appBarData.uCallbackMessage = _regInfo.CallbackID;

                NativeMethods.SHAppBarMessage((int)AppBarMsg.ABM_NEW, ref _appBarData);
            }

            window.WindowStyle = WindowStyle.None;
            window.ResizeMode = ResizeMode.NoResize;

            DockAppBar(window, screen, edge);
        }
                
        private static void DockAppBar(Window window, Screen screen, DockEdge edge)
        {
            APPBARDATA _appBarData = new APPBARDATA();
            _appBarData.cbSize = Marshal.SizeOf(_appBarData);
            _appBarData.hWnd = new WindowInteropHelper(window).Handle;
            _appBarData.uEdge = (int)edge;

            int _left = screen.WorkingArea.Left;
            int _top = screen.WorkingArea.Top;
            int _right = screen.WorkingArea.Right;
            int _bottom = screen.WorkingArea.Bottom;

            int _width = screen.WorkingArea.Width;
            int _height = screen.WorkingArea.Width;

            int _windowWidth = (int)Math.Round(window.ActualWidth);
            int _windowHeight = (int)Math.Round(window.ActualHeight);

            DockEdge _edge = (DockEdge)_appBarData.uEdge;
            
            switch (_edge)
            {
                case DockEdge.Left:
                    _right = _left + _windowWidth;
                    break;

                case DockEdge.Right:
                    _left = _right - _windowWidth;
                    break;

                case DockEdge.Top:
                    _bottom = _top + _windowHeight;
                    break;

                case DockEdge.Bottom:
                    _top = _bottom - _windowHeight;
                    break;
            }

            _appBarData.rc = new RECT()
            {
                left = _left,
                top = _top,
                right = _right,
                bottom = _bottom
            };

            NativeMethods.SHAppBarMessage((int)AppBarMsg.ABM_QUERYPOS, ref _appBarData);

            NativeMethods.SHAppBarMessage((int)AppBarMsg.ABM_SETPOS, ref _appBarData);

            Rect _rect = new Rect(
                _appBarData.rc.left,
                _appBarData.rc.top,
                (_appBarData.rc.right - _appBarData.rc.left),
                (_appBarData.rc.bottom - _appBarData.rc.top)
                );

            window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
            {
                window.Width = _rect.Width;
                window.Height = _rect.Height;
                window.Top = _rect.Top;
                window.Left = _rect.Left;
            }));
        }

        private class RegisterInfo
        {
            public int CallbackID { get; set; }
            public bool IsRegistered { get; set; }
            public Window Window { get; set; }
            public Screen Screen { get; set; }
            public DockEdge Edge { get; set; }
            public WindowStyle OriginalStyle { get; set; }
            public Point OriginalPosition { get; set; }
            public Size OriginalSize { get; set; }
            public ResizeMode OriginalResizeMode { get; set; }

            public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                if (msg == CallbackID)
                {
                    if (wParam.ToInt32() == (int)AppBarNotify.ABN_POSCHANGED)
                    {
                        DockAppBar(Window, Screen, Edge);
                        handled = true;
                    }
                }

                return IntPtr.Zero;
            }
        }

        private static RegisterInfo GetRegisterInfo(Window window, Screen screen)
        {
            RegisterInfo _regInfo;

            if (_windowDict.ContainsKey(window))
            {
                _regInfo = _windowDict[window];
            }
            else
            {
                _regInfo = new RegisterInfo()
                {
                    CallbackID = 0,
                    IsRegistered = false,
                    Window = window,
                    Screen = screen,
                    Edge = DockEdge.Top,
                    OriginalStyle = window.WindowStyle,
                    OriginalPosition = new Point(window.Left, window.Top),
                    OriginalSize = new Size(window.ActualWidth, window.ActualHeight),
                    OriginalResizeMode = window.ResizeMode,
                };

                _windowDict.Add(window, _regInfo);
            }

            return _regInfo;
        }

        private static Dictionary<Window, RegisterInfo> _windowDict = new Dictionary<Window, RegisterInfo>();
    }

    public enum DockEdge : byte
    {
        Left,
        Top,
        Right,
        Bottom,
        None
    }
}