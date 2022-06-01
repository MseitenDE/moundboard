using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using Windows.Devices.Enumeration;

namespace MoundBoard;

public class MyMidiDeviceWatcher
{
    private readonly DeviceWatcher _deviceWatcher;
    private readonly string _deviceSelectorString;
    private readonly ListBox _deviceListBox;
    private readonly Dispatcher _coreDispatcher;
    
    public DeviceInformationCollection? DeviceInformationCollection { get; private set; }
    
    public MyMidiDeviceWatcher(string midiDeviceSelectorString, ListBox midiDeviceListBox, Dispatcher dispatcher)
    {
        _deviceListBox = midiDeviceListBox;
        _coreDispatcher = dispatcher;

        _deviceSelectorString = midiDeviceSelectorString;

        _deviceWatcher = DeviceInformation.CreateWatcher(_deviceSelectorString);
        _deviceWatcher.Added += DeviceWatcher_Added;
        _deviceWatcher.Removed += DeviceWatcher_Removed;
        _deviceWatcher.Updated += DeviceWatcher_Updated;
        _deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
    }
    
    private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        await _coreDispatcher.InvokeAsync(async () =>
        {
            // Update the device list
            await UpdateDevices();
        });
    }

    private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
    {
        await _coreDispatcher.InvokeAsync(async () =>
        {
            // Update the device list
            await UpdateDevices();
        });
    }

    private async void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
    {
        // Update the device list
        await _coreDispatcher.InvokeAsync(async () =>
        {
            await UpdateDevices();
        });
    }

    private async void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        await _coreDispatcher.InvokeAsync(async () =>
        {
            // Update the device list
            await UpdateDevices();
        });
    }
    private async Task UpdateDevices()
    {
        // Get a list of all MIDI devices
        DeviceInformationCollection = await DeviceInformation.FindAllAsync(_deviceSelectorString);

        _deviceListBox.Items.Clear();

        if (!DeviceInformationCollection.Any())
        {
            _deviceListBox.Items.Add("No MIDI devices found!");
        }

        foreach (var deviceInformation in DeviceInformationCollection)
        {
            _deviceListBox.Items.Add(deviceInformation.Name);
        }
    }
    
    public void StartWatcher()
    {
        _deviceWatcher.Start();
    }
    public void StopWatcher()
    {
        _deviceWatcher.Stop();
    }
    
    ~MyMidiDeviceWatcher()
    {
        _deviceWatcher.Added -= DeviceWatcher_Added;
        _deviceWatcher.Removed -= DeviceWatcher_Removed;
        _deviceWatcher.Updated -= DeviceWatcher_Updated;
        _deviceWatcher.EnumerationCompleted -= DeviceWatcher_EnumerationCompleted;
    }
}