using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Text;
using Android.Util;
using iFactr.Core;
using iFactr.UI;
using MonoCross.Navigation;
using MonoCross.Utilities;
using MonoCross.Utilities.Storage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace iFactr.Droid
{
    public class ImageGetter : Java.Lang.Object, Html.IImageGetter
    {
        public static Android.Content.Res.Resources Resources { get; }
        private static readonly SerializableDictionary<string, List<Action<Drawable, string>>> PendingDownloads = new SerializableDictionary<string, List<Action<Drawable, string>>>();

        static ImageGetter()
        {
            Resources = DroidFactory.MainActivity.Resources;
        }

        public Drawable GetDrawable(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) return null;
            Drawable drawable = null;
            var mre = new ManualResetEventSlim();
            SetDrawable(source, (result, url) =>
            {
                drawable = result;
                mre.Set();
            });
            mre.Wait(2000);
            return drawable;
        }

        public static void SetDrawable(string url, Action<Drawable, string> callback, ImageCreationOptions options = ImageCreationOptions.None, TimeSpan cacheDuration = default(TimeSpan))
        {
            if (callback == null) return;

            if (string.IsNullOrEmpty(url))
            {
                callback.Invoke(null, url);
                return;
            }

            #region Check memory cache

            var skipCache = (options & ImageCreationOptions.IgnoreCache) == ImageCreationOptions.IgnoreCache;
            var cached = skipCache ? null : Device.ImageCache.Get(url);
            if (cached is ImageData droidImage)
            {
                callback.Invoke(new BitmapDrawable(Resources, droidImage.Bitmap), url);
                return;
            }

            #endregion

            if (cacheDuration == default(TimeSpan))
            {
                cacheDuration = Timeout.InfiniteTimeSpan;
            }

            //Check to see if another view is already waiting for this url so we don't download it again
            var currentDownload = PendingDownloads.GetValueOrDefault(url);
            if (currentDownload != null)
            {
                currentDownload.Add(callback);
                return;
            }
            else
            {
                PendingDownloads[url] = new List<Action<Drawable, string>> { callback };
            }

            Device.Thread.Start(() =>
            {
                var storage = (AndroidFile)Device.File;
                var cacheFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    url.GetHashCode().ToString(CultureInfo.InvariantCulture).Replace("-", "N") + ".urlimage");

                Bitmap bitmap = null;
                Stream assetStream;
                Drawable drawable = null;
                var resourceId = 0;

                if (url.StartsWith("data:"))
                {
                    // Load data URIs like files
                    if (!storage.Exists(cacheFile))
                    {
                        storage.Save(cacheFile, ImageUtility.DecodeImageFromDataUri(url, out string ext));
                    }
                }
                else if ((resourceId = storage.ResourceFromFileName(url)) > 0)
                {
                    drawable = Resources.GetDrawable(resourceId);
                }
                else if ((assetStream = storage.GetAsset(url)) != null)
                {
                    bitmap = BitmapFactory.DecodeStream(assetStream);
                }
                else if (cached != null)
                {
                    var bytes = cached.GetBytes();
                    if (bytes != null && bytes.Length > 0)
                    {
                        bitmap = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, new BitmapFactory.Options
                        {
                            InDensity = (int)DisplayMetricsDensity.Default,
                            InTargetDensity = (int)((int)DisplayMetricsDensity.Default * DroidFactory.DisplayScale),
                        });
                    }
                }
                else if (!storage.Exists(url) && (!storage.Exists(cacheFile) || new FileInfo(cacheFile).LastWriteTimeUtc > DateTime.UtcNow - cacheDuration))
                {
                    try
                    {
                        var uri = Java.Net.URI.Create(url);
                        var bytes = string.IsNullOrEmpty(uri.Scheme) ? null : Device.Network.GetBytes(url);
                        if (bytes != null && bytes.Length > 0)
                        {
                            storage.Save(cacheFile, bytes);
                        }
                    }
                    catch (Exception ex)
                    {
                        Device.Log.Error("Image download failed", ex);
                    }
                }
                else
                {
                    bitmap = LoadFromStorage(url, 0, 0);
                }

                if (storage.Exists(cacheFile))
                {
                    bitmap = LoadFromStorage(cacheFile, 0, 0);
                }

                if (!skipCache && bitmap != null)
                {
                    Device.ImageCache.Add(url, new ImageData(bitmap, url));
                }

                var downloads = PendingDownloads[url];
                PendingDownloads.Remove(url);
                if (drawable == null && bitmap != null)
                {
                    drawable = new BitmapDrawable(Resources, bitmap);
                }
                foreach (var iv in downloads)
                {
                    Device.Thread.ExecuteOnMainThread(() =>
                    {
                        iv.Invoke(drawable, url);
                    });
                }
            });
        }

        public static Bitmap LoadFromStorage(string url, double width, double height)
        {
            var options = new BitmapFactory.Options { InJustDecodeBounds = true, };
            double optionsWidth, optionsHeight;
            var fetchPath = url;

            if (iApp.Encryption.Required)
            {
                try
                {
                    var bytes = iApp.File.Read(fetchPath);
                    fetchPath = iApp.Factory.TempPath.AppendPath(url.GetHashCode() + System.IO.Path.GetExtension(url));
                    iApp.File.Save(fetchPath, bytes, EncryptionMode.NoEncryption);
                }
                catch (CryptographicException e)
                {
                    iApp.Log.Warn("Failed to decrypt file: {0}", e, url);
                }
            }

            using (BitmapFactory.DecodeFile(fetchPath, options))
            {
                optionsWidth = options.OutWidth;
                optionsHeight = options.OutHeight;
                if (optionsWidth < 1 || optionsHeight < 1)
                {
                    Device.Log.Error("Failed to read image from {0}", url);
                    return null;
                }
            }

            options = new BitmapFactory.Options
            {
                InMutable = true,
                InTargetDensity = 1,
            };

            //allow 5% margin of error before using resize algorithm
            if (width > 0 && optionsWidth > width * 1.05 && height > 0 && optionsHeight > height * 1.05)
            {
                options.InDensity = (int)Math.Ceiling(Math.Max(Math.Max(1, optionsWidth / width), optionsHeight / height));
            }

            var matrix = new Matrix();
            if (fetchPath.EndsWith(".jpg") || fetchPath.EndsWith(".jpeg"))
            {
                var exif = new Android.Media.ExifInterface(fetchPath);
                var orientation = exif.GetAttributeInt(Android.Media.ExifInterface.TagOrientation, 1);
                switch (orientation)
                {
                    case 6:
                        matrix.PostRotate(90);
                        break;
                    case 3:
                        matrix.PostRotate(180);
                        break;
                    case 8:
                        matrix.PostRotate(270);
                        break;
                }
            }

            Bitmap retval;
            try
            {
                using (var bit = BitmapFactory.DecodeFile(fetchPath, options))
                {
                    if (bit == null)
                    {
                        Device.Log.Error("Failed to read image from {0}", url);
                        return null;
                    }
                    retval = Bitmap.CreateBitmap(bit, 0, 0, bit.Width, bit.Height, matrix, true);
                    bit.Recycle();
                }
            }
            catch (Exception e)
            {
                Device.Log.Error("Failed to read [{0}] image to [{1}]", e, new UI.Size(optionsWidth, optionsHeight), new UI.Size(width, height));
                return null;
            }
            finally
            {
                if (fetchPath != url)
                {
                    iApp.File.Delete(fetchPath);
                }
            }

            return retval;
        }
    }
}