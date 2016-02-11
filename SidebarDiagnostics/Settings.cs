using System;
using System.IO;
using Newtonsoft.Json;
using SidebarDiagnostics.Utilities;
using SidebarDiagnostics.Monitoring;
using SidebarDiagnostics.Windows;

namespace SidebarDiagnostics.Framework
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Settings
    {
        private Settings() { }

        public void Save()
        {
            if (!Directory.Exists(Paths.Local))
            {
                Directory.CreateDirectory(Paths.Local);
            }

            using (StreamWriter _stream = File.CreateText(Paths.SettingsFile))
            {
                new JsonSerializer().Serialize(_stream, this);
            }
        }

        public void Reload()
        {
            _instance = Load();
        }

        private static Settings Load()
        {
            if (File.Exists(Paths.SettingsFile))
            {
                using (StreamReader _stream = File.OpenText(Paths.SettingsFile))
                {
                    return (new JsonSerializer().Deserialize(_stream, typeof(Settings)) as Settings) ?? new Settings();
                }
            }

            return new Settings();
        }

        [JsonProperty]
        public bool InitialSetup { get; set; } = true;

        [JsonProperty]
        public DockEdge DockEdge { get; set; } = DockEdge.Right;

        [JsonProperty]
        public int ScreenIndex { get; set; } = 0;

        [JsonProperty]
        public bool HighDPISupport { get; set; } = false;
        
        [JsonProperty]
        public bool UseAppBar { get; set; } = true;

        [JsonProperty]
        public bool AlwaysTop { get; set; } = true;

        [JsonProperty]
        public bool AutoUpdate { get; set; } = true;

        [JsonProperty]
        public bool RunAtStartup { get; set; } = true;

        [JsonProperty]
        public double UIScale { get; set; } = 1d;

        [JsonProperty]
        public int XOffset { get; set; } = 0;

        [JsonProperty]
        public int YOffset { get; set; } = 0;

        [JsonProperty]
        public int PollingInterval { get; set; } = 1000;

        [JsonProperty]
        public bool ClickThrough { get; set; } = false;

        [JsonProperty]
        public bool ShowTrayIcon { get; set; } = true;

        [JsonProperty]
        public bool CollapseMenuBar { get; set; } = false;

        [JsonProperty]
        public int SidebarWidth { get; set; } = 180;

        [JsonProperty]
        public string BGColor { get; set; } = "#000000";

        [JsonProperty]
        public double BGOpacity { get; set; } = 0.85d;

        [JsonProperty]
        public FontSetting FontSetting { get; set; } = FontSetting.x14;
        
        [JsonProperty]
        public string FontColor { get; set; } = "#FFFFFF";

        [JsonProperty]
        public string AlertFontColor { get; set; } = "#FF4136";

        [JsonProperty]
        public bool ShowClock { get; set; } = true;

        [JsonProperty]
        public bool Clock24HR { get; set; } = false;

        [JsonProperty]
        public DateSetting DateSetting { get; set; } = DateSetting.Short;

        [JsonProperty]
        public MonitorConfig[] MonitorConfig { get; set; } = null;

        [JsonProperty]
        public Hotkey[] Hotkeys { get; set; } = new Hotkey[0];

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
