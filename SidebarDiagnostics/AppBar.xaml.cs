using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SidebarDiagnostics.Windows;
using SidebarDiagnostics.Models;

namespace SidebarDiagnostics
{
    /// <summary>
    /// Interaction logic for AppBar.xaml
    /// </summary>
    public partial class AppBar : Window
    {
        public AppBar()
        {
            InitializeComponent();
        }

        public void ABShow()
        {
            Shown = true;

            Show();
            InitAppBar();
            Activate();
        }

        public void ABHide()
        {
            Shown = false;
            
            ClearAppBar();
            Hide();
        }

        public void Reload()
        {
            App._reloading = true;

            Close();
        }

        private void InitAppBar()
        {
            WorkArea _workArea = Monitors.GetWorkArea(this);

            Left = _workArea.Left;
            Top = _workArea.Top;
            Height = _workArea.Bottom - _workArea.Top;
            
            Topmost = Properties.Settings.Default.AlwaysTop;

            if (Properties.Settings.Default.ClickThrough)
            {
                ClickThroughWindow.SetClickThrough(this);
            }

            if (Properties.Settings.Default.UseAppBar)
            {
                AppBarWindow.SetAppBar(this, _workArea, Properties.Settings.Default.DockEdge);
            }
        }

        private void ClearAppBar()
        {
            if (AppBarWindow.IsRegistered(this))
            {
                AppBarWindow.SetAppBar(this, null, DockEdge.None);
            }
        }

        private void InitContent()
        {
            DataContext = Model = new AppBarModel();
            
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += new EventHandler(ClockTimer_Tick);
            _clockTimer.Start();
            
            _hardwareTimer = new DispatcherTimer();
            _hardwareTimer.Interval = TimeSpan.FromMilliseconds(Properties.Settings.Default.PollingInterval);
            _hardwareTimer.Tick += new EventHandler(HardwareTimer_Tick);
            _hardwareTimer.Start();
        }
        
        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            Model.UpdateTime();
        }

        private void HardwareTimer_Tick(object sender, EventArgs e)
        {
            Model.UpdateMonitors();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Settings _settings = new Settings();
            _settings.Owner = this;

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
            {
                _settings.ShowDialog();
            }));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ScrollViewer_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            (sender as ScrollViewer).VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }

        private void ScrollViewer_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            (sender as ScrollViewer).VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitAppBar();
            InitContent();
        }

        private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            WindowControls.Visibility = Visibility.Visible;
        }

        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            WindowControls.Visibility = Visibility.Hidden;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_hardwareTimer != null)
            {
                _hardwareTimer.Stop();
            }

            if (_clockTimer != null)
            {
                _clockTimer.Stop();
            }

            if (Model != null)
            {
                Model.Dispose();
            }

            ClearAppBar();
            AppBarWindow.DisposeWindow(this);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (App._reloading)
            {
                App._reloading = false;

                new AppBar().Show();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        public AppBarModel Model { get; private set; }

        public bool Shown { get; private set; } = true;

        private DispatcherTimer _clockTimer { get; set; }

        private DispatcherTimer _hardwareTimer { get; set; }
    }
}