using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.Provider;
using Android.Webkit;
using Android.Widget;
using Java.IO;
using MonoCross.Utilities;
using iFactr.UI;
using Environment = Android.OS.Environment;
using IOException = System.IO.IOException;
using Uri = Android.Net.Uri;
using iFactr.Core;

namespace iFactr.Droid
{
    public static class CameraExtensions
    {
        public const string Scheme = "image://";
        public const string CallbackParam = "PhotoImage";
        public const int ImageResult = 12;
        public const int GalleryResult = 13;
        private static string _callback;

        public static void Launch(Link link)
        {
            _callback = link.Parameters.GetValueOrDefault("callback");

            const string Camera = "camera";
            const string Gallery = "gallery";

            var cameraEnabled = DroidFactory.MainActivity.PackageManager.HasSystemFeature(PackageManager.FeatureCamera) &&
                                link.Parameters.GetValueOrDefault(Camera).TryParseBoolean(true);
            var galleryEnabled = link.Parameters.GetValueOrDefault(Gallery).TryParseBoolean(true);

            if (cameraEnabled && galleryEnabled)
            {
                new AlertDialog.Builder(DroidFactory.MainActivity)
                    .SetCancelable(true)
                    .SetMessage(iApp.Factory.GetResourceString("PhotoChoose") ?? "Do you want to take a photo or choose one from the gallery?")
                    .SetPositiveButton(iApp.Factory.GetResourceString("TakePhoto") ?? "Take A Photo", (o, e) => StartCamera())
                    .SetNegativeButton(iApp.Factory.GetResourceString("ChoosePhoto") ?? "Choose A Photo", (o, e) => StartGallery())
                    .Show();
            }
            else if (cameraEnabled)
            {
                StartCamera();
            }
            else if (galleryEnabled)
            {
                StartGallery();
            }
        }

        private static void StartCamera()
        {
            var intent = new Intent(MediaStore.ActionImageCapture);
            // Specify the output. This will be unique.
            _currentFile = new File(Environment.ExternalStorageDirectory, Guid.NewGuid() + ".jpg");
            intent.PutExtra(MediaStore.ExtraOutput, Uri.FromFile(_currentFile));
            // Keep a list for afterwards
            FillPhotoList();
            // finally start the intent and wait for a result.
            DroidFactory.MainActivity.StartActivityForResult(intent, ImageResult);
        }

        private static void StartGallery()
        {
            var photoPickerIntent = new Intent(Intent.ActionGetContent);
            photoPickerIntent.SetType("image/*");
            photoPickerIntent.SetAction(Intent.ActionGetContent);
            DroidFactory.MainActivity.StartActivityForResult(Intent.CreateChooser(photoPickerIntent, iApp.Factory.GetResourceString("ChoosePhoto") ?? "Choose A Photo"), GalleryResult);
        }

        public static void OnCameraResult()
        {
            // Some versions of Android save to the MediaStore as well as the ExtraOutput.
            // Not sure why!  We don't know what name Android will give either, so we get
            // to search for this manually and remove it.  
            string[] projection = { MediaStore.Images.ImageColumns.Size,
                                 MediaStore.Images.ImageColumns.DisplayName,
                                 MediaStore.Images.ImageColumns.Data,
                                 BaseColumns.Id };
            // intialize the Uri and the Cursor, and the current expected size.
            ICursor c = null;
            var u = MediaStore.Images.Media.ExternalContentUri;
            if (_currentFile == null) return;
            // Query the Uri to get the data path.  Only if the Uri is valid,
            // and we had a valid size to be searching for.
            if ((u != null) && (_currentFile.Length() > 0))
            {
                c = DroidFactory.MainActivity.ManagedQuery(u, projection, null, null, null);
            }
            // If we found the cursor and found a record in it (we also have the size).
            if ((c != null) && c.MoveToFirst())
            {
                do
                {
                    // Check each previously-built area in the gallery.
                    if (GalleryList.Any(sGallery => string.Equals(sGallery, c.GetString(1), StringComparison.InvariantCultureIgnoreCase)))
                        continue;

                    // This is the NEW image.  If the size is bigger, copy it.
                    // Then delete it!
                    var f = new File(c.GetString(2));

                    // Ensure it's there, check size, and delete!
                    if (f.Exists() && (_currentFile.Length() < c.GetLong(0)) && _currentFile.Delete())
                    {
                        // Finally we can stop the copy.
                        try
                        {
                            FileInputStream source = null;
                            try
                            {
                                source = new FileInputStream(f);
                                var fileContent = new byte[f.Length()];
                                source.Read(fileContent);
                                iApp.File.Save(_currentFile.AbsolutePath, fileContent, EncryptionMode.NoEncryption);
                            }
                            finally
                            {
                                source?.Close();
                            }
                        }
                        catch (IOException)
                        {
                            // Could not copy the file over.
                            Toast.MakeText(DroidFactory.MainActivity, iApp.Factory.GetResourceString("ErrorText") ?? "An error occurred. Please try again.", ToastLength.Short).Show();
                        }
                    }
                    DroidFactory.MainActivity.ContentResolver.Delete(MediaStore.Images.Media.ExternalContentUri, BaseColumns.Id + "=" + c.GetString(3), null);
                    break;
                }
                while (c.MoveToNext());
            }

            var id = _currentFile.Name.Remove(_currentFile.Name.LastIndexOf('.'));
            DroidFactory.Instance.StoreImage(Uri.FromFile(_currentFile), "jpg", id);
            iApp.File.Delete(_currentFile.Path);

            if (_callback == null) return;
            DroidFactory.Navigate(new Link(_callback, new Dictionary<string, string> { { CallbackParam, id }, }));
            _callback = null;
        }

        public static void OnGalleryResult(Uri uri)
        {
            var extension = "jpg";
            var uriString = uri?.ToString().ToLowerInvariant() ?? string.Empty;
            if (uriString.StartsWith("content:"))
            {
                extension = MimeTypeMap.Singleton.GetExtensionFromMimeType(DroidFactory.MainActivity.ContentResolver.GetType(uri));
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
            if (mime == null || !mime.StartsWith("image"))
            {
                Toast.MakeText(DroidFactory.MainActivity, iApp.Factory.GetResourceString("InvalidFile") ?? "Invalid file", ToastLength.Short).Show();
                return;
            }

            var imageId = DroidFactory.Instance.StoreImage(uri, extension);
            if (_callback == null) return;
            DroidFactory.Navigate(new Link(_callback, new Dictionary<string, string> { { CallbackParam, imageId }, }));
            _callback = null;
        }

        private static File _currentFile;
        private static readonly List<string> GalleryList = new List<string>();

        private static void FillPhotoList()
        {
            // initialize the list!
            GalleryList.Clear();
            string[] projection = { MediaStore.Images.ImageColumns.DisplayName };
            // intialize the Uri and the Cursor, and the current expected size.
            ICursor c = null;
            var u = MediaStore.Images.Media.ExternalContentUri;
            // Query the Uri to get the data path.  Only if the Uri is valid.
            if (u != null)
            {
                c = DroidFactory.MainActivity.ManagedQuery(u, projection, null, null, null);
            }

            if (c == null || !c.MoveToFirst()) return;

            // If we found the cursor and found a record in it (we also have the id).
            do
            {
                // Loop each and add to the list.
                var entry = c.GetString(0);
                if (!string.IsNullOrEmpty(entry))
                    GalleryList.Add(entry);
            }
            while (c.MoveToNext());
        }
    }
}