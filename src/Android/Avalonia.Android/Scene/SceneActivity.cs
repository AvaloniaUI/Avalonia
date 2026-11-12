using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Java.Lang;

namespace Avalonia.Android.Scene
{
    public abstract class SceneActivity : AvaloniaActivity, ISceneHost
    {
        public void Close()
        {
            Finish();
        }
    }

    public abstract class SceneActivity<T> : SceneActivity where T : Controls.Scene, new()
    {
        public T? Scene { get; set; }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            var requestKey = Intent?.GetIntExtra(SceneManager.RequestIdName, -1) ?? -1;

            WindowRequest? request = null;

            if (requestKey != -1)
            {
                request = SceneManager.GetWindowRequest(requestKey);

                if (request != null)
                    Scene = (T?)request.Scene;
            }

            Scene ??= new T();

            (this as IAvaloniaActivity).Activated += SceneActivity_Activated;
            (this as IAvaloniaActivity).Deactivated += SceneActivity_Deactivated;

            base.OnCreate(savedInstanceState);

            if (Application is IAndroidApplication application
                && application.Lifetime is MultiSceneApplicationLifetime lifetime)
            {
                lifetime.AddScene(Scene);
            }

            Scene.AttachHost(this);

            request?.SetComplete();
        }

        private void SceneActivity_Deactivated(object? sender, ActivatedEventArgs e)
        {
            Scene?.OnDeactivated(e);
        }

        private void SceneActivity_Activated(object? sender, ActivatedEventArgs e)
        {
            Scene?.OnActivated(e);
        }

        private protected override void InitializeAvaloniaView(object? initialContent)
        {
            base.InitializeAvaloniaView(initialContent);

            Content = Scene;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            (this as IAvaloniaActivity).Activated -= SceneActivity_Activated;
            (this as IAvaloniaActivity).Deactivated -= SceneActivity_Deactivated;

            if (Scene != null 
                && Application is IAndroidApplication application 
                && application.Lifetime is MultiSceneApplicationLifetime lifetime)
            {
                lifetime.RemoveScene(Scene);
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);

            if(Scene is { } scene)
            {
                var state = new Dictionary<string, object?>();
                var stateEventArgs = new ApplicationLifetimeStateEventArgs(Controls.Scene.SaveStateEvent)
                {
                    State = state
                };

                Scene.RaiseEvent(stateEventArgs);

                if(stateEventArgs.Handled)
                {
                    foreach (var (key, value) in state)
                    {
                        switch (value)
                        {
                            case bool boolean:
                                outState.PutBoolean(key, boolean);
                                break;
                            case byte byteValue:
                                outState.PutByte(key, (sbyte)byteValue);
                                break;
                            case char character:
                                outState.PutChar(key, character);
                                break;
                            case short shortValue:
                                outState.PutShort(key, shortValue);
                                break;
                            case int integer:
                                outState.PutInt(key, integer);
                                break;
                            case long longValue:
                                outState.PutLong(key, longValue);
                                break;
                            case float floatValue:
                                outState.PutFloat(key, floatValue);
                                break;
                            case double doubleValue:
                                outState.PutDouble(key, doubleValue);
                                break;
                            case string text:
                                outState.PutString(key, text);
                                break;
                            case bool[] booleans:
                                outState.PutBooleanArray(key, booleans);
                                break;
                            case byte[] bytes:
                                outState.PutByteArray(key, bytes);
                                break;
                            case char[] characters:
                                outState.PutCharArray(key, characters);
                                break;
                            case short[] shorts:
                                outState.PutShortArray(key, shorts);
                                break;
                            case int[] integers:
                                outState.PutIntArray(key, integers);
                                break;
                            case long[] longs:
                                outState.PutLongArray(key, longs);
                                break;
                            case float[] floats:
                                outState.PutFloatArray(key, floats);
                                break;
                            case double[] doubles:
                                outState.PutDoubleArray(key, doubles);
                                break;
                            case string[] strings:
                                outState.PutStringArray(key, strings);
                                break;
                            case IEnumerable<bool> booleans:
                                outState.PutBooleanArray(key, booleans.ToArray());
                                break;
                            case IEnumerable<byte> bytes:
                                outState.PutByteArray(key, bytes.ToArray());
                                break;
                            case IEnumerable<char> characters:
                                outState.PutCharArray(key, characters.ToArray());
                                break;
                            case IEnumerable<short> shorts:
                                outState.PutShortArray(key, shorts.ToArray());
                                break;
                            case IEnumerable<int> integers:
                                outState.PutIntArray(key, integers.ToArray());
                                break;
                            case IEnumerable<long> longs:
                                outState.PutLongArray(key, longs.ToArray());
                                break;
                            case IEnumerable<float> floats:
                                outState.PutFloatArray(key, floats.ToArray());
                                break;
                            case IEnumerable<double> doubles:
                                outState.PutDoubleArray(key, doubles.ToArray());
                                break;
                            case IEnumerable<string> strings:
                                outState.PutStringArray(key, strings.ToArray());
                                break;
                            default:
                                Logging.Logger.Sink?.Log(Logging.LogEventLevel.Warning, Logging.LogArea.AndroidPlatform, value, $"Scene State doesn't support storing value of type {value?.GetType()}");
                                break;
                        }
                    }
                }
            }
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);

            if (Scene is { } scene)
            {
                var keySet = savedInstanceState.KeySet()?.ToList();
                var state = new Dictionary<string, object?>();

                if(keySet?.Count > 0)
                {
                    foreach(var key in keySet)
                    {
                        var value = savedInstanceState.Get(key);

                        if(value != null)
                        {
                            object? convertedValue = value switch
                            {
                                Integer i => i.IntValue(),
                                Java.Lang.Boolean b => b.BooleanValue(),
                                Java.Lang.Long longValue => longValue.LongValue(),
                                Java.Lang.Double doubleValue => doubleValue.DoubleValue(),
                                Java.Lang.Float floatValue => floatValue.FloatValue(),
                                Java.Lang.Short shortValue => shortValue.ShortValue(),
                                Java.Lang.Byte byteValue => byteValue.ByteValue(),
                                Java.Lang.Character character => character.CharValue(),
                                Java.Lang.String text => text.ToString(),
                                _ => null
                            };

                            if(convertedValue == null)
                            {
                                convertedValue = savedInstanceState.GetBooleanArray(key);
                                if(convertedValue == null)
                                    convertedValue = savedInstanceState.GetByteArray(key);
                                if(convertedValue == null)
                                    convertedValue = savedInstanceState.GetCharArray(key);
                                if(convertedValue == null)
                                    convertedValue = savedInstanceState.GetShortArray(key);
                                if(convertedValue == null)
                                    convertedValue = savedInstanceState.GetIntArray(key);
                                if(convertedValue == null)
                                    convertedValue = savedInstanceState.GetLongArray(key);
                                if(convertedValue == null)
                                    convertedValue = savedInstanceState.GetFloatArray(key);
                                if(convertedValue == null)
                                    convertedValue = savedInstanceState.GetDoubleArray(key);
                                if(convertedValue == null)
                                    convertedValue = savedInstanceState.GetStringArray(key);
                                if(convertedValue == null)
                                    convertedValue = savedInstanceState.GetCharSequenceArray(key);
                            }

                            if(convertedValue != null)
                            {
                                state[key] = convertedValue;
                            }
                        }
                    }
                }

                var stateEventArgs = new ApplicationLifetimeStateEventArgs(Controls.Scene.RestoreStateEvent)
                {
                    State = state
                };

                Scene.RaiseEvent(stateEventArgs);
            }
        }

        protected static void RegisterInManager<TScene>(Type activityType) where TScene : Controls.Scene, new()
        {
            var manager = AvaloniaLocator.Current.GetService<ISceneManager>() as SceneManager;
            manager?.RegisterActivityType<TScene>(activityType);
        }
    }
}
