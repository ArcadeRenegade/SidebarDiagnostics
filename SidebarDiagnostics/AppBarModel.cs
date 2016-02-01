using System;
using System.Linq;
using System.ComponentModel;
using System.Windows.Threading;
using SidebarDiagnostics.Monitor;

namespace SidebarDiagnostics.Models
{
    public class AppBarModel : INotifyPropertyChanged, IDisposable
    {
        public AppBarModel()
        {
            if (Properties.Settings.Default.ShowClock)
            {
                InitClock();
            }

            InitMonitors();
        }

        public void Dispose()
        {
            if (_hardwareTimer != null)
            {
                _hardwareTimer.Stop();
            }

            if (_clockTimer != null)
            {
                _clockTimer.Stop();
            }

            if (MonitorManager != null)
            {
                MonitorManager.Dispose();
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler _handler = PropertyChanged;

            if (_handler != null)
            {
                _handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void InitClock()
        {
            ShowDate = !Properties.Settings.Default.DateSetting.Equals(Properties.DateSetting.Disabled);

            UpdateClock();

            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += new EventHandler(ClockTimer_Tick);
            _clockTimer.Start();
        }

        private void InitMonitors()
        {
            MonitorManager = new MonitorManager(App._computer);

            foreach (MonitorConfig _config in Properties.Settings.Default.MonitorConfig.Where(c => c.Enabled).OrderBy(c => c.Order))
            {
                MonitorManager.AddPanel(_config);
            }

            MonitorManager.Initialize();
            MonitorManager.Update();

            _hardwareTimer = new DispatcherTimer();
            _hardwareTimer.Interval = TimeSpan.FromMilliseconds(Properties.Settings.Default.PollingInterval);
            _hardwareTimer.Tick += new EventHandler(HardwareTimer_Tick);
            _hardwareTimer.Start();
        }

        private void UpdateClock()
        {
            DateTime _now = DateTime.Now;

            Time = _now.ToString(Properties.Settings.Default.Clock24HR ? "H:mm:ss" : "h:mm:ss tt");

            if (ShowDate)
            {
                Date = _now.ToString(Properties.Settings.Default.DateSetting.Format);
            }
        }

        private void UpdateMonitors()
        {
            MonitorManager.Update();
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            UpdateClock();
        }

        private void HardwareTimer_Tick(object sender, EventArgs e)
        {
            UpdateMonitors();
        }

        private string _time { get; set; }

        public string Time
        {
            get
            {
                return _time;
            }
            set
            {
                _time = value;

                NotifyPropertyChanged("Time");
            }
        }

        private bool _showDate { get; set; }

        public bool ShowDate
        {
            get
            {
                return _showDate;
            }
            set
            {
                _showDate = value;

                NotifyPropertyChanged("ShowDate");
            }
        }

        private string _date { get; set; }

        public string Date
        {
            get
            {
                return _date;
            }
            set
            {
                _date = value;

                NotifyPropertyChanged("Date");
            }
        }

        public MonitorManager MonitorManager { get; private set; }

        private DispatcherTimer _clockTimer { get; set; }

        private DispatcherTimer _hardwareTimer { get; set; }
    }
}
