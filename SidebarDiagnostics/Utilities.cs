using System.Windows.Forms;
using System.Reflection;
using Microsoft.Win32;

namespace SidebarDiagnostics
{
    public static class Utilities
    {
        public static Screen[] GetScreens()
        {
            return Screen.AllScreens;
        }

        public static int GetNoScreens()
        {
            return Screen.AllScreens.Length;
        }

        public static Screen GetScreenFromIndex(int index)
        {
            Screen[] _screens = Screen.AllScreens;

            if (index < _screens.Length)
                return _screens[index];
            else
                return Screen.PrimaryScreen;
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
}
