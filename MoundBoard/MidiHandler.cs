using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using Windows.Storage.Streams;
using ListBox = System.Windows.Controls.ListBox;

namespace MoundBoard;

public class MidiHandler
{
    public static bool isMartin;
    private readonly ListBox _midiInPortListBox;
    private readonly ListBox _midiOutPortListBox;
    private readonly MyMidiDeviceWatcher _inputDeviceWatcher;
    private readonly MyMidiDeviceWatcher _outputDeviceWatcher;
    private MidiInPort _midiInPort;
    private IMidiOutPort _midiOutPort;

    private string _lightshowState;
    private int _enumeration;

    public MidiHandler(Dispatcher dispatcher, ListBox midiInPortListBox, ListBox midiOutPortListBox)
    {
        _midiInPortListBox = midiInPortListBox;
        _midiOutPortListBox = midiOutPortListBox;

        _inputDeviceWatcher = new MyMidiDeviceWatcher(MidiInPort.GetDeviceSelector(), midiInPortListBox, dispatcher);
        _inputDeviceWatcher.StartWatcher();

        _outputDeviceWatcher = new MyMidiDeviceWatcher(MidiOutPort.GetDeviceSelector(), midiOutPortListBox, dispatcher);
        _outputDeviceWatcher.StartWatcher();
    }

    private async Task EnumerateMidiInputDevices()
    {
        await EnumerateMidiDevices(MidiInPort.GetDeviceSelector(), _midiInPortListBox);
    }

    private async Task EnumerateMidiOutputDevices()
    {
        await EnumerateMidiDevices(MidiOutPort.GetDeviceSelector(), _midiOutPortListBox);
    }

    private static async Task EnumerateMidiDevices(string midiQueryString, ItemsControl listBox)
    {
        var midiDevices = await DeviceInformation.FindAllAsync(midiQueryString);

        listBox.Items.Clear();

        // Return if no external devices are connected
        if (midiDevices.Count == 0)
        {
            listBox.Items.Add("No MIDI output devices found!");
            listBox.IsEnabled = false;
            return;
        }

        // Else, add each connected input device to the list
        foreach (var deviceInfo in midiDevices)
        {
            listBox.Items.Add(deviceInfo.Name);
        }

        listBox.IsEnabled = true;
    }

    public async void OnSelectedInputChanged()
    {
        var deviceInfoCollection = _inputDeviceWatcher.DeviceInformationCollection;

        if (deviceInfoCollection == null)
        {
            return;
        }

        var devInfo = deviceInfoCollection[_midiInPortListBox.SelectedIndex];

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

        _lightshowState = "startup";
        _enumeration = 0;
    }

    public async void OnSelectedOutputChanged()
    {
        var deviceInfoCollection = _outputDeviceWatcher.DeviceInformationCollection;

        if (deviceInfoCollection == null)
        {
            return;
        }

        var devInfo = deviceInfoCollection[_midiOutPortListBox.SelectedIndex];

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

        var dataWriter = new DataWriter();
        string[] sysExMessages = { "F0 00 20 29 02 0E 0E 01 F7" };

        foreach (var sysExMessage in sysExMessages)
        {
            var sysExMessageLength = sysExMessage.Length;


            int loopCount = (sysExMessageLength + 1) / 3;

            for (int i = 0; i < loopCount; i++)
            {
                var messageString = sysExMessage.Substring(3 * i, 2);
                var messageByte = Convert.ToByte(messageString, 16);
                dataWriter.WriteByte(messageByte);
            }

            IMidiMessage midiMessageToSend = new MidiSystemExclusiveMessage(dataWriter.DetachBuffer());
            _midiOutPort.SendMessage(midiMessageToSend);
        }
    }

    private void MidiInPort_MessageReceived(MidiInPort sender, MidiMessageReceivedEventArgs args)
    {
        var receivedMidiMessage = args.Message;

        Debug.WriteLine(receivedMidiMessage.Timestamp.ToString());

        if (receivedMidiMessage.Type == MidiMessageType.NoteOn)
        {
            var midiNote = (MidiNoteOnMessage)receivedMidiMessage;
            Debug.WriteLine(midiNote.Channel);
            Debug.WriteLine(midiNote.Note);
            Debug.WriteLine(midiNote.Velocity);
        }
    }

    public void MidiOutPort_sendMessage()
    {
        IMidiMessage midiMessageToSend = null;
        var dataWriter = new DataWriter();
        var sysExMessage = "F0 00 20 29 02 0E 0E 1 F7";
        if (isMartin)
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
        int loopCount = (sysExMessageLength + 1) / 3;

        // Expecting a string of format "F0 NN NN NN NN.... F7", where NN is a byte in hex
        for (int i = 0; i < loopCount; i++)
        {
            var messageString = sysExMessage.Substring(3 * i, 2);
            var messageByte = Convert.ToByte(messageString, 16);
            dataWriter.WriteByte(messageByte);
        }

        midiMessageToSend = new MidiSystemExclusiveMessage(dataWriter.DetachBuffer());
        _midiOutPort.SendMessage(midiMessageToSend);
    }

    public void SetLightShowState(string state)
    {
        _lightshowState = state;
        _enumeration = 0;
    }

    private void OnClockSignal(MidiInPort sender, MidiMessageReceivedEventArgs args)
    {
        if (_midiOutPort == null) return;
        var receivedMidiMessage = args.Message;

        Debug.WriteLine(receivedMidiMessage.Timestamp.ToString());

        List<IMidiMessage> messages = new List<IMidiMessage>();
        if (receivedMidiMessage.Type == MidiMessageType.TimingClock)
        {
            if (_lightshowState.Equals("startup"))
            {
                byte channel = 0x00;
                byte note = 0x0B;
                byte velocity = 0x05;
                switch (_enumeration)
                {
                    case 0:


                        messages.Add(new MidiNoteOnMessage(channel, note, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 21, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 31, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 41, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 72, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 44, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 66, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 76, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 77, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 67, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 47, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 37, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 27, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 69, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 79, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 89, velocity));
                        break;
                    case 8:
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 21, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 31, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 41, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 72, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 44, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 66, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 76, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 77, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 67, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 47, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 37, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 27, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 69, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 79, velocity));

                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 20, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 30, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 40, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 70, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 43, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 64, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 75, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 76, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 66, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 46, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 36, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 26, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 28, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 38, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 48, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 68, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 78, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 89, velocity));

                        break;
                    case 16:

                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 20, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 30, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 40, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 70, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 43, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 64, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 75, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 76, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 66, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 46, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 36, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 26, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 28, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 38, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 48, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 68, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 78, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 89, velocity));
                        ///////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 70, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 42, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 64, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 74, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 75, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 45, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 35, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 25, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 67, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 77, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 89, velocity));
                        break;
                    case 24:
                        messages.Add(new MidiNoteOffMessage(channel, 70, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 42, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 64, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 74, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 75, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 45, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 35, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 25, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 67, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 77, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 89, velocity));

                        ///////////////
                        messages.Add(new MidiNoteOnMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 41, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 73, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 74, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 64, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 44, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 34, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 24, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 66, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 76, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 29, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 39, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 49, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 89, velocity));


                        break;
                    case 32:
                        messages.Add(new MidiNoteOffMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 41, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 73, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 74, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 64, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 44, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 34, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 24, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 66, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 76, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 29, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 39, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 49, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 89, velocity));
                        /////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 40, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 72, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 73, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 43, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 33, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 23, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 75, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 28, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 38, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 48, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 89, velocity));
                        break;
                    case 40:
                        messages.Add(new MidiNoteOffMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 40, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 72, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 73, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 43, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 33, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 23, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 75, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 28, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 38, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 48, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 89, velocity));
                        ///////////////////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 72, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 42, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 32, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 22, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 64, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 74, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 27, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 37, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 47, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 89, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 79, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 69, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 49, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 39, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 29, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 19, velocity));

                        break;
                    case 48:

                        messages.Add(new MidiNoteOffMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 72, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 42, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 32, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 22, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 64, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 74, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 27, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 37, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 47, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 89, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 79, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 69, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 49, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 39, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 29, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 19, velocity));
                        ////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 70, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 41, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 31, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 21, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 73, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 26, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 36, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 46, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 78, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 68, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 48, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 38, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 28, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 89, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 19, velocity));

                        break;
                    case 56:
                        messages.Add(new MidiNoteOffMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 70, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 41, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 31, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 21, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 73, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 26, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 36, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 46, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 78, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 68, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 48, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 38, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 28, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 89, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 19, velocity));
                        /////////////////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 70, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 40, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 30, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 20, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 72, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 25, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 35, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 45, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 77, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 67, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 47, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 37, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 27, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 89, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 19, velocity));
                        break;
                    case 64:
                        messages.Add(new MidiNoteOffMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 70, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 40, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 30, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 20, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 72, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 25, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 35, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 45, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 77, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 67, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 47, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 37, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 27, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 89, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 19, velocity));
                        /////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 24, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 34, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 44, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 76, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 66, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 46, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 36, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 26, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 89, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 19, velocity));
                        break;
                    case 72:
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 24, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 34, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 44, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 76, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 66, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 46, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 36, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 26, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 89, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 19, velocity));
                        /////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 70, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 23, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 33, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 43, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 75, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 45, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 35, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 25, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 29, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 39, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 49, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 69, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 79, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 89, velocity));

                        break;
                    case 80:
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 70, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 23, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 33, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 43, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 75, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 45, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 35, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 25, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 29, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 39, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 49, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 69, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 79, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 89, velocity));
                        //////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 22, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 32, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 42, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 74, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 64, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 44, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 34, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 24, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 28, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 38, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 48, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 68, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 78, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 19, velocity));
                        break;
                    case 88:
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 22, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 32, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 42, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 74, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 64, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 44, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 34, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 24, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 28, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 38, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 48, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 68, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 78, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 19, velocity));

                        //////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 21, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 31, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 41, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 73, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 43, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 33, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 23, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 27, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 37, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 47, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 67, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 77, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 19, velocity));


                        break;
                    case 96:
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 21, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 31, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 41, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 73, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 43, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 33, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 23, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 27, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 37, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 47, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 67, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 77, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 19, velocity));


                        //////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 20, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 30, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 40, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 72, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 42, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 32, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 22, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 26, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 36, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 46, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 66, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 76, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 19, velocity));


                        break;
                    case 104:
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 20, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 30, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 40, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 72, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 42, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 32, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 22, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 26, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 36, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 46, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 66, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 76, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 19, velocity));
                        /////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 41, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 31, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 21, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 25, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 35, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 45, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 75, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 29, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 39, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 49, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 69, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 79, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 89, velocity));


                        break;
                    case 112:
                        messages.Add(new MidiNoteOffMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 41, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 31, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 21, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 25, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 35, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 45, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 75, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 29, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 39, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 49, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 69, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 79, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 89, velocity));
                        ////////////////////////////////////////////////////////////////////////

                        messages.Add(new MidiNoteOnMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 70, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 40, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 30, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 20, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 24, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 34, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 44, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 64, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 74, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 28, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 38, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 48, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 68, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 78, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 79, velocity));
                        break;
                    case 120:
                        messages.Add(new MidiNoteOffMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 70, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 40, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 30, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 20, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 24, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 34, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 44, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 64, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 74, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 28, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 38, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 48, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 68, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 78, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 79, velocity));
                        ////////////////////////////////////////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 23, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 33, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 43, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 73, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 27, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 37, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 47, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 67, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 77, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 78, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 69, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 59, velocity));
                        break;
                    case 128:
                        messages.Add(new MidiNoteOffMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 23, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 33, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 43, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 73, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 27, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 37, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 47, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 67, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 77, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 78, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 69, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 59, velocity));
                        /////////////////////////////////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 22, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 32, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 42, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 72, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 26, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 36, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 46, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 66, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 76, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 77, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 68, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 49, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 39, velocity));
                        break;
                    case 136:
                        messages.Add(new MidiNoteOffMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 22, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 32, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 42, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 72, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 26, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 36, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 46, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 66, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 76, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 77, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 68, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 49, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 39, velocity));
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 21, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 31, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 41, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 25, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 35, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 45, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 75, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 76, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 67, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 48, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 38, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 29, velocity));
                        break;
                    case 144:
                        messages.Add(new MidiNoteOffMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 21, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 31, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 41, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 25, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 35, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 45, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 75, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 76, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 67, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 48, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 38, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 29, velocity));
                        /////////////////////////////////////////////////////////////////////////////////////////      
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 20, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 30, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 40, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 70, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 24, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 34, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 44, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 64, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 74, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 75, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 66, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 47, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 37, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 28, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 29, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 39, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 49, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 69, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 79, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 89, velocity));
                        break;
                    case 152:
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 20, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 30, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 40, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 70, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 24, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 34, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 44, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 64, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 74, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 75, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 66, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 47, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 37, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 28, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 29, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 39, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 49, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 69, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 79, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 89, velocity));
                        /////////////////////////////////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 23, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 33, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 43, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 73, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 74, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 46, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 36, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 27, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 28, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 38, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 48, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 68, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 78, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 89, velocity));
                        break;
                    case 160:
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 23, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 33, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 43, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 73, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 83, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 74, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 46, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 36, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 27, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 28, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 38, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 48, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 68, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 78, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 89, velocity));
                        //////////////////////////////////////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 22, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 32, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 42, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 72, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 73, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 64, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 45, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 35, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 26, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 27, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 37, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 47, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 67, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 77, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 89, velocity));
                        break;
                    case 168:
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 22, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 32, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 42, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 72, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 82, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 73, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 64, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 45, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 35, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 26, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 27, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 37, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 47, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 57, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 67, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 77, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 19, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 89, velocity));
                        ///////////////////////////////////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 21, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 31, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 41, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 72, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 44, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 34, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 25, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 26, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 36, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 46, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 66, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 76, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 29, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 79, velocity));
                        break;
                    case 176:
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 21, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 31, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 41, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 72, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 63, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 44, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 34, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 25, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 26, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 36, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 46, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 56, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 66, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 76, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 18, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 88, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 29, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 79, velocity));
                        ////////////////////////////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 20, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 30, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 40, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 70, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 43, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 33, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 24, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 25, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 35, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 45, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 75, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 28, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 78, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 39, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 49, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 69, velocity));

                        break;
                    case 184:
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 20, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 30, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 40, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 50, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 60, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 70, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 62, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 43, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 33, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 24, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 25, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 35, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 45, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 55, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 65, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 75, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 17, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 87, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 28, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 78, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 39, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 49, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 59, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 69, velocity));
                        ///////////////////////////////////////////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 70, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 61, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 51, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 42, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 32, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 23, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 24, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 34, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 44, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 54, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 64, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 74, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 84, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 85, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 16, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 86, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 27, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 77, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 38, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 48, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 58, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 68, velocity));
                        break;
                    case 192:
                        messages.Add(new MidiNoteOffMessage(channel, 70, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 61, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 51, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 42, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 32, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 23, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 24, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 34, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 44, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 54, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 64, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 74, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 84, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 85, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 16, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 86, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 27, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 77, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 38, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 48, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 58, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 68, velocity)); 
                        //////////////////////////////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 60, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 50, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 41, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 31, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 22, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 23, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 33, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 43, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 53, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 63, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 73, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 83, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 84, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 15, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 85, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 26, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 76, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 37, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 47, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 57, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 67, velocity)); 
                        break;
                    case 200:
                        messages.Add(new MidiNoteOffMessage(channel, 60, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 50, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 41, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 31, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 22, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 23, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 33, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 43, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 53, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 63, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 73, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 83, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 84, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 15, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 85, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 26, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 76, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 37, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 47, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 57, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 67, velocity));  
                        //////////////////////////////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 40, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 30, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 21, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 22, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 32, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 42, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 52, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 62, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 72, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 82, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 83, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 14, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 84, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 25, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 75, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 36, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 46, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 56, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 66, velocity));  
                        break;
                    case 208:
                        messages.Add(new MidiNoteOffMessage(channel, 40, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 30, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 21, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 22, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 32, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 42, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 52, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 62, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 72, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 82, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 83, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 14, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 84, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 25, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 75, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 36, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 46, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 56, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 66, velocity)); 
                        ////////////////////////////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 20, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 21, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 31, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 41, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 51, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 61, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 71, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 81, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 82, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 13, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 83, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 24, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 74, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 35, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 45, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 55, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 65, velocity)); 
                        break;
                    case 216:
                        messages.Add(new MidiNoteOffMessage(channel, 20, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 21, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 31, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 41, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 51, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 61, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 71, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 81, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 82, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 13, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 83, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 24, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 74, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 35, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 45, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 55, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 65, velocity));
                        ///////////////////////////////////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 20, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 30, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 40, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 50, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 60, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 70, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 80, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 81, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 12, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 82, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 23, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 73, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 34, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 44, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 54, velocity)); 
                        messages.Add(new MidiNoteOnMessage(channel, 64, velocity)); 
                        break;
                    case 224:
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 20, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 30, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 40, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 50, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 60, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 70, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 80, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 81, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 12, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 82, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 23, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 73, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 34, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 44, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 54, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 64, velocity)); 
                        ////////////////////////////////////////////////////////////////////////////////        
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 80, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 11, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 81, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 22, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 72, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 33, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 43, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 53, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 63, velocity));  
                        
                        break;
                    case 232:
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 11, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 81, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 22, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 72, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 33, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 43, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 53, velocity));
                        messages.Add(new MidiNoteOffMessage(channel, 63, velocity));
                        ///////////////////////////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 10, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 80, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 21, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 71, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 32, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 42, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 52, velocity));
                        messages.Add(new MidiNoteOnMessage(channel, 62, velocity));
                        
                        break;
                    case 240:
                        messages.Add(new MidiNoteOffMessage(channel, 10, velocity));     
                        messages.Add(new MidiNoteOffMessage(channel, 80, velocity));     
                        messages.Add(new MidiNoteOffMessage(channel, 21, velocity));     
                        messages.Add(new MidiNoteOffMessage(channel, 71, velocity));     
                        messages.Add(new MidiNoteOffMessage(channel, 32, velocity));     
                        messages.Add(new MidiNoteOffMessage(channel, 42, velocity));     
                        messages.Add(new MidiNoteOffMessage(channel, 52, velocity));     
                        messages.Add(new MidiNoteOffMessage(channel, 62, velocity));    
                        ///////////////////////////////////////////////////////////////////////////  
                        messages.Add(new MidiNoteOnMessage(channel, 20, velocity));     
                        messages.Add(new MidiNoteOnMessage(channel, 70, velocity));     
                        messages.Add(new MidiNoteOnMessage(channel, 31, velocity));     
                        messages.Add(new MidiNoteOnMessage(channel, 41, velocity));     
                        messages.Add(new MidiNoteOnMessage(channel, 51, velocity));     
                        messages.Add(new MidiNoteOnMessage(channel, 61, velocity));     
                        break;
                    case 248:
                        messages.Add(new MidiNoteOffMessage(channel, 20, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 70, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 31, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 41, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 51, velocity));  
                        messages.Add(new MidiNoteOffMessage(channel, 61, velocity));  
                        //////////////////////////////////////////////////////////////////////////////
                        messages.Add(new MidiNoteOnMessage(channel, 30, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 40, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 50, velocity));  
                        messages.Add(new MidiNoteOnMessage(channel, 60, velocity));  
                        break;
                    case 256:
                        messages.Add(new MidiNoteOffMessage(channel, 30, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 40, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 50, velocity)); 
                        messages.Add(new MidiNoteOffMessage(channel, 60, velocity)); 
                        break;
                        _lightshowState = "";
                    default:
                        //messages = null;
                        break;
                }

                if (!_lightshowState.Equals("")) _enumeration++;

                foreach (var VARIABLE in messages)
                {
                    _midiOutPort.SendMessage(VARIABLE);
                }
            }
        }
    }
}