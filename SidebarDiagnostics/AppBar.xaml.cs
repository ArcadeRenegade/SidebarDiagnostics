using System;
using System.Windows;
using System.Windows.Threading;
using SidebarDiagnostics.Windows;
using SidebarDiagnostics.Helpers;
using SidebarDiagnostics.Hardware;

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

        private void InitAppBar()
        {
            var _screen = Utilities.GetScreenFromIndex(Properties.Settings.Default.ScreenIndex);

            double _left;

            switch (Properties.Settings.Default.DockEdge)
            {
                case DockEdge.Left:
                    _left = _screen.WorkingArea.Left;
                    break;

                case DockEdge.Right:
                    _left = _screen.WorkingArea.Right - ActualWidth;
                    break;

                default:
                    _left = 0;
                    break;
            }

            Left = _left;
            Top = _screen.WorkingArea.Top;
            Height = _screen.WorkingArea.Height;
            
            Topmost = Properties.Settings.Default.AlwaysTop;

            if (Properties.Settings.Default.ClickThrough)
            {
                ClickThroughWindow.SetClickThrough(this);
            }

            if (Properties.Settings.Default.UseAppBar)
            {
                AppBarWindow.SetAppBar(this, _screen, Properties.Settings.Default.DockEdge);
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

            if (Properties.Settings.Default.PollingInterval < 100)
            {
                Properties.Settings.Default.PollingInterval = 1000;
            }

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
            _hwManager = new HWManager(App._computer, CPUStackPanel, RAMStackPanel, GPUStackPanel);

            if (_hwManager.HasCPU)
            {
                CPUTitle.Visibility = Visibility.Visible;
                CPUStackPanel.Visibility = Visibility.Visible;
            }

            if (_hwManager.HasRam)
            {
                RAMTitle.Visibility = Visibility.Visible;
                RAMStackPanel.Visibility = Visibility.Visible;
            }
            
            if (_hwManager.HasGPU)
            {
                GPUTitle.Visibility = Visibility.Visible;
                GPUStackPanel.Visibility = Visibility.Visible;
            }
        }

        private void UpdateHardware()
        {
            _hwManager.Update();
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitAppBar();
            InitContent();
        }

        private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!Properties.Settings.Default.ClickThrough)
            {
                WindowControlsStackPanel.Visibility = Visibility.Visible;
            }
        }

        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            WindowControlsStackPanel.Visibility = Visibility.Hidden;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsLoaded)
            {
                _hardwareTimer.Stop();
                _clockTimer.Stop();

                AppBarWindow.SetAppBar(this, null, DockEdge.None);
            }
        }

        private DispatcherTimer _clockTimer { get; set; }

        private DispatcherTimer _hardwareTimer { get; set; }

        private HWManager _hwManager { get; set; }
    }
}