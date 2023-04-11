using System;
using System.IO;
using System.Text.Json;

namespace VirtualizationDemo.Models;

public class ChatFile
{
    public ChatMessage[]? Chat { get; set; }

    public static ChatFile Load(string path)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        using var s = File.OpenRead(path);
        return JsonSerializer.Deserialize<ChatFile>(s, options)!;
    }
}

public record ChatMessage(string Sender, string Message, DateTimeOffset Timestamp);
