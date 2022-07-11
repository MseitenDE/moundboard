﻿using Windows.Devices.Midi;
using LaunchpadApi.Animations;
using LaunchpadApi.Utils;
using MoundBoard.Core;

namespace LaunchpadApi.Entities;

public class LaunchpadButtonFaderVertical
{
    private Colors? _color = Colors.Green;
    public Layout Layout { get; }
    public byte X { get; }
    public byte Y { get; }
    public LaunchpadButton[,] Buttons { get; }
    public ButtonEffect Effect { get; set; }
    private byte NoteIndex => (byte) (X + Y * Layout.Launchpad.RowCount);

    public Colors? Color
    {
        get => _color;
        set
        {
            if (_color == value) return;
            _color = value ?? Colors.Black;
            SendUpdate();
        }
    }

    public LaunchpadButtonFaderVertical(Layout layout, byte x, LaunchpadButton[,] buttons)
    {
        Layout = layout;
        X = x;
        Buttons = buttons;
        Y = 1;
        Buttons![x,0] = new LaunchpadButton(layout, X, Y);
        Buttons[x,1] = new LaunchpadButton(layout, X, (byte)(Y + 10));
        Buttons[x,2] = new LaunchpadButton(layout, X, (byte)(Y + 20));
        Buttons[x,3] = new LaunchpadButton(layout, X, (byte)(Y + 30));
        Buttons[x,4] = new LaunchpadButton(layout, X, (byte)(Y + 40));
        Buttons[x,5] = new LaunchpadButton(layout, X, (byte)(Y + 50));
        Buttons[x,6] = new LaunchpadButton(layout, X, (byte)(Y + 60));
        Buttons[x,7] = new LaunchpadButton(layout, X, (byte)(Y + 70));
    }

    /// <summary>
    /// Updates the button on the launchpad, ignoring if 
    /// </summary>
    public void SendUpdate()
    {
        if (!Layout.IsActive) return;

        var noteMessage = new MidiNoteOnMessage((byte) Effect, NoteIndex, Color.Convert());
        Layout.Launchpad.OutPort.SendMessage(noteMessage);
    }
}