using System.ComponentModel;
using System.Windows;
using Lumos.ViewModels;

namespace Lumos.UI.WPF;

public partial class DashboardWindow : Window
{
    public DashboardWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Hide the window instead of closing it to keep the app running in the system tray
        e.Cancel = true;
        Hide();
    }
}
