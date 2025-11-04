using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Logging;

namespace Avalonia.Markup.Xaml.SourceInfo
{
    /// <summary>
    /// Provides an abstraction for navigating to a specific source file and position.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface are responsible for opening a source file
    /// in an external IDE or editor (e.g., Visual Studio, Rider, VS Code, etc.)
    /// at the given line and column.
    /// 
    /// Avalonia.Core or Avalonia.Diagnostics code should *not* depend on any IDE-specific logic.
    /// Instead, they call into <see cref="SourceNavigatorRegistry"/> which delegates
    /// to whichever <see cref="IAvaloniaSourceNavigator"/> has been registered.
    /// </remarks>
    public interface IAvaloniaSourceNavigator
    {
        /// <summary>
        /// Determines whether this navigator is able to handle navigation
        /// </summary>
        /// <returns><c>true</c> if this navigator is enable to handle navigation requests; otherwise, <c>false</c>.</returns>
        bool CanNavigate();


        /// <summary>
        /// Attempts to navigate to the specified file and position.
        /// </summary>
        /// <param name="filePath">The absolute path of the file to open.</param>
        /// <param name="line">The 1-based line number within the file.</param>
        /// <param name="column">The 1-based column number within the file.</param>
        /// <returns>
        /// A task that completes when navigation has been attempted.
        /// The task should not throw on failure; instead, it should
        /// handle errors internally or log them as appropriate.
        /// </returns>
        Task NavigateToAsync(string filePath, int line, int column);
    }

    /// <summary>
    /// Provides a central registry for registering and resolving
    /// <see cref="IAvaloniaSourceNavigator"/> implementations.
    /// </summary>
    /// <remarks>
    /// This allows IDE-specific integrations (e.g. Rider, Visual Studio)
    /// or third-party tools to register their own source navigators
    /// without introducing hard dependencies into Avalonia.Core.
    /// 
    /// Consumers such as the Avalonia Designer or DevTools can simply call:
    /// <code>
    ///     await SourceNavigatorRegistry.NavigateToAsync(file, line, col);
    /// </code>
    /// without knowing or caring which IDE or navigator is being used.
    /// 
    /// The registry exposes a Bindable <see cref="Instance"/> singleton pattern,
    /// allowing XAML bindings to directly observe properties such as
    /// <see cref="IsEnabled"/> without manually wiring up events.
    /// </remarks>
    public class SourceNavigatorRegistry : INotifyPropertyChanged
    {
        private static readonly List<IAvaloniaSourceNavigator> _navigators = new();

        public static SourceNavigatorRegistry Instance { get; } = new SourceNavigatorRegistry();



        public event PropertyChangedEventHandler? PropertyChanged;
        
        /// <summary>
        /// Indicates whether source navigation is currently possible.
        /// Typically bound to UI elements such as a “Jump to Source” button.
        /// </summary>
        public bool IsEnabled
        {
            get => CanNavigate();
        }

        /// <summary>
        /// Triggers a change notification for <see cref="IsEnabled"/>.
        /// Call this after modifying the registered navigators to refresh bindings.
        /// </summary>
        public void NotifyIsEnabledChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
        }

        private SourceNavigatorRegistry() { }





        /// <summary>
        /// Registers a new source navigator instance.
        /// </summary>
        /// <param name="navigator">The navigator to register.</param>
        /// <remarks>
        /// Multiple navigators may be registered; they will be queried in registration order.
        /// Typically, an IDE or design-time host will register one navigator during startup.
        /// </remarks>
        public static void Register(IAvaloniaSourceNavigator navigator)
        {
            if (navigator == null)
                throw new ArgumentNullException(nameof(navigator));

            _navigators.Add(navigator);
            Instance.NotifyIsEnabledChanged();
        }

        /// <summary>
        /// Registers a source navigator of type <typeparamref name="T"/> only if
        /// no instance of that type has already been registered.
        /// </summary>
        /// <typeparam name="T">
        /// The concrete <see cref="IAvaloniaSourceNavigator"/> type to register.
        /// </typeparam>
        /// <param name="factory">
        /// A factory delegate used to create a new instance when registration is needed.
        /// </param>
        /// <returns>
        /// <c>true</c> if a new navigator was created and registered; 
        /// <c>false</c> if a navigator of the same type already exists.
        /// </returns>
        public static bool RegisterIfNotExists<T>(Func<T> factory) where T : IAvaloniaSourceNavigator
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (_navigators.Any(x => x is T))
                return false;

            _navigators.Add(factory());
            Instance.NotifyIsEnabledChanged();
            return true;
        }

        /// <summary>
        /// Removes a previously registered navigator.
        /// </summary>
        /// <param name="navigator">The navigator instance to remove.</param>
        public static void Unregister(IAvaloniaSourceNavigator navigator)
        {
            _navigators.Remove(navigator);
            Instance.NotifyIsEnabledChanged();
        }

        /// <summary>
        /// Removes all registered navigators.
        /// </summary>
        public static void Clear() 
        {
            _navigators.Clear(); 
            Instance.NotifyIsEnabledChanged();
        }

        /// <summary>
        /// Determines whether any registered navigator is currently able
        /// to perform a source navigation action.
        /// </summary>
        /// <remarks>
        /// This method can be used by UI components (e.g. DevTools or Designer)
        /// to decide whether to enable or disable a “Jump to Source” button.
        ///
        /// The result is <c>true</c> if at least one registered
        /// <see cref="IAvaloniaSourceNavigator"/> reports that it can navigate
        /// in the current environment (for example, if an IDE is running and
        /// ready to receive navigation requests).
        /// </remarks>
        /// <returns>
        /// <c>true</c> if any registered navigator can currently handle
        /// a navigation request; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanNavigate()
        {
            return _navigators.Any(c => c.CanNavigate());
        }

        /// <summary>
        /// Attempts to navigate to a source location using the first
        /// registered navigator that reports <see cref="IAvaloniaSourceNavigator.CanNavigate"/>.
        /// </summary>
        /// <param name="filePath">The absolute path of the file to open.</param>
        /// <param name="line">The 1-based line number within the file.</param>
        /// <param name="column">The 1-based column number within the file.</param>
        /// <returns>
        /// <c>true</c> if a registered navigator successfully handled the navigation; otherwise, <c>false</c>.
        /// </returns>
        public static async Task<bool> NavigateToAsync(string filePath, int line, int column)
        {
            foreach (var navigator in _navigators)
            {
                try
                {
                    if (navigator.CanNavigate())
                    {
                        await navigator.NavigateToAsync(filePath, line, column);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // Swallow or log exceptions — core shouldn’t crash because of an IDE failure.
                    Logger.TryGet(LogEventLevel.Error, LogArea.Platform)?.Log("SourceNavigatorRegistry", $"Source navigation failed: {ex}");
                }
            }

            return false;
        }


        /// <summary>
        /// Attempts to navigate to a source location using the first
        /// registered navigator that reports <see cref="IAvaloniaSourceNavigator.CanNavigate"/>.
        /// </summary>
        /// <param name="avaloniaObject">The avaloniaObject to navigate to.</param>
        /// <returns>
        /// <c>true</c> if a registered navigator successfully handled the navigation; otherwise, <c>false</c>.
        /// </returns>
        public static async Task<bool> NavigateToAsync(AvaloniaObject avaloniaObject)
        {
            SourceInfo sourceInfo = Source.GetSourceInfo(avaloniaObject);
            if (sourceInfo != default)
            {
                string? filePath = sourceInfo.FilePath;
                if (filePath == "runtimexaml:0")
                {
                    filePath = null;

                    //if we are in the desinger, we do not get the current filepath, 
                    //try to read the filepath from the root XamlSourceInfoAttribute
                    if (avaloniaObject is Visual visual)
                    {
                        if (visual.VisualRoot is { } root)
                        {
                            if (root.GetType().GetCustomAttribute(typeof(XamlSourceInfoAttribute)) is XamlSourceInfoAttribute customAttribute)
                            {
                                filePath = customAttribute.SourceFileName;
                            }
                            else if (root is Avalonia.Controls.Window { Content: { } content })  //e.g. a UserControl
                            {
                                if (content.GetType().GetCustomAttribute(typeof(XamlSourceInfoAttribute)) is XamlSourceInfoAttribute customAttribute2)
                                {
                                    filePath = customAttribute2.SourceFileName;
                                }
                            }
                        }
                    }
                }
                if (filePath != null)
                {
                    return await NavigateToAsync(filePath, sourceInfo.Line, sourceInfo.Column);
                }
            }
            return false;
        }

        /// <summary>
        /// Gets a snapshot of all currently registered source navigators.
        /// </summary>
        public static IReadOnlyList<IAvaloniaSourceNavigator> RegisteredNavigators => _navigators.AsReadOnly();
    }
}
