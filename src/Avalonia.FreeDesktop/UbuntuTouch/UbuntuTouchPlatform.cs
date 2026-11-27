using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Avalonia.FreeDesktop.UbuntuTouch;

internal static class UbuntuTouchPlatform
{
    internal static bool IsEnabled => Environment.GetEnvironmentVariable("AVALONIA_UBUNTU_TOUCH") switch
    {
        "1" => true,
        "0" => false,
        _ => OperatingSystem.IsLinux() && Detect(Read("/etc/os-release"), Read("/etc/system-image/channel.ini"),
            string.Join(":", Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP"),
                Environment.GetEnvironmentVariable("XDG_SESSION_DESKTOP"), Environment.GetEnvironmentVariable("DESKTOP_SESSION")))
    };

    internal static bool Detect(string osRelease, string systemImage, string desktop)
    {
        var release = Parse(osRelease);
        var image = Parse(systemImage);
        release.TryGetValue("ID", out var id);
        release.TryGetValue("VARIANT_ID", out var variant);
        image.TryGetValue("channel", out var channel);
        image.TryGetValue("base", out var server);
        var lomiri = desktop.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(s => s.Equals("Lomiri", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("Unity8", StringComparison.OrdinalIgnoreCase) || s.Equals("ubuntu-touch", StringComparison.OrdinalIgnoreCase));
        var touchImage = channel?.StartsWith("ubports/", StringComparison.OrdinalIgnoreCase) == true ||
            channel?.StartsWith("ubuntu-touch/", StringComparison.OrdinalIgnoreCase) == true ||
            (server == "system-image.ubports.com" && !string.IsNullOrEmpty(channel));
        return id is "ubuntu-touch" or "ubports" || id == "ubuntu" && variant == "touch" ||
            touchImage && (lomiri || id == "ubuntu");
    }

    internal static double? GetScale(string? gridUnit)
        => double.TryParse(gridUnit, NumberStyles.Float, CultureInfo.InvariantCulture, out var grid) &&
           double.IsFinite(grid) && grid is >= 8 and <= 48 ? grid / 8 : null;

    private static Dictionary<string, string> Parse(string text)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('#') || trimmed.StartsWith('['))
                continue;
            var separator = trimmed.IndexOf('=');
            var colon = trimmed.IndexOf(':');
            if (colon > 0 && (separator < 0 || colon < separator))
                separator = colon;
            if (separator > 0)
                result[trimmed[..separator].Trim()] = trimmed[(separator + 1)..].Trim().Trim('\'', '"');
        }
        return result;
    }

    private static string Read(string path)
    {
        try
        {
            using var reader = File.OpenText(path);
            var buffer = new char[65536];
            return new string(buffer, 0, reader.ReadBlock(buffer, 0, buffer.Length));
        }
        catch (IOException) { return ""; }
        catch (UnauthorizedAccessException) { return ""; }
    }
}
