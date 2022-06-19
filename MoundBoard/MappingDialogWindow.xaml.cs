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

        Canvas.Background =panel.Button.Color.ConvertToBrush();
    }

    private void Canvas_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        // var d = new ColorDialog();
        // d.ShowDialog();
        
        Panel.Button.Color = ColorConverter.GetRandomColor();

        Canvas.Background =Panel.Button.Color.ConvertToBrush();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        Panel.Update();
    }
}