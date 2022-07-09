using System;
using LaunchpadApi.Entities;
using MoundBoard.Core;

namespace MoundBoard.Actions;

public class NextColorAction
{
    // public static void NextColor(LaunchpadButton button)
    // {
    // button.Color = new Colors() + 1;
    // // }

    public void NextColor(LaunchpadButton button)
    {
        button.Color = new Colors() + 1;
    }

    public NextColorAction(LaunchpadButton button)
    {
        if (button.Color > (Colors?)64)
        {
            button.Color = 0;
        }
        else button.Color = button.Color + 1;
    }
}