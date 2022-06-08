using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Threading;
using Windows.Devices.Midi;
using Buffer = Windows.Storage.Streams.Buffer;

namespace MoundBoard;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }


    private void IOButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        Window midiIoSettingsWindow = new MidiIOSettings();
        midiIoSettingsWindow.Show();
    }

    private void MappingButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        Window mappingWindow = new MappingWindow();
        mappingWindow.Show();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        
    }

    private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
    {
        MidiHandler.isMartin = true;
    }

    private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
    {
        MidiHandler.isMartin = false;
    }
}