using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using OpenHardwareMonitor.Hardware;
using Hardcodet.Wpf.TaskbarNotification;
using SidebarDiagnostics.Updates;

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

            // CONFIG
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            // TRAY ICON
            TrayIcon = (TaskbarIcon)FindResource("TrayIcon");
            TrayIcon.ToolTipText = Assembly.GetExecutingAssembly().GetName().Name;
            TrayIcon.Visibility = Visibility.Visible;

            // OHM COMPUTER
            _computer = new Computer()
            {
                CPUEnabled = true,
                FanControllerEnabled = true,
                GPUEnabled = true,
                HDDEnabled = false,
                MainboardEnabled = true,
                RAMEnabled = true
            };

            _computer.Open();

            // CHECK FOR UPDATES
            if (SidebarDiagnostics.Properties.Settings.Default.CheckForUpdates)
            {
                await UpdateManager.Check(false);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _computer.Close();
            TrayIcon.Dispose();

            base.OnExit(e);
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            AppBar _appBar = GetAppBar;

            if (_appBar == null)
                return;

            Settings _settings = new Settings();
            _settings.Owner = _appBar;
            _settings.ShowDialog();
        }

        private void Reload_Click(object sender, EventArgs e)
        {
            AppBar _appBar = GetAppBar;

            if (_appBar == null)
                return;

            _appBar.Reload();
        }

        private void Visibility_SubmenuOpened(object sender, EventArgs e)
        {
            AppBar _appBar = GetAppBar;

            if (_appBar == null)
                return;

            MenuItem _this = (MenuItem)sender;

            (_this.Items.GetItemAt(0) as MenuItem).IsChecked = _appBar.Shown;
            (_this.Items.GetItemAt(1) as MenuItem).IsChecked = !_appBar.Shown;
        }
        
        private void Show_Click(object sender, EventArgs e)
        {
            AppBar _appBar = GetAppBar;

            if (_appBar == null || _appBar.Shown)
                return;

            _appBar.ABShow();
        }

        private void Hide_Click(object sender, EventArgs e)
        {
            AppBar _appBar = GetAppBar;

            if (_appBar == null || !_appBar.Shown)
                return;

            _appBar.ABHide();
        }

        private async void Update_Click(object sender, EventArgs e)
        {
            await UpdateManager.Check(true);
        }

        private void Close_Click(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private static AppBar GetAppBar
        {
            get
            {
                return Application.Current.Windows.OfType<AppBar>().FirstOrDefault();
            }
        }

        private static TaskbarIcon TrayIcon { get; set; }

        internal static Computer _computer { get; set; }
    }
}