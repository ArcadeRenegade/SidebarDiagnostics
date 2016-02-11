using System;
using System.Diagnostics;
using System.Linq;
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

            #if !DEBUG
            using (UpdateManager _manager = new UpdateManager(@"C:\Users\Ryan\Documents\Visual Studio 2015\Projects\SidebarDiagnostics\Releases"))
            {
                UpdateInfo _update = await _manager.CheckForUpdate();
                
                if (_update.ReleasesToApply.Any())
                {
                    Version _newVersion = _update.ReleasesToApply.OrderByDescending(r => r.Version).First().Version.Version;

                    MessageBox.Show(_newVersion.ToString(), "New Version", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);

                    await _manager.UpdateApp();
                }
            }
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
            if (SidebarDiagnostics.Framework.Settings.Default.InitialSetup)
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
            _trayIcon.Visibility = SidebarDiagnostics.Framework.Settings.Default.ShowTrayIcon ? Visibility.Visible : Visibility.Collapsed;
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
            if (SidebarDiagnostics.Framework.Settings.Default.UpgradeRequired)
            {
                SidebarDiagnostics.Framework.Settings.Default.Upgrade();
                SidebarDiagnostics.Framework.Settings.Default.UpgradeRequired = false;
            }

            SidebarDiagnostics.Framework.Settings.Default.MonitorConfig = MonitorConfig.CheckConfig(SidebarDiagnostics.Framework.Settings.Default.MonitorConfig);

            SidebarDiagnostics.Framework.Settings.Default.Save();
        }

        //private async Task SquirrelUpdate()
        //{
        //    await Task.Run(async () =>
        //    {
        //        using (Task<UpdateManager> _task = UpdateManager.GitHubUpdateManager(Constants.GITHUB.REPO, prerelease: true))
        //        {
        //            SquirrelAwareApp.HandleEvents(
        //                onInitialInstall: async (v) =>
        //                {
        //                    UpdateManager _manager = await _task;
        //                    _manager.CreateShortcutForThisExe();
        //                    await _manager.CreateUninstallerRegistryEntry();

        //                    if (SidebarDiagnostics.Properties.Settings.Default.RunAtStartup)
        //                    {
        //                        Utilities.Startup.EnableStartupTask();
        //                    }

        //                    MessageBox.Show(System.Reflection.Assembly.GetEntryAssembly().Location);
        //                },
        //                onAppUpdate: async (v) =>
        //                {
        //                    UpdateManager _manager = await _task;
        //                    _manager.CreateShortcutForThisExe();
        //                    await _manager.CreateUninstallerRegistryEntry();

        //                    if (SidebarDiagnostics.Properties.Settings.Default.RunAtStartup)
        //                    {
        //                        Utilities.Startup.EnableStartupTask();
        //                    }

        //                    MessageBox.Show(System.Reflection.Assembly.GetEntryAssembly().Location);
        //                },
        //                onAppObsoleted: async (v) =>
        //                {
        //                    UpdateManager _manager = await _task;
        //                    _manager.RemoveShortcutForThisExe();
        //                    _manager.RemoveUninstallerRegistryEntry();
        //                },
        //                onAppUninstall: async (v) =>
        //                {
        //                    UpdateManager _manager = await _task;
        //                    _manager.RemoveShortcutForThisExe();
        //                    _manager.RemoveUninstallerRegistryEntry();
        //                });

        //            await _task.Result.UpdateApp();
        //        }
        //    });
        //}

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