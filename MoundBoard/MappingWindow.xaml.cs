using System.Windows.Controls;
using MoundBoard.Entities;
using MoundBoard.Panels;

namespace MoundBoard;

public partial class MappingWindow
{
    public Layout Layout { get; }

    public MappingWindow(Layout layout)
    {
        Layout = layout;
        
        InitializeComponent();

        for (var x = 0; x < 8; x++)
        {
            for (var y = 0; y < 8; y++)
            {
                var panel = new ButtonPanel(layout.Buttons[x, y]);
                
                panel.SetValue(Grid.RowProperty, x + 1);
                panel.SetValue(Grid.ColumnProperty, y + 1);

                GridButtons.Children.Add(panel);
            }
        }
    }
}