using System.Collections.ObjectModel;
using System.Windows.Threading;
using NAudio.CoreAudioApi;
using SimpleAudioMon.Audio;

namespace SimpleAudioMon.ViewModels;

/// <summary>
/// Root view model: enumerates active input/output endpoints, polls their
/// meters ~30x per second, and rebuilds the list when devices change.
/// </summary>
public sealed class MainViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan MeterInterval = TimeSpan.FromMilliseconds(33);
    private static readonly TimeSpan RefreshDebounce = TimeSpan.FromMilliseconds(500);

    private readonly DispatcherTimer _meterTimer;
    private readonly DispatcherTimer _refreshTimer;
    private MMDeviceEnumerator? _enumerator;
    private EndpointNotificationClient? _notificationClient;
    private string _statusText = "Starting…";
    private bool _hasNoOutputs;
    private bool _hasNoInputs;
    private bool _disposed;

    public MainViewModel()
    {
        _meterTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = MeterInterval };
        _meterTimer.Tick += (_, _) => PollMeters();

        _refreshTimer = new DispatcherTimer { Interval = RefreshDebounce };
        _refreshTimer.Tick += (_, _) =>
        {
            _refreshTimer.Stop();
            RefreshDevices();
        };
    }

    public ObservableCollection<AudioDeviceViewModel> OutputDevices { get; } = new();

    public ObservableCollection<AudioDeviceViewModel> InputDevices { get; } = new();

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool HasNoOutputs
    {
        get => _hasNoOutputs;
        private set => SetProperty(ref _hasNoOutputs, value);
    }

    public bool HasNoInputs
    {
        get => _hasNoInputs;
        private set => SetProperty(ref _hasNoInputs, value);
    }

    /// <summary>Call once from the UI thread after the window has loaded.</summary>
    public void Start()
    {
        if (_enumerator is not null || _disposed)
        {
            return;
        }

        try
        {
            _enumerator = new MMDeviceEnumerator();
            _notificationClient = new EndpointNotificationClient();
            _notificationClient.DevicesChanged += ScheduleRefresh;
            _enumerator.RegisterEndpointNotificationCallback(_notificationClient);
        }
        catch (Exception ex)
        {
            StatusText = $"Audio system unavailable: {ex.Message}";
            return;
        }

        RefreshDevices();
        _meterTimer.Start();
    }

    /// <summary>Marshals device-change notifications (MTA threads) onto the UI thread, debounced.</summary>
    private void ScheduleRefresh()
    {
        _refreshTimer.Dispatcher.BeginInvoke(() =>
        {
            if (_disposed)
            {
                return;
            }

            _refreshTimer.Stop();
            _refreshTimer.Start();
        });
    }

    private void PollMeters()
    {
        var anyFailed = false;
        foreach (var device in OutputDevices)
        {
            anyFailed |= !device.UpdateLevels();
        }

        foreach (var device in InputDevices)
        {
            anyFailed |= !device.UpdateLevels();
        }

        if (anyFailed)
        {
            ScheduleRefresh();
        }
    }

    private void RefreshDevices()
    {
        if (_enumerator is null || _disposed)
        {
            return;
        }

        ClearDevices();

        try
        {
            var defaultRenderId = TryGetDefaultId(DataFlow.Render);
            var defaultCaptureId = TryGetDefaultId(DataFlow.Capture);

            foreach (var device in _enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active))
            {
                try
                {
                    var isDefault = device.ID == (device.DataFlow == DataFlow.Capture ? defaultCaptureId : defaultRenderId);
                    var viewModel = new AudioDeviceViewModel(device, isDefault);
                    var target = device.DataFlow == DataFlow.Capture ? InputDevices : OutputDevices;

                    // Keep the default device at the top of its group.
                    if (isDefault)
                    {
                        target.Insert(0, viewModel);
                    }
                    else
                    {
                        target.Add(viewModel);
                    }
                }
                catch
                {
                    // Device vanished between enumeration and inspection; skip it.
                    device.Dispose();
                }
            }

            StatusText = $"{OutputDevices.Count} output · {InputDevices.Count} input — metering at 30 Hz";
        }
        catch (Exception ex)
        {
            StatusText = $"Device enumeration failed: {ex.Message}";
        }

        HasNoOutputs = OutputDevices.Count == 0;
        HasNoInputs = InputDevices.Count == 0;
    }

    private string? TryGetDefaultId(DataFlow flow)
    {
        try
        {
            if (_enumerator!.HasDefaultAudioEndpoint(flow, Role.Multimedia))
            {
                using var device = _enumerator.GetDefaultAudioEndpoint(flow, Role.Multimedia);
                return device.ID;
            }
        }
        catch
        {
            // No default endpoint available.
        }

        return null;
    }

    private void ClearDevices()
    {
        foreach (var device in OutputDevices)
        {
            device.Dispose();
        }

        foreach (var device in InputDevices)
        {
            device.Dispose();
        }

        OutputDevices.Clear();
        InputDevices.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _meterTimer.Stop();
        _refreshTimer.Stop();

        if (_enumerator is not null)
        {
            if (_notificationClient is not null)
            {
                try
                {
                    _enumerator.UnregisterEndpointNotificationCallback(_notificationClient);
                }
                catch
                {
                    // Shutdown races with the audio service are harmless here.
                }
            }

            _enumerator.Dispose();
            _enumerator = null;
        }

        ClearDevices();
    }
}
