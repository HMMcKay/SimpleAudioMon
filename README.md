# SimpleAudioMon

A simple audio input and output display for Windows 11.

SimpleAudioMon shows every **active** audio endpoint on the system — both
output (render) and input (capture) devices — with a live peak meter for each
individual channel (Left, Right, Front Center, LFE, Side Left, …) plus a
master peak readout per device. The default output and input devices are
marked with a **Default** badge and sorted to the top of their group.

The app uses WPF on .NET 9 with the built-in **Windows 11 Fluent dark theme**
(`ThemeMode="Dark"`), so all controls (progress bars, tabs, scroll bars,
status bar) are the native Windows 11 dark-styled components.

## Features

- Lists all active output and input audio devices, updating automatically
  when devices are plugged in, removed, enabled, or disabled.
- Per-channel peak meters at ~30 Hz on a −60 dBFS…0 dBFS scale, with a fast
  decay so transients stay visible, plus a numeric dB readout per channel.
- Channel names resolved from the device's real speaker mask
  (`WAVEFORMATEXTENSIBLE.dwChannelMask` via the endpoint property store), with
  sensible fallbacks by channel count (Mono, Left/Right, 5.1, 7.1, …).
- Device form factor labels (Speakers, Headphones, Microphone, HDMI, …) and
  default-device badges.
- Metering uses the endpoint meter API (`IAudioMeterInformation`), so it
  observes levels without opening capture streams or changing audio state.

## Requirements

- Windows 10 or Windows 11 (the Fluent dark styling targets Windows 11).
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) to build.

## Building and running

```powershell
dotnet build src/SimpleAudioMon/SimpleAudioMon.csproj
dotnet run --project src/SimpleAudioMon
```

To produce a single self-contained executable (no .NET install needed on the
target machine):

```powershell
dotnet publish src/SimpleAudioMon/SimpleAudioMon.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

### VS Code

Open the repository folder in VS Code and install the recommended extensions
(C# Dev Kit). Press **F5** to build and launch with the debugger, or run the
default build task with **Ctrl+Shift+B**. A `publish (self-contained exe)`
task is also provided.

The project sets `EnableWindowsTargeting`, so it also compiles on non-Windows
machines/CI (it only runs on Windows).

## Architecture

```
src/SimpleAudioMon/
├── App.xaml(.cs)                     Application entry; ThemeMode="Dark" (Win11 Fluent)
├── MainWindow.xaml(.cs)              Layout: Levels tab, device cards, status bar
├── Audio/
│   ├── ChannelLayout.cs              Channel names from speaker mask / count fallback
│   ├── DeviceInfo.cs                 Form factor labels from the property store
│   └── EndpointNotificationClient.cs Device add/remove/default-change notifications
└── ViewModels/
    ├── MainViewModel.cs              Device enumeration, 30 Hz meter polling, refresh
    ├── AudioDeviceViewModel.cs       One endpoint + its channels and master peak
    ├── ChannelLevelViewModel.cs      Per-channel dB meter with decay smoothing
    └── ObservableObject.cs           INotifyPropertyChanged base
```

Audio access is via [NAudio](https://github.com/naudio/NAudio)'s Core Audio
(WASAPI) wrappers: `MMDeviceEnumerator` for enumeration/notifications and
`AudioMeterInformation` for per-channel peaks.

## Roadmap / extension points

The UI is a `TabControl` with a single **Levels** tab today; planned views
slot in as sibling tabs:

- **Spectrum** — real-time FFT magnitude per device, fed by a
  `WasapiCapture`/`WasapiLoopbackCapture` stream per selected device.
- **Spectrogram** — scrolling time–frequency view built on the same capture
  pipeline.

`AudioDeviceViewModel` is the natural owner for a per-device capture/FFT
pipeline; the meter polling in `MainViewModel` stays independent of it.
