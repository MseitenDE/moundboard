using MoundBoard.Core;

namespace LaunchpadApi.Entities;

public class LayoutFader : Layout
{
    public Launchpad Launchpad { get; }
    public bool IsActive { get; protected internal set; }
    public string Name { get; }
    public LaunchpadButtonFaderVertical[] Fader { get; }
    

    public LayoutFader(string name, Launchpad launchpad)
    {
        Name = name;
        Launchpad = launchpad;
        
        for (byte x = 0; x < launchpad.ColumnCount; x++)
        {
            
            Fader[x] = new LaunchpadButtonFaderVertical(this, x);
            // for (byte y = 0; y < launchpad.RowCount; y++)
            // {
            //     Buttons[x, y] = new LaunchpadButton(this, x, y);
            // }
        }
    }

    public void SetSolidColor(Colors color)
    {
        ApplyForAll(button => button.Color = color);
    }

    public void ApplyForAll(Action<LaunchpadButton> button)
    {
        for (var x = 0; x < Launchpad.ColumnCount; x++)
        {
            for (var y = 0; y < Launchpad.RowCount; y++)
            {
                button.Invoke(Buttons[x, y]);
            }
        }
    }
    
    public LaunchpadButton this[int x, int y] => Buttons[x, y];

    /// <summary>
    /// Force updates all buttons
    /// </summary>
    public void Apply()
    {
        ApplyForAll(button => button.SendUpdate());
    }
}