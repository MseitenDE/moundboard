using System.Windows.Media;

namespace MoundBoard.Entities;

public class LaunchpadButton
{
    public int X { get; }
    public int Y { get; }
    public Color Color { get; set; } = Colors.Black;

    public LaunchpadButton(int x, int y)
    {
        X = x;
        Y = y;
    }
}