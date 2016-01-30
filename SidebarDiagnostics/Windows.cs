using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Media;
using System.ComponentModel;

namespace SidebarDiagnostics.Windows
{
    public enum WinOS : byte
    {
        Unknown,
        Other,
        Win7,
        Win8,
        Win10
    }

    public static class OS
    {
        private static WinOS _os { get; set; } = WinOS.Unknown;

        public static WinOS Get
        {
            get
            {
                if (_os != WinOS.Unknown)
                {
                    return _os;
                }

                Version _version = Environment.OSVersion.Version;

                if (_version.Major == 10)
                {
                    _os = WinOS.Win10;
                }
                else if (_version.Major == 6 && new int[2] { 2, 3 }.Contains(_version.Minor))
                {
                    _os = WinOS.Win8;
                }
                else if (_version.Major == 6 && _version.Minor == 1)
                {
                    _os = WinOS.Win7;
                }
                else
                {
                    _os = WinOS.Other;
                }

                return _os;
            }
        }

        public static bool SupportDPI
        {
            get
            {
                return OS.Get >= WinOS.Win8;
            }
        }

        public static bool SupportVirtualDesktop
        {
            get
            {
                return OS.Get >= WinOS.Win10;
            }
        }
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        internal static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        internal static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwnd_after, int x, int y, int cx, int cy, uint uflags);

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

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public class MonitorInfo
    {
        internal const uint DPICONST = 96u;

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

            uint _dpiX = MonitorInfo.DPICONST;
            uint _dpiY = MonitorInfo.DPICONST;

            if (OS.SupportDPI)
            {
                NativeMethods.GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out _dpiX, out _dpiY);
            }

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

            double _screenX = _screen.ScaleX;
            double _screenY = _screen.ScaleY;

            if (OS.SupportDPI)
            {
                window.UpdateScale(_screen.ScaleX, _screen.ScaleY, false);
            }

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
        private static class WM_MESSAGES
        {
            public const int WM_DPICHANGED = 0x02E0;
            public const int WM_GETMINMAXINFO = 0x0024;
            public const int WM_SIZE = 0x0005;
            public const int WM_WINDOWPOSCHANGING = 0x0046;
            public const int WM_WINDOWPOSCHANGED = 0x0047;
        }

        public override void EndInit()
        {
            base.EndInit();

            _originalWidth = base.Width;
            _originalHeight = base.Height;

            if (AutoDPI && OS.SupportDPI)
            {
                Loaded += DPIAwareWindow_Loaded;
            }
        }

        public void HandleDPI()
        {
            IntPtr _hwnd = new WindowInteropHelper(this).Handle;

            IntPtr _hmonitor = NativeMethods.MonitorFromWindow(_hwnd, 0);

            MonitorInfo _monitorInfo = Monitor.GetMonitor(_hmonitor);

            UpdateScale(_monitorInfo.ScaleX, _monitorInfo.ScaleY, true);
        }

        public void UpdateScale(double scaleX, double scaleY, bool resize)
        {
            GetVisualChild(0).SetValue(LayoutTransformProperty, new ScaleTransform(scaleX, scaleY));

            if (resize)
            {
                SizeToContent _autosize = SizeToContent;
                SizeToContent = SizeToContent.Manual;

                base.Width = _originalWidth * scaleX;
                base.Height = _originalHeight * scaleY;

                SizeToContent = _autosize;
            }
        }

        private void DPIAwareWindow_Loaded(object sender, RoutedEventArgs e)
        {
            HandleDPI();

            //(PresentationSource.FromVisual(this) as HwndSource).AddHook(WindowHook);
        }

        private IntPtr WindowHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MESSAGES.WM_DPICHANGED)
            {
                HandleDPI();

                handled = true;
            }

            return IntPtr.Zero;
        }

        public static readonly DependencyProperty AutoDPIProperty = DependencyProperty.Register("AutoDPI", typeof(bool), typeof(DPIAwareWindow), new UIPropertyMetadata(true));

        public bool AutoDPI
        {
            get
            {
                return (bool)GetValue(AutoDPIProperty);
            }
            set
            {
                SetValue(AutoDPIProperty, value);
            }
        }

        public new double Width
        {
            get
            {
                return base.Width;
            }
            set
            {
                _originalWidth = base.Width = value;
            }
        }

        public new double Height
        {
            get
            {
                return base.Height;
            }
            set
            {
                _originalHeight = base.Height = value;
            }
        }

        private double _originalWidth { get; set; }

        private double _originalHeight { get; set; }
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

        private static class APPBARMSG
        {
            public const int ABM_NEW = 0;
            public const int ABM_REMOVE = 1;
            public const int ABM_QUERYPOS = 2;
            public const int ABM_SETPOS = 3;
            public const int ABM_GETSTATE = 4;
            public const int ABM_GETTASKBARPOS = 5;
            public const int ABM_ACTIVATE = 6;
            public const int ABM_GETAUTOHIDEBAR = 7;
            public const int ABM_SETAUTOHIDEBAR = 8;
            public const int ABM_WINDOWPOSCHANGED = 9;
            public const int ABM_SETSTATE = 10;
        }

        private static class APPBARNOTIFY
        {
            public const int ABN_STATECHANGE = 0;
            public const int ABN_POSCHANGED = 1;
            public const int ABN_FULLSCREENAPP = 2;
            public const int ABN_WINDOWARRANGE = 3;
        }

        private static class HWND_FLAG
        {
            public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
            public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

            public const uint SWP_NOSIZE = 0x0001;
            public const uint SWP_NOMOVE = 0x0002;
            public const uint SWP_NOACTIVATE = 0x0010;
        }

        public void SetTop()
        {
            NativeMethods.SetWindowPos(
                new WindowInteropHelper(this).Handle,
                HWND_FLAG.HWND_TOPMOST,
                0,
                0,
                0,
                0,
                HWND_FLAG.SWP_NOMOVE | HWND_FLAG.SWP_NOSIZE
                );
        }

        public void SetBottom()
        {
            NativeMethods.SetWindowPos(
                new WindowInteropHelper(this).Handle,
                HWND_FLAG.HWND_BOTTOM,
                0,
                0,
                0,
                0,
                HWND_FLAG.SWP_NOMOVE | HWND_FLAG.SWP_NOSIZE | HWND_FLAG.SWP_NOACTIVATE
                );
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

                NativeMethods.SHAppBarMessage(APPBARMSG.ABM_NEW, ref _data);
            }
            
            DockAppBar(edge, workArea);
        }

        public void ClearAppBar()
        {
            if (!IsAppBar)
            {
                throw new InvalidOperationException("This window is not a registered AppBar.");
            }

            _source.RemoveHook(AppBarHook);
            _source = null;

            APPBARDATA _data = NewData();

            NativeMethods.SHAppBarMessage(APPBARMSG.ABM_REMOVE, ref _data);

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

            NativeMethods.SHAppBarMessage(APPBARMSG.ABM_QUERYPOS, ref _data);

            NativeMethods.SHAppBarMessage(APPBARMSG.ABM_SETPOS, ref _data);

            _source = HwndSource.FromHwnd(_data.hWnd);

            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
            {
                Top = workArea.Top;
                Left = workArea.Left;
                Width = workArea.Width;
                Height = workArea.Height;

                //LocationChanged += AppBarWindow_LocationChanged;

                _source.AddHook(AppBarHook);
            }));
        }

        private void AppBarWindow_LocationChanged(object sender, EventArgs e)
        {
            LocationChanged -= AppBarWindow_LocationChanged;

            _source.AddHook(AppBarHook);
        }

        private IntPtr AppBarHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == _callbackID)
            {
                switch (wParam.ToInt32())
                {
                    case APPBARNOTIFY.ABN_POSCHANGED:
                        ClearAppBar();

                        Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
                        {
                            WorkArea _workArea = Monitor.GetWorkArea(this);

                            SetAppBar(DockEdge, _workArea);
                        }));

                        break;

                    case APPBARNOTIFY.ABN_FULLSCREENAPP:
                        if (lParam.ToInt32() == 1)
                        {
                            SetBottom();
                        }
                        else
                        {
                            if (Properties.Settings.Default.AlwaysTop)
                            {
                                SetTop();
                            }
                        }

                        break;
                }

                handled = true;
            }

            return IntPtr.Zero;
        }

        public bool IsAppBar { get; private set; } = false;

        public DockEdge DockEdge { get; private set; } = DockEdge.None;

        private int _callbackID { get; set; }

        private int _prevZOrder { get; set; }

        private HwndSource _source { get; set; }
    }
}