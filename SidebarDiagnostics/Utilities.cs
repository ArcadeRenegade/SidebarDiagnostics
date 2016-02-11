using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32.TaskScheduler;

namespace SidebarDiagnostics.Utilities
{
    public static class Paths
    {
        private const string LOCALAPPDATA = "LocalAppData";
        private const string SETTINGS = "settings.json";

        private static string _local { get; set; } = null;

        public static string Local
        {
            get
            {
                if (_local == null)
                {
                    _local = Path.Combine(Environment.GetEnvironmentVariable(LOCALAPPDATA), Assembly.GetExecutingAssembly().GetName().Name);
                }

                return _local;
            }
        }

        private static string _settingsFile { get; set; } = null;

        public static string SettingsFile
        {
            get
            {
                if (_settingsFile == null)
                {
                    _settingsFile = Path.Combine(Local, SETTINGS);
                }

                return _settingsFile;
            }
        }
    }

    public static class Startup
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
}
