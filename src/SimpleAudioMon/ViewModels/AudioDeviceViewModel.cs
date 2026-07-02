using System.Collections.ObjectModel;
using System.Globalization;
using NAudio.CoreAudioApi;
using SimpleAudioMon.Audio;

namespace SimpleAudioMon.ViewModels;

/// <summary>
/// One audio endpoint (input or output) with its per-channel live meters.
/// Owns the underlying <see cref="MMDevice"/> and must be disposed.
/// </summary>
public sealed class AudioDeviceViewModel : ObservableObject, IDisposable
{
    private readonly MMDevice _device;
    private string _masterText = "−∞ dB";
    private bool _disposed;

    public AudioDeviceViewModel(MMDevice device, bool isDefault)
    {
        _device = device;
        Id = device.ID;
        Name = device.FriendlyName;
        IsDefault = isDefault;

        var channelCount = device.AudioMeterInformation.PeakValues.Count;
        var names = ChannelLayout.GetChannelNames(device, channelCount);
        foreach (var name in names)
        {
            Channels.Add(new ChannelLevelViewModel(name));
        }

        var formFactor = DeviceInfo.GetFormFactorLabel(device);
        Description = channelCount == 1
            ? $"{formFactor} · 1 channel"
            : $"{formFactor} · {channelCount} channels";
    }

    public string Id { get; }

    public string Name { get; }

    public string Description { get; }

    public bool IsDefault { get; }

    public ObservableCollection<ChannelLevelViewModel> Channels { get; } = new();

    /// <summary>Master peak readout shown in the card header.</summary>
    public string MasterText
    {
        get => _masterText;
        private set => SetProperty(ref _masterText, value);
    }

    /// <summary>
    /// Polls the endpoint meter and updates all channel levels.
    /// Returns false when the device has become unavailable (unplugged/disabled),
    /// signalling the caller to refresh the device list.
    /// </summary>
    public bool UpdateLevels()
    {
        if (_disposed)
        {
            return false;
        }

        try
        {
            var meter = _device.AudioMeterInformation;
            var peaks = meter.PeakValues;
            var count = Math.Min(peaks.Count, Channels.Count);
            for (var i = 0; i < count; i++)
            {
                Channels[i].Update(peaks[i]);
            }

            var masterDb = ChannelLevelViewModel.ToDb(meter.MasterPeakValue);
            MasterText = masterDb <= ChannelLevelViewModel.MinDb
                ? "−∞ dB"
                : string.Create(CultureInfo.InvariantCulture, $"{masterDb:F1} dB");
            return true;
        }
        catch
        {
            // COM failure: the endpoint went away mid-poll.
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _device.Dispose();
    }
}
