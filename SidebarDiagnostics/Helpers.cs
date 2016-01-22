using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SidebarDiagnostics.Helpers
{
    public static class Utilities
    {
        public static int GetScreenCount()
        {
            return Screen.AllScreens.Length;
        }

        public static Screen GetScreenFromIndex(int index)
        {
            Screen[] _screens = Screen.AllScreens.ToArray();

            if (index < _screens.Length)
                return _screens[index];
            else
                return _screens.Where(s => s.Primary).Single();
        }

        public static bool IsStartupEnabled()
        {
            using (RegistryKey _registryKey = GetRegistryKey(false))
            {
                return IsStartupEnabled(_registryKey);
            }
        }

        private static bool IsStartupEnabled(RegistryKey registryKey)
        {
            return registryKey.GetValue(_regKeyName) != null;
        }

        public static void SetStartupEnabled(bool enable)
        {
            using (RegistryKey _registryKey = GetRegistryKey(true))
            {
                bool _isStartupEnabled = IsStartupEnabled(_registryKey);

                if (enable)
                {
                    _registryKey.SetValue(_regKeyName, string.Format("\"{0}\"", Assembly.GetEntryAssembly().Location));
                }
                else if (!enable && _isStartupEnabled)
                {
                    _registryKey.DeleteValue(_regKeyName);
                }
            }
        }

        private static RegistryKey GetRegistryKey(bool writable)
        {
            return Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", writable);
        }

        private static readonly string _regKeyName = Assembly.GetExecutingAssembly().GetName().Name;
    }
     
    public class FontSetting
    {
        private FontSetting() { }

        public override bool Equals(object obj)
        {
            FontSetting _fontSetting = obj as FontSetting;

            if (_fontSetting == null)
            {
                return false;
            }

            return this.FontSize == _fontSetting.FontSize;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static readonly FontSetting x10 = new FontSetting() { IconSize = 18, TitleFontSize = 12, FontSize = 10 };
        public static readonly FontSetting x12 = new FontSetting() { IconSize = 22, TitleFontSize = 14, FontSize = 12 };
        public static readonly FontSetting x14 = new FontSetting() { IconSize = 24, TitleFontSize = 16, FontSize = 14 };
        public static readonly FontSetting x16 = new FontSetting() { IconSize = 28, TitleFontSize = 18, FontSize = 16 };
        public static readonly FontSetting x18 = new FontSetting() { IconSize = 32, TitleFontSize = 20, FontSize = 18 };

        public int IconSize { get; set; }
        public int TitleFontSize { get; set; }
        public int FontSize { get; set; }
    }
}
