namespace LaunchpadApi.Entities;

public class LayoutButtonsFader : Layout
{
    // TODO: Fader wird nicht initialisiert, das wird dir irgendwann um die Ohren fliegen 🙂
    public LaunchpadButtonFaderVertical[] Fader { get; }

    public LayoutButtonsFader(string name, Launchpad launchpad) : base(name, launchpad)
    {
        for (byte x = 0; x < launchpad.ColumnCount; x++)
        {
            for (byte y = 0; y < launchpad.RowCount; y++)
            {
                // Buttons[x, y] = new LaunchpadButton(this, x, y);
                if (Fader != null) Fader[x] = new LaunchpadButtonFaderVertical(this, x);
            }
        }
    }
}