using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SidebarDiagnostics.Windows;
using SidebarDiagnostics.Monitor;

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
            _reloading = true;

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
            if (Properties.Settings.Default.UseAppBar)
            {
                AppBarWindow.SetAppBar(this, null, DockEdge.None);
            }
        }

        private void InitContent()
        {
            UpdateClock();

            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += new EventHandler(ClockTimer_Tick);
            _clockTimer.Start();

            GetHardware();
            UpdateHardware();

            _hardwareTimer = new DispatcherTimer();
            _hardwareTimer.Interval = TimeSpan.FromMilliseconds(Properties.Settings.Default.PollingInterval);
            _hardwareTimer.Tick += new EventHandler(HardwareTimer_Tick);
            _hardwareTimer.Start();
        }
        
        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            UpdateClock();
        }

        private void UpdateClock()
        {
            ClockLabel.Content = DateTime.Now.ToString(Properties.Settings.Default.Clock24HR ? "H:mm:ss" : "h:mm:ss tt");
        }

        private void HardwareTimer_Tick(object sender, EventArgs e)
        {
            UpdateHardware();
        }

        private void GetHardware()
        {
            _monitorManager = new MonitorManager(App._computer, MainStackPanel);

            _monitorManager.AddPanel(MonitorType.CPU, true);
            _monitorManager.AddPanel(MonitorType.RAM, true);
            _monitorManager.AddPanel(MonitorType.GPU, true);
        }

        private void UpdateHardware()
        {
            _monitorManager.Update();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Settings _settings = new Settings();
            _settings.Owner = this;
            _settings.ShowDialog();
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
            WindowControlsStackPanel.Visibility = Visibility.Visible;
        }

        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            WindowControlsStackPanel.Visibility = Visibility.Hidden;
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

            ClearAppBar();
            AppBarWindow.DisposeWindow(this);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_reloading)
            {
                AppBar _newAppBar = new AppBar();
                _newAppBar.Show();

                foreach (Settings _settings in Application.Current.Windows.OfType<Settings>())
                {
                    _settings.Owner = _newAppBar;
                }
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        public bool Shown { get; private set; } = true;

        private DispatcherTimer _clockTimer { get; set; }

        private DispatcherTimer _hardwareTimer { get; set; }

        private MonitorManager _monitorManager { get; set; }

        private bool _reloading { get; set; } = false;
    }
}