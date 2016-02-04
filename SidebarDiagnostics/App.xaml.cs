using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using OpenHardwareMonitor.Hardware;
using Hardcodet.Wpf.TaskbarNotification;
using SidebarDiagnostics.Updates;
using SidebarDiagnostics.Monitor;

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
            _trayIcon.ToolTipText = Assembly.GetExecutingAssembly().GetName().Name;

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
            new AppBar(openSettings).Show();

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

        private void CheckSettings()
        {
            bool _save = false;

            if (SidebarDiagnostics.Properties.Settings.Default.UpgradeRequired)
            {
                SidebarDiagnostics.Properties.Settings.Default.Upgrade();
                SidebarDiagnostics.Properties.Settings.Default.UpgradeRequired = false;

                _save = true;
            }

            MonitorConfig[] _new = null;

            if (!MonitorConfig.CheckConfig(SidebarDiagnostics.Properties.Settings.Default.MonitorConfig, ref _new))
            {
                SidebarDiagnostics.Properties.Settings.Default.MonitorConfig = _new;

                _save = true;
            }

            if (_save)
            {
                SidebarDiagnostics.Properties.Settings.Default.Save();
            }
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            AppBar _appBar = GetAppBar;

            if (_appBar == null)
            {
                return;
            }

            new Settings(_appBar);
        }

        private void Reload_Click(object sender, EventArgs e)
        {
            AppBar _appBar = GetAppBar;

            if (_appBar == null)
            {
                return;
            }

            _appBar.Reload();
        }

        private void Visibility_SubmenuOpened(object sender, EventArgs e)
        {
            AppBar _appBar = GetAppBar;

            if (_appBar == null)
            {
                return;
            }

            MenuItem _this = (MenuItem)sender;

            (_this.Items.GetItemAt(0) as MenuItem).IsChecked = _appBar.Visibility == Visibility.Visible;
            (_this.Items.GetItemAt(1) as MenuItem).IsChecked = _appBar.Visibility == Visibility.Hidden;
        }
        
        private void Show_Click(object sender, EventArgs e)
        {
            AppBar _appBar = GetAppBar;

            if (_appBar == null || _appBar.Visibility == Visibility.Visible)
            {
                return;
            }

            _appBar.AppBarShow();
        }

        private void Hide_Click(object sender, EventArgs e)
        {
            AppBar _appBar = GetAppBar;

            if (_appBar == null || _appBar.Visibility == Visibility.Hidden)
            {
                return;
            }

            _appBar.AppBarHide();
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

        private void PART_CLOSE_Click(object sender, RoutedEventArgs e)
        {
            Button _button = (Button)sender;

            if (_button != null)
            {
                Window _window = Window.GetWindow(_button);

                if (_window != null && _window.IsInitialized)
                {
                    _window.Close();
                }
            }
        }

        #if !DEBUG
        private static void AppDomain_Error(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;

            MessageBox.Show(ex.ToString(), Constants.Generic.ERRORTITLE, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }
        #endif
        
        public AppBar GetAppBar
        {
            get
            {
                return Windows.OfType<AppBar>().FirstOrDefault();
            }
        }

        internal static bool _reloading { get; set; } = false;

        private static TaskbarIcon _trayIcon { get; set; }
    }
}