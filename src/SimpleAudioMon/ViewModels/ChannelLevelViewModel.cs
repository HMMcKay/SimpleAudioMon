using System.Globalization;

namespace SimpleAudioMon.ViewModels;

/// <summary>
/// Live level for a single channel of a device. The meter bar uses a dB scale
/// from <see cref="MinDb"/> to 0 dBFS with a short decay so transients stay visible.
/// </summary>
public sealed class ChannelLevelViewModel : ObservableObject
{
    /// <summary>Bottom of the visible meter range, in dBFS.</summary>
    public const double MinDb = -60.0;

    /// <summary>Per-tick decay applied to the displayed level (fast fall-back).</summary>
    private const double DecayFactor = 0.82;

    private double _displayPeak;
    private double _levelPercent;
    private string _levelDbText = "−∞ dB";

    public ChannelLevelViewModel(string name)
    {
        Name = name;
    }

    public string Name { get; }

    /// <summary>Meter position, 0–100, on a dB scale.</summary>
    public double LevelPercent
    {
        get => _levelPercent;
        private set => SetProperty(ref _levelPercent, value);
    }

    public string LevelDbText
    {
        get => _levelDbText;
        private set => SetProperty(ref _levelDbText, value);
    }

    /// <summary>Feeds a new linear peak sample (0..1) into the meter.</summary>
    public void Update(float peak)
    {
        _displayPeak = Math.Max(peak, _displayPeak * DecayFactor);
        var db = ToDb(_displayPeak);
        LevelPercent = Math.Clamp((db - MinDb) / -MinDb, 0.0, 1.0) * 100.0;
        LevelDbText = db <= MinDb
            ? "−∞ dB"
            : string.Create(CultureInfo.InvariantCulture, $"{db,6:F1} dB");
    }

    public static double ToDb(double linear)
        => linear <= 0 ? double.NegativeInfinity : 20.0 * Math.Log10(linear);
}
