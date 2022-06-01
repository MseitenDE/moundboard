using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;

namespace MoundBoard;

public partial class MainWindow
{
    MyMidiDeviceWatcher inputDeviceWatcher;
    MyMidiDeviceWatcher outputDeviceWatcher;
    MidiInPort midiInPort;
    IMidiOutPort midiOutPort;
    
    public MainWindow()
    {
        
        inputDeviceWatcher =
            new MyMidiDeviceWatcher(MidiInPort.GetDeviceSelector(), midiInPortListBox, Dispatcher);

        inputDeviceWatcher.StartWatcher();

        outputDeviceWatcher =
            new MyMidiDeviceWatcher(MidiOutPort.GetDeviceSelector(), midiOutPortListBox, Dispatcher);

        outputDeviceWatcher.StartWatcher();
        InitializeComponent();
    }
    
    
    
    //Ab da hab ich versucht die dumme dokumentation zu verstehen
    
   
    
    
    
    
    
    
    //
    
    private async Task EnumerateMidiInputDevices()
    {
        // Find all input MIDI devices
        string midiInputQueryString = MidiInPort.GetDeviceSelector();
        DeviceInformationCollection midiInputDevices = await DeviceInformation.FindAllAsync(midiInputQueryString);

        midiInPortListBox.Items.Clear();

        // Return if no external devices are connected
        if (midiInputDevices.Count == 0)
        {
            this.midiInPortListBox.Items.Add("No MIDI input devices found!");
            this.midiInPortListBox.IsEnabled = false;
            return;
        }

        // Else, add each connected input device to the list
        foreach (DeviceInformation deviceInfo in midiInputDevices)
        {
            this.midiInPortListBox.Items.Add(deviceInfo.Name);
        }
        this.midiInPortListBox.IsEnabled = true;
    }
    private async Task EnumerateMidiOutputDevices()
    {

        // Find all output MIDI devices
        string midiOutportQueryString = MidiOutPort.GetDeviceSelector();
        DeviceInformationCollection midiOutputDevices = await DeviceInformation.FindAllAsync(midiOutportQueryString);

        midiOutPortListBox.Items.Clear();

        // Return if no external devices are connected
        if (midiOutputDevices.Count == 0)
        {
            this.midiOutPortListBox.Items.Add("No MIDI output devices found!");
            this.midiOutPortListBox.IsEnabled = false;
            return;
        }

        // Else, add each connected input device to the list
        foreach (DeviceInformation deviceInfo in midiOutputDevices)
        {
            this.midiOutPortListBox.Items.Add(deviceInfo.Name);
        }
        this.midiOutPortListBox.IsEnabled = true;
    }
    
    
    
    
    
    //
    
    
    
    private async void midiInPortListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var deviceInformationCollection = inputDeviceWatcher.DeviceInformationCollection;

        if (deviceInformationCollection == null)
        {
            return;
        }

        DeviceInformation devInfo = deviceInformationCollection[midiInPortListBox.SelectedIndex];

        if (devInfo == null)
        {
            return;
        }

        midiInPort = await MidiInPort.FromIdAsync(devInfo.Id);

        if (midiInPort == null)
        {
            System.Diagnostics.Debug.WriteLine("Unable to create MidiInPort from input device");
            return;
        }
        midiInPort.MessageReceived += MidiInPort_MessageReceived;
    }
    
    private void MidiInPort_MessageReceived(MidiInPort sender, MidiMessageReceivedEventArgs args)
    {
        IMidiMessage receivedMidiMessage = args.Message;

        System.Diagnostics.Debug.WriteLine(receivedMidiMessage.Timestamp.ToString());

        if (receivedMidiMessage.Type == MidiMessageType.NoteOn)
        {
            System.Diagnostics.Debug.WriteLine(((MidiNoteOnMessage)receivedMidiMessage).Channel);
            System.Diagnostics.Debug.WriteLine(((MidiNoteOnMessage)receivedMidiMessage).Note);
            System.Diagnostics.Debug.WriteLine(((MidiNoteOnMessage)receivedMidiMessage).Velocity);
        }
    }
    
    
    //
    
    private async void midiOutPortListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var deviceInformationCollection = outputDeviceWatcher.DeviceInformationCollection;

        if (deviceInformationCollection == null)
        {
            return;
        }

        DeviceInformation devInfo = deviceInformationCollection[midiOutPortListBox.SelectedIndex];

        if (devInfo == null)
        {
            return;
        }

        midiOutPort = await MidiOutPort.FromIdAsync(devInfo.Id);

        if (midiOutPort == null)
        {
            System.Diagnostics.Debug.WriteLine("Unable to create MidiOutPort from output device");
            return;
        }

    }
    
}