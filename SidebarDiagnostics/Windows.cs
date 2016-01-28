using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        internal static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lpRect, Monitor.EnumCallback callback, int dwData);

        [DllImport("user32.dll")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MonitorInfo
    {
        public int cbSize;
        public RECT WorkAreaArea;
        public RECT WorkArea;
        public bool IsPrimary;
    }

    public class WorkArea
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Right { get; set; }
        public double Bottom { get; set; }
    }

    public static class Monitor
    {
        internal delegate bool EnumCallback(IntPtr hDesktop, IntPtr hdc, ref RECT pRect, int dwData);

        public static MonitorInfo[] GetMonitors()
        {
            List<MonitorInfo> _monitors = new List<MonitorInfo>();

            EnumCallback _callback = (IntPtr hDesktop, IntPtr hdc, ref RECT pRect, int dwData) =>
            {
                MonitorInfo _info = new MonitorInfo();
                _info.cbSize = Marshal.SizeOf(_info);

                NativeMethods.GetMonitorInfo(hDesktop, ref _info);

                _monitors.Add(_info);

                return true;
            };

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, _callback, 0);

            return _monitors.OrderByDescending(m => m.IsPrimary).ToArray();
        }

        public static MonitorInfo GetMonitorFromIndex(int index)
        {
            MonitorInfo[] _monitors = GetMonitors();

            if (index < _monitors.Length)
                return _monitors[index];
            else
                return _monitors.Where(s => s.IsPrimary).Single();
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

    public static class ClickThrough
    {
        private const int WS_EX_TRANSPARENT = 32;
        private const int GWL_EXSTYLE = -20;

        public static void SetClickThrough(Window window)
        {
            IntPtr _hwnd = new WindowInteropHelper(window).Handle;
            int _style = NativeMethods.GetWindowLong(_hwnd, GWL_EXSTYLE);

            NativeMethods.SetWindowLong(_hwnd, GWL_EXSTYLE, _style | WS_EX_TRANSPARENT);
        }
    }

    [Serializable]
    public enum DockEdge : byte
    {
        Left,
        Top,
        Right,
        Bottom,
        None
    }

    public class AppBarWindow : Window
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

        private enum APPBARMSG : int
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

        private enum APPBARNOTIFY : int
        {
            ABN_STATECHANGE,
            ABN_POSCHANGED,
            ABN_FULLSCREENAPP,
            ABN_WINDOWARRANGE
        }

        public void SetAppBar(WorkArea workArea, DockEdge edge)
        {
            if (edge == DockEdge.None)
            {
                throw new ArgumentException("This parameter cannot be set to 'none'.", "edge");
            }

            APPBARDATA _data = NewData();

            if (!IsAppBar)
            {
                IsAppBar = true;
                DockEdge = edge;

                _callbackID = _data.uCallbackMessage = NativeMethods.RegisterWindowMessage("AppBarMessage");

                NativeMethods.SHAppBarMessage((int)APPBARMSG.ABM_NEW, ref _data);
            }
            
            DockAppBar(workArea, edge);
        }

        public void ClearAppBar()
        {
            if (!IsAppBar)
            {
                throw new InvalidOperationException("This window is not a registered AppBar.");
            }

            _source.RemoveHook(_hook);

            _source = null;
            _hook = null;

            APPBARDATA _data = NewData();

            NativeMethods.SHAppBarMessage((int)APPBARMSG.ABM_REMOVE, ref _data);

            IsAppBar = false;
        }

        private APPBARDATA NewData()
        {
            APPBARDATA _data = new APPBARDATA();
            _data.cbSize = Marshal.SizeOf(_data);
            _data.hWnd = new WindowInteropHelper(this).Handle;

            return _data;
        }

        private void DockAppBar(WorkArea workArea, DockEdge edge)
        {
            APPBARDATA _data = NewData();
            _data.uEdge = (int)edge;
            _data.rc = new RECT()
            {
                Left = (int)Math.Round(workArea.Left),
                Top = (int)Math.Round(workArea.Top),
                Right = (int)Math.Round(workArea.Right),
                Bottom = (int)Math.Round(workArea.Bottom)
            };

            NativeMethods.SHAppBarMessage((int)APPBARMSG.ABM_QUERYPOS, ref _data);

            NativeMethods.SHAppBarMessage((int)APPBARMSG.ABM_SETPOS, ref _data);

            Rect _rect = new Rect(
                _data.rc.Left,
                _data.rc.Top,
                (_data.rc.Right - _data.rc.Left),
                (_data.rc.Bottom - _data.rc.Top)
                );

            _hook = new HwndSourceHook(AppBarCallback);
            _source = HwndSource.FromHwnd(_data.hWnd);

            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
            {                
                Width = _rect.Width;
                Height = _rect.Height;
                Top = _rect.Top;
                Left = _rect.Left;
                
                LocationChanged += AppBarWindow_LocationChanged;
            }));
        }

        private void AppBarWindow_LocationChanged(object sender, EventArgs e)
        {
            LocationChanged -= AppBarWindow_LocationChanged;

            _source.AddHook(_hook);
        }

        private IntPtr AppBarCallback(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == _callbackID)
            {
                if (wParam.ToInt32() == (int)APPBARNOTIFY.ABN_POSCHANGED)
                {
                    ClearAppBar();

                    Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
                    {
                        WorkArea _workArea = Monitor.GetWorkArea(this);

                        SetAppBar(_workArea, DockEdge);
                    }));
                }

                handled = true;
            }

            return IntPtr.Zero;
        }

        public bool IsAppBar { get; private set; } = false;

        public DockEdge DockEdge { get; private set; } = DockEdge.None;

        private int _callbackID { get; set; }

        private HwndSource _source { get; set; }

        private HwndSourceHook _hook { get; set; }
    }
}