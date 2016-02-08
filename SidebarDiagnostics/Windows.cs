using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Media;
using SidebarDiagnostics.Style;

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

                if (_version.Major >= 10)
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
        internal static extern int GetWindowLongPtr(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        internal static extern int SetWindowLongPtr(IntPtr hwnd, int index, int newStyle);

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

        [DllImport("user32.dll")]
        internal static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint vk);

        [DllImport("user32.dll")]
        internal static extern bool UnregisterHotKey(IntPtr hwnd, int id);

        [DllImport("user32.dll")]
        internal static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr notificationFilter, int flags);

        [DllImport("user32.dll")]
        internal static extern bool UnregisterDeviceNotification(IntPtr handle);
    }

    public static class Devices
    {
        private const int WM_DEVICECHANGE = 0x0219;

        private static class DBCH_DEVICETYPE
        {
            public const int DBT_DEVTYP_DEVICEINTERFACE = 5;
            public const int DBT_DEVTYP_HANDLE = 6;
            public const int DBT_DEVTYP_OEM = 0;
            public const int DBT_DEVTYP_PORT = 3;
            public const int DBT_DEVTYP_VOLUME = 2;
        }
        
        private static class FLAGS
        {
            public const int DEVICE_NOTIFY_WINDOW_HANDLE = 0;
            public const int DEVICE_NOTIFY_SERVICE_HANDLE = 1;
            public const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 4;
        }

        private static class WM_DEVICECHANGE_EVENT
        {
            public const int DBT_CONFIGCHANGECANCELED = 0x0019;
            public const int DBT_CONFIGCHANGED = 0x0018;
            public const int DBT_CUSTOMEVENT = 0x8006;
            public const int DBT_DEVICEARRIVAL = 0x8000;
            public const int DBT_DEVICEQUERYREMOVE = 0x8001;
            public const int DBT_DEVICEQUERYREMOVEFAILED = 0x8002;
            public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
            public const int DBT_DEVICEREMOVEPENDING = 0x8003;
            public const int DBT_DEVICETYPESPECIFIC = 0x8005;
            public const int DBT_DEVNODES_CHANGED = 0x0007;
            public const int DBT_QUERYCHANGECONFIG = 0x0017;
            public const int DBT_USERDEFINED = 0xFFFF;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DEV_BROADCAST_HDR
        {
            public int dbch_size;
            public int dbch_devicetype;
            public int dbch_reserved;
        }

        public static void Initialize(Window window)
        {
            DEV_BROADCAST_HDR _data = new DEV_BROADCAST_HDR();
            _data.dbch_size = Marshal.SizeOf(_data);
            _data.dbch_devicetype = DBCH_DEVICETYPE.DBT_DEVTYP_DEVICEINTERFACE;

            IntPtr _buffer = Marshal.AllocHGlobal(_data.dbch_size);
            Marshal.StructureToPtr(_data, _buffer, true);

            IntPtr _hwnd = new WindowInteropHelper(window).Handle;

            NativeMethods.RegisterDeviceNotification(
                _hwnd,
                _buffer,
                FLAGS.DEVICE_NOTIFY_ALL_INTERFACE_CLASSES
                );

            HwndSource.FromHwnd(_hwnd).AddHook(DeviceHook);
        }

        private static IntPtr DeviceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DEVICECHANGE)
            {                
                switch (wParam.ToInt32())
                {
                    case WM_DEVICECHANGE_EVENT.DBT_DEVICEARRIVAL:
                    case WM_DEVICECHANGE_EVENT.DBT_DEVICEREMOVECOMPLETE:

                        if (_cancelRestart != null)
                        {
                            _cancelRestart.Cancel();
                        }

                        _cancelRestart = new CancellationTokenSource();

                        Task.Delay(TimeSpan.FromSeconds(1), _cancelRestart.Token).ContinueWith(_ =>
                        {
                            if (_.IsCanceled)
                            {
                                return;
                            }

                            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
                            {
                                AppBar _appbar = (Application.Current as App).GetAppBar;

                                if (_appbar != null && _appbar.Ready)
                                {
                                    _appbar.Model.Reload();
                                }
                            }));

                            _cancelRestart = null;
                        });
                        break;
                }

                handled = true;
            }

            return IntPtr.Zero;
        }

        private static CancellationTokenSource _cancelRestart { get; set; }
    }

    [Serializable]
    public class Hotkey
    {
        private const int WM_HOTKEY = 0x0312;

        private static class MODIFIERS
        {
            public const uint MOD_NOREPEAT = 0x4000;
            public const uint MOD_ALT = 0x0001;
            public const uint MOD_CONTROL = 0x0002;
            public const uint MOD_SHIFT = 0x0004;
            public const uint MOD_WIN = 0x0008;
        }

        public enum KeyAction : byte
        {
            Toggle,
            Show,
            Hide,
            Reload,
            Close
        }

        public Hotkey() { }

        public Hotkey(int index, KeyAction action, uint virtualKey, bool altMod = false, bool ctrlMod = false, bool shiftMod = false, bool winMod = false)
        {
            Index = index;
            Action = action;
            VirtualKey = virtualKey;
            AltMod = altMod;
            CtrlMod = ctrlMod;
            ShiftMod = shiftMod;
            WinMod = winMod;
        }

        public KeyAction Action { get; set; }

        public uint VirtualKey { get; set; }

        public bool AltMod { get; set; }

        public bool CtrlMod { get; set; }

        public bool ShiftMod { get; set; }

        public bool WinMod { get; set; }

        public Key WinKey
        {
            get
            {
                return KeyInterop.KeyFromVirtualKey((int)VirtualKey);
            }
            set
            {
                VirtualKey = (uint)KeyInterop.VirtualKeyFromKey(value);
            }
        }

        private int Index { get; set; }

        public static void Initialize(AppBar window, Hotkey[] settings)
        {
            if (settings == null)
            {
                return;
            }

            Disable();

            _window = window;
            _index = 0;

            RegisteredKeys = settings.Select(h =>
            {
                h.Index = _index;
                _index++;
                return h;
            }).ToArray();

            (PresentationSource.FromVisual(window) as HwndSource).AddHook(KeyHook);
        }

        public static void Enable()
        {
            if (RegisteredKeys == null)
            {
                return;
            }

            foreach (Hotkey _hotkey in RegisteredKeys)
            {
                Register(_hotkey);
            }
        }

        public static void Disable()
        {
            if (RegisteredKeys == null)
            {
                return;
            }

            foreach (Hotkey _hotkey in RegisteredKeys)
            {
                Unregister(_hotkey);
            }
        }

        private static void Register(Hotkey hotkey)
        {
            uint _mods = MODIFIERS.MOD_NOREPEAT;

            if (hotkey.AltMod)
            {
                _mods |= MODIFIERS.MOD_ALT;
            }

            if (hotkey.CtrlMod)
            {
                _mods |= MODIFIERS.MOD_CONTROL;
            }

            if (hotkey.ShiftMod)
            {
                _mods |= MODIFIERS.MOD_SHIFT;
            }

            if (hotkey.WinMod)
            {
                _mods |= MODIFIERS.MOD_WIN;
            }

            NativeMethods.RegisterHotKey(
                new WindowInteropHelper(_window).Handle,
                hotkey.Index,
                _mods,
                hotkey.VirtualKey
                );
        }

        private static void Unregister(Hotkey hotkey)
        {
            NativeMethods.UnregisterHotKey(
                new WindowInteropHelper(_window).Handle,
                hotkey.Index
                );
        }

        public static Hotkey[] RegisteredKeys { get; private set; }
        
        private static IntPtr KeyHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int _id = wParam.ToInt32();

                Hotkey _hotkey = RegisteredKeys.FirstOrDefault(k => k.Index == _id);

                if (_hotkey != null && _window != null && _window.Ready)
                {
                    switch (_hotkey.Action)
                    {
                        case KeyAction.Toggle:
                            if (_window.Visibility == Visibility.Visible)
                            {
                                _window.AppBarHide();
                            }
                            else
                            {
                                _window.AppBarShow();
                            }
                            break;

                        case KeyAction.Show:
                            _window.AppBarShow();
                            break;

                        case KeyAction.Hide:
                            _window.AppBarHide();
                            break;

                        case KeyAction.Reload:
                            _window.Reload();
                            break;

                        case KeyAction.Close:
                            Application.Current.Shutdown();
                            break;
                    }
                }

                handled = true;
            }

            return IntPtr.Zero;
        }

        private static AppBar _window { get; set; }

        private static int _index { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
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

    public class Monitor
    {
        private const uint DPICONST = 96u;

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

        internal delegate bool EnumCallback(IntPtr hDesktop, IntPtr hdc, ref RECT pRect, int dwData);

        public static Monitor GetMonitor(IntPtr hMonitor)
        {
            MONITORINFO _info = new MONITORINFO();
            _info.cbSize = Marshal.SizeOf(_info);

            NativeMethods.GetMonitorInfo(hMonitor, ref _info);

            uint _dpiX = Monitor.DPICONST;
            uint _dpiY = Monitor.DPICONST;

            if (OS.SupportDPI)
            {
                NativeMethods.GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out _dpiX, out _dpiY);
            }

            return new Monitor()
            {
                Size = _info.Size,
                WorkArea = _info.WorkArea,
                DPIx = _dpiX,
                DPIy = _dpiY,
                IsPrimary = _info.IsPrimary
            };
        }

        public static Monitor[] GetMonitors()
        {
            List<Monitor> _monitors = new List<Monitor>();

            EnumCallback _callback = (IntPtr hMonitor, IntPtr hdc, ref RECT pRect, int dwData) =>
            {
                _monitors.Add(GetMonitor(hMonitor));

                return true;
            };

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, _callback, 0);

            return _monitors.OrderByDescending(m => m.IsPrimary).ToArray();
        }

        public static Monitor GetMonitorFromIndex(int index)
        {
            Monitor[] _monitors = GetMonitors();

            if (index < _monitors.Length)
                return _monitors[index];
            else
                return _monitors.Where(s => s.IsPrimary).Single();
        }
        
        public static void GetWorkArea(AppBarWindow window, out int screen, out DockEdge edge, out WorkArea windowWA, out WorkArea appbarWA)
        {
            screen = Properties.Settings.Default.ScreenIndex;
            edge = Properties.Settings.Default.DockEdge;

            Monitor _screen = GetMonitorFromIndex(screen);

            double _screenX = _screen.ScaleX;
            double _screenY = _screen.ScaleY;

            double _inverseX = _screen.InverseScaleX;
            double _inverseY = _screen.InverseScaleY;

            double _uiScale = Properties.Settings.Default.UIScale;

            double _abScaleX = _screenX * _uiScale;
            double _abScaleY = _screenY * _uiScale;
            
            if (OS.SupportDPI)
            {
                window.UpdateScale(_uiScale, _uiScale, false);
            }

            windowWA = new WorkArea()
            {
                Left = _screen.WorkArea.Left,
                Top = _screen.WorkArea.Top,
                Right = _screen.WorkArea.Right,
                Bottom = _screen.WorkArea.Bottom
            };

            if (Properties.Settings.Default.HighDPISupport)
            {
                windowWA.Left *= _inverseX;
                windowWA.Top *= _inverseY;
                windowWA.Right *= _inverseX;
                windowWA.Bottom *= _inverseY;
            }

            double _windowWidth = Properties.Settings.Default.SidebarWidth * _uiScale;

            double _modifyX = 0d;

            if (window.IsAppBar && window.Screen == screen && window.DockEdge == edge)
            {
                _modifyX = window.AppBarWidth;
            }

            switch (edge)
            {
                case DockEdge.Left:
                    windowWA.Right = windowWA.Left + _windowWidth - _modifyX;
                    windowWA.Left -= _modifyX;
                    break;

                case DockEdge.Right:
                    windowWA.Left = windowWA.Right - _windowWidth + _modifyX;
                    windowWA.Right += _modifyX;
                    break;
            }

            int _offsetX = Properties.Settings.Default.XOffset;
            int _offsetY = Properties.Settings.Default.YOffset;

            windowWA.Left += _offsetX;
            windowWA.Top += _offsetY;
            windowWA.Right += _offsetX;
            windowWA.Bottom += _offsetY;

            appbarWA = new WorkArea()
            {
                Left = windowWA.Left,
                Top = windowWA.Top,
                Right = windowWA.Right,
                Bottom = windowWA.Bottom
            };

            if (Properties.Settings.Default.HighDPISupport)
            {
                double _abWidth = appbarWA.Width * _abScaleX;

                switch (edge)
                {
                    case DockEdge.Left:
                        appbarWA.Right = appbarWA.Left + _abWidth;
                        break;

                    case DockEdge.Right:
                        appbarWA.Left = appbarWA.Right - _abWidth;
                        break;
                }
            }
        }
    }

    public partial class DPIAwareWindow : FlatWindow
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
            //IntPtr _hwnd = new WindowInteropHelper(this).Handle;

            //IntPtr _hmonitor = NativeMethods.MonitorFromWindow(_hwnd, 0);

            //Monitor _monitorInfo = Monitor.GetMonitor(_hmonitor);

            double _uiScale = Properties.Settings.Default.UIScale;

            UpdateScale(_uiScale, _uiScale, true);
        }

        private void UIScale_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UIScale")
            {
                HandleDPI();
            }
        }

        public void UpdateScale(double scaleX, double scaleY, bool resize)
        {
            if (VisualChildrenCount > 0)
            {
                GetVisualChild(0).SetValue(LayoutTransformProperty, new ScaleTransform(scaleX, scaleY));
            }

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
            Properties.Settings.Default.PropertyChanged += UIScale_PropertyChanged;

            HandleDPI();

            (PresentationSource.FromVisual(this) as HwndSource).AddHook(WindowHook);
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
            public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

            public const uint SWP_NOSIZE = 0x0001;
            public const uint SWP_NOMOVE = 0x0002;
            public const uint SWP_NOACTIVATE = 0x0010;
        }

        private static class WND_STYLE
        {
            public const int GWL_EXSTYLE = -20;
            public const int WS_EX_TRANSPARENT = 32;
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

        public void ClearTop()
        {
            NativeMethods.SetWindowPos(
                new WindowInteropHelper(this).Handle,
                HWND_FLAG.HWND_NOTOPMOST,
                0,
                0,
                0,
                0,
                HWND_FLAG.SWP_NOMOVE | HWND_FLAG.SWP_NOSIZE | HWND_FLAG.SWP_NOACTIVATE
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

        public void SetClickThrough()
        {
            IntPtr _hwnd = new WindowInteropHelper(this).Handle;
            int _style = NativeMethods.GetWindowLongPtr(_hwnd, WND_STYLE.GWL_EXSTYLE);

            NativeMethods.SetWindowLongPtr(_hwnd, WND_STYLE.GWL_EXSTYLE, _style | WND_STYLE.WS_EX_TRANSPARENT);
        }

        public void ClearClickThrough()
        {
            IntPtr _hwnd = new WindowInteropHelper(this).Handle;
            int _style = NativeMethods.GetWindowLongPtr(_hwnd, WND_STYLE.GWL_EXSTYLE);

            NativeMethods.SetWindowLongPtr(_hwnd, WND_STYLE.GWL_EXSTYLE, _style & ~WND_STYLE.WS_EX_TRANSPARENT);
        }

        public void SetAppBar(int screen, DockEdge edge, WorkArea windowWA, WorkArea appbarWA)
        {
            if (edge == DockEdge.None)
            {
                throw new ArgumentException("This parameter cannot be set to 'none'.", "edge");
            }

            if (!IsAppBar)
            {
                IsAppBar = true;

                APPBARDATA _data = NewData();

                _callbackID = _data.uCallbackMessage = NativeMethods.RegisterWindowMessage("AppBarMessage");

                NativeMethods.SHAppBarMessage(APPBARMSG.ABM_NEW, ref _data);
            }

            Screen = screen;
            DockEdge = edge;

            DockAppBar(edge, windowWA, appbarWA);
        }

        public void ClearAppBar()
        {
            if (!IsAppBar)
            {
                return;
            }

            _source.RemoveHook(AppBarHook);
            _source = null;

            APPBARDATA _data = NewData();

            NativeMethods.SHAppBarMessage(APPBARMSG.ABM_REMOVE, ref _data);

            IsAppBar = false;
        }

        public void AppBarShow()
        {
            if (Properties.Settings.Default.UseAppBar)
            {
                int _screen;
                DockEdge _edge;
                WorkArea _windowWA;
                WorkArea _appbarWA;

                Monitor.GetWorkArea(this, out _screen, out _edge, out _windowWA, out _appbarWA);

                SetAppBar(_screen, _edge, _windowWA, _appbarWA);
            }

            Show();
            Activate();
        }

        public void AppBarHide()
        {
            Hide();

            if (IsAppBar)
            {
                ClearAppBar();
            }
        }

        private APPBARDATA NewData()
        {
            APPBARDATA _data = new APPBARDATA();
            _data.cbSize = Marshal.SizeOf(_data);
            _data.hWnd = new WindowInteropHelper(this).Handle;

            return _data;
        }

        private void DockAppBar(DockEdge edge, WorkArea windowWA, WorkArea appbarWA)
        {
            APPBARDATA _data = NewData();
            _data.uEdge = (int)edge;
            _data.rc = new RECT()
            {
                Left = (int)Math.Round(appbarWA.Left),
                Top = (int)Math.Round(appbarWA.Top),
                Right = (int)Math.Round(appbarWA.Right),
                Bottom = (int)Math.Round(appbarWA.Bottom)
            };

            NativeMethods.SHAppBarMessage(APPBARMSG.ABM_QUERYPOS, ref _data);

            NativeMethods.SHAppBarMessage(APPBARMSG.ABM_SETPOS, ref _data);

            appbarWA.Left = _data.rc.Left;
            appbarWA.Top = _data.rc.Top;
            appbarWA.Right = _data.rc.Right;
            appbarWA.Bottom = _data.rc.Bottom;

            AppBarWidth = appbarWA.Width;

            _source = HwndSource.FromHwnd(_data.hWnd);

            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
            {
                Top = windowWA.Top;
                Left = windowWA.Left;
                Width = windowWA.Width;
                Height = windowWA.Height;

                _source.AddHook(AppBarHook);
            }));
        }

        private IntPtr AppBarHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == _callbackID)
            {
                switch (wParam.ToInt32())
                {
                    case APPBARNOTIFY.ABN_POSCHANGED:
                        if (_cancelReposition != null)
                        {
                            _cancelReposition.Cancel();
                        }

                        _cancelReposition = new CancellationTokenSource();

                        Task.Delay(TimeSpan.FromMilliseconds(50), _cancelReposition.Token).ContinueWith(_ =>
                        {
                            if (_.IsCanceled)
                            {
                                return;
                            }

                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                            {
                                int _screen;
                                DockEdge _edge;
                                WorkArea _windowWA;
                                WorkArea _appbarWA;

                                Monitor.GetWorkArea(this, out _screen, out _edge, out _windowWA, out _appbarWA);

                                SetAppBar(_screen, _edge, _windowWA, _appbarWA);
                            }));

                            _cancelReposition = null;
                        });
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

        public int Screen { get; private set; } = 0;

        public DockEdge DockEdge { get; private set; } = DockEdge.None;

        public double AppBarWidth { get; private set; } = 0;

        private int _callbackID { get; set; }

        private int _prevZOrder { get; set; }

        private HwndSource _source { get; set; }

        private CancellationTokenSource _cancelReposition { get; set; }
    }
}