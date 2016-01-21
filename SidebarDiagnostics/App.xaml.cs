using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;
using OpenHardwareMonitor.Hardware;

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

            // NOTIFY ICON
            MenuItem _settingsMenuItem = new MenuItem()
            {
                Header = "Settings"
            };
            _settingsMenuItem.Click += SettingsMenuItem_Click;

            MenuItem _showMenuItem = new MenuItem()
            {
                Header = "Show"
            };
            _showMenuItem.Click += ShowMenuItem_Click;

            MenuItem _closeMenuItem = new MenuItem()
            {
                Header = "Close"
            };
            _closeMenuItem.Click += CloseMenuItem_Click;

            ContextMenu _contextMenu = new ContextMenu();
            _contextMenu.Items.Add(_settingsMenuItem);
            _contextMenu.Items.Add(_showMenuItem);
            _contextMenu.Items.Add(_closeMenuItem);

            _taskbarIcon = new TaskbarIcon()
            {
                Icon = SidebarDiagnostics.Properties.Resources.TrayIcon,
                ToolTipText = Assembly.GetExecutingAssembly().GetName().Name,
                ContextMenu = _contextMenu,
                LeftClickCommand = new ShowAppBarCommand()
            };

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
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _computer.Close();
            _taskbarIcon.Dispose();

            base.OnExit(e);
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Settings _settings = new Settings();
            _settings.Owner = Application.Current.Windows.OfType<AppBar>().First();
            _settings.ShowDialog();
        }

        private void ShowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Windows.OfType<AppBar>().First().Activate();
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private static TaskbarIcon _taskbarIcon { get; set; }

        internal static Computer _computer { get; set; }

        public class ShowAppBarCommand: ICommand
        {
            public void Execute(object parameter)
            {
                Application.Current.Windows.OfType<AppBar>().First().Activate();
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;
        }
    }
}
