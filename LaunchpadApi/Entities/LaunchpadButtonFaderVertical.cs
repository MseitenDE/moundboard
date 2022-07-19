namespace LaunchpadApi.Entities;

public class LaunchpadButtonFaderVertical : LaunchpadButton
{
    public LaunchpadButtonFaderVertical(Layout layout, byte x) : base(layout, x, 1)
    {
        var buttons = layout.Buttons;
        
        buttons[x,0] = new LaunchpadButton(layout, X, Y);
        buttons[x,1] = new LaunchpadButton(layout, X, (byte)(Y + 10));
        buttons[x,2] = new LaunchpadButton(layout, X, (byte)(Y + 20));
        buttons[x,3] = new LaunchpadButton(layout, X, (byte)(Y + 30));
        buttons[x,4] = new LaunchpadButton(layout, X, (byte)(Y + 40));
        buttons[x,5] = new LaunchpadButton(layout, X, (byte)(Y + 50));
        buttons[x,6] = new LaunchpadButton(layout, X, (byte)(Y + 60));
        buttons[x,7] = new LaunchpadButton(layout, X, (byte)(Y + 70));
    }
}