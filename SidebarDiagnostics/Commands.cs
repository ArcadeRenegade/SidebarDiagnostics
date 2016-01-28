using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace SidebarDiagnostics.Commands
{
    internal static class Utilities
    {
        public static AppBar GetAppBar
        {
            get
            {
                return Application.Current.Windows.OfType<AppBar>().FirstOrDefault();
            }
        }
    }

    public class ActivateCommand : ICommand
    {
        public void Execute(object parameter)
        {
            Utilities.GetAppBar.Activate();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
