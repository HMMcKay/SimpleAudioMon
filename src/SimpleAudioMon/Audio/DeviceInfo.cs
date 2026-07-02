using NAudio.CoreAudioApi;

namespace SimpleAudioMon.Audio;

/// <summary>Helpers for describing audio endpoints.</summary>
public static class DeviceInfo
{
    /// <summary>
    /// Returns a friendly label for the endpoint's physical form factor
    /// (Speakers, Headphones, Microphone, ...), from PKEY_AudioEndpoint_FormFactor.
    /// </summary>
    public static string GetFormFactorLabel(MMDevice device)
    {
        try
        {
            if (device.Properties.Contains(PropertyKeys.PKEY_AudioEndpoint_FormFactor))
            {
                var value = device.Properties[PropertyKeys.PKEY_AudioEndpoint_FormFactor].Value;
                var formFactor = value switch
                {
                    uint u => u,
                    int i => (uint)i,
                    _ => uint.MaxValue,
                };

                // EndpointFormFactor enumeration (mmdeviceapi.h)
                return formFactor switch
                {
                    0 => "Network device",
                    1 => "Speakers",
                    2 => "Line level",
                    3 => "Headphones",
                    4 => "Microphone",
                    5 => "Headset",
                    6 => "Handset",
                    7 => "Digital passthrough",
                    8 => "S/PDIF",
                    9 => "HDMI / display audio",
                    _ => DefaultLabel(device),
                };
            }
        }
        catch
        {
            // Property store access can fail for devices in transition; use the fallback.
        }

        return DefaultLabel(device);
    }

    private static string DefaultLabel(MMDevice device)
        => device.DataFlow == DataFlow.Capture ? "Input device" : "Output device";
}
