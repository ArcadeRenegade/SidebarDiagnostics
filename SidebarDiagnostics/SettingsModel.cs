using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using SidebarDiagnostics.Utilities;
using SidebarDiagnostics.Monitoring;
using SidebarDiagnostics.Windows;
using SidebarDiagnostics.Framework;

namespace SidebarDiagnostics.Models
{
    public class SettingsModel : INotifyPropertyChanged
    {
        public SettingsModel(Sidebar sidebar)
        {
            DockEdgeItems = new DockEdge[2] { DockEdge.Left, DockEdge.Right };
            DockEdge = Framework.Settings.Default.DockEdge;

            Monitor[] _monitors = Monitor.GetMonitors();

            ScreenItems = _monitors.Select((s, i) => new ScreenItem() { Index = i, Text = string.Format("#{0}", i + 1) }).ToArray();

            if (Framework.Settings.Default.ScreenIndex < _monitors.Length)
            {
                ScreenIndex = Framework.Settings.Default.ScreenIndex;
            }
            else
            {
                ScreenIndex = _monitors.Where(s => s.IsPrimary).Select((s, i) => i).Single();
            }

            UIScale = Framework.Settings.Default.UIScale;

            XOffset = Framework.Settings.Default.XOffset;

            YOffset = Framework.Settings.Default.YOffset;

            PollingInterval = Framework.Settings.Default.PollingInterval;

            UseAppBar = Framework.Settings.Default.UseAppBar;

            AlwaysTop = Framework.Settings.Default.AlwaysTop;

            HighDPISupport = Framework.Settings.Default.HighDPISupport;

            ClickThrough = Framework.Settings.Default.ClickThrough;

            ShowTrayIcon = Framework.Settings.Default.ShowTrayIcon;

            AutoUpdate = Framework.Settings.Default.AutoUpdate;

            RunAtStartup = Framework.Settings.Default.RunAtStartup;

            SidebarWidth = Framework.Settings.Default.SidebarWidth;

            BGColor = Framework.Settings.Default.BGColor;

            BGOpacity = Framework.Settings.Default.BGOpacity;

            FontSettingItems = new FontSetting[5]
            {
                FontSetting.x10,
                FontSetting.x12,
                FontSetting.x14,
                FontSetting.x16,
                FontSetting.x18
            };
            FontSetting = Framework.Settings.Default.FontSetting;

            FontColor = Framework.Settings.Default.FontColor;

            AlertFontColor = Framework.Settings.Default.AlertFontColor;

            DateSettingItems = new DateSetting[4]
            {
                DateSetting.Disabled,
                DateSetting.Short,
                DateSetting.Normal,
                DateSetting.Long
            };
            DateSetting = Framework.Settings.Default.DateSetting;

            CollapseMenuBar = Framework.Settings.Default.CollapseMenuBar;

            ShowClock = Framework.Settings.Default.ShowClock;

            Clock24HR = Framework.Settings.Default.Clock24HR;
            
            if (sidebar.Ready)
            {
                foreach (MonitorConfig _record in Framework.Settings.Default.MonitorConfig)
                {
                    _record.Hardware = (
                        from hw in sidebar.Model.MonitorManager.GetHardware(_record.Type)
                        join config in _record.Hardware on hw.ID equals config.ID into c
                        from config in c.DefaultIfEmpty(hw)
                        select config
                        ).ToArray();
                }
            }

            MonitorConfig = Framework.Settings.Default.MonitorConfig.Select(c => c.Clone()).ToArray();

            if (Framework.Settings.Default.Hotkeys != null)
            {
                ToggleKey = Framework.Settings.Default.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Toggle);

                ShowKey = Framework.Settings.Default.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Show);

                HideKey = Framework.Settings.Default.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Hide);

                ReloadKey = Framework.Settings.Default.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Reload);

                CloseKey = Framework.Settings.Default.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Close);
            }

            IsChanged = false;
        }

        public void Save()
        {
            Framework.Settings.Default.DockEdge = DockEdge;
            Framework.Settings.Default.ScreenIndex = ScreenIndex;
            Framework.Settings.Default.UIScale = UIScale;
            Framework.Settings.Default.XOffset = XOffset;
            Framework.Settings.Default.YOffset = YOffset;
            Framework.Settings.Default.PollingInterval = PollingInterval;
            Framework.Settings.Default.UseAppBar = UseAppBar;
            Framework.Settings.Default.AlwaysTop = AlwaysTop;
            Framework.Settings.Default.HighDPISupport = HighDPISupport;
            Framework.Settings.Default.ClickThrough = ClickThrough;
            Framework.Settings.Default.ShowTrayIcon = ShowTrayIcon;
            Framework.Settings.Default.AutoUpdate = AutoUpdate;
            Framework.Settings.Default.RunAtStartup = RunAtStartup;
            Framework.Settings.Default.SidebarWidth = SidebarWidth;
            Framework.Settings.Default.BGColor = BGColor;
            Framework.Settings.Default.BGOpacity = BGOpacity;
            Framework.Settings.Default.FontSetting = FontSetting;
            Framework.Settings.Default.FontColor = FontColor;
            Framework.Settings.Default.AlertFontColor = AlertFontColor;
            Framework.Settings.Default.DateSetting = DateSetting;
            Framework.Settings.Default.CollapseMenuBar = CollapseMenuBar;
            Framework.Settings.Default.ShowClock = ShowClock;
            Framework.Settings.Default.Clock24HR = Clock24HR;
            Framework.Settings.Default.MonitorConfig = MonitorConfig;

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

            Framework.Settings.Default.Hotkeys = _hotkeys.ToArray();

            Framework.Settings.Default.Save();

            App.RefreshIcon();

            if (RunAtStartup)
            {
                Startup.EnableStartupTask();
            }
            else
            {
                Startup.DisableStartupTask();
            }

            IsChanged = false;
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler _handler = PropertyChanged;

            if (_handler == null)
            {
                return;
            }

            _handler(this, new PropertyChangedEventArgs(propertyName));

            if (propertyName != "IsChanged")
            {
                IsChanged = true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Child_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IsChanged = true;
        }

        private bool _isChanged { get; set; } = false;

        public bool IsChanged
        {
            get
            {
                return _isChanged;
            }
            set
            {
                _isChanged = value;

                NotifyPropertyChanged("IsChanged");
            }
        }

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

        private double _uiScale { get; set; }

        public double UIScale
        {
            get
            {
                return _uiScale;
            }
            set
            {
                _uiScale = value;

                NotifyPropertyChanged("UIScale");
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

        private bool _autoUpdate { get; set; }

        public bool AutoUpdate
        {
            get
            {
                return _autoUpdate;
            }
            set
            {
                _autoUpdate = value;

                NotifyPropertyChanged("AutoUpdate");
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
                return _monitorConfig;
            }
            set
            {
                _monitorConfig = value;

                foreach (MonitorConfig _config in _monitorConfig)
                {
                    _config.PropertyChanged += Child_PropertyChanged;

                    foreach (HardwareConfig _hardware in _config.Hardware)
                    {
                        _hardware.PropertyChanged += Child_PropertyChanged;
                    }

                    foreach (ConfigParam _param in _config.Params)
                    {
                        _param.PropertyChanged += Child_PropertyChanged;
                    }
                }

                NotifyPropertyChanged("MonitorConfig");
            }
        }

        public MonitorConfig[] MonitorConfigSorted
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
