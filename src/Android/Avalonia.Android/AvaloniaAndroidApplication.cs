using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Runtime;

namespace Avalonia.Android
{
    internal interface IAndroidApplication
    {
        ApplicationLifetime? Lifetime { get; set; }
    }

    public class AvaloniaAndroidApplication<TApp> : global::Android.App.Application, IAndroidApplication
        where TApp : Application, new()
    {
        ApplicationLifetime? IAndroidApplication.Lifetime { get; set; }

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

            var lifetime = new ApplicationLifetime();

            ((IAndroidApplication)this).Lifetime = lifetime;

            builder.SetupWithLifetime(lifetime);
        }

        protected virtual AppBuilder CreateAppBuilder() => AppBuilder.Configure<TApp>().UseAndroid();
        protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder;
    }
}
