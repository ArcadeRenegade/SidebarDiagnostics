using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using OpenHardwareMonitor.Hardware;
using SidebarDiagnostics.Updates;

namespace SidebarDiagnostics
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // CONFIG
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // NOTIFY ICON
            var _settings = new System.Windows.Forms.MenuItem() { Text = "Settings" };
            _settings.Click += SettingsMenuItem_Click;

            var _update = new System.Windows.Forms.MenuItem() { Text = "Update" };
            _update.Click += UpdateMenuItem_Click;

            var _close = new System.Windows.Forms.MenuItem() { Text = "Close" };
            _close.Click += CloseMenuItem_Click;

            ShowMenuItem = new System.Windows.Forms.MenuItem() { Text = "Show" };
            ShowMenuItem.Click += ShowMenuItem_Click;
            ShowMenuItem.Checked = true;

            HideMenuItem = new System.Windows.Forms.MenuItem() { Text = "Hide" };
            HideMenuItem.Click += HideMenuItem_Click;

            var _visiblity = new System.Windows.Forms.MenuItem() { Text = "Visibility" };
            _visiblity.MenuItems.Add(ShowMenuItem);
            _visiblity.MenuItems.Add(HideMenuItem);
            _visiblity.Popup += VisibilityMenuItem_Popup;

            var _contextMenu = new System.Windows.Forms.ContextMenu();
            _contextMenu.MenuItems.Add(_settings);
            _contextMenu.MenuItems.Add(_visiblity);
            _contextMenu.MenuItems.Add(_update);
            _contextMenu.MenuItems.Add(_close);

            TrayIcon = new System.Windows.Forms.NotifyIcon()
            {
                Icon = SidebarDiagnostics.Properties.Resources.TrayIcon,
                Text = Assembly.GetExecutingAssembly().GetName().Name,
                ContextMenu = _contextMenu
            };
            TrayIcon.Click += TrayIcon_Click;
            TrayIcon.Visible = true;

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
                UpdateManager.Check(false);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _computer.Close();
            TrayIcon.Dispose();

            base.OnExit(e);
        }

        private void SettingsMenuItem_Click(object sender, EventArgs e)
        {
            AppBar _appBar = GetAppBar;

            if (_appBar == null)
                return;

            Settings _settings = new Settings();
            _settings.Owner = _appBar;
            _settings.ShowDialog();
        }

        private void UpdateMenuItem_Click(object sender, EventArgs e)
        {
            UpdateManager.Check(true);
        }

        private void CloseMenuItem_Click(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void VisibilityMenuItem_Popup(object sender, EventArgs e)
        {
            AppBar _appBar = GetAppBar;

            if (_appBar == null)
                return;

            ShowMenuItem.Checked = _appBar.Shown;
            HideMenuItem.Checked = !_appBar.Shown;
        }

        private void ShowMenuItem_Click(object sender, EventArgs e)
        {
            AppBar _appBar = GetAppBar;

            if (_appBar == null || _appBar.Shown)
                return;

            _appBar.ABShow();
        }

        private void HideMenuItem_Click(object sender, EventArgs e)
        {
            AppBar _appBar = GetAppBar;

            if (_appBar == null || !_appBar.Shown)
                return;

            _appBar.ABHide();
        }

        private void TrayIcon_Click(object sender, EventArgs e)
        {
            AppBar _appBar = GetAppBar;

            if (_appBar == null)
                return;

            _appBar.Activate();
        }

        private static AppBar GetAppBar
        {
            get
            {
                return Application.Current.Windows.OfType<AppBar>().FirstOrDefault();
            }
        }

        private static System.Windows.Forms.NotifyIcon TrayIcon { get; set; }

        private static System.Windows.Forms.MenuItem ShowMenuItem { get; set; }

        private static System.Windows.Forms.MenuItem HideMenuItem { get; set; }

        internal static Computer _computer { get; set; }
    }
}