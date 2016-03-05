using System;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Win32.TaskScheduler;
using SidebarDiagnostics.Framework;

namespace SidebarDiagnostics.Utilities
{
    public static class Paths
    {
        private const string SETTINGS = "settings.json";
        private const string CHANGELOG = "ChangeLog.json";

        public static string Install(Version version)
        {
            return Path.Combine(LocalApp, string.Format("app-{0}", version.ToString(3)));
        }

        public static string Exe(Version version)
        {
            return Path.Combine(Install(version), ExeName);
        }

        public static string ChangeLog
        {
            get
            {
                return Path.Combine(CurrentDirectory, CHANGELOG);
            }
        }

        public static string CurrentDirectory
        {
            get
            {
                return Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            }
        }

        public static string TaskBar
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
            }
        }

        private static string _assemblyName { get; set; } = null;

        public static string AssemblyName
        {
            get
            {
                if (_assemblyName == null)
                {
                    _assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                }

                return _assemblyName;
            }
        }

        private static string _exeName { get; set; } = null;

        public static string ExeName
        {
            get
            {
                if (_exeName == null)
                {
                    _exeName = string.Format("{0}.exe", AssemblyName);
                }

                return _exeName;
            }
        }

        private static string _settingsFile { get; set; } = null;

        public static string SettingsFile
        {
            get
            {
                if (_settingsFile == null)
                {
                    _settingsFile = Path.Combine(LocalApp, SETTINGS);
                }

                return _settingsFile;
            }
        }

        private static string _localApp { get; set; } = null;

        public static string LocalApp
        {
            get
            {
                if (_localApp == null)
                {
                    _localApp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AssemblyName);
                }

                return _localApp;
            }
        }
    }

    public static class Startup
    {        
        public static bool StartupTaskExists()
        {
            using (TaskService _taskService = new TaskService())
            {
                Task _task = _taskService.FindTask(Constants.Generic.TASKNAME);

                if (_task == null)
                {
                    return false;
                }

                ExecAction _action = _task.Definition.Actions.OfType<ExecAction>().FirstOrDefault();

                if (_action == null || _action.Path != Assembly.GetExecutingAssembly().Location)
                {
                    return false;
                }

                return true;
            }
        }

        public static void EnableStartupTask(string exePath = null)
        {
            using (TaskService _taskService = new TaskService())
            {
                TaskDefinition _def = _taskService.NewTask();
                _def.Triggers.Add(new LogonTrigger() { Enabled = true });
                _def.Actions.Add(new ExecAction(exePath ?? Assembly.GetExecutingAssembly().Location));
                _def.Principal.RunLevel = TaskRunLevel.Highest;

                _taskService.RootFolder.RegisterTaskDefinition(Constants.Generic.TASKNAME, _def);
            }
        }

        public static void DisableStartupTask()
        {
            using (TaskService _taskService = new TaskService())
            {
                _taskService.RootFolder.DeleteTask(Constants.Generic.TASKNAME, false);
            }
        }
    }

    public static class Culture
    {
        public const string DEFAULT = "Default";

        public static void SetDefault()
        {
            Default = Thread.CurrentThread.CurrentUICulture;
        }

        public static void SetCurrent(bool init)
        {
            SetCurrent(Framework.Settings.Instance.Culture, init);
        }

        public static void SetCurrent(string name, bool init)
        {
            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = string.Equals(name, DEFAULT, StringComparison.Ordinal) ? Default : new CultureInfo(name);

            if (init)
            {
                FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.Name)));
            }
        }

        public static CultureItem[] GetAll()
        {
            return new CultureItem[1] { new CultureItem() { Value = DEFAULT, Text = Resources.SettingsLanguageDefault } }.Concat(CultureInfo.GetCultures(CultureTypes.SpecificCultures).Where(c => Languages.Contains(c.TwoLetterISOLanguageName)).OrderBy(c => c.DisplayName).Select(c => new CultureItem() { Value = c.Name, Text = c.DisplayName })).ToArray();
        }

        public static string[] Languages
        {
            get
            {
                return new string[7] { "en", "da", "de", "fr", "ja", "nl", "zh" };
            }
        }

        public static CultureInfo Default { get; private set; }
    }

    public class CultureItem
    {
        public string Value { get; set; }

        public string Text { get; set; }
    }
}
