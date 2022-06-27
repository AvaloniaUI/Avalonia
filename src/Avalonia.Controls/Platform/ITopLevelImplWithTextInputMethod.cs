using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform
{
    [Unstable]
    public interface ITopLevelImplWithTextInputMethod : ITopLevelImpl
    {
        public ITextInputMethodImpl? TextInputMethod { get; }
    }
}
