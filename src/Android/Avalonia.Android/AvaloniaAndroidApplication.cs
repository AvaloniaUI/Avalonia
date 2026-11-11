using System;
using Android.Runtime;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Android
{
    internal interface IAndroidApplication
    {
        internal static Type? WindowActivityType { get; set; }
        IApplicationLifetime? Lifetime { get; set; }
    }

    public class AvaloniaAndroidApplication<TApp> : global::Android.App.Application, IAndroidApplication
        where TApp : Application, new()
    {
        IApplicationLifetime? IAndroidApplication.Lifetime { get; set; }

        protected AvaloniaAndroidApplication(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            InitializeAppLifetime();
        }

        private void InitializeAppLifetime()
        {
            var builder = CreateAppBuilder();
            builder = CustomizeAppBuilder(builder);

            IApplicationLifetime lifetime = AndroidPlatform.UseScenes ? new MultiSceneApplicationLifetime() : new ApplicationLifetime();

            ((IAndroidApplication)this).Lifetime = lifetime;

            builder.SetupWithLifetime(lifetime);
        }

        protected virtual AppBuilder CreateAppBuilder() => AppBuilder.Configure<TApp>().UseAndroid();
        protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder;

        public void RegisterWindowActivityType(Type? type)
        {
            if(type != null && !type.IsAssignableTo(typeof(AvaloniaActivity)))
            {
                throw new ArgumentException($"Activity Type {type} must be a {typeof(AvaloniaActivity)}");
            }
            IAndroidApplication.WindowActivityType = type;
        }
    }
}
