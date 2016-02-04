using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SidebarDiagnostics.Windows;

namespace SidebarDiagnostics
{
    /// <summary>
    /// Interaction logic for Dummy.xaml
    /// </summary>
    public partial class Dummy : AppBarWindow
    {
        public Dummy()
        {
            InitializeComponent();
        }

        public void Position()
        {
            WorkArea _windowWA;
            WorkArea _appbarWA;

            Windows.Monitor.GetWorkArea(this, out _windowWA, out _appbarWA);

            Left = _windowWA.Left;
            Top = _windowWA.Top;
            Width = _windowWA.Width;
            Height = _windowWA.Height;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Position();
        }
    }
}
