using System;
using LaunchpadApi.Entities;
using MoundBoard.Actions;
using MoundBoard.Core;

namespace MoundBoard.Utils;

internal static class LaunchpadButtonActionConverter
{
    public static NextColorAction Convert(this Core.LaunchpadButtonAction buttonAction)
    {
        return buttonAction switch
        {
            LaunchpadButtonAction.nextColor => new NextColorAction(MidiHandler.Instance.Launchpad.CurrentLayout[1,1]),
            
            _ => throw new ArgumentOutOfRangeException(nameof(buttonAction), buttonAction, null)
        };
    }
}