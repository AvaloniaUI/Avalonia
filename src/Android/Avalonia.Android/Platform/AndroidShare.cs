using System.Threading.Tasks;
using Android.Content;
using Android.Webkit;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Android.Net;
using System.Collections.Generic;
using Android.OS;

namespace Avalonia.Android.Platform
{
    internal class AndroidShare : IShare
    {
        private readonly Context _context;

        public AndroidShare(Context context)
        {
            _context = context;
        }

        public void Share(string text)
        {
            var intent = new Intent(Intent.ActionSend);
            intent.SetType("text/plain");
            intent.SetAction(Intent.ActionSend);
            intent.PutExtra(Intent.ExtraText, text);

            var shareIntent = Intent.CreateChooser(intent, "Sharing Text");
            _context.StartActivity(shareIntent);
        }

        public void Share(IStorageFile file)
        {
            if (file.TryGetUri(out var uri))
            {
                string mimeType = MimeTypeMap.Singleton.GetMimeTypeFromExtension(System.IO.Path.GetExtension(file.Name).Remove(0, 1));

                var intent = new Intent(Intent.ActionSend);
                intent.SetType(mimeType);
                intent.SetAction(Intent.ActionSend);
                intent.PutExtra(Intent.ExtraStream, Uri.Parse(uri.AbsoluteUri));
                intent.SetFlags(ActivityFlags.GrantReadUriPermission);

                var shareIntent = Intent.CreateChooser(intent, "Sharing File");
                _context.StartActivity(shareIntent);
            }
        }

        public void Share(IList<IStorageFile> files)
        {
            IList<IParcelable> uris = new List<IParcelable>();

            string mimeType = null;

            foreach (var file in files)
            {
                if (file.TryGetUri(out var uri))
                {
                    uris.Add(Uri.Parse(uri.AbsoluteUri));
                }

                var fileMimeType = MimeTypeMap.Singleton.GetMimeTypeFromExtension(System.IO.Path.GetExtension(file.Name).Remove(0, 1));
                if (mimeType == null)
                {
                    mimeType = fileMimeType;
                }
                else if(mimeType != fileMimeType)
                {
                    mimeType = "application/octet-stream";
                }
            }

            var intent = new Intent(Intent.ActionSend);
            intent.SetType(mimeType);
            intent.SetAction(Intent.ActionSendMultiple);
            intent.PutParcelableArrayListExtra(Intent.ExtraStream, uris);
            intent.SetFlags(ActivityFlags.GrantReadUriPermission);

            var shareIntent = Intent.CreateChooser(intent, "Sharing File");
            _context.StartActivity(shareIntent);
        }
    }
}
