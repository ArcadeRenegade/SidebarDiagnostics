using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Media;

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
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref Monitor.MONITORINFO lpmi);

        [DllImport("shcore.dll")]
        internal static extern IntPtr GetDpiForMonitor(IntPtr hmonitor, Monitor.MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY);

        [DllImport("user32.dll")]
        internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
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

    public class MonitorInfo
    {
        private const double DPICONST = 96d;

        public RECT Size { get; set; }

        public RECT WorkArea { get; set; }

        public double DPIx { get; set; }

        public double ScaleX
        {
            get
            {
                return DPIx / DPICONST;
            }
        }

        public double InverseScaleX
        {
            get
            {
                return 1 / ScaleX;
            }
        }

        public double DPIy { get; set; }

        public double ScaleY
        {
            get
            {
                return DPIy / DPICONST;
            }
        }

        public double InverseScaleY
        {
            get
            {
                return 1 / ScaleY;
            }
        }

        public bool IsPrimary { get; set; }
    }

    public class WorkArea
    {
        public double Left { get; set; }

        public double Top { get; set; }

        public double Right { get; set; }

        public double Bottom { get; set; }

        public double Width
        {
            get
            {
                return Right - Left;
            }
        }

        public double Height
        {
            get
            {
                return Bottom - Top;
            }
        }
    }

    public static class Monitor
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct MONITORINFO
        {
            public int cbSize;
            public RECT Size;
            public RECT WorkArea;
            public bool IsPrimary;
        }

        internal enum MONITOR_DPI_TYPE : int
        {
            MDT_EFFECTIVE_DPI = 0,
            MDT_ANGULAR_DPI = 1,
            MDT_RAW_DPI = 2,
            MDT_DEFAULT = MDT_EFFECTIVE_DPI
        }

        internal delegate bool EnumCallback(IntPtr hDesktop, IntPtr hdc, ref RECT pRect, int dwData);

        public static MonitorInfo GetMonitor(IntPtr hMonitor)
        {
            MONITORINFO _info = new MONITORINFO();
            _info.cbSize = Marshal.SizeOf(_info);

            NativeMethods.GetMonitorInfo(hMonitor, ref _info);

            uint _dpiX;
            uint _dpiY;

            NativeMethods.GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out _dpiX, out _dpiY);

            return new MonitorInfo()
            {
                Size = _info.Size,
                WorkArea = _info.WorkArea,
                DPIx = _dpiX,
                DPIy = _dpiY,
                IsPrimary = _info.IsPrimary
            };
        }

        public static MonitorInfo[] GetMonitors()
        {
            List<MonitorInfo> _monitors = new List<MonitorInfo>();

            EnumCallback _callback = (IntPtr hMonitor, IntPtr hdc, ref RECT pRect, int dwData) =>
            {
                _monitors.Add(GetMonitor(hMonitor));

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
        
        public static WorkArea GetWorkArea(DPIAwareWindow window)
        {
            MonitorInfo _screen = GetMonitorFromIndex(Properties.Settings.Default.ScreenIndex);

            //PresentationSource _presentationSource = PresentationSource.FromVisual(window);

            //double _wpfX = _presentationSource.CompositionTarget.TransformToDevice.M11;
            //double _wpfY = _presentationSource.CompositionTarget.TransformToDevice.M22;

            //double _iwpfX = 1d / _wpfX;
            //double _iwpfY = 1d / _wpfY;

            double _screenX = _screen.ScaleX;
            double _screenY = _screen.ScaleY;

            //double _iScreenX = _screen.InverseScaleX;
            //double _iScreenY = _screen.InverseScaleY;

            window.UpdateScale(_screen.ScaleX, _screen.ScaleY, false);

            WorkArea _workArea = new WorkArea()
            {
                Left = _screen.WorkArea.Left,
                Top = _screen.WorkArea.Top,
                Right = _screen.WorkArea.Right,
                Bottom = _screen.WorkArea.Bottom
            };

            double _windowWidth = Properties.Settings.Default.SidebarWidth * _screenX;

            switch (Properties.Settings.Default.DockEdge)
            {
                case DockEdge.Left:
                    _workArea.Right = _workArea.Left + _windowWidth;
                    break;

                case DockEdge.Right:
                    _workArea.Left = _workArea.Right - _windowWidth;
                    break;
            }

            return _workArea;
        }
    }

    public partial class DPIAwareWindow : Window
    {
        public override void EndInit()
        {
            base.EndInit();

            Loaded += DPIAwareWindow_Loaded;
        }

        private void DPIAwareWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!HandleOwnDPI)
            {
                IntPtr _hwnd = new WindowInteropHelper(this).Handle;

                IntPtr _hmonitor = NativeMethods.MonitorFromWindow(_hwnd, 0);

                MonitorInfo _monitorInfo = Monitor.GetMonitor(_hmonitor);

                UpdateScale(_monitorInfo.ScaleX, _monitorInfo.ScaleY, true);
            }
        }

        public void UpdateScale(double scaleX, double scaleY, bool resize)
        {
            GetVisualChild(0).SetValue(LayoutTransformProperty, new ScaleTransform(scaleX, scaleY));

            if (resize)
            {
                Width *= scaleX;
                Height *= scaleY;
            }
        }

        public bool HandleOwnDPI { get; set; } = false;

        public int OriginalWidth { get; set; }

        public int OriginalHeight { get; set; }
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

    public partial class AppBarWindow : DPIAwareWindow
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

        public void SetAppBar(DockEdge edge, WorkArea workArea)
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
            
            DockAppBar(edge, workArea);
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

        private void DockAppBar(DockEdge edge, WorkArea workArea)
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

            //Rect _rect = new Rect(
            //    _data.rc.Left,
            //    _data.rc.Top,
            //    (_data.rc.Right - _data.rc.Left),
            //    (_data.rc.Bottom - _data.rc.Top)
            //    );

            _hook = new HwndSourceHook(AppBarCallback);
            _source = HwndSource.FromHwnd(_data.hWnd);

            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
            {
                Top = workArea.Top;
                Left = workArea.Left;
                Width = workArea.Width;
                Height = workArea.Height;
                
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

                        SetAppBar(DockEdge, _workArea);
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