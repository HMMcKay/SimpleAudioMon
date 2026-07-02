using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace SimpleAudioMon.Audio;

/// <summary>
/// Receives device add/remove/state/default-change notifications from the
/// audio endpoint enumerator. Callbacks arrive on MTA worker threads, so the
/// single <see cref="DevicesChanged"/> event must be marshalled to the UI
/// thread by the subscriber.
/// </summary>
public sealed class EndpointNotificationClient : IMMNotificationClient
{
    public event Action? DevicesChanged;

    public void OnDeviceStateChanged(string deviceId, DeviceState newState) => DevicesChanged?.Invoke();

    public void OnDeviceAdded(string pwstrDeviceId) => DevicesChanged?.Invoke();

    public void OnDeviceRemoved(string deviceId) => DevicesChanged?.Invoke();

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) => DevicesChanged?.Invoke();

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {
        // Property changes fire constantly (volume, metering config, ...);
        // they don't affect the device list, so ignore them.
    }
}
