using System;
using System.Reflection;
using Microsoft.Win32.TaskScheduler;

namespace SidebarDiagnostics.Helpers
{
    public static class Utilities
    {        
        public static bool StartupTaskExists()
        {
            using (TaskService _taskService = new TaskService())
            {
                return _taskService.FindTask(_taskName) != null;
            }
        }

        public static void EnableStartupTask()
        {
            using (TaskService _taskService = new TaskService())
            {
                TaskDefinition _def = _taskService.NewTask();
                _def.Triggers.Add(new LogonTrigger() { Enabled = true });
                _def.Actions.Add(new ExecAction(Assembly.GetEntryAssembly().Location));
                _def.Principal.RunLevel = TaskRunLevel.Highest;

                _taskService.RootFolder.RegisterTaskDefinition(_taskName, _def);
            }
        }

        public static void DisableStartupTask()
        {
            using (TaskService _taskService = new TaskService())
            {
                _taskService.RootFolder.DeleteTask(_taskName, false);
            }
        }

        private const string _taskName = "SidebarStartup";
    }

    [Serializable]
    public class FontSetting
    {
        private FontSetting() { }

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
