using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using MoundBoard.Panels;

namespace MoundBoard;

public partial class MappingDialogWindow
{
    public ButtonPanel Panel { get; }

    public MappingDialogWindow(ButtonPanel panel)
    {
        Panel = panel;
        
        InitializeComponent();

        Canvas.Background = new SolidColorBrush(panel.Button.Color);
    }

    private void Canvas_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        var d = new ColorDialog();
        d.ShowDialog();
        
        Panel.Button.Color = Color.FromArgb(d.Color.A, d.Color.R, d.Color.G, d.Color.B);

        Canvas.Background = new SolidColorBrush(Panel.Button.Color);
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        Panel.Update();
    }
}