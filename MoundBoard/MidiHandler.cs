using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
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
        string[] sysExMessages = { "F0 00 20 29 02 0E 0E 01 F7"};

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

    private void OnClockSignal(MidiInPort sender, MidiMessageReceivedEventArgs args)
    {
        if(_midiOutPort == null ) return;
        var receivedMidiMessage = args.Message;

        Debug.WriteLine(receivedMidiMessage.Timestamp.ToString());
        
        List<IMidiMessage> messages = new List<IMidiMessage>();
        if (receivedMidiMessage.Type == MidiMessageType.TimingClock)
        {
            if (_lightshowState.Equals("startup"))
            {
                
                switch (_enumeration)
                {
                    case 0:
                        byte channel = 0x00;
                        byte note = 0x0B;
                        byte velocity = 0x05;
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
                        break;
                    default:
                        //messages = null;
                        break;
                }
                _enumeration++;
                foreach (var VARIABLE in messages)
                {
                    _midiOutPort.SendMessage(VARIABLE);
                }
            }
        }
    }
}