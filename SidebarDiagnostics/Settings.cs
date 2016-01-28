using System;
using System.Configuration;
using System.ComponentModel;

namespace SidebarDiagnostics.Properties
{
    internal sealed partial class Settings
    {        
        public Settings() { }
        
        private void SettingChangingEventHandler(object sender, SettingChangingEventArgs e) { }
        
        private void SettingsSavingEventHandler(object sender, CancelEventArgs e) { }
    }

    [Serializable]
    public class FontSetting
    {
        public FontSetting() { }

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

        public int FontSize { get; set; }

        public int TitleFontSize
        {
            get
            {
                return FontSize + 2;
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
}
