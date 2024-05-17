using Avalonia.Input;
using Avalonia.Metadata;

namespace Avalonia.Controls;

[Unstable, PrivateApi]
internal interface IToolTipService
{
    void Update(IInputRoot root, Visual? candidateToolTipHost);
}
