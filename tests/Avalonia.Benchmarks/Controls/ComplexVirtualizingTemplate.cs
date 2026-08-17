using System.Runtime.CompilerServices;
using Avalonia.Controls.Templates;

#nullable enable

namespace Avalonia.Benchmarks.Controls
{
    /// <summary>
    /// The opt-in arm of the container-virtualization comparison, and a working example of the
    /// production API: a plain <see cref="FuncDataTemplate{T}"/> with a
    /// <see cref="FuncDataTemplate.RecycleKeySelector"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Fork-only.</b> <c>RecycleKeySelector</c> does not exist at the merge-base, so this file is
    /// left behind when copying the benchmark directory into a baseline worktree. It registers
    /// itself through a <see cref="ModuleInitializerAttribute"/>, so its absence needs no edit
    /// anywhere else — <see cref="ComplexItems.AvailableModes"/> then reports only the arm that
    /// exists.
    /// </para>
    /// <para>
    /// The key is <see cref="ComplexItem.Kind"/>, not the item's CLR type. All four row kinds are
    /// one class, but they are four different subtrees, and a container built for one kind must
    /// never be handed an item of another — it would keep the wrong tree. This is the case a XAML
    /// <c>DataTemplate</c> cannot express, because it keys on <c>DataType</c>.
    /// </para>
    /// </remarks>
    internal static class ComplexVirtualizingTemplate
    {
        [ModuleInitializer]
        internal static void Register() =>
            ComplexItems.VirtualizingTemplateFactory = counters =>
                new FuncDataTemplate<ComplexItem>((item, _) => ComplexItems.Build(item, counters))
                {
                    RecycleKeySelector = data => (data as ComplexItem)?.Kind,
                    MaxPoolSizePerKey = 8,
                    MinPoolSizePerKey = 2,
                };
    }
}
