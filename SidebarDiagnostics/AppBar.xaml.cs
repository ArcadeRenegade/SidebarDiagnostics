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

            if (Properties.Settings.Default.PollingInterval < 100)
            {
                Properties.Settings.Default.PollingInterval = 1000;
            }

            _hardwareTimer = new DispatcherTimer();
            _hardwareTimer.Interval = TimeSpan.FromMilliseconds(Properties.Settings.Default.PollingInterval);
            _hardwareTimer.Tick += new EventHandler(HardwareTimer_Tick);
            _hardwareTimer.Start();
        }

        public void SettingsUpdate()
        {
            InitAppBar();
            UpdateLayout();

            _hardwareTimer.Interval = TimeSpan.FromMilliseconds(Properties.Settings.Default.PollingInterval);
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _hardwareTimer.Stop();
            _clockTimer.Stop();

            AppBarFunctions.SetAppBar(this, ABEdge.None, false);
        }
                
        private DispatcherTimer _clockTimer { get; set; }

        private DispatcherTimer _hardwareTimer { get; set; }

        private HWManager _hwManager { get; set; }
        
        private class HWManager
        {
            public HWManager(IComputer computer, StackPanel cpuStackPanel, StackPanel ramStackPanel, StackPanel gpuStackPanel)
            {
                BoardHW = App._computer.Hardware.Where(h => h.HardwareType == HardwareType.Mainboard).FirstOrDefault();
                HasBoard = BoardHW != null;

                CPU = App._computer.Hardware.Where(h => h.HardwareType == HardwareType.CPU).Select(hw => new HWPanel(BoardHW, HWType.CPU, hw, cpuStackPanel)).ToArray();
                HasCPU = CPU.Length > 0;

                RAM = App._computer.Hardware.Where(h => h.HardwareType == HardwareType.RAM).Select(hw => new HWPanel(BoardHW, HWType.RAM, hw, ramStackPanel)).ToArray();
                HasRam = RAM.Length > 0;

                GPU = App._computer.Hardware.Where(h => new HardwareType[2] { HardwareType.GpuNvidia, HardwareType.GpuAti }.Contains(h.HardwareType)).Select(hw => new HWPanel(BoardHW, HWType.GPU, hw, gpuStackPanel)).ToArray();
                HasGPU = GPU.Length > 0;
            }

            public void Update()
            {
                if (BoardHW != null)
                {
                    BoardHW.Update();
                }

                foreach (HWPanel _cpuPanel in CPU)
                {
                    _cpuPanel.Update(true);
                }

                foreach (HWPanel _ramPanel in RAM)
                {
                    _ramPanel.Update(true);
                }

                foreach (HWPanel _gpuPanel in GPU)
                {
                    _gpuPanel.Update(true);
                }
            }

            public IHardware BoardHW { get; private set; }
            public HWPanel[] CPU { get; private set; }
            public HWPanel[] RAM { get; private set; }
            public HWPanel[] GPU { get; private set; }

            public bool HasBoard { get; private set; }
            public bool HasCPU { get; private set; }
            public bool HasRam { get; private set; }
            public bool HasGPU { get; private set; }
            
            public class HWPanel
            {
                public HWPanel(IHardware board, HWType type, IHardware hardware, StackPanel parent)
                {
                    Type = type;

                    Hardware = hardware;

                    StackPanel = new StackPanel();
                    StackPanel.Style = (Style)Application.Current.FindResource("HardwarePanel");
                    parent.Children.Add(StackPanel);

                    TextBlock _subtitle = new TextBlock();
                    _subtitle.Style = (Style)Application.Current.FindResource("AppSubtitle");
                    _subtitle.Text = hardware.Name;
                    StackPanel.Children.Add(_subtitle);

                    Sensors = new List<HWSensor>();

                    hardware.Update();

                    switch (type)
                    {
                        case HWType.CPU:
                            InitCPU(board);
                            break;

                        case HWType.RAM:
                            InitRAM(board);
                            break;

                        case HWType.GPU:
                            InitGPU();
                            break;
                    }

                    Update(false);
                }

                public void Update(bool updateHW)
                {
                    if (updateHW)
                    {
                        Hardware.Update();
                    }

                    foreach (HWSensor _hwSensor in Sensors)
                    {
                        _hwSensor.UpdateLabel();
                    }
                }
                
                private void InitCPU(IHardware board)
                {
                    ISensor _coreClock = Hardware.Sensors.Where(s => s.SensorType == SensorType.Clock && s.Name.Contains("CPU")).FirstOrDefault();

                    if (_coreClock != null)
                    {
                        Sensors.Add(new HWSensor(_coreClock, "Clock", " MHz", StackPanel));
                    }

                    ISensor _voltage = null;
                    ISensor _tempSensor = null;

                    if (board != null)
                    {
                        _voltage = board.Sensors.Where(s => s.SensorType == SensorType.Voltage && s.Name.Contains("CPU")).FirstOrDefault();
                        _tempSensor = board.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Name.Contains("CPU")).FirstOrDefault();
                    }

                    if (_voltage == null)
                    {
                        _voltage = Hardware.Sensors.Where(s => s.SensorType == SensorType.Voltage).FirstOrDefault();
                    }

                    if (_tempSensor == null)
                    {
                        _tempSensor =
                            Hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Name == "CPU Package").FirstOrDefault() ??
                            Hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature).FirstOrDefault();
                    }

                    if (_voltage != null)
                    {
                        Sensors.Add(new HWSensor(_voltage, "Volt", " V", StackPanel));
                    }

                    if (_tempSensor != null)
                    {
                        Sensors.Add(new HWSensor(_tempSensor, "Temp", " C", StackPanel));
                    }

                    ISensor _fanSensor = Hardware.Sensors.Where(s => new SensorType[2] { SensorType.Fan, SensorType.Control }.Contains(s.SensorType)).FirstOrDefault();

                    if (_fanSensor != null)
                    {
                        Sensors.Add(new HWSensor(_fanSensor, "Fan", " RPM", StackPanel));
                    }

                    List<ISensor> _loadSensors = Hardware.Sensors.Where(s => s.SensorType == SensorType.Load).ToList();

                    ISensor _totalCPU = _loadSensors.Where(s => s.Index == 0).FirstOrDefault();

                    if (_totalCPU != null)
                    {
                        Sensors.Add(new HWSensor(_totalCPU, "Load", "%", StackPanel));
                    }

                    for (int i = 1; i <= _loadSensors.Max(s => s.Index); i++)
                    {
                        ISensor _coreLoad = _loadSensors.Where(s => s.Index == i).FirstOrDefault();

                        if (_coreLoad != null)
                        {
                            Sensors.Add(new HWSensor(_coreLoad, string.Format("Core #{0}", i), "%", StackPanel));
                        }
                    }
                }

                public void InitRAM(IHardware board)
                {
                    ISensor _ramClock = Hardware.Sensors.Where(s => s.SensorType == SensorType.Clock).FirstOrDefault();

                    if (_ramClock != null)
                    {
                        Sensors.Add(new HWSensor(_ramClock, "Clock", " MHz", StackPanel));
                    }

                    ISensor _voltage = null;

                    if (board != null)
                    {
                        _voltage = board.Sensors.Where(s => s.SensorType == SensorType.Voltage && s.Name.Contains("RAM")).FirstOrDefault();
                    }

                    if (_voltage == null)
                    {
                        _voltage = Hardware.Sensors.Where(s => s.SensorType == SensorType.Voltage).FirstOrDefault();
                    }

                    if (_voltage != null)
                    {
                        Sensors.Add(new HWSensor(_voltage, "Volt", " V", StackPanel));
                    }

                    ISensor _loadSensor = Hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 0).FirstOrDefault();

                    if (_loadSensor != null)
                    {
                        Sensors.Add(new HWSensor(_loadSensor, "Load", "%", StackPanel));
                    }

                    ISensor _usedSensor = Hardware.Sensors.Where(s => s.SensorType == SensorType.Data && s.Index == 0).FirstOrDefault();

                    if (_usedSensor != null)
                    {
                        Sensors.Add(new HWSensor(_usedSensor, "Used", " GB", StackPanel));
                    }

                    ISensor _availSensor = Hardware.Sensors.Where(s => s.SensorType == SensorType.Data && s.Index == 1).FirstOrDefault();

                    if (_availSensor != null)
                    {
                        Sensors.Add(new HWSensor(_availSensor, "Free", " GB", StackPanel));
                    }
                }

                public void InitGPU()
                {
                    ISensor _coreClock = Hardware.Sensors.Where(s => s.SensorType == SensorType.Clock && s.Index == 0).FirstOrDefault();

                    if (_coreClock != null)
                    {
                        Sensors.Add(new HWSensor(_coreClock, "Core", " MHz", StackPanel));
                    }

                    ISensor _memoryClock = Hardware.Sensors.Where(s => s.SensorType == SensorType.Clock && s.Index == 1).FirstOrDefault();

                    if (_memoryClock != null)
                    {
                        Sensors.Add(new HWSensor(_memoryClock, "RAM", " MHz", StackPanel));
                    }

                    ISensor _coreLoad = Hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 0).FirstOrDefault();

                    if (_coreLoad != null)
                    {
                        Sensors.Add(new HWSensor(_coreLoad, "Core", "%", StackPanel));
                    }

                    ISensor _memoryLoad = Hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 3).FirstOrDefault();

                    if (_memoryLoad != null)
                    {
                        Sensors.Add(new HWSensor(_memoryLoad, "RAM", "%", StackPanel));
                    }

                    ISensor _tempSensor = Hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Index == 0).FirstOrDefault();

                    if (_tempSensor != null)
                    {
                        Sensors.Add(new HWSensor(_tempSensor, "Temp", " C", StackPanel));
                    }

                    ISensor _fanSensor = Hardware.Sensors.Where(s => s.SensorType == SensorType.Control && s.Index == 0).FirstOrDefault();

                    if (_fanSensor != null)
                    {
                        Sensors.Add(new HWSensor(_fanSensor, "Fan", " RPM", StackPanel));
                    }
                }
                
                public HWType Type { get; private set; }
                public IHardware Hardware { get; private set; }
                public List<HWSensor> Sensors { get; private set; }
                private StackPanel StackPanel { get; set; }

                public class HWSensor
                {
                    public HWSensor(ISensor sensor, string text, string append, StackPanel stackPanel)
                    {
                        Sensor = sensor;

                        Text = text;

                        Append = append;

                        Control = new Label();
                        Control.Style = (Style)Application.Current.FindResource("AppLabel");

                        stackPanel.Children.Add(Control);
                    }

                    public void UpdateLabel()
                    {
                        Control.Content = string.Format("{0}: {1:0.##}{2}", Text, Sensor.Value, Append);
                    }

                    public ISensor Sensor { get; private set; }
                    public string Text { get; set; }
                    public string Append { get; set; }
                    public Label Control { get; private set; }
                }
            }

            public enum HWType : byte
            {
                CPU,
                RAM,
                GPU
            }
        }
    }
}