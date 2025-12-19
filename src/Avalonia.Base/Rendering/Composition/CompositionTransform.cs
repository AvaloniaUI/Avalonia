using Avalonia.Media;

// ReSharper disable CheckNamespace
namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerCompositionSimpleTransform : ITransform
{
    Matrix ITransform.Value => this.Value.ToMatrix();
}