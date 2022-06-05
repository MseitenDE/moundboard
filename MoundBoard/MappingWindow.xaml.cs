using System.Windows;
using System.Windows.Forms;

namespace MoundBoard;

public partial class MappingWindow : Window
{
    public MappingWindow()
    {
        InitializeComponent();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        Window dialogwindow = new MappingDialogWindow(sender,e);
        dialogwindow.Show();
        
    }
}