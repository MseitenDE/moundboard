using System.Windows.Controls;
using LaunchpadApi.Entities;
using MoundBoard.Panels;

namespace MoundBoard;

public partial class MappingWindow
{
    public LayoutButtons LayoutButtons { get; }

    public MappingWindow(LayoutButtons layoutButtons)
    {
        LayoutButtons = layoutButtons;
        
        InitializeComponent();

        for (var x = 0; x < 8; x++)
        {
            for (var y = 0; y < 8; y++)
            {
                var panel = new ButtonPanel(layoutButtons.Buttons[x, y]);
                
                panel.SetValue(Grid.RowProperty, x + 1);
                panel.SetValue(Grid.ColumnProperty, y + 1);

                GridButtons.Children.Add(panel);
            }
        }
    }
}