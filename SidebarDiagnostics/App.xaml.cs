using System;
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

            // UPDATE
            #if !DEBUG
            await SquirrelUpdate(false);
            #endif

            // ERROR HANDLING
            #if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomain_Error);
            #endif
            
            // SETTINGS
            CheckSettings();

            // TRAY ICON
            _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
            _trayIcon.ToolTipText = string.Format("{0} v{1}", Constants.Generic.PROGRAMNAME, Assembly.GetExecutingAssembly().GetName().Version.ToString(3));

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
            _trayIcon.Dispose();

            base.OnExit(e);
        }

        public static void StartApp(bool openSettings)
        {
            new Sidebar(openSettings).Show();

            RefreshIcon();
        }

        public static void RefreshIcon()
        {
            _trayIcon.Visibility = Framework.Settings.Instance.ShowTrayIcon ? Visibility.Visible : Visibility.Collapsed;
        }

        public static void ShowPerformanceCounterError()
        {
            MessageBoxResult _result = MessageBox.Show(Constants.Generic.PERFORMANCECOUNTERERROR, Constants.Generic.ERRORTITLE, MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);

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

            Sidebar _sidebar = GetSidebar;

            if (_sidebar == null)
            {
                return;
            }

            new Settings(_sidebar);
        }

        private async Task SquirrelUpdate(bool showInfo)
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

                        string _newExePath = Utilities.Paths.Exe(_newVersion);

                        if (Framework.Settings.Instance.RunAtStartup)
                        {
                            Utilities.Startup.EnableStartupTask(_newExePath);
                        }

                        Process.Start(_newExePath);

                        Shutdown();
                    }
                    else if (showInfo)
                    {
                        MessageBox.Show(Constants.Generic.UPDATEMSG, Constants.Generic.PROGRAMNAME, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    }
                }
            }
            catch (WebException)
            {
                if (showInfo)
                {
                    MessageBox.Show(Constants.Generic.UPDATEERROR, Constants.Generic.UPDATEERRORTITLE, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }
            }
        }

        private void CheckSettings()
        {
            if (Framework.Settings.Instance.RunAtStartup && !Utilities.Startup.StartupTaskExists())
            {
                Utilities.Startup.EnableStartupTask();
            }

            Framework.Settings.Instance.MonitorConfig = MonitorConfig.CheckConfig(Framework.Settings.Instance.MonitorConfig);
        }
        
        private void Settings_Click(object sender, EventArgs e)
        {
            OpenSettings();
        }

        private void Reload_Click(object sender, EventArgs e)
        {
            Sidebar _sidebar = GetSidebar;

            if (_sidebar == null)
            {
                return;
            }

            _sidebar.Reload();
        }

        private void Visibility_SubmenuOpened(object sender, EventArgs e)
        {
            Sidebar _sidebar = GetSidebar;

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
            Sidebar _sidebar = GetSidebar;

            if (_sidebar == null || _sidebar.Visibility == Visibility.Visible)
            {
                return;
            }

            _sidebar.AppBarShow();
        }

        private void Hide_Click(object sender, EventArgs e)
        {
            Sidebar _sidebar = GetSidebar;

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

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            await SquirrelUpdate(true);
        }

        private void Close_Click(object sender, EventArgs e)
        {
            Shutdown();
        }

        #if !DEBUG
        private static void AppDomain_Error(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;

            MessageBox.Show(ex.ToString(), Constants.Generic.ERRORTITLE, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }
        #endif
        
        public Sidebar GetSidebar
        {
            get
            {
                return Windows.OfType<Sidebar>().FirstOrDefault();
            }
        }

        internal static bool _reloading { get; set; } = false;

        private static TaskbarIcon _trayIcon { get; set; }
    }
}