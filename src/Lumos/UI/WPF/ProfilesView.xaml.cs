using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Lumos.UI.WPF;

public partial class ProfilesView : System.Windows.Controls.UserControl
{
    public ProfilesView()
    {
        InitializeComponent();
    }

    private void CommitBrightnessTextBox(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox textBox)
        {
            textBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
        }
    }

    private void CommitBrightnessTextBoxOnEnter(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        textBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
        Keyboard.ClearFocus();
        e.Handled = true;
    }

    private void OpenInstalledAppsComboBox(object sender, System.Windows.RoutedEventArgs e)
    {
        if (InstalledAppsComboBox.IsKeyboardFocusWithin || e is MouseButtonEventArgs)
        {
            InstalledAppsComboBox.IsDropDownOpen = true;
        }
    }

}
