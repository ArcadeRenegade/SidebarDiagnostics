using System.ComponentModel;

namespace SidebarDiagnostics.Models
{
    public class UpdateModel : INotifyPropertyChanged
    {
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private double _progress { get; set; } = 0d;

        public double Progress
        {
            get
            {
                return _progress;
            }
            set
            {
                _progress = value;

                NotifyPropertyChanged("Progress");
                NotifyPropertyChanged("ProgressNormalized");
            }
        }

        public double ProgressNormalized
        {
            get
            {
                return _progress / 100d;
            }
        }
    }
}
