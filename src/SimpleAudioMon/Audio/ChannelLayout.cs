using NAudio.CoreAudioApi;

namespace SimpleAudioMon.Audio;

/// <summary>
/// Resolves human-readable channel names (Front Left, Front Right, Center, ...)
/// for an audio endpoint, preferring the real speaker mask from the device's
/// mix format and falling back to standard layouts by channel count.
/// </summary>
public static class ChannelLayout
{
    // Speaker position bits from ksmedia.h (WAVEFORMATEXTENSIBLE.dwChannelMask),
    // in mask-bit order, which is also the channel order within the stream.
    private static readonly (uint Bit, string Name)[] SpeakerPositions =
    {
        (0x1, "Front Left"),
        (0x2, "Front Right"),
        (0x4, "Front Center"),
        (0x8, "LFE (Subwoofer)"),
        (0x10, "Back Left"),
        (0x20, "Back Right"),
        (0x40, "Front Left of Center"),
        (0x80, "Front Right of Center"),
        (0x100, "Back Center"),
        (0x200, "Side Left"),
        (0x400, "Side Right"),
        (0x800, "Top Center"),
        (0x1000, "Top Front Left"),
        (0x2000, "Top Front Center"),
        (0x4000, "Top Front Right"),
        (0x8000, "Top Back Left"),
        (0x10000, "Top Back Center"),
        (0x20000, "Top Back Right"),
    };

    /// <summary>
    /// Returns exactly <paramref name="channelCount"/> channel names for the device.
    /// </summary>
    public static IReadOnlyList<string> GetChannelNames(MMDevice device, int channelCount)
    {
        var mask = TryGetChannelMask(device);
        if (mask is > 0)
        {
            var names = NamesFromMask(mask.Value);
            if (names.Count == channelCount)
            {
                return names;
            }
        }

        return DefaultNames(channelCount, device.DataFlow);
    }

    /// <summary>
    /// Reads the endpoint's mix format from the property store and, when it is a
    /// WAVEFORMATEXTENSIBLE, extracts dwChannelMask. NAudio does not expose the
    /// mask on WaveFormatExtensible, so the raw blob is parsed directly.
    /// </summary>
    private static uint? TryGetChannelMask(MMDevice device)
    {
        try
        {
            if (!device.Properties.Contains(PropertyKeys.PKEY_AudioEngine_DeviceFormat))
            {
                return null;
            }

            if (device.Properties[PropertyKeys.PKEY_AudioEngine_DeviceFormat].Value is not byte[] blob)
            {
                return null;
            }

            // WAVEFORMATEX header is 18 bytes; WAVEFORMATEXTENSIBLE appends
            // wValidBitsPerSample (2) + dwChannelMask (4) + SubFormat (16).
            if (blob.Length < 40)
            {
                return null;
            }

            var formatTag = BitConverter.ToUInt16(blob, 0);
            var cbSize = BitConverter.ToUInt16(blob, 16);
            const ushort WaveFormatExtensibleTag = 0xFFFE;
            if (formatTag != WaveFormatExtensibleTag || cbSize < 22)
            {
                return null;
            }

            return BitConverter.ToUInt32(blob, 20);
        }
        catch
        {
            return null;
        }
    }

    private static List<string> NamesFromMask(uint mask)
    {
        var names = new List<string>();
        foreach (var (bit, name) in SpeakerPositions)
        {
            if ((mask & bit) != 0)
            {
                names.Add(name);
            }
        }

        return names;
    }

    private static string[] DefaultNames(int channelCount, DataFlow flow) => channelCount switch
    {
        1 => new[] { "Mono" },
        2 => new[] { "Left", "Right" },
        4 => new[] { "Front Left", "Front Right", "Back Left", "Back Right" },
        6 => new[] { "Front Left", "Front Right", "Front Center", "LFE (Subwoofer)", "Back Left", "Back Right" },
        8 => new[] { "Front Left", "Front Right", "Front Center", "LFE (Subwoofer)", "Back Left", "Back Right", "Side Left", "Side Right" },
        _ => Enumerable.Range(1, Math.Max(channelCount, 0))
                       .Select(i => $"Channel {i}")
                       .ToArray(),
    };
}
