using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32.TaskScheduler;

namespace SidebarDiagnostics.Utilities
{
    public static class Paths
    {
        public static string Local
        {
            get
            {
                return Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), Assembly.GetExecutingAssembly().GetName().Name);
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
