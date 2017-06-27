using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Content;
using Android.Provider;
using Android.Webkit;
using Android.Widget;
using iFactr.UI;
using Uri = Android.Net.Uri;
using iFactr.Core;
using MonoCross.Utilities;

namespace iFactr.Droid
{
    public static class VideoRecordingExtensions
    {
        public const string Scheme = "videorecording://";
        public const string CallbackParam = "VideoId";
        private static string _callback;
        public const int VideoResult = 14;
        public const int VideoGalleryResult = 15;

        public static void Launch(Link link)
        {
            _callback = link.Parameters.GetValueOrDefault("callback");

            const string Video = "video";
            const string Gallery = "gallery";
            var cameraEnabled = link.Parameters.GetValueOrDefault(Video).TryParseBoolean(true);
            var galleryEnabled = link.Parameters.GetValueOrDefault(Gallery).TryParseBoolean(true);

            if (cameraEnabled && galleryEnabled)
            {
                new AlertDialog.Builder(DroidFactory.MainActivity)
                    .SetCancelable(true)
                    .SetMessage(iApp.Factory.GetResourceString("VideoChoose") ?? "Do you want to make a new video or choose one from the gallery?")
                    .SetPositiveButton(iApp.Factory.GetResourceString("RecordVideo") ?? "Record A Video", (o, e) => StartVideo())
                    .SetNegativeButton(iApp.Factory.GetResourceString("ChooseVideo") ?? "Choose A Video", (o, e) => StartGallery())
                    .Show();
            }
            else if (cameraEnabled)
            {
                StartVideo();
            }
            else if (galleryEnabled)
            {
                StartGallery();
            }
        }

        private static void StartVideo()
        {
            var intent = new Intent(MediaStore.ActionVideoCapture);
            DroidFactory.MainActivity.StartActivityForResult(intent, VideoResult);
        }

        private static void StartGallery()
        {
            var photoPickerIntent = new Intent(Intent.ActionGetContent);
            photoPickerIntent.SetType("video/*");
            photoPickerIntent.SetAction(Intent.ActionGetContent);
            DroidFactory.MainActivity.StartActivityForResult(Intent.CreateChooser(photoPickerIntent, iApp.Factory.GetResourceString("ChooseVideo") ?? "Choose A Video"), VideoGalleryResult);
        }

        public static void OnNewVideoResult(Uri result)
        {
            OnVideoResult(result, true);
        }

        public static void OnVideoResult(Uri result, bool deleteFile = false)
        {
            var loader = ProgressDialog.Show(DroidFactory.MainActivity, string.Empty, iApp.Factory.GetResourceString("SaveVideo") ?? "Saving video...", true);
            iApp.Thread.QueueWorker(o =>
            {
                string extension = null;
                var uriString = result?.ToString().ToLowerInvariant() ?? string.Empty;
                if (uriString.StartsWith("content:"))
                {
                    extension = MimeTypeMap.Singleton.GetExtensionFromMimeType(DroidFactory.MainActivity.ContentResolver.GetType(result));
                }
                else if (uriString.StartsWith("file://"))
                {
                    extension = uriString.Substring(uriString.LastIndexOf('.') + 1);
                }

                if (extension == null)
                {
                    Toast.MakeText(DroidFactory.MainActivity, iApp.Factory.GetResourceString("InvalidFile") ?? "Invalid file", ToastLength.Short).Show();
                    return;
                }

                var mime = MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension);
                if (mime == null || !mime.StartsWith("video"))
                {
                    Toast.MakeText(DroidFactory.MainActivity, iApp.Factory.GetResourceString("InvalidFile") ?? "Invalid file", ToastLength.Short).Show();
                    return;
                }

                string videoId = null;
                try
                {
                    videoId = DroidFactory.Instance.StoreImage(result, extension);
                    if (deleteFile)
                        DroidFactory.MainActivity.ContentResolver.Delete(result, null, null);
                }
                catch (IOException e)
                {
                    iApp.Log.Error(e);
                    Toast.MakeText(DroidFactory.MainActivity, iApp.Factory.GetResourceString("VideoError") ?? "There was a problem saving the video. Please check your disk usage.", ToastLength.Long).Show();
                }

                DroidFactory.MainActivity.RunOnUiThread(loader.Dismiss);

                if (_callback == null) return;
                DroidFactory.Navigate(new Link(_callback, new Dictionary<string, string> { { CallbackParam, videoId } }));
                _callback = null;
            });
        }
    }
}