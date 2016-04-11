using System;
using System.ComponentModel;
using System.Windows.Threading;
using SidebarDiagnostics.Monitoring;

namespace SidebarDiagnostics.Models
{
    public class SidebarModel : INotifyPropertyChanged, IDisposable
    {
        public SidebarModel()
        {
            InitClock();
            InitMonitors();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    DisposeClock();
                    DisposeMonitors();
                }

                _disposed = true;
            }
        }

        ~SidebarModel()
        {
            Dispose(false);
        }

        public void Start()
        {
            StartClock();
            StartMonitors();
        }

        public void Reload()
        {
            DisposeMonitors();
            InitMonitors();
            StartMonitors();
        }

        public void Pause()
        {
            PauseClock();
            PauseMonitors();
        }

        public void Resume()
        {
            ResumeClock();
            ResumeMonitors();
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void InitClock()
        {
            ShowClock = Framework.Settings.Instance.ShowClock;

            if (!ShowClock)
            {
                return;
            }

            ShowDate = !Framework.Settings.Instance.DateSetting.Equals(Framework.DateSetting.Disabled);

            UpdateClock();
        }

        private void InitMonitors()
        {
            MonitorManager = new MonitorManager(Framework.Settings.Instance.MonitorConfig);
            MonitorManager.Update();
        }

        private void StartClock()
        {
            if (!ShowClock)
            {
                return;
            }

            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += new EventHandler(ClockTimer_Tick);
            _clockTimer.Start();
        }

        private void StartMonitors()
        {
            _monitorTimer = new DispatcherTimer();
            _monitorTimer.Interval = TimeSpan.FromMilliseconds(Framework.Settings.Instance.PollingInterval);
            _monitorTimer.Tick += new EventHandler(MonitorTimer_Tick);
            _monitorTimer.Start();
        }

        private void UpdateClock()
        {
            DateTime _now = DateTime.Now;

            Time = _now.ToString(Framework.Settings.Instance.Clock24HR ? "H:mm:ss" : "h:mm:ss tt");

            if (ShowDate)
            {
                Date = _now.ToString(Framework.Settings.Instance.DateSetting.Format);
            }
        }

        private void UpdateMonitors()
        {
            MonitorManager.Update();
        }

        private void PauseClock()
        {
            if (_clockTimer != null)
            {
                _clockTimer.Stop();
            }
        }

        private void PauseMonitors()
        {
            if (_monitorTimer != null)
            {
                _monitorTimer.Stop();
            }
        }

        private void ResumeClock()
        {
            if (_clockTimer != null)
            {
                _clockTimer.Start();
            }
        }

        private void ResumeMonitors()
        {
            if (_monitorTimer != null)
            {
                _monitorTimer.Start();
            }
        }

        private void DisposeClock()
        {
            if (_clockTimer != null)
            {
                _clockTimer.Stop();
                _clockTimer = null;
            }
        }

        private void DisposeMonitors()
        {
            if (_monitorTimer != null)
            {
                _monitorTimer.Stop();
                _monitorTimer = null;
            }
            
            if (MonitorManager != null)
            {
                MonitorManager.Dispose();
                _monitorManager = null;
            }
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            UpdateClock();
        }

        private void MonitorTimer_Tick(object sender, EventArgs e)
        {
            UpdateMonitors();
        }

        private bool _ready { get; set; } = false;

        public bool Ready
        {
            get
            {
                return _ready;
            }
            set
            {
                _ready = value;

                NotifyPropertyChanged("Ready");
            }
        }

        private bool _showClock { get; set; }

        public bool ShowClock
        {
            get
            {
                return _showClock;
            }
            set
            {
                _showClock = value;

                NotifyPropertyChanged("ShowClock");
            }
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

        private MonitorManager _monitorManager { get; set; }

        public MonitorManager MonitorManager
        {
            get
            {
                return _monitorManager;
            }
            set
            {
                _monitorManager = value;

                NotifyPropertyChanged("MonitorManager");
            }
        }

        private DispatcherTimer _clockTimer { get; set; }

        private DispatcherTimer _monitorTimer { get; set; }

        private bool _disposed { get; set; } = false;
    }
}
