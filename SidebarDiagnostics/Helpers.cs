using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace SidebarDiagnostics.Helpers
{
    public static class Utilities
    {
        public static int GetNoOfMonitors()
        {
            return Monitor.AllMonitors.Count();
        }

        public static Monitor GetMonitorFromIndex(int index)
        {
            Monitor[] _monitors = Monitor.AllMonitors.ToArray();

            if (index < _monitors.Length)
                return _monitors[index];
            else
                return _monitors.Where(m => m.IsPrimary).Single();
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

    public class Monitor
    {
        #region Dll imports

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [ResourceExposure(ResourceScope.None)]
        private static extern bool GetMonitorInfo
                      (HandleRef hmonitor, [In, Out]MonitorInfoEx info);

        [DllImport("user32.dll", ExactSpelling = true)]
        [ResourceExposure(ResourceScope.None)]
        private static extern bool EnumDisplayMonitors
             (HandleRef hdc, IntPtr rcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        private delegate bool MonitorEnumProc
                     (IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        private class MonitorInfoEx
        {
            internal int cbSize = Marshal.SizeOf(typeof(MonitorInfoEx));
            internal Rect rcMonitor = new Rect();
            internal Rect rcWork = new Rect();
            internal int dwFlags = 0;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            internal char[] szDevice = new char[32];
        }

        private const int MonitorinfofPrimary = 0x00000001;

        #endregion

        public static HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

        public System.Windows.Rect Bounds { get; private set; }

        public System.Windows.Rect WorkingArea { get; private set; }

        public string Name { get; private set; }

        public bool IsPrimary { get; private set; }

        private Monitor(IntPtr monitor, IntPtr hdc)
        {
            var info = new MonitorInfoEx();

            GetMonitorInfo(new HandleRef(null, monitor), info);

            Bounds = new System.Windows.Rect(
                        info.rcMonitor.left, info.rcMonitor.top,
                        info.rcMonitor.right - info.rcMonitor.left,
                        info.rcMonitor.bottom - info.rcMonitor.top);

            WorkingArea = new System.Windows.Rect(
                        info.rcWork.left, info.rcWork.top,
                        info.rcWork.right - info.rcWork.left,
                        info.rcWork.bottom - info.rcWork.top);

            IsPrimary = ((info.dwFlags & MonitorinfofPrimary) != 0);

            Name = new string(info.szDevice).TrimEnd((char)0);
        }

        public static IEnumerable<Monitor> AllMonitors
        {
            get
            {
                var closure = new MonitorEnumCallback();
                var proc = new MonitorEnumProc(closure.Callback);
                EnumDisplayMonitors(NullHandleRef, IntPtr.Zero, proc, IntPtr.Zero);
                return closure.Monitors.Cast<Monitor>();
            }
        }

        private class MonitorEnumCallback
        {
            public ArrayList Monitors { get; private set; }

            public MonitorEnumCallback()
            {
                Monitors = new ArrayList();
            }

            public bool Callback(IntPtr monitor, IntPtr hdc,
                           IntPtr lprcMonitor, IntPtr lparam)
            {
                Monitors.Add(new Monitor(monitor, hdc));
                return true;
            }
        }
    }
}
