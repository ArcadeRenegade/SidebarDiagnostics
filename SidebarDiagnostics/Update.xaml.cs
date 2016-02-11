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
        }

        public void SetProgress(double percent)
        {
            UpdateProgress.Value = percent;
        }
    }
}
