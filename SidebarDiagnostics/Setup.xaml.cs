using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using SidebarDiagnostics.Windows;

namespace SidebarDiagnostics
{
    /// <summary>
    /// Interaction logic for Setup.xaml
    /// </summary>
    public partial class Setup : DPIAwareWindow
    {
        public Setup()
        {
            Sidebar = new Dummy();
            Sidebar.Show();

            InitializeComponent();

            Owner = Sidebar;
            ShowDialog();
        }

        private void ShowPage(Page page)
        {
            foreach (DockPanel _panel in SetupGrid.Children)
            {
                if (_panel.Name == page.ToString())
                {
                    CurrentPage = page;

                    _panel.IsEnabled = true;
                }
                else
                {
                    _panel.IsEnabled = false;
                }
            }
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            switch (CurrentPage)
            {
                case Page.Initial:
                case Page.EndHighDPI:
                case Page.BeginCustom:
                    ShowPage(Page.Final);
                    return;

                case Page.BeginHighDPI:
                    Properties.Settings.Default.HighDPISupport = true;
                    Sidebar.Position();
                    ShowPage(Page.EndHighDPI);
                    return;
            }
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            switch (CurrentPage)
            {
                case Page.Initial:
                    ShowPage(Page.BeginHighDPI);
                    return;

                case Page.BeginHighDPI:
                case Page.EndHighDPI:
                    ShowPage(Page.BeginCustom);
                    return;
            }
        }

        private void OffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_cancelReposition != null)
            {
                _cancelReposition.Cancel();
            }

            _cancelReposition = new CancellationTokenSource();

            Task.Delay(TimeSpan.FromMilliseconds(500), _cancelReposition.Token).ContinueWith(_ =>
            {
                if (_.IsCanceled)
                {
                    return;
                }

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    Properties.Settings.Default.XOffset = (int)XOffsetSlider.Value;
                    Properties.Settings.Default.YOffset = (int)YOffsetSlider.Value;

                    Sidebar.Position();
                }));

                _cancelReposition = null;
            });
        }

        private void NumberBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (new Regex("[^0-9.-]+").IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            _openSettings = true;

            Sidebar.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Sidebar.Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.InitialSetup = false;
            Properties.Settings.Default.Save();

            App.StartApp(_openSettings);
        }

        public Dummy Sidebar { get; private set; }

        public Page CurrentPage { get; set; } = Page.Initial;

        private CancellationTokenSource _cancelReposition { get; set; }

        private bool _openSettings { get; set; } = false;

        public enum Page : byte
        {
            Initial,
            BeginHighDPI,
            EndHighDPI,
            BeginCustom,
            Final
        }
    }
}
