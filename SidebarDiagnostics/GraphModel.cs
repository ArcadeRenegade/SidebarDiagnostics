using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SidebarDiagnostics.Monitoring;

namespace SidebarDiagnostics.Models
{
    public class GraphModel : INotifyPropertyChanged
    {
        public void BindData(MonitorManager manager)
        {
            BindMonitors(manager.MonitorPanels);
            BindHardware(new iMonitor[0]);
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void BindMonitors(MonitorPanel[] panels)
        {
            MonitorItems = new MonitorItem[1] { MonitorItem.Default }.Concat(panels.Select(p => new MonitorItem() { Text = p.Title, Value = p })).ToArray();
            Monitor = null;
        }

        private void BindHardware(iMonitor[] monitors)
        {
            HardwareItems = new HardwareItem[1] { HardwareItem.Default }.Concat(monitors.Select(m => new HardwareItem() { Text = m.Name, Value = m })).ToArray();
            Hardware = null;
        }

        private MonitorItem[] _monitorItems { get; set; }

        public MonitorItem[] MonitorItems
        {
            get
            {
                return _monitorItems;
            }
            set
            {
                _monitorItems = value;

                NotifyPropertyChanged("MonitorItems");
            }
        }

        private MonitorPanel _monitor { get; set; }

        public MonitorPanel Monitor
        {
            get
            {
                return _monitor;
            }
            set
            {
                _monitor = value;

                if (_monitor == null)
                {
                    BindHardware(new iMonitor[0]);
                }
                else
                {
                    BindHardware(_monitor.Monitors);
                }

                NotifyPropertyChanged("Monitor");
            }
        }

        private HardwareItem[] _hardwareItems { get; set; }

        public HardwareItem[] HardwareItems
        {
            get
            {
                return _hardwareItems;
            }
            set
            {
                _hardwareItems = value;

                NotifyPropertyChanged("HardwareItems");
            }
        }

        private iMonitor _hardware { get; set; }

        public iMonitor Hardware
        {
            get
            {
                return _hardware;
            }
            set
            {
                _hardware = value;

                NotifyPropertyChanged("Hardware");
            }
        }
    }

    public class MonitorItem
    {
        public MonitorPanel Value { get; set; }

        public string Text { get; set; }

        public static MonitorItem Default = new MonitorItem() { Text = "Select Monitor" };
    }

    public class HardwareItem
    {
        public iMonitor Value { get; set; }

        public string Text { get; set; }

        public static HardwareItem Default = new HardwareItem() { Text = "Select Hardware" };
    }

    public class MetricItem
    {
        public iMonitor Value { get; set; }

        public string Text { get; set; }

        public static MetricItem Default = new MetricItem() { Text = "Select Metrics" };
    }
}
