using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Windows.Devices.Midi;
using Buffer = Windows.Storage.Streams.Buffer;

namespace MoundBoard;

public partial class MidiIOSettings : Window
{
    public MidiHandler MidiHandler { get; }

    public MidiIOSettings()
    {
        InitializeComponent();
        
        MidiHandler = new MidiHandler(Dispatcher, MidiInPortListBox, MidiOutPortListBox);

    }
    private void MidiInPortListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        MidiHandler.OnSelectedInputChanged();
    }

    private void MidiOutPortListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        MidiHandler.OnSelectedOutputChanged();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
       MidiHandler.MidiOutPort_sendMessage();

        
    }
}