using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform
{
    public interface ITopLevelImplWithTextInputMethod : ITopLevelImpl
    {
        public ITextInputMethodImpl? TextInputMethod { get; }
    }
}
