using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Squirrel;
using Hardcodet.Wpf.TaskbarNotification;
using SidebarDiagnostics.Monitoring;
using SidebarDiagnostics.Utilities;
using SidebarDiagnostics.Windows;

namespace SidebarDiagnostics
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected async override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ERROR HANDLING
            #if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomain_Error);
            #endif

            // LANGUAGE
            Culture.SetDefault();
            Culture.SetCurrent(true);

            // UPDATE
            #if !DEBUG
            if (Framework.Settings.Instance.AutoUpdate)
            {
                await AppUpdate(false);
            }
            #endif

            // SETTINGS
            CheckSettings();

            // VERSION
            Version _version = Assembly.GetExecutingAssembly().GetName().Version;
            string _vstring = _version.ToString(3);

            // TRAY ICON
            TrayIcon = (TaskbarIcon)FindResource("TrayIcon");
            TrayIcon.ToolTipText = string.Format("{0} v{1}", Framework.Resources.AppName, _vstring);
            TrayIcon.TrayContextMenuOpen += TrayIcon_TrayContextMenuOpen;

            // START APP
            if (Framework.Settings.Instance.InitialSetup)
            {
                new Setup();
            }
            else
            {
                StartApp(false);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            TrayIcon.Dispose();

            base.OnExit(e);
        }

        public static void StartApp(bool openSettings)
        {
            Version _version = Assembly.GetExecutingAssembly().GetName().Version;
            string _vstring = _version.ToString(3);

            if (!string.Equals(Framework.Settings.Instance.ChangeLog, _vstring, StringComparison.OrdinalIgnoreCase))
            {
                Framework.Settings.Instance.ChangeLog = _vstring;
                Framework.Settings.Instance.Save();

                new ChangeLog(_version).Show();
            }

            new Sidebar(openSettings, Framework.Settings.Instance.InitiallyHidden).Show();

            RefreshIcon();
        }

        public static void RefreshIcon()
        {
            TrayIcon.Visibility = Framework.Settings.Instance.ShowTrayIcon ? Visibility.Visible : Visibility.Collapsed;
        }

        public static void ShowPerformanceCounterError()
        {
            MessageBoxResult _result = MessageBox.Show(Framework.Resources.ErrorPerformanceCounter, Framework.Resources.ErrorTitle, MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);

            if (_result == MessageBoxResult.OK)
            {
                Process.Start(ConfigurationManager.AppSettings["WikiURL"]);
            }
        }

        public void OpenSettings()
        {
            Settings _settings = Windows.OfType<Settings>().FirstOrDefault();

            if (_settings != null)
            {
                _settings.WindowState = WindowState.Normal;
                _settings.Activate();
                return;
            }

            Sidebar _sidebar = Sidebar;

            if (_sidebar == null)
            {
                return;
            }

            new Settings(_sidebar);
        }

        public void OpenGraph()
        {
            Sidebar _sidebar = Sidebar;

            if (_sidebar == null || !_sidebar.Ready)
            {
                return;
            }

            new Graph(_sidebar);
        }

        private async Task AppUpdate(bool showInfo)
        {
            string _exe = await SquirrelUpdate(showInfo);

            if (_exe != null)
            {
                if (Framework.Settings.Instance.RunAtStartup)
                {
                    Utilities.Startup.EnableStartupTask(_exe);
                }

                Process.Start(_exe);

                Shutdown();
            }
        }

        private async Task<string> SquirrelUpdate(bool showInfo)
        {
            try
            {
                using (UpdateManager _manager = new UpdateManager(ConfigurationManager.AppSettings["CurrentReleaseURL"]))
                {
                    UpdateInfo _update = await _manager.CheckForUpdate();

                    if (_update.ReleasesToApply.Any())
                    {
                        Version _newVersion = _update.ReleasesToApply.OrderByDescending(r => r.Version).First().Version.Version;

                        Update _updateWindow = new Update();
                        _updateWindow.Show();

                        await _manager.UpdateApp((p) => _updateWindow.SetProgress(p));

                        _updateWindow.Close();

                        return Utilities.Paths.Exe(_newVersion);
                    }
                    else if (showInfo)
                    {
                        MessageBox.Show(Framework.Resources.UpdateSuccessText, Framework.Resources.AppName, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    }
                }
            }
            catch (WebException)
            {
                if (showInfo)
                {
                    MessageBox.Show(Framework.Resources.UpdateErrorText, Framework.Resources.UpdateErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }
            }
            catch (Exception e)
            {
                Framework.Settings.Instance.AutoUpdate = false;
                Framework.Settings.Instance.Save();

                using (EventLog _log = new EventLog("Application"))
                {
                    _log.Source = Framework.Resources.AppName;
                    _log.WriteEntry(e.ToString(), EventLogEntryType.Error, 100, 1);
                }

                MessageBox.Show(Framework.Resources.UpdateErrorFatalText, Framework.Resources.UpdateErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            }

            return null;
        }

        private void CheckSettings()
        {
            if (Framework.Settings.Instance.RunAtStartup && !Utilities.Startup.StartupTaskExists())
            {
                Utilities.Startup.EnableStartupTask();
            }

            Framework.Settings.Instance.MonitorConfig = MonitorConfig.CheckConfig(Framework.Settings.Instance.MonitorConfig);
        }

        private void TrayIcon_TrayContextMenuOpen(object sender, RoutedEventArgs e)
        {
            Monitor _primary = Monitor.GetMonitors().GetPrimary();

            TrayIcon.ContextMenu.HorizontalOffset *= _primary.InverseScaleX;
            TrayIcon.ContextMenu.VerticalOffset *= _primary.InverseScaleY;
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            OpenSettings();
        }

        private void Reload_Click(object sender, EventArgs e)
        {
            Sidebar _sidebar = Sidebar;

            if (_sidebar == null)
            {
                return;
            }

            _sidebar.Reload();
        }

        private void Graph_Click(object sender, EventArgs e)
        {
            OpenGraph();
        }

        private void Visibility_SubmenuOpened(object sender, EventArgs e)
        {
            Sidebar _sidebar = Sidebar;

            if (_sidebar == null)
            {
                return;
            }

            MenuItem _this = (MenuItem)sender;

            (_this.Items.GetItemAt(0) as MenuItem).IsChecked = _sidebar.Visibility == Visibility.Visible;
            (_this.Items.GetItemAt(1) as MenuItem).IsChecked = _sidebar.Visibility == Visibility.Hidden;
        }
        
        private void Show_Click(object sender, EventArgs e)
        {
            Sidebar _sidebar = Sidebar;

            if (_sidebar == null || _sidebar.Visibility == Visibility.Visible)
            {
                return;
            }

            _sidebar.AppBarShow();
        }

        private void Hide_Click(object sender, EventArgs e)
        {
            Sidebar _sidebar = Sidebar;

            if (_sidebar == null || _sidebar.Visibility == Visibility.Hidden)
            {
                return;
            }

            _sidebar.AppBarHide();
        }

        private void Donate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(ConfigurationManager.AppSettings["DonateURL"]);
        }

        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(ConfigurationManager.AppSettings["RepoURL"]);
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            await AppUpdate(true);
        }

        private void Close_Click(object sender, EventArgs e)
        {
            Shutdown();
        }
        
        private static void AppDomain_Error(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;

            MessageBox.Show(ex.ToString(), Framework.Resources.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }
        
        public Sidebar Sidebar
        {
            get
            {
                return Windows.OfType<Sidebar>().FirstOrDefault();
            }
        }

        public IEnumerable<Graph> Graphs
        {
            get
            {
                return Windows.OfType<Graph>();
            }
        }

        public new static App Current
        {
            get
            {
                return (App)Application.Current;
            }
        }

        public static TaskbarIcon TrayIcon { get; set; }

        internal static bool _reloading { get; set; } = false;
    }
}