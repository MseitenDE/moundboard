using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MoundBoard.Panels;
using MoundBoard.Utils;
using ColorConverter = MoundBoard.Utils.ColorConverter;

namespace MoundBoard;

public partial class MappingDialogWindow
{
    public ButtonPanel Panel { get; }

    public MappingDialogWindow(ButtonPanel panel)
    {
        Panel = panel;
        
        InitializeComponent();

        Canvas.Background = new SolidColorBrush(panel.Button.Color.Convert());
    }

    private void Canvas_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        // var d = new ColorDialog();
        // d.ShowDialog();
        
        Panel.Button.Color = ColorConverter.GetRandomColor();

        Canvas.Background = new SolidColorBrush(Panel.Button.Color.Convert());
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        Panel.Update();
    }
}