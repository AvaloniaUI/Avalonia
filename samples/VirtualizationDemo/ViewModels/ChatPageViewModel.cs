using System;
using System.Collections.ObjectModel;
using System.IO;
using VirtualizationDemo.Models;

namespace VirtualizationDemo.ViewModels;

public class ChatPageViewModel
{
    public ChatPageViewModel()
    {
        var chat = ChatFile.Load(Path.Combine("Assets", "chat.json"));
        Messages = new(chat.Chat ?? Array.Empty<ChatMessage>());
    }

    public ObservableCollection<ChatMessage> Messages { get; }
}
