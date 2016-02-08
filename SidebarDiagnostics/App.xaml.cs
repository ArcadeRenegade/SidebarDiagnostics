using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using SidebarDiagnostics.Updates;
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

            // ERROR HANDLING
            #if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomain_Error);
            #endif
            
            // SETTINGS
            CheckSettings();

            // TRAY ICON
            _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
            _trayIcon.ToolTipText = Constants.Generic.PROGRAMNAME;

            // CHECK FOR UPDATES
            if (SidebarDiagnostics.Properties.Settings.Default.CheckForUpdates)
            {
                await UpdateManager.Check(false);
            }

            // START APP
            if (SidebarDiagnostics.Properties.Settings.Default.InitialSetup)
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
            _trayIcon.Visibility = SidebarDiagnostics.Properties.Settings.Default.ShowTrayIcon ? Visibility.Visible : Visibility.Collapsed;
        }

        public static void ShowPerformanceCounterError()
        {
            MessageBoxResult _result = MessageBox.Show(Constants.Generic.PERFORMANCECOUNTERERROR, Constants.Generic.ERRORTITLE, MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);

            if (_result == MessageBoxResult.OK)
            {
                Process.Start(Constants.URL.WIKI);
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

        private void CheckSettings()
        {
            if (SidebarDiagnostics.Properties.Settings.Default.UpgradeRequired)
            {
                SidebarDiagnostics.Properties.Settings.Default.Upgrade();
                SidebarDiagnostics.Properties.Settings.Default.UpgradeRequired = false;
            }

            SidebarDiagnostics.Properties.Settings.Default.MonitorConfig = MonitorConfig.CheckConfig(SidebarDiagnostics.Properties.Settings.Default.MonitorConfig);

            SidebarDiagnostics.Properties.Settings.Default.Save();
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
            Process.Start(Constants.URL.DONATE);
        }

        private async void Update_Click(object sender, EventArgs e)
        {
            await UpdateManager.Check(true);
        }

        private void Close_Click(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
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