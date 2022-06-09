using System.Windows.Input;
using System.Windows.Media;
using MoundBoard.Entities;

namespace MoundBoard.Panels;

public partial class ButtonPanel
{
    public LaunchpadButton Button { get; }

    public ButtonPanel(LaunchpadButton button)
    {
        Button = button;
        
        InitializeComponent();
        
        Update();
    }

    public void Update()
    {
        Background = new SolidColorBrush(Button.Color);
    }

    private void ButtonPanel_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        new MappingDialogWindow(this).Show();
    }
}