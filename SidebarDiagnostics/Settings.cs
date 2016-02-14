using System;
using System.IO;
using System.ComponentModel;
using Newtonsoft.Json;
using SidebarDiagnostics.Utilities;
using SidebarDiagnostics.Monitoring;
using SidebarDiagnostics.Windows;

namespace SidebarDiagnostics.Framework
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Settings : INotifyPropertyChanged
    {
        private Settings() { }

        public void Save()
        {
            if (!Directory.Exists(Paths.LocalApp))
            {
                Directory.CreateDirectory(Paths.LocalApp);
            }

            using (StreamWriter _writer = File.CreateText(Paths.SettingsFile))
            {
                new JsonSerializer().Serialize(_writer, this);
            }
        }

        public void Reload()
        {
            _instance = Load();
        }

        private static Settings Load()
        {
            Settings _return = null;

            if (File.Exists(Paths.SettingsFile))
            {
                using (StreamReader _reader = File.OpenText(Paths.SettingsFile))
                {
                    _return = (Settings)new JsonSerializer().Deserialize(_reader, typeof(Settings));
                }
            }

            return _return ?? new Settings();
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string _changeLog { get; set; } = null;

        [JsonProperty]
        public string ChangeLog
        {
            get
            {
                return _changeLog;
            }
            set
            {
                _changeLog = value;

                NotifyPropertyChanged("ChangeLog");
            }
        }

        private bool _initialSetup { get; set; } = true;

        [JsonProperty]
        public bool InitialSetup
        {
            get
            {
                return _initialSetup;
            }
            set
            {
                _initialSetup = value;

                NotifyPropertyChanged("InitialSetup");
            }
        }

        private DockEdge _dockEdge { get; set; } = DockEdge.Right;

        [JsonProperty]
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

        private int _screenIndex { get; set; } = 0;

        [JsonProperty]
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

        private bool _highDPISupport { get; set; } = false;

        [JsonProperty]
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

        private bool _useAppBar { get; set; } = true;
        
        [JsonProperty]
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

        private bool _alwaysTop { get; set; } = true;

        [JsonProperty]
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

        private bool _autoUpdate { get; set; } = true;

        [JsonProperty]
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

        private bool _runAtStartup { get; set; } = true;

        [JsonProperty]
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

        private double _uiScale { get; set; } = 1d;

        [JsonProperty]
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

        private int _xOffset { get; set; } = 0;

        [JsonProperty]
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

        private int _yOffset { get; set; } = 0;

        [JsonProperty]
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

        private int _pollingInterval { get; set; } = 1000;

        [JsonProperty]
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

        private bool _clickThrough { get; set; } = false;

        [JsonProperty]
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

        private bool _showTrayIcon { get; set; } = true;

        [JsonProperty]
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

        private bool _collapseMenuBar { get; set; } = false;

        [JsonProperty]
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

        private int _sidebarWidth { get; set; } = 180;

        [JsonProperty]
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

        private bool _autoBGColor { get; set; } = false;

        [JsonProperty]
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

        private string _bgColor { get; set; } = "#000000";

        [JsonProperty]
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

        private double _bgOpacity { get; set; } = 0.85d;

        [JsonProperty]
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

        private FontSetting _fontSetting { get; set; } = FontSetting.x14;

        [JsonProperty]
        public FontSetting FontSetting
        {
            get
            {
                return _fontSetting;
            }
            set
            {
                _fontSetting = value;

                NotifyPropertyChanged("FontSetting");
            }
        }

        private string _fontColor { get; set; } = "#FFFFFF";
        
        [JsonProperty]
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

        private string _alertFontColor { get; set; } = "#FF4136";

        [JsonProperty]
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

        private bool _showClock { get; set; } = true;

        [JsonProperty]
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

        private bool _clock24HR { get; set; } = false;

        [JsonProperty]
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

        private DateSetting _dateSetting { get; set; } = DateSetting.Short;

        [JsonProperty]
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

        private MonitorConfig[] _monitorConfig { get; set; } = null;

        [JsonProperty]
        public MonitorConfig[] MonitorConfig
        {
            get
            {
                return _monitorConfig;
            }
            set
            {
                _monitorConfig = value;

                NotifyPropertyChanged("MonitorConfig");
            }
        }

        private Hotkey[] _hotkeys { get; set; } = new Hotkey[0];

        [JsonProperty]
        public Hotkey[] Hotkeys
        {
            get
            {
                return _hotkeys;
            }
            set
            {
                _hotkeys = value;

                NotifyPropertyChanged("Hotkeys");
            }
        }

        private static Settings _instance { get; set; } = null;

        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }

                return _instance;
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class FontSetting
    {
        internal FontSetting() { }

        private FontSetting(int fontSize)
        {
            FontSize = fontSize;
        }

        public override bool Equals(object obj)
        {
            FontSetting _that = obj as FontSetting;

            if (_that == null)
            {
                return false;
            }

            return this.FontSize == _that.FontSize;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static readonly FontSetting x10 = new FontSetting(10);
        public static readonly FontSetting x12 = new FontSetting(12);
        public static readonly FontSetting x14 = new FontSetting(14);
        public static readonly FontSetting x16 = new FontSetting(16);
        public static readonly FontSetting x18 = new FontSetting(18);

        [JsonProperty]
        public int FontSize { get; set; }

        public int TitleFontSize
        {
            get
            {
                return FontSize + 2;
            }
        }

        public int SmallFontSize
        {
            get
            {
                return FontSize - 2;
            }
        }

        public int IconSize
        {
            get
            {
                switch (FontSize)
                {
                    case 10:
                        return 18;

                    case 12:
                        return 22;

                    case 14:
                    default:
                        return 24;

                    case 16:
                        return 28;

                    case 18:
                        return 32;
                }
            }
        }

        public int BarHeight
        {
            get
            {
                return FontSize - 3;
            }
        }

        public int BarWidth
        {
            get
            {
                return BarHeight * 6;
            }
        }

        public int BarWidthWide
        {
            get
            {
                return BarHeight * 8;
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class DateSetting
    {
        internal DateSetting() { }

        private DateSetting(string format)
        {
            Format = format;
        }

        [JsonProperty]
        public string Format { get; set; }

        public string Display
        {
            get
            {
                if (string.Equals(Format, "Disabled", StringComparison.Ordinal))
                {
                    return Format;
                }

                return DateTime.Today.ToString(Format);
            }
        }

        public override bool Equals(object obj)
        {
            DateSetting _that = obj as DateSetting;

            if (_that == null)
            {
                return false;
            }

            return string.Equals(this.Format, _that.Format, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static readonly DateSetting Disabled = new DateSetting("Disabled");
        public static readonly DateSetting Short = new DateSetting("M");
        public static readonly DateSetting Normal = new DateSetting("d");
        public static readonly DateSetting Long = new DateSetting("D");
    }
}
