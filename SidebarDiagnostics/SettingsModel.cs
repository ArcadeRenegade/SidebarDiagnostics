using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using SidebarDiagnostics.Helpers;
using SidebarDiagnostics.Monitor;
using SidebarDiagnostics.Windows;
using SidebarDiagnostics.Properties;

namespace SidebarDiagnostics.Models
{
    public class SettingsModel : INotifyPropertyChanged
    {
        public SettingsModel()
        {
            DockEdgeItems = new DockEdge[2] { DockEdge.Left, DockEdge.Right };
            DockEdge = Properties.Settings.Default.DockEdge;

            MonitorInfo[] _monitors = Windows.Monitor.GetMonitors();

            ScreenItems = _monitors.Select((s, i) => new ScreenItem() { Index = i, Text = string.Format("#{0}", i + 1) }).ToArray();

            if (Properties.Settings.Default.ScreenIndex < _monitors.Length)
            {
                ScreenIndex = Properties.Settings.Default.ScreenIndex;
            }
            else
            {
                ScreenIndex = _monitors.Where(s => s.IsPrimary).Select((s, i) => i).Single();
            }

            XOffset = Properties.Settings.Default.XOffset;

            YOffset = Properties.Settings.Default.YOffset;

            PollingInterval = Properties.Settings.Default.PollingInterval;

            UseAppBar = Properties.Settings.Default.UseAppBar;

            AlwaysTop = Properties.Settings.Default.AlwaysTop;

            HighDPISupport = Properties.Settings.Default.HighDPISupport;

            ClickThrough = Properties.Settings.Default.ClickThrough;

            ShowTrayIcon = Properties.Settings.Default.ShowTrayIcon;

            CheckForUpdates = Properties.Settings.Default.CheckForUpdates;

            RunAtStartup = Startup.StartupTaskExists();

            SidebarWidth = Properties.Settings.Default.SidebarWidth;

            BGColor = Properties.Settings.Default.BGColor;

            BGOpacity = Properties.Settings.Default.BGOpacity;

            FontSettingItems = new FontSetting[5]
            {
                FontSetting.x10,
                FontSetting.x12,
                FontSetting.x14,
                FontSetting.x16,
                FontSetting.x18
            };
            FontSetting = Properties.Settings.Default.FontSetting;

            FontColor = Properties.Settings.Default.FontColor;

            AlertFontColor = Properties.Settings.Default.AlertFontColor;

            DateSettingItems = new DateSetting[4]
            {
                DateSetting.Disabled,
                DateSetting.Short,
                DateSetting.Normal,
                DateSetting.Long
            };
            DateSetting = Properties.Settings.Default.DateSetting;

            CollapseMenuBar = Properties.Settings.Default.CollapseMenuBar;

            ShowClock = Properties.Settings.Default.ShowClock;

            Clock24HR = Properties.Settings.Default.Clock24HR;

            _monitorConfig = Properties.Settings.Default.MonitorConfig;

            if (Properties.Settings.Default.Hotkeys != null)
            {
                ToggleKey = Properties.Settings.Default.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Toggle);

                ShowKey = Properties.Settings.Default.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Show);

                HideKey = Properties.Settings.Default.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Hide);

                ReloadKey = Properties.Settings.Default.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Reload);

                CloseKey = Properties.Settings.Default.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Close);
            }
        }

        public void Save()
        {
            Properties.Settings.Default.DockEdge = DockEdge;
            Properties.Settings.Default.ScreenIndex = ScreenIndex;
            Properties.Settings.Default.XOffset = XOffset;
            Properties.Settings.Default.YOffset = YOffset;
            Properties.Settings.Default.PollingInterval = PollingInterval;
            Properties.Settings.Default.UseAppBar = UseAppBar;
            Properties.Settings.Default.AlwaysTop = AlwaysTop;
            Properties.Settings.Default.HighDPISupport = HighDPISupport;
            Properties.Settings.Default.ClickThrough = ClickThrough;
            Properties.Settings.Default.ShowTrayIcon = ShowTrayIcon;
            Properties.Settings.Default.CheckForUpdates = CheckForUpdates;
            Properties.Settings.Default.SidebarWidth = SidebarWidth;
            Properties.Settings.Default.BGColor = BGColor;
            Properties.Settings.Default.BGOpacity = BGOpacity;
            Properties.Settings.Default.FontSetting = FontSetting;
            Properties.Settings.Default.FontColor = FontColor;
            Properties.Settings.Default.AlertFontColor = AlertFontColor;
            Properties.Settings.Default.DateSetting = DateSetting;
            Properties.Settings.Default.CollapseMenuBar = CollapseMenuBar;
            Properties.Settings.Default.ShowClock = ShowClock;
            Properties.Settings.Default.Clock24HR = Clock24HR;
            Properties.Settings.Default.MonitorConfig = _monitorConfig;

            List<Hotkey> _hotkeys = new List<Hotkey>();

            if (ToggleKey != null)
            {
                _hotkeys.Add(ToggleKey);
            }
            
            if (ShowKey != null)
            {
                _hotkeys.Add(ShowKey);
            }

            if (HideKey != null)
            {
                _hotkeys.Add(HideKey);
            }

            if (ReloadKey != null)
            {
                _hotkeys.Add(ReloadKey);
            }

            if (CloseKey != null)
            {
                _hotkeys.Add(CloseKey);
            }

            Properties.Settings.Default.Hotkeys = _hotkeys.ToArray();

            Properties.Settings.Default.Save();

            App.RefreshIcon();

            if (RunAtStartup)
            {
                Startup.EnableStartupTask();
            }
            else
            {
                Startup.DisableStartupTask();
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler _handler = PropertyChanged;

            if (_handler != null)
            {
                _handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private DockEdge _dockEdge { get; set; }

        public DockEdge DockEdge
        {
            get
            {
                return _dockEdge;
            }
            set
            {
                _dockEdge = value;

                NotifyPropertyChanged("DockEdge");
            }
        }

        private DockEdge[] _dockEdgeItems { get; set; }

        public DockEdge[] DockEdgeItems
        {
            get
            {
                return _dockEdgeItems;
            }
            set
            {
                _dockEdgeItems = value;

                NotifyPropertyChanged("DockEdgeItems");
            }
        }

        private int _screenIndex { get; set; }

        public int ScreenIndex
        {
            get
            {
                return _screenIndex;
            }
            set
            {
                _screenIndex = value;

                NotifyPropertyChanged("ScreenIndex");
            }
        }

        private ScreenItem[] _screenItems { get; set; }

        public ScreenItem[] ScreenItems
        {
            get
            {
                return _screenItems;
            }
            set
            {
                _screenItems = value;

                NotifyPropertyChanged("ScreenItems");
            }
        }

        private int _xOffset { get; set; }

        public int XOffset
        {
            get
            {
                return _xOffset;
            }
            set
            {
                _xOffset = value;

                NotifyPropertyChanged("XOffset");
            }
        }

        private int _yOffset { get; set; }

        public int YOffset
        {
            get
            {
                return _yOffset;
            }
            set
            {
                _yOffset = value;

                NotifyPropertyChanged("YOffset");
            }
        }

        private int _pollingInterval { get; set; }

        public int PollingInterval
        {
            get
            {
                return _pollingInterval;
            }
            set
            {
                _pollingInterval = value;

                NotifyPropertyChanged("PollingInterval");
            }
        }

        private bool _useAppBar { get; set; }

        public bool UseAppBar
        {
            get
            {
                return _useAppBar;
            }
            set
            {
                _useAppBar = value;

                NotifyPropertyChanged("UseAppBar");
            }
        }

        private bool _alwaysTop { get; set; }

        public bool AlwaysTop
        {
            get
            {
                return _alwaysTop;
            }
            set
            {
                _alwaysTop = value;

                NotifyPropertyChanged("AlwaysTop");
            }
        }

        private bool _highDPISupport { get; set; }

        public bool HighDPISupport
        {
            get
            {
                return _highDPISupport;
            }
            set
            {
                _highDPISupport = value;

                NotifyPropertyChanged("HighDPISupport");
            }
        }

        private bool _clickThrough { get; set; }

        public bool ClickThrough
        {
            get
            {
                return _clickThrough;
            }
            set
            {
                _clickThrough = value;

                NotifyPropertyChanged("ClickThrough");
            }
        }

        private bool _showTrayIcon { get; set; }

        public bool ShowTrayIcon
        {
            get
            {
                return _showTrayIcon;
            }
            set
            {
                _showTrayIcon = value;

                NotifyPropertyChanged("ShowTrayIcon");
            }
        }

        private bool _checkForUpdates { get; set; }

        public bool CheckForUpdates
        {
            get
            {
                return _checkForUpdates;
            }
            set
            {
                _checkForUpdates = value;

                NotifyPropertyChanged("CheckForUpdates");
            }
        }

        private bool _runAtStartup { get; set; }

        public bool RunAtStartup
        {
            get
            {
                return _runAtStartup;
            }
            set
            {
                _runAtStartup = value;

                NotifyPropertyChanged("RunAtStartup");
            }
        }

        private int _sidebarWidth { get; set; }

        public int SidebarWidth
        {
            get
            {
                return _sidebarWidth;
            }
            set
            {
                _sidebarWidth = value;

                NotifyPropertyChanged("SidebarWidth");
            }
        }

        private string _bgColor { get; set; }

        public string BGColor
        {
            get
            {
                return _bgColor;
            }
            set
            {
                _bgColor = value;

                NotifyPropertyChanged("BGColor");
            }
        }

        private double _bgOpacity { get; set; }

        public double BGOpacity
        {
            get
            {
                return _bgOpacity;
            }
            set
            {
                _bgOpacity = value;

                NotifyPropertyChanged("BGOpacity");
            }
        }

        private FontSetting _fontSetting { get; set; }

        public FontSetting FontSetting
        {
            get
            {
                return _fontSetting;
            }
            set
            {
                _fontSetting = value;

                NotifyPropertyChanged("FontSize");
            }
        }

        private FontSetting[] _fontSettingItems { get;  set;}

        public FontSetting[] FontSettingItems
        {
            get
            {
                return _fontSettingItems;
            }
            set
            {
                _fontSettingItems = value;

                NotifyPropertyChanged("FontSizeItems");
            }
        }

        private string _fontColor { get; set; }

        public string FontColor
        {
            get
            {
                return _fontColor;
            }
            set
            {
                _fontColor = value;

                NotifyPropertyChanged("FontColor");
            }
        }

        private string _alertFontColor { get; set; }

        public string AlertFontColor
        {
            get
            {
                return _alertFontColor;
            }
            set
            {
                _alertFontColor = value;

                NotifyPropertyChanged("AlertFontColor");
            }
        }

        private DateSetting _dateSetting { get; set; }

        public DateSetting DateSetting
        {
            get
            {
                return _dateSetting;
            }
            set
            {
                _dateSetting = value;

                NotifyPropertyChanged("DateSetting");
            }
        }

        private DateSetting[] _dateSettingItems { get; set; }

        public DateSetting[] DateSettingItems
        {
            get
            {
                return _dateSettingItems;
            }
            set
            {
                _dateSettingItems = value;

                NotifyPropertyChanged("DateSettingItems");
            }
        }

        private bool _collapseMenuBar { get; set; }

        public bool CollapseMenuBar
        {
            get
            {
                return _collapseMenuBar;
            }
            set
            {
                _collapseMenuBar = value;

                NotifyPropertyChanged("CollapseMenuBar");
            }
        }

        private bool _showClock { get; set; }

        public bool ShowClock
        {
            get
            {
                return _showClock;
            }
            set
            {
                _showClock = value;

                NotifyPropertyChanged("ShowClock");
            }
        }

        private bool _clock24HR { get; set; }

        public bool Clock24HR
        {
            get
            {
                return _clock24HR;
            }
            set
            {
                _clock24HR = value;

                NotifyPropertyChanged("Clock24HR");
            }
        }

        private MonitorConfig[] _monitorConfig { get; set; }

        public MonitorConfig[] MonitorConfig
        {
            get
            {
                return _monitorConfig.OrderBy(c => c.Order).ToArray();
            }
        }

        private Hotkey _toggleKey { get; set; }

        public Hotkey ToggleKey
        {
            get
            {
                return _toggleKey;
            }
            set
            {
                _toggleKey = value;

                NotifyPropertyChanged("ToggleKey");
            }
        }

        private Hotkey _showKey { get; set; }

        public Hotkey ShowKey
        {
            get
            {
                return _showKey;
            }
            set
            {
                _showKey = value;

                NotifyPropertyChanged("ShowKey");
            }
        }

        private Hotkey _hideKey { get; set; }

        public Hotkey HideKey
        {
            get
            {
                return _hideKey;
            }
            set
            {
                _hideKey = value;

                NotifyPropertyChanged("HideKey");
            }
        }

        private Hotkey _reloadKey { get; set; }

        public Hotkey ReloadKey
        {
            get
            {
                return _reloadKey;
            }
            set
            {
                _reloadKey = value;

                NotifyPropertyChanged("ReloadKey");
            }
        }

        private Hotkey _closeKey { get; set; }

        public Hotkey CloseKey
        {
            get
            {
                return _closeKey;
            }
            set
            {
                _closeKey = value;

                NotifyPropertyChanged("CloseKey");
            }
        }
    }

    public class ScreenItem
    {
        public int Index { get; set; }
        public string Text { get; set; }
    }
}
