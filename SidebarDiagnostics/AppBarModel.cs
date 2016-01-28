using System;
using System.Linq;
using System.ComponentModel;
using SidebarDiagnostics.Monitor;

namespace SidebarDiagnostics.Models
{
    public class AppBarModel : INotifyPropertyChanged
    {
        public AppBarModel()
        {
            if (Properties.Settings.Default.ShowClock)
            {
                UpdateTime();
            }

            MonitorManager = new MonitorManager(App._computer);

            foreach (MonitorConfig _config in Properties.Settings.Default.MonitorConfig.Where(c => c.Enabled).OrderBy(c => c.Order))
            {
                MonitorManager.AddPanel(_config);
            }

            MonitorManager.Update();
        }

        public void UpdateTime()
        {
            Time = DateTime.Now.ToString(Properties.Settings.Default.Clock24HR ? "H:mm:ss" : "h:mm:ss tt");
        }

        public void UpdateMonitors()
        {
            MonitorManager.Update();
        }

        public void Dispose()
        {
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

        public MonitorManager MonitorManager { get; private set; }
    }
}
