using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;
using SidebarDiagnostics.Models;
using SidebarDiagnostics.Windows;

namespace SidebarDiagnostics
{
    /// <summary>
    /// Interaction logic for Update.xaml
    /// </summary>
    public partial class Update : DPIAwareWindow
    {
        public Update()
        {
            InitializeComponent();

            DataContext = Model = new UpdateModel();
        }

        public void SetProgress(double percent)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {
                Model.Progress = percent;
            }));
        }

        public new void Close()
        {
            _close = true;

            base.Close();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            DragMove();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_close)
            {
                base.OnClosing(e);
            }
            else
            {
                e.Cancel = true;
            }
        }

        public UpdateModel Model { get; private set; }

        private bool _close { get; set; } = false;
    }
}
