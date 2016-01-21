using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OpenHardwareMonitor.Hardware;
using SidebarDiagnostics.AB;
using SidebarDiagnostics.Helpers;

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

        public void InitAppBar()
        {
            AppBarFunctions.SetAppBar(this, ABEdge.None, Properties.Settings.Default.AlwaysTop);

            Monitor _monitor = Utilities.GetMonitorFromIndex(Properties.Settings.Default.ScreenIndex);

            Top = _monitor.WorkingArea.Top;

            double _left = _monitor.WorkingArea.Left;

            if (Properties.Settings.Default.DockEdge == ABEdge.Right)
            {
                _left += _monitor.WorkingArea.Width - Width;
            }

            Left = _left;

            AppBarFunctions.SetAppBar(this, Properties.Settings.Default.DockEdge, Properties.Settings.Default.AlwaysTop);
        }

        public void InitContent()
        {
            GetClock();

            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += new EventHandler(ClockTimer_Tick);
            _clockTimer.Start();

            GetHardware();
            UpdateHardware();

            _hardwareTimer = new DispatcherTimer();
            _hardwareTimer.Interval = TimeSpan.FromSeconds(Properties.Settings.Default.PollingInterval);
            _hardwareTimer.Tick += new EventHandler(HardwareTimer_Tick);
            _hardwareTimer.Start();
        }

        public void SettingsUpdate()
        {
            InitAppBar();
            UpdateLayout();

            _hardwareTimer.Interval = TimeSpan.FromSeconds(Properties.Settings.Default.PollingInterval);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitAppBar();
            InitContent();
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            GetClock();
        }

        private void GetClock()
        {
            ClockLabel.Content = DateTime.Now.ToString("h:mm:ss tt");
        }

        private void HardwareTimer_Tick(object sender, EventArgs e)
        {
            UpdateHardware();
        }

        private void GetHardware()
        {
            _boardHW = App._computer.Hardware.Where(h => h.HardwareType == HardwareType.Mainboard).FirstOrDefault();

            _cpuHW = App._computer.Hardware.Where(h => h.HardwareType == HardwareType.CPU).Select(hw => new HWPanel(hw, CPUStackPanel)).ToArray();

            if (_cpuHW.Length > 0)
            {
                CPUTitle.Visibility = Visibility.Visible;
                CPUStackPanel.Visibility = Visibility.Visible;
            }

            _ramHW = App._computer.Hardware.Where(h => h.HardwareType == HardwareType.RAM).Select(hw => new HWPanel(hw, RAMStackPanel)).ToArray();

            if (_ramHW.Length > 0)
            {
                RAMTitle.Visibility = Visibility.Visible;
                RAMStackPanel.Visibility = Visibility.Visible;
            }

            _gpuHW = App._computer.Hardware.Where(h => new HardwareType[2] { HardwareType.GpuNvidia, HardwareType.GpuAti }.Contains(h.HardwareType)).Select(hw => new HWPanel(hw, GPUStackPanel)).ToArray();

            if (_gpuHW.Length > 0)
            {
                GPUTitle.Visibility = Visibility.Visible;
                GPUStackPanel.Visibility = Visibility.Visible;
            }
        }

        private void UpdateHardware()
        {
            UpdateBoard();
            UpdateCPU();
            UpdateRAM();
            UpdateGPU();
        }

        private void UpdateBoard()
        {
            if (_boardHW != null)
            {
                _boardHW.Update();
            }
        }

        private void UpdateCPU()
        {
            foreach (HWPanel _hwPanel in _cpuHW)
            {
                _hwPanel.Hardware.Update();
                
                ISensor _coreClock = _hwPanel.Hardware.Sensors.Where(s => s.SensorType == SensorType.Clock && s.Name.Contains("CPU")).FirstOrDefault();

                if (_coreClock != null)
                {
                    _hwPanel.UpdateLabel(_coreClock.Identifier, string.Format("Clock: {0:0.##} MHz", _coreClock.Value));
                }

                ISensor _voltage = null;
                ISensor _tempSensor = null;

                if (_boardHW != null)
                {
                    _voltage = _boardHW.Sensors.Where(s => s.SensorType == SensorType.Voltage && s.Name.Contains("CPU")).FirstOrDefault();
                    _tempSensor = _boardHW.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Name.Contains("CPU")).FirstOrDefault();
                }

                if (_voltage == null)
                {
                    _voltage = _hwPanel.Hardware.Sensors.Where(s => s.SensorType == SensorType.Voltage).FirstOrDefault();
                }

                if (_tempSensor == null)
                {
                    _tempSensor =
                        _hwPanel.Hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Name == "CPU Package").FirstOrDefault() ??
                        _hwPanel.Hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature).FirstOrDefault();
                }

                if (_voltage != null)
                {
                    _hwPanel.UpdateLabel(_voltage.Identifier, string.Format("Volt: {0:0.##} V", _voltage.Value));
                }

                if (_tempSensor != null)
                {
                    _hwPanel.UpdateLabel(_tempSensor.Identifier, string.Format("Temp: {0:0.##} C", _tempSensor.Value));
                }

                ISensor _fanSensor = _hwPanel.Hardware.Sensors.Where(s => new SensorType[2] { SensorType.Fan, SensorType.Control }.Contains(s.SensorType)).FirstOrDefault();

                if (_fanSensor != null)
                {
                    _hwPanel.UpdateLabel(_fanSensor.Identifier, string.Format("Fan: {0:0.##} RPM", _fanSensor.Value));
                }

                List<ISensor> _loadSensors = _hwPanel.Hardware.Sensors.Where(s => s.SensorType == SensorType.Load).ToList();

                ISensor _totalCPU = _loadSensors.Where(s => s.Index == 0).FirstOrDefault();

                if (_totalCPU != null)
                {
                    _hwPanel.UpdateLabel(_totalCPU.Identifier, string.Format("Load: {0:0.##}%", _totalCPU.Value));
                }

                for (int i = 1; i <= _loadSensors.Max(s => s.Index); i++)
                {
                    ISensor _coreLoad = _loadSensors.Where(s => s.Index == i).FirstOrDefault();

                    if (_coreLoad != null)
                    {
                        _hwPanel.UpdateLabel(_coreLoad.Identifier, string.Format("Core #{0}: {1:0.##}%", i, _coreLoad.Value));
                    }
                }
            }
        }

        private void UpdateRAM()
        {
            foreach (HWPanel _hwPanel in _ramHW)
            {
                _hwPanel.Hardware.Update();

                ISensor _ramClock = _hwPanel.Hardware.Sensors.Where(s => s.SensorType == SensorType.Clock).FirstOrDefault();

                if (_ramClock != null)
                {
                    _hwPanel.UpdateLabel(_ramClock.Identifier, string.Format("Clock: {0:0.##} MHz", _ramClock.Value));
                }

                if (_boardHW != null)
                {
                    ISensor _voltage = _boardHW.Sensors.Where(s => s.SensorType == SensorType.Voltage && s.Name.Contains("RAM")).FirstOrDefault();

                    if (_voltage != null)
                    {
                        _hwPanel.UpdateLabel(_voltage.Identifier, string.Format("Volt: {0:0.##} V", _voltage.Value));
                    }
                }

                ISensor _loadSensor = _hwPanel.Hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 0).FirstOrDefault();

                if (_loadSensor != null)
                {
                    _hwPanel.UpdateLabel(_loadSensor.Identifier, string.Format("Load: {0:0.##}%", _loadSensor.Value));
                }

                ISensor _usedSensor = _hwPanel.Hardware.Sensors.Where(s => s.SensorType == SensorType.Data && s.Index == 0).FirstOrDefault();

                if (_usedSensor != null)
                {
                    _hwPanel.UpdateLabel(_usedSensor.Identifier, string.Format("Used: {0:0.##} GB", _usedSensor.Value));
                }

                ISensor _availSensor = _hwPanel.Hardware.Sensors.Where(s => s.SensorType == SensorType.Data && s.Index == 1).FirstOrDefault();

                if (_availSensor != null)
                {
                    _hwPanel.UpdateLabel(_availSensor.Identifier, string.Format("Free: {0:0.##} GB", _availSensor.Value));
                }
            }
        }

        private void UpdateGPU()
        {
            foreach (HWPanel _hwPanel in _gpuHW)
            {
                _hwPanel.Hardware.Update();

                ISensor _coreClock = _hwPanel.Hardware.Sensors.Where(s => s.SensorType == SensorType.Clock && s.Index == 0).FirstOrDefault();

                if (_coreClock != null)
                {
                    _hwPanel.UpdateLabel(_coreClock.Identifier, string.Format("Core: {0:0.##} MHz", _coreClock.Value));
                }

                ISensor _memoryClock = _hwPanel.Hardware.Sensors.Where(s => s.SensorType == SensorType.Clock && s.Index == 1).FirstOrDefault();

                if (_memoryClock != null)
                {
                    _hwPanel.UpdateLabel(_memoryClock.Identifier, string.Format("RAM: {0:0.##} MHz", _memoryClock.Value));
                }

                ISensor _coreLoad = _hwPanel.Hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 0).FirstOrDefault();

                if (_coreLoad != null)
                {
                    _hwPanel.UpdateLabel(_coreLoad.Identifier, string.Format("Core: {0:0.##}%", _coreLoad.Value));
                }

                ISensor _memoryLoad = _hwPanel.Hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 3).FirstOrDefault();

                if (_memoryLoad != null)
                {
                    _hwPanel.UpdateLabel(_memoryLoad.Identifier, string.Format("RAM: {0:0.##}%", _memoryLoad.Value));
                }

                ISensor _tempSensor = _hwPanel.Hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Index == 0).FirstOrDefault();

                if (_tempSensor != null)
                {
                    _hwPanel.UpdateLabel(_tempSensor.Identifier, string.Format("Temp: {0:0.##} C", _tempSensor.Value));
                }

                ISensor _fanSensor = _hwPanel.Hardware.Sensors.Where(s => s.SensorType == SensorType.Control && s.Index == 0).FirstOrDefault();

                if (_fanSensor != null)
                {
                    _hwPanel.UpdateLabel(_fanSensor.Identifier, string.Format("Fan: {0:0.##} RPM", _fanSensor.Value));
                }
            }
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _hardwareTimer.Stop();
            _clockTimer.Stop();

            AppBarFunctions.SetAppBar(this, ABEdge.None, false);
        }
                
        private DispatcherTimer _clockTimer { get; set; }

        private DispatcherTimer _hardwareTimer { get; set; }

        private IHardware _boardHW { get; set; }

        private HWPanel[] _cpuHW { get; set; }

        private HWPanel[] _ramHW { get; set; }

        private HWPanel[] _gpuHW { get; set; }
        
        private class HWPanel
        {
            public HWPanel(IHardware hardware, StackPanel parent)
            {
                Hardware = hardware;

                StackPanel = new StackPanel();
                StackPanel.Style = (Style)Application.Current.FindResource("HardwarePanel");
                parent.Children.Add(StackPanel);

                TextBlock _subtitle = new TextBlock();
                _subtitle.Style = (Style)Application.Current.FindResource("AppSubtitle");
                _subtitle.Text = hardware.Name;
                StackPanel.Children.Add(_subtitle);

                Controls = new Dictionary<Identifier, FrameworkElement>();
            }

            public void UpdateLabel(Identifier id, string text)
            {
                Label _label;

                if (Controls.ContainsKey(id))
                {
                    _label = (Label)Controls[id];
                }
                else
                {
                    _label = new Label();
                    _label.Style = (Style)Application.Current.FindResource("AppLabel");

                    StackPanel.Children.Add(_label);

                    Controls.Add(id, _label);
                }

                _label.Content = text;
            }

            public IHardware Hardware { get; set; }
            public StackPanel StackPanel { get; set; }
            public Dictionary<Identifier, FrameworkElement> Controls { get; set; }
        }
    }
}