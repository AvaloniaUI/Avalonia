namespace Avalonia.Controls.ApplicationLifetimes
{
    public interface IApplicationLifetime
    {
        /// <summary>
        /// This is called when the setup of the platform is completed.
        /// </summary>
        void OnSetupCompleted();
    }
}
