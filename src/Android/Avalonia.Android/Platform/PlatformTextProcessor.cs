using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Avalonia.Input.TextInput;

namespace Avalonia.Android.Platform
{
    internal class PlatformTextProcessor : IPlatformTextProcessorImpl
    {
        private readonly AvaloniaActivity? _context;
        private Dictionary<string, ResolveInfo> _cachedActivityInfos = new Dictionary<string, ResolveInfo>();

        public PlatformTextProcessor(Context context)
        {
            _context = context as AvaloniaActivity;
        }

        public async Task<IEnumerable<TextProcessingAction>> GetActions()
        {
            List<TextProcessingAction> actions = [];
            if (_context?.PackageManager is not { } packageManager)
            {
                return actions;
            }

            var intent = new Intent()
                .SetAction(Intent.ActionProcessText)
                .SetType("text/plain");

            var activities = packageManager.QueryIntentActivities(intent, 0);
            _cachedActivityInfos.Clear();

            foreach (var activity in activities)
            {
                if (activity.ActivityInfo?.Name is { } name)
                {
                    actions.Add(new TextProcessingAction()
                    {
                        Id = name,
                        Label = activity.LoadLabel(packageManager)
                    });
                    _cachedActivityInfos.Add(name, activity);
                }
            }

            return actions;
        }

        public async Task<string?> ProcessText(object id, string text, bool isReadOnly)
        {
            if (_cachedActivityInfos == null ||
                _context == null ||
                id is not string key ||
                !_cachedActivityInfos.TryGetValue(key, out var activityInfo) ||
                activityInfo.ActivityInfo == null ||
                activityInfo.ActivityInfo.Name is not { } name ||
                activityInfo.ActivityInfo.PackageName is not { } package)
                return null;

            var intent = new Intent()
                .SetClassName(package, name)
                .SetAction(Intent.ActionProcessText)
                .SetType("text/plain")
                .PutExtra(Intent.ExtraProcessText, text)
                .PutExtra(Intent.ExtraProcessTextReadonly, isReadOnly);

            var currentRequestCode = PlatformSupport.GetNextRequestCode();

            var taskCompletionSource = new TaskCompletionSource<string?>();

            _context.ActivityResult += OnActivityResult;

            void OnActivityResult(int arg1, Result result, Intent? intent)
            {
                if (arg1 == currentRequestCode)
                {
                    _context.ActivityResult -= OnActivityResult;

                    taskCompletionSource.SetResult(intent?.DataString);
                }
            }

            _context.StartActivityForResult(intent, currentRequestCode);

            var result = await taskCompletionSource.Task;

            return result;
        }
    }
}
