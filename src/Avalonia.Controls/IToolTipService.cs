using Avalonia.Metadata;

namespace Avalonia.Controls;

[Unstable, PrivateApi]
internal interface IToolTipService
{
    void Update(Visual? candidateToolTipHost);
}
