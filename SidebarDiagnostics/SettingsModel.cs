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
            DockEdge = Framework.Settings.Instance.DockEdge;

            Monitor[] _monitors = Monitor.GetMonitors();

            ScreenItems = _monitors.Select((s, i) => new ScreenItem() { Index = i, Text = string.Format("#{0}", i + 1) }).ToArray();

            if (Framework.Settings.Instance.ScreenIndex < _monitors.Length)
            {
                ScreenIndex = Framework.Settings.Instance.ScreenIndex;
            }
            else
            {
                ScreenIndex = _monitors.Where(s => s.IsPrimary).Select((s, i) => i).Single();
            }

            UIScale = Framework.Settings.Instance.UIScale;
            XOffset = Framework.Settings.Instance.XOffset;
            YOffset = Framework.Settings.Instance.YOffset;
            PollingInterval = Framework.Settings.Instance.PollingInterval;
            UseAppBar = Framework.Settings.Instance.UseAppBar;
            AlwaysTop = Framework.Settings.Instance.AlwaysTop;
            HighDPISupport = Framework.Settings.Instance.HighDPISupport;
            ClickThrough = Framework.Settings.Instance.ClickThrough;
            ShowTrayIcon = Framework.Settings.Instance.ShowTrayIcon;
            AutoUpdate = Framework.Settings.Instance.AutoUpdate;
            RunAtStartup = Framework.Settings.Instance.RunAtStartup;
            SidebarWidth = Framework.Settings.Instance.SidebarWidth;
            AutoBGColor = Framework.Settings.Instance.AutoBGColor;
            BGColor = Framework.Settings.Instance.BGColor;
            BGOpacity = Framework.Settings.Instance.BGOpacity;

            FontSettingItems = new FontSetting[5]
            {
                FontSetting.x10,
                FontSetting.x12,
                FontSetting.x14,
                FontSetting.x16,
                FontSetting.x18
            };

            FontSetting = Framework.Settings.Instance.FontSetting;
            FontColor = Framework.Settings.Instance.FontColor;
            AlertFontColor = Framework.Settings.Instance.AlertFontColor;

            DateSettingItems = new DateSetting[4]
            {
                DateSetting.Disabled,
                DateSetting.Short,
                DateSetting.Normal,
                DateSetting.Long
            };

            DateSetting = Framework.Settings.Instance.DateSetting;
            CollapseMenuBar = Framework.Settings.Instance.CollapseMenuBar;
            ShowClock = Framework.Settings.Instance.ShowClock;
            Clock24HR = Framework.Settings.Instance.Clock24HR;
            
            if (sidebar.Ready)
            {
                foreach (MonitorConfig _record in Framework.Settings.Instance.MonitorConfig)
                {
                    _record.Hardware = (
                        from hw in sidebar.Model.MonitorManager.GetHardware(_record.Type)
                        join config in _record.Hardware on hw.ID equals config.ID into c
                        from config in c.DefaultIfEmpty(hw)
                        select config
                        ).ToArray();
                }
            }

            MonitorConfig = Framework.Settings.Instance.MonitorConfig.Select(c => c.Clone()).ToArray();

            if (Framework.Settings.Instance.Hotkeys != null)
            {
                ToggleKey = Framework.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Toggle);
                ShowKey = Framework.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Show);
                HideKey = Framework.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Hide);
                ReloadKey = Framework.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Reload);
                CloseKey = Framework.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Close);
                CycleEdgeKey = Framework.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.CycleEdge);
                CycleScreenKey = Framework.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.CycleScreen);
            }

            IsChanged = false;
        }

        public void Save()
        {
            Framework.Settings.Instance.DockEdge = DockEdge;
            Framework.Settings.Instance.ScreenIndex = ScreenIndex;
            Framework.Settings.Instance.UIScale = UIScale;
            Framework.Settings.Instance.XOffset = XOffset;
            Framework.Settings.Instance.YOffset = YOffset;
            Framework.Settings.Instance.PollingInterval = PollingInterval;
            Framework.Settings.Instance.UseAppBar = UseAppBar;
            Framework.Settings.Instance.AlwaysTop = AlwaysTop;
            Framework.Settings.Instance.HighDPISupport = HighDPISupport;
            Framework.Settings.Instance.ClickThrough = ClickThrough;
            Framework.Settings.Instance.ShowTrayIcon = ShowTrayIcon;
            Framework.Settings.Instance.AutoUpdate = AutoUpdate;
            Framework.Settings.Instance.RunAtStartup = RunAtStartup;
            Framework.Settings.Instance.SidebarWidth = SidebarWidth;
            Framework.Settings.Instance.AutoBGColor = AutoBGColor;
            Framework.Settings.Instance.BGColor = BGColor;
            Framework.Settings.Instance.BGOpacity = BGOpacity;
            Framework.Settings.Instance.FontSetting = FontSetting;
            Framework.Settings.Instance.FontColor = FontColor;
            Framework.Settings.Instance.AlertFontColor = AlertFontColor;
            Framework.Settings.Instance.DateSetting = DateSetting;
            Framework.Settings.Instance.CollapseMenuBar = CollapseMenuBar;
            Framework.Settings.Instance.ShowClock = ShowClock;
            Framework.Settings.Instance.Clock24HR = Clock24HR;
            Framework.Settings.Instance.MonitorConfig = MonitorConfig;

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

            if (CycleEdgeKey != null)
            {
                _hotkeys.Add(CycleEdgeKey);
            }

            if (CycleScreenKey != null)
            {
                _hotkeys.Add(CycleScreenKey);
            }

            Framework.Settings.Instance.Hotkeys = _hotkeys.ToArray();

            Framework.Settings.Instance.Save();

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
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

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

        private bool _autoBGColor { get; set; }

        public bool AutoBGColor
        {
            get
            {
                return _autoBGColor;
            }
            set
            {
                _autoBGColor = value;

                NotifyPropertyChanged("AutoBGColor");
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

        private Hotkey _cycleEdgeKey { get; set; }

        public Hotkey CycleEdgeKey
        {
            get
            {
                return _cycleEdgeKey;
            }
            set
            {
                _cycleEdgeKey = value;

                NotifyPropertyChanged("CycleEdgeKey");
            }
        }

        private Hotkey _cycleScreenKey { get; set; }

        public Hotkey CycleScreenKey
        {
            get
            {
                return _cycleScreenKey;
            }
            set
            {
                _cycleScreenKey = value;

                NotifyPropertyChanged("CycleScreenKey");
            }
        }
    }

    public class ScreenItem
    {
        public int Index { get; set; }
        public string Text { get; set; }
    }
}
