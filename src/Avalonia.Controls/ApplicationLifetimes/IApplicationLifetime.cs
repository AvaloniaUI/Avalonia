namespace Avalonia.Controls.ApplicationLifetimes
{
    public interface IApplicationLifetime
    {
        /// <summary>
        /// This is called when the framework initialization process is completed.
        /// </summary>
        void OnFrameworkInitializationCompleted();
    }
}
