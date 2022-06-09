namespace MoundBoard.Entities;

public class Layout
{
    public string Name { get; set; }
    public LaunchpadButton[,] Buttons { get; } = new LaunchpadButton[8, 8];

    public Layout()
    {
        for (var x = 0; x < 8; x++)
        {
            for (var y = 0; y < 8; y++)
            {
                Buttons[x, y] = new LaunchpadButton(x, y);
            }
        }
    }
}