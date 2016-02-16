using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Media;
using SidebarDiagnostics.Style;
using Newtonsoft.Json;

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
        internal static extern long GetWindowLongPtr(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        internal static extern long SetWindowLongPtr(IntPtr hwnd, int index, long newStyle);

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

        [DllImport("user32.dll")]
        internal static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, ShowDesktop.WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        internal static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        internal static extern int GetClassName(IntPtr hwnd, StringBuilder name, int count);
    }

    public static class ShowDesktop
    {
        private const uint WINEVENT_OUTOFCONTEXT = 0u;
        private const uint EVENT_SYSTEM_FOREGROUND = 3u;

        private const string WORKERW = "WorkerW";
        private const string PROGMAN = "Progman";

        public static void AddHook(Window window)
        {
            if (IsHooked)
            {
                return;
            }

            IsHooked = true;

            _delegate = new WinEventDelegate(WinEventHook);
            _hookIntPtr = NativeMethods.SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _delegate, 0, 0, WINEVENT_OUTOFCONTEXT);
            _window = window;
        }

        public static void RemoveHook()
        {
            if (!IsHooked)
            {
                return;
            }

            IsHooked = false;

            NativeMethods.UnhookWinEvent(_hookIntPtr);
            Marshal.FreeHGlobal(_hookIntPtr);

            _delegate = null;
            _window = null;
        }

        private static string GetWindowClass(IntPtr hwnd)
        {
            StringBuilder _sb = new StringBuilder(32);
            NativeMethods.GetClassName(hwnd, _sb, _sb.Capacity);
            return _sb.ToString();
        }

        internal delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        private static void WinEventHook(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == EVENT_SYSTEM_FOREGROUND)
            {
                string _class = GetWindowClass(hwnd);

                if (string.Equals(_class, WORKERW, StringComparison.Ordinal) || string.Equals(_class, PROGMAN, StringComparison.Ordinal))
                {
                    _window.Topmost = true;
                }
                else
                {
                    _window.Topmost = false;
                }
            }
        }

        public static bool IsHooked { get; private set; } = false;

        private static IntPtr _hookIntPtr { get; set; }

        private static WinEventDelegate _delegate { get; set; }

        private static Window _window { get; set; }
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

        public static void AddHook(Sidebar window)
        {
            if (IsHooked)
            {
                return;
            }

            IsHooked = true;

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

            window.HwndSource.AddHook(DeviceHook);
        }

        public static void RemoveHook(Sidebar window)
        {
            if (!IsHooked)
            {
                return;
            }

            IsHooked = false;

            window.HwndSource.RemoveHook(DeviceHook);
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
                                Sidebar _sidebar = (Application.Current as App).GetSidebar;

                                if (_sidebar != null)
                                {
                                    _sidebar.ContentReload();
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

        public static bool IsHooked { get; private set; } = false;

        private static CancellationTokenSource _cancelRestart { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
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
            Close,
            CycleEdge,
            CycleScreen
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

        [JsonProperty]
        public KeyAction Action { get; set; }

        [JsonProperty]
        public uint VirtualKey { get; set; }

        [JsonProperty]
        public bool AltMod { get; set; }

        [JsonProperty]
        public bool CtrlMod { get; set; }

        [JsonProperty]
        public bool ShiftMod { get; set; }

        [JsonProperty]
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

        public static void Initialize(Sidebar window, Hotkey[] settings)
        {
            if (IsHooked || settings == null || settings.Length == 0)
            {
                return;
            }

            IsHooked = true;

            Disable();

            _sidebar = window;
            _index = 0;

            RegisteredKeys = settings.Select(h =>
            {
                h.Index = _index;
                _index++;
                return h;
            }).ToArray();

            window.HwndSource.AddHook(KeyHook);
        }

        public static void Dispose()
        {
            if (!IsHooked)
            {
                return;
            }

            IsHooked = false;

            Disable();

            RegisteredKeys = null;

            _sidebar.HwndSource.RemoveHook(KeyHook);
            _sidebar = null;
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
                new WindowInteropHelper(_sidebar).Handle,
                hotkey.Index,
                _mods,
                hotkey.VirtualKey
                );
        }

        private static void Unregister(Hotkey hotkey)
        {
            NativeMethods.UnregisterHotKey(
                new WindowInteropHelper(_sidebar).Handle,
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

                if (_hotkey != null && _sidebar != null && _sidebar.Ready)
                {
                    switch (_hotkey.Action)
                    {
                        case KeyAction.Toggle:
                            if (_sidebar.Visibility == Visibility.Visible)
                            {
                                _sidebar.AppBarHide();
                            }
                            else
                            {
                                _sidebar.AppBarShow();
                            }
                            break;

                        case KeyAction.Show:
                            _sidebar.AppBarShow();
                            break;

                        case KeyAction.Hide:
                            _sidebar.AppBarHide();
                            break;

                        case KeyAction.Reload:
                            _sidebar.Reload();
                            break;

                        case KeyAction.Close:
                            Application.Current.Shutdown();
                            break;

                        case KeyAction.CycleEdge:
                            if (_sidebar.Visibility == Visibility.Visible)
                            {
                                switch (Framework.Settings.Instance.DockEdge)
                                {
                                    case DockEdge.Right:
                                        Framework.Settings.Instance.DockEdge = DockEdge.Left;
                                        break;

                                    default:
                                    case DockEdge.Left:
                                        Framework.Settings.Instance.DockEdge = DockEdge.Right;
                                        break;
                                }

                                Framework.Settings.Instance.Save();

                                _sidebar.Reposition();
                            }
                            break;

                        case KeyAction.CycleScreen:
                            if (_sidebar.Visibility == Visibility.Visible)
                            {
                                Monitor[] _monitors = Monitor.GetMonitors();

                                if (Framework.Settings.Instance.ScreenIndex < (_monitors.Length - 1))
                                {
                                    Framework.Settings.Instance.ScreenIndex++;
                                }
                                else
                                {
                                    Framework.Settings.Instance.ScreenIndex = 0;
                                }

                                Framework.Settings.Instance.Save();

                                _sidebar.Reposition();
                            }
                            break;
                    }

                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        public static bool IsHooked { get; private set; } = false;

        private static Sidebar _sidebar { get; set; }

        private static int _index { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width
        {
            get
            {
                return Right - Left;
            }
        }

        public int Height
        {
            get
            {
                return Bottom - Top;
            }
        }
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
            screen = Framework.Settings.Instance.ScreenIndex;
            edge = Framework.Settings.Instance.DockEdge;

            Monitor _screen = GetMonitorFromIndex(screen);

            double _screenX = _screen.ScaleX;
            double _screenY = _screen.ScaleY;

            double _inverseX = _screen.InverseScaleX;
            double _inverseY = _screen.InverseScaleY;

            double _uiScale = Framework.Settings.Instance.UIScale;

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

            if (Framework.Settings.Instance.HighDPISupport)
            {
                windowWA.Left *= _inverseX;
                windowWA.Top *= _inverseY;
                windowWA.Right *= _inverseX;
                windowWA.Bottom *= _inverseY;
            }

            double _windowWidth = Framework.Settings.Instance.SidebarWidth * _uiScale;

            double _modifyX = 0d;

            if (
                window.IsAppBar &&
                window.Screen == screen &&
                window.DockEdge == edge &&
                (_screen.WorkArea.Width + window.AppBarWidth) <= _screen.Size.Width
                )
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

            int _offsetX = Framework.Settings.Instance.XOffset;
            int _offsetY = Framework.Settings.Instance.YOffset;

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

            if (Framework.Settings.Instance.HighDPISupport)
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

            double _uiScale = Framework.Settings.Instance.UIScale;

            UpdateScale(_uiScale, _uiScale, true);
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
            HandleDPI();

            Framework.Settings.Instance.PropertyChanged += UIScale_PropertyChanged;

            //HwndSource.AddHook(WindowHook);
        }

        private void UIScale_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UIScale")
            {
                HandleDPI();
            }
        }

        //private IntPtr WindowHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        //{
        //    if (msg == WM_MESSAGES.WM_DPICHANGED)
        //    {
        //        HandleDPI();

        //        handled = true;
        //    }

        //    return IntPtr.Zero;
        //}

        public HwndSource HwndSource
        {
            get
            {
                return (HwndSource)PresentationSource.FromVisual(this);
            }
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

        private static class WM_WINDOWPOSCHANGING
        {
            public const int MSG = 0x0046;
            public const int SWP_NOMOVE = 0x0002;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPOS
        {
            public IntPtr hWnd;
            public IntPtr hWndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            Loaded += AppBarWindow_Loaded;
        }

        private void AppBarWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PreventMove();
        }

        public void Move(WorkArea workArea)
        {
            AllowMove();

            Left = workArea.Left;
            Top = workArea.Top;
            Width = workArea.Width;
            Height = workArea.Height;

            PreventMove();
        }

        private void PreventMove()
        {
            if (!_canMove)
            {
                return;
            }

            _canMove = false;

            HwndSource.AddHook(MoveHook);
        }

        private void AllowMove()
        {
            if (_canMove)
            {
                return;
            }

            _canMove = true;

            HwndSource.RemoveHook(MoveHook);
        }

        private IntPtr MoveHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_WINDOWPOSCHANGING.MSG)
            {
                WINDOWPOS _pos = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

                _pos.flags |= WM_WINDOWPOSCHANGING.SWP_NOMOVE;

                Marshal.StructureToPtr(_pos, lParam, true);

                handled = true;
            }

            return IntPtr.Zero;
        }

        public void SetTopMost()
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

        public void ClearTopMost()
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
            int _style = (int)NativeMethods.GetWindowLongPtr(_hwnd, WND_STYLE.GWL_EXSTYLE);

            NativeMethods.SetWindowLongPtr(_hwnd, WND_STYLE.GWL_EXSTYLE, _style | WND_STYLE.WS_EX_TRANSPARENT);
        }

        public void ClearClickThrough()
        {
            IntPtr _hwnd = new WindowInteropHelper(this).Handle;
            int _style = (int)NativeMethods.GetWindowLongPtr(_hwnd, WND_STYLE.GWL_EXSTYLE);

            NativeMethods.SetWindowLongPtr(_hwnd, WND_STYLE.GWL_EXSTYLE, _style & ~WND_STYLE.WS_EX_TRANSPARENT);
        }

        public void SetAppBar(int screen, DockEdge edge, WorkArea windowWA, WorkArea appbarWA, Action callback)
        {
            if (edge == DockEdge.None)
            {
                throw new ArgumentException("This parameter cannot be set to 'none'.", "edge");
            }

            bool _init = false;

            if (!IsAppBar)
            {
                IsAppBar = _init = true;

                APPBARDATA _data = NewData();

                _callbackID = _data.uCallbackMessage = NativeMethods.RegisterWindowMessage("AppBarMessage");

                NativeMethods.SHAppBarMessage(APPBARMSG.ABM_NEW, ref _data);
            }

            Screen = screen;
            DockEdge = edge;

            DockAppBar(edge, windowWA, appbarWA, _init, callback);
        }

        public void ClearAppBar()
        {
            if (!IsAppBar)
            {
                return;
            }

            HwndSource.RemoveHook(AppBarHook);

            APPBARDATA _data = NewData();

            NativeMethods.SHAppBarMessage(APPBARMSG.ABM_REMOVE, ref _data);

            IsAppBar = false;
        }

        public void AppBarShow()
        {
            if (Framework.Settings.Instance.UseAppBar)
            {
                int _screen;
                DockEdge _edge;
                WorkArea _windowWA;
                WorkArea _appbarWA;

                Monitor.GetWorkArea(this, out _screen, out _edge, out _windowWA, out _appbarWA);

                SetAppBar(_screen, _edge, _windowWA, _appbarWA, null);
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

        private void DockAppBar(DockEdge edge, WorkArea windowWA, WorkArea appbarWA, bool init, Action callback)
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

            Move(windowWA);

            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
            {
                if (init)
                {
                    HwndSource.AddHook(AppBarHook);
                }

                if (callback != null)
                {
                    callback();
                }
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

                        Task.Delay(TimeSpan.FromMilliseconds(100), _cancelReposition.Token).ContinueWith(_ =>
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

                                SetAppBar(_screen, _edge, _windowWA, _appbarWA, null);
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
                            if (Framework.Settings.Instance.AlwaysTop)
                            {
                                SetTopMost();
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

        private bool _canMove { get; set; } = true;

        private int _callbackID { get; set; }

        private int _prevZOrder { get; set; }

        private CancellationTokenSource _cancelReposition { get; set; }
    }
}