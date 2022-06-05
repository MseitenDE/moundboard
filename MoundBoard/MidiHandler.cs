using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;

namespace MoundBoard;

public class MidiHandler
{
    private readonly ListBox _midiInPortListBox;
    private readonly ListBox _midiOutPortListBox;
    private readonly MyMidiDeviceWatcher _inputDeviceWatcher;
    private readonly MyMidiDeviceWatcher _outputDeviceWatcher;
    private MidiInPort _midiInPort;
    private IMidiOutPort _midiOutPort;

    public MidiHandler(Dispatcher dispatcher, ListBox midiInPortListBox, ListBox midiOutPortListBox)
    {
        _midiInPortListBox = midiInPortListBox;
        _midiOutPortListBox = midiOutPortListBox;
        
        _inputDeviceWatcher = new MyMidiDeviceWatcher(MidiInPort.GetDeviceSelector(), midiInPortListBox, dispatcher);
        _inputDeviceWatcher.StartWatcher();

        _outputDeviceWatcher =new MyMidiDeviceWatcher(MidiOutPort.GetDeviceSelector(), midiOutPortListBox, dispatcher);
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
        }
    }
    
    private void MidiInPort_MessageReceived(MidiInPort sender, MidiMessageReceivedEventArgs args)
    {
        var receivedMidiMessage = args.Message;

        Debug.WriteLine(receivedMidiMessage.Timestamp.ToString());

        if (receivedMidiMessage.Type == MidiMessageType.NoteOn)
        {
            var midiNote = (MidiNoteOnMessage) receivedMidiMessage;
            Debug.WriteLine(midiNote.Channel);
            Debug.WriteLine(midiNote.Note);
            Debug.WriteLine(midiNote.Velocity);
        }
    }

    private void MidiOutPort_sendMessage()
    {
        byte channel = 0;
        byte controller = 0;
        byte[] controlValue = { 240, 0, 32, 41, 2, 14, 14, 0, 247 };


        // IMidiMessage message = new MidiControlChangeMessage(channel,controller, controlValue);

        foreach (var VARIABLE in controlValue)
        {
            IMidiMessage message = new MidiControlChangeMessage(channel,controller, VARIABLE);
            _midiOutPort.SendMessage(message);
        }

        
        // IMidiMessage NoteMessage = new MidiNoteOnMessage(0, 64, 100);
        // _midiOutPort.SendMessage(NoteMessage);



    }
}