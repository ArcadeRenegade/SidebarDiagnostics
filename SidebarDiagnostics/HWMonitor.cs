using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using OpenHardwareMonitor.Hardware;

namespace SidebarDiagnostics.Hardware
{
    public class HWManager
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

                foreach (IHardware _subHardware in BoardHW.SubHardware)
                {
                    _subHardware.Update();
                }
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
    }

    public class HWPanel
    {
        public HWPanel(IHardware board, HWType type, IHardware hardware, StackPanel parent)
        {
            Type = type;

            Hardware = hardware;

            StackPanel = new StackPanel();
            StackPanel.Style = (Style)Application.Current.FindResource("HardwarePanel");
            parent.Children.Add(StackPanel);

            TextBlock _hardwareName = new TextBlock();
            _hardwareName.Style = (Style)Application.Current.FindResource("HardwareText");
            _hardwareName.Text = hardware.Name;
            StackPanel.Children.Add(_hardwareName);
            
            UpdateHardware();

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
                UpdateHardware();
            }

            foreach (HWSensor _hwSensor in Sensors)
            {
                _hwSensor.UpdateLabel();
            }
        }

        private void UpdateHardware()
        {
            Hardware.Update();

            foreach (IHardware _subHardware in Hardware.SubHardware)
            {
                _subHardware.Update();
            }
        }

        private void InitCPU(IHardware board)
        {
            List<HWSensor> _sensors = new List<HWSensor>();

            ISensor _coreClock = Hardware.Sensors.Where(s => s.SensorType == SensorType.Clock && s.Name.Contains("CPU")).FirstOrDefault();

            if (_coreClock != null)
            {
                _sensors.Add(new HWSensor(_coreClock, "Clock", " MHz", StackPanel));
            }

            ISensor _voltage = null;
            ISensor _tempSensor = null;
            ISensor _fanSensor = null;

            if (board != null)
            {
                _voltage = board.Sensors.Where(s => s.SensorType == SensorType.Voltage && s.Name.Contains("CPU")).FirstOrDefault();
                _tempSensor = board.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Name.Contains("CPU")).FirstOrDefault();
                _fanSensor = board.Sensors.Where(s => new SensorType[2] { SensorType.Fan, SensorType.Control }.Contains(s.SensorType) && s.Name.Contains("CPU")).FirstOrDefault();
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

            if (_fanSensor == null)
            {
                _fanSensor = Hardware.Sensors.Where(s => new SensorType[2] { SensorType.Fan, SensorType.Control }.Contains(s.SensorType)).FirstOrDefault();
            }

            if (_voltage != null)
            {
                _sensors.Add(new HWSensor(_voltage, "Volt", " V", StackPanel));
            }

            if (_tempSensor != null)
            {
                _sensors.Add(new HWSensor(_tempSensor, "Temp", " C", StackPanel));
            }

            if (_fanSensor != null)
            {
                _sensors.Add(new HWSensor(_fanSensor, "Fan", " RPM", StackPanel));
            }

            List<ISensor> _loadSensors = Hardware.Sensors.Where(s => s.SensorType == SensorType.Load).ToList();

            ISensor _totalCPU = _loadSensors.Where(s => s.Index == 0).FirstOrDefault();

            if (_totalCPU != null)
            {
                _sensors.Add(new HWSensor(_totalCPU, "Load", "%", StackPanel));
            }

            for (int i = 1; i <= _loadSensors.Max(s => s.Index); i++)
            {
                ISensor _coreLoad = _loadSensors.Where(s => s.Index == i).FirstOrDefault();

                if (_coreLoad != null)
                {
                    _sensors.Add(new HWSensor(_coreLoad, string.Format("Core #{0}", i), "%", StackPanel));
                }
            }

            Sensors = _sensors.ToArray();
        }

        public void InitRAM(IHardware board)
        {
            List<HWSensor> _sensors = new List<HWSensor>();

            ISensor _ramClock = Hardware.Sensors.Where(s => s.SensorType == SensorType.Clock).FirstOrDefault();

            if (_ramClock != null)
            {
                _sensors.Add(new HWSensor(_ramClock, "Clock", " MHz", StackPanel));
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
                _sensors.Add(new HWSensor(_voltage, "Volt", " V", StackPanel));
            }

            ISensor _loadSensor = Hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 0).FirstOrDefault();

            if (_loadSensor != null)
            {
                _sensors.Add(new HWSensor(_loadSensor, "Load", "%", StackPanel));
            }

            ISensor _usedSensor = Hardware.Sensors.Where(s => s.SensorType == SensorType.Data && s.Index == 0).FirstOrDefault();

            if (_usedSensor != null)
            {
                _sensors.Add(new HWSensor(_usedSensor, "Used", " GB", StackPanel));
            }

            ISensor _availSensor = Hardware.Sensors.Where(s => s.SensorType == SensorType.Data && s.Index == 1).FirstOrDefault();

            if (_availSensor != null)
            {
                _sensors.Add(new HWSensor(_availSensor, "Free", " GB", StackPanel));
            }

            Sensors = _sensors.ToArray();
        }

        public void InitGPU()
        {
            List<HWSensor> _sensors = new List<HWSensor>();

            ISensor _coreClock = Hardware.Sensors.Where(s => s.SensorType == SensorType.Clock && s.Index == 0).FirstOrDefault();

            if (_coreClock != null)
            {
                _sensors.Add(new HWSensor(_coreClock, "Core", " MHz", StackPanel));
            }

            ISensor _memoryClock = Hardware.Sensors.Where(s => s.SensorType == SensorType.Clock && s.Index == 1).FirstOrDefault();

            if (_memoryClock != null)
            {
                _sensors.Add(new HWSensor(_memoryClock, "VRAM", " MHz", StackPanel));
            }

            ISensor _coreLoad = Hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 0).FirstOrDefault();

            if (_coreLoad != null)
            {
                _sensors.Add(new HWSensor(_coreLoad, "Core", "%", StackPanel));
            }

            ISensor _memoryLoad = Hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 3).FirstOrDefault();

            if (_memoryLoad != null)
            {
                _sensors.Add(new HWSensor(_memoryLoad, "VRAM", "%", StackPanel));
            }

            ISensor _tempSensor = Hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Index == 0).FirstOrDefault();

            if (_tempSensor != null)
            {
                _sensors.Add(new HWSensor(_tempSensor, "Temp", " C", StackPanel));
            }

            ISensor _fanSensor = Hardware.Sensors.Where(s => s.SensorType == SensorType.Control && s.Index == 0).FirstOrDefault();

            if (_fanSensor != null)
            {
                _sensors.Add(new HWSensor(_fanSensor, "Fan", "%", StackPanel));
            }

            Sensors = _sensors.ToArray();
        }

        public HWType Type { get; private set; }
        public IHardware Hardware { get; private set; }
        public HWSensor[] Sensors { get; private set; }
        private StackPanel StackPanel { get; set; }
    }

    public class HWSensor
    {
        public HWSensor(ISensor sensor, string text, string append, StackPanel stackPanel)
        {
            Sensor = sensor;

            Text = text;

            Append = append;

            Control = new TextBlock();
            Control.Style = (Style)Application.Current.FindResource("SensorText");

            stackPanel.Children.Add(Control);
        }

        public void UpdateLabel()
        {
            Control.Text = string.Format("{0}: {1:0.##}{2}", Text, Sensor.Value, Append);
        }

        public ISensor Sensor { get; private set; }
        public string Text { get; set; }
        public string Append { get; set; }
        public TextBlock Control { get; private set; }
    }

    public enum HWType : byte
    {
        CPU,
        RAM,
        GPU
    }
}
