using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace SidebarDiagnostics.Windows
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        internal static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int RegisterWindowMessage(string msg);

        [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern UIntPtr SHAppBarMessage(int dwMessage, ref AppBarWindow.APPBARDATA pData);
        
        [DllImport("user32.dll")]
        internal static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lpRect, Monitors.MonitorEnumProc callback, int dwData);

        [DllImport("user32.dll")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref Monitors.MonitorInfo lpmi);
    }

    public class WorkArea
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Right { get; set; }
        public double Bottom { get; set; }
    }

    public static class Monitors
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MonitorInfo
        {
            public int cbSize;
            public RECT WorkAreaArea;
            public RECT WorkArea;
            public bool IsPrimary;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        internal delegate bool MonitorEnumProc(IntPtr hDesktop, IntPtr hdc, ref RECT pRect, int dwData);

        public static MonitorInfo[] GetMonitors()
        {
            List<MonitorInfo> _monitors = new List<MonitorInfo>();

            MonitorEnumProc _callback = (IntPtr hDesktop, IntPtr hdc, ref RECT pRect, int dwData) =>
            {
                MonitorInfo _info = new MonitorInfo();
                _info.cbSize = Marshal.SizeOf(_info);

                NativeMethods.GetMonitorInfo(hDesktop, ref _info);

                _monitors.Add(_info);

                return true;
            };

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, _callback, 0);

            return _monitors.ToArray();
        }

        public static MonitorInfo GetMonitorFromIndex(int index)
        {
            MonitorInfo[] _monitors = GetMonitors();

            if (index < _monitors.Length)
                return _monitors[index];
            else
                return _monitors.Where(s => s.IsPrimary).Single();
        }

        public static int GetNoOfMonitors()
        {
            return GetMonitors().Length;
        }

        public static WorkArea GetWorkArea(Window window)
        {
            MonitorInfo _screen = GetMonitorFromIndex(Properties.Settings.Default.ScreenIndex);

            PresentationSource _presentationSource = PresentationSource.FromVisual(window);
            double _scaleX = 1 / _presentationSource.CompositionTarget.TransformToDevice.M11;
            double _scaleY = 1 / _presentationSource.CompositionTarget.TransformToDevice.M22;

            WorkArea _workArea = new WorkArea()
            {
                Left = _screen.WorkArea.Left * _scaleX,
                Top = _screen.WorkArea.Top * _scaleY,
                Right = _screen.WorkArea.Right * _scaleX,
                Bottom = _screen.WorkArea.Bottom * _scaleY
            };

            switch (Properties.Settings.Default.DockEdge)
            {
                case DockEdge.Left:
                    _workArea.Right = _workArea.Left + window.ActualWidth;
                    break;

                case DockEdge.Right:
                    _workArea.Left = _workArea.Right - window.ActualWidth;
                    break;
            }

            return _workArea;
        }
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
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
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

        public static void SetAppBar(Window window, WorkArea workArea, DockEdge edge)
        {
            RegisterInfo _regInfo = GetRegisterInfo(window, workArea);

            APPBARDATA _appBarData = new APPBARDATA();
            _appBarData.cbSize = Marshal.SizeOf(_appBarData);
            _appBarData.hWnd = new WindowInteropHelper(window).Handle;

            if (edge == DockEdge.None)
            {
                if (_regInfo.IsRegistered)
                {
                    _regInfo.Source.RemoveHook(_regInfo.Hook);

                    NativeMethods.SHAppBarMessage((int)AppBarMsg.ABM_REMOVE, ref _appBarData);

                    _regInfo.IsRegistered = false;
                }

                return;
            }

            if (!_regInfo.IsRegistered)
            {
                _regInfo.IsRegistered = true;
                _regInfo.CallbackID = NativeMethods.RegisterWindowMessage("AppBarMessage");
                _regInfo.Edge = edge;

                _appBarData.uCallbackMessage = _regInfo.CallbackID;

                NativeMethods.SHAppBarMessage((int)AppBarMsg.ABM_NEW, ref _appBarData);
            }

            window.WindowStyle = WindowStyle.None;
            window.ResizeMode = ResizeMode.NoResize;

            DockAppBar(window, workArea, edge, _regInfo);
        }
                
        private static void DockAppBar(Window window, WorkArea workArea, DockEdge edge, RegisterInfo regInfo)
        {
            APPBARDATA _appBarData = new APPBARDATA();
            _appBarData.cbSize = Marshal.SizeOf(_appBarData);
            _appBarData.hWnd = new WindowInteropHelper(window).Handle;
            _appBarData.uEdge = (int)edge;

            _appBarData.rc = new RECT()
            {
                Left = (int)Math.Round(workArea.Left),
                Top = (int)Math.Round(workArea.Top),
                Right = (int)Math.Round(workArea.Right),
                Bottom = (int)Math.Round(workArea.Bottom)
            };

            NativeMethods.SHAppBarMessage((int)AppBarMsg.ABM_QUERYPOS, ref _appBarData);

            NativeMethods.SHAppBarMessage((int)AppBarMsg.ABM_SETPOS, ref _appBarData);

            Rect _rect = new Rect(
                _appBarData.rc.Left,
                _appBarData.rc.Top,
                (_appBarData.rc.Right - _appBarData.rc.Left),
                (_appBarData.rc.Bottom - _appBarData.rc.Top)
                );

            window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
            {
                window.Width = _rect.Width;
                window.Height = _rect.Height;
                window.Top = _rect.Top;
                window.Left = _rect.Left;

                Task.Delay(500).ContinueWith(_ =>
                {
                    regInfo.Hook = new HwndSourceHook(regInfo.WndProc);
                    regInfo.Source = HwndSource.FromHwnd(_appBarData.hWnd);
                    regInfo.Source.AddHook(regInfo.Hook);
                });
            }));
        }

        private class RegisterInfo
        {
            public int CallbackID { get; set; }
            public bool IsRegistered { get; set; }
            public Window Window { get; set; }
            public WorkArea WorkArea { get; set; }
            public DockEdge Edge { get; set; }
            public HwndSource Source { get; set; }
            public HwndSourceHook Hook { get; set; }

            public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                if (msg == CallbackID)
                {
                    if (wParam.ToInt32() == (int)AppBarNotify.ABN_POSCHANGED)
                    {
                        SetAppBar(Window, null, DockEdge.None);

                        WorkArea _workArea = Monitors.GetWorkArea(Window);

                        Window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
                        {
                            SetAppBar(Window, _workArea, Edge);
                        }));
                    }

                    handled = true;
                }

                return IntPtr.Zero;
            }
        }

        private static RegisterInfo GetRegisterInfo(Window window, WorkArea workArea)
        {
            RegisterInfo _regInfo;

            if (_windowDict.ContainsKey(window))
            {
                _regInfo = _windowDict[window];
                _regInfo.WorkArea = workArea ?? _regInfo.WorkArea;
            }
            else
            {
                _regInfo = new RegisterInfo()
                {
                    CallbackID = 0,
                    IsRegistered = false,
                    Window = window,
                    WorkArea = workArea,
                    Edge = DockEdge.Top
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