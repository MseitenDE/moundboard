

using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;

namespace MoundBoard;

public partial class MappingDialogWindow : Window
{
    private Button button { get; set; }
    private RoutedEventArgs _eventArgs { get; set; }
    private System.Drawing.Color _color { get; set; }

    public MappingDialogWindow(object sender, RoutedEventArgs e)
    {
        
        InitializeComponent();
        button = (Button?)sender;
        _eventArgs = e;


    }

    private void Canvas_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        ColorDialog d = new ColorDialog();
        d.ShowDialog();
        
        SolidColorBrush brush =  new SolidColorBrush();
        brush.Color = Color.FromArgb(d.Color.A,d.Color.R,d.Color.G,d.Color.B);
        _color = d.Color;


        this.Canvas.Background = brush;
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        button.Background = this.Canvas.Background;

    }
}