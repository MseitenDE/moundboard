using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Threading;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using Windows.Storage.Streams;
using LaunchpadApi.Animations;
using LaunchpadApi.Entities;
using MoundBoard.Core;
using MoundBoard.Utils;
using ListBox = System.Windows.Controls.ListBox;

namespace MoundBoard;

public class MidiHandler
{
    public static MidiHandler Instance { get; private set; }
    public static bool IsMk2 { get; set; }
    public Launchpad Launchpad { get; private set; }
    private readonly MyMidiDeviceWatcher _inputDeviceWatcher;
    private readonly MyMidiDeviceWatcher _outputDeviceWatcher;
    private MidiInPort _midiInPort;
    private IMidiOutPort _midiOutPort;

    public MidiHandler(Dispatcher dispatcher, ListBox midiInPortListBox, ListBox midiOutPortListBox)
    {
        Instance = this;

        _inputDeviceWatcher = new MyMidiDeviceWatcher(MidiInPort.GetDeviceSelector(), midiInPortListBox, dispatcher);
        _inputDeviceWatcher.StartWatcher();

        _outputDeviceWatcher = new MyMidiDeviceWatcher(MidiOutPort.GetDeviceSelector(), midiOutPortListBox, dispatcher);
        _outputDeviceWatcher.StartWatcher();
    }

    public static async Task<DeviceInformationCollection> EnumerateMidiInputDevices()
    {
        return await EnumerateMidiDevices(MidiInPort.GetDeviceSelector());
    }

    public static async Task<DeviceInformationCollection> EnumerateMidiOutputDevices()
    {
        return await EnumerateMidiDevices(MidiOutPort.GetDeviceSelector());
    }

    private static async Task<DeviceInformationCollection> EnumerateMidiDevices(string midiQueryString)
    {
        return await DeviceInformation.FindAllAsync(midiQueryString);
    }

    public async void OnSelectedInputChanged(int index)
    {
        var deviceInfoCollection = _inputDeviceWatcher.DeviceInformationCollection;

        if (deviceInfoCollection == null)
        {
            return;
        }

        var devInfo = deviceInfoCollection[index];

        if (devInfo == null)
        {
            return;
        }

        _midiInPort = await MidiInPort.FromIdAsync(devInfo.Id);

        if (_midiInPort == null)
        {
            Debug.WriteLine("Unable to create MidiInPort from input device");
            return;
        }


        _midiInPort.MessageReceived += MidiInPort_MessageReceived;
        _midiInPort.MessageReceived += OnClockSignal;

        MarkAnimation.SetLightShowState("startup");
    }

    public async void OnSelectedOutputChanged(int index)
    {
        var deviceInfoCollection = _outputDeviceWatcher.DeviceInformationCollection;

        if (deviceInfoCollection == null)
        {
            return;
        }

        var devInfo = deviceInfoCollection[index];

        if (devInfo == null)
        {
            return;
        }

        _midiOutPort = await MidiOutPort.FromIdAsync(devInfo.Id);

        if (_midiOutPort == null)
        {
            Debug.WriteLine("Unable to create MidiOutPort from output device");
            return;
        }

        Launchpad = new Launchpad(_midiInPort, _midiOutPort);

        MidiOutPort_sendMessage();

        PlayStartupAnimation();
    }

    private void MidiInPort_MessageReceived(MidiInPort sender, MidiMessageReceivedEventArgs args)
    {
        var receivedMidiMessage = args.Message;

        Debug.WriteLine(receivedMidiMessage.Timestamp.ToString());

        if (receivedMidiMessage.Type == MidiMessageType.NoteOn)
        {
            var midiNote = (MidiNoteOnMessage)receivedMidiMessage;


            var X = midiNote.Note % 10;
            var Y = (midiNote.Note - X) / 10;

            LaunchpadButtonActionConverter.Convert(Launchpad.CurrentLayout[X,Y].LaunchpadButtonAction);
        }
    }

    private void MidiOutPort_sendMessage()
    {
        var dataWriter = new DataWriter();
        // TODO @Mark Changed 1 to 01
        var sysExMessage = "F0 00 20 29 02 0E 0E 01 F7";
        if (IsMk2)
        {
            sysExMessage = "F0 00 20 29 02 18 0E 01 F7";
        }


        var sysExMessageLength = sysExMessage.Length;

        // Do not send a blank SysEx message
        if (sysExMessageLength == 0)
        {
            return;
        }

        // SysEx messages are two characters long with 1-character space in between them
        // So we add 1 to the message length, so that it is perfectly divisible by 3
        // The loop count tracks the number of individual message pieces
        var loopCount = (sysExMessageLength + 1) / 3;

        // Expecting a string of format "F0 NN NN NN NN.... F7", where NN is a byte in hex
        for (var i = 0; i < loopCount; i++)
        {
            var messageString = sysExMessage.Substring(3 * i, 2);
            var messageByte = Convert.ToByte(messageString, 16);
            dataWriter.WriteByte(messageByte);
        }

        var midiMessageToSend = new MidiSystemExclusiveMessage(dataWriter.DetachBuffer());
        _midiOutPort.SendMessage(midiMessageToSend);
    }

    private void OnClockSignal(MidiInPort sender, MidiMessageReceivedEventArgs args)
    {
        if (_midiOutPort == null) return;
        var receivedMidiMessage = args.Message;

        Debug.WriteLine(receivedMidiMessage.Timestamp.ToString());

        if (receivedMidiMessage.Type == MidiMessageType.TimingClock)
        {
            MarkAnimation.PlayFrame(_midiOutPort);
        }
    }

    public void PlayStartupAnimation()
    {
        var layout = new LayoutButtons("Startup Animation", Launchpad);

        Launchpad.CurrentLayout = layout;
        layout.SetSolidColor(Colors.Black);
        var colors = new[,]
        {
            { Colors.Red, Colors.Black, Colors.Blue, Colors.Yellow, Colors.Green, Colors.Red, Colors.Red, Colors.Red },
            { Colors.Red, Colors.Black, Colors.Blue, Colors.Yellow, Colors.Green, Colors.Red, Colors.Red, Colors.Red },
            { Colors.Red, Colors.Black, Colors.Blue, Colors.Yellow, Colors.Green, Colors.Red, Colors.Red, Colors.Red },
            { Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black},
            { Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black},
            { Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black},
            { Colors.Red, Colors.Black, Colors.Blue, Colors.Yellow, Colors.Green, Colors.Red, Colors.Red, Colors.Red },
            { Colors.Red, Colors.Black, Colors.Blue, Colors.Yellow, Colors.Green, Colors.Red, Colors.Red, Colors.Red },
            { Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black},
            { Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black}
        };
        var shiftAnimation = new ShiftAnimation(100, colors);
        shiftAnimation.Start(layout);
    }
}