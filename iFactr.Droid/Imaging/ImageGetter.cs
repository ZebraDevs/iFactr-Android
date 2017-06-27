using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Text;
using Android.Util;
using MonoCross;
using MonoCross.Utilities;
using MonoCross.Utilities.Storage;
using iFactr.UI;
using iFactr.Core;

namespace iFactr.Droid
{
    public class ImageGetter : Java.Lang.Object, Html.IImageGetter
    {
        public static Android.Content.Res.Resources Resources { get; }
        private static readonly Dictionary<string, List<Action<Bitmap, string, bool>>> PendingDownloads = new Dictionary<string, List<Action<Bitmap, string, bool>>>();

        static ImageGetter()
        {
            var context = AndroidDevice.Instance.Context;
            var metrics = new DisplayMetrics();
            context.WindowManager.DefaultDisplay.GetMetrics(metrics);
            Resources = new Android.Content.Res.Resources(context.Assets, metrics, context.Resources.Configuration);

            foreach (var f in Device.File.GetFileNames(Environment.GetFolderPath(Environment.SpecialFolder.Personal))
                .Where(file => file.EndsWith(".urlimage") && DateTime.UtcNow > new FileInfo(file).LastWriteTimeUtc))
            {
                Device.File.Delete(f);
            }
        }

        public Drawable GetDrawable(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) return null;
            Drawable drawable = null;
            var mre = new ManualResetEventSlim();
            SetDrawable(source, (result, url, fromCache) =>
            {
                drawable = new BitmapDrawable(Resources, result);
                mre.Set();
            });
            mre.Wait(500);
            return drawable;
        }

        public static async void SetDrawable(string url, Action<Bitmap, string, bool> callback, ImageCreationOptions options = ImageCreationOptions.None, TimeSpan cacheDuration = default(TimeSpan))
        {
            if (callback == null) return;

            if (string.IsNullOrEmpty(url))
            {
                callback.Invoke(null, url, false);
                return;
            }

            #region Check memory cache

            var skipCache = (options & ImageCreationOptions.IgnoreCache) == ImageCreationOptions.IgnoreCache;
            var cached = skipCache ? null : Device.ImageCache.Get(url);
            var droidImage = cached as ImageData;
            if (droidImage != null)
            {
                callback.Invoke(droidImage.Bitmap, url, true);
                return;
            }

            #endregion

            #region Try to parse assets and resources syncronously

            var storage = (AndroidFile)Device.File;
            if (!url.StartsWith("data"))
            {
                Stream assetStream;
                Bitmap bitmap = null;
                var resourceId = storage.ResourceFromFileName(url);
                if (resourceId > 0)
                {
                    bitmap = ((BitmapDrawable)Resources.GetDrawable(resourceId)).Bitmap;
                }
                else if ((assetStream = storage.GetAsset(url)) != null)
                {
                    bitmap = BitmapFactory.DecodeStream(assetStream);
                }

                if (bitmap != null)
                {
                    Device.ImageCache.Add(url, new ImageData(bitmap, url));
                    callback.Invoke(bitmap, url, false);
                    return;
                }
            }

            #endregion

            //Check to see if another view is already waiting for this url so we don't download it again
            var currentDownload = PendingDownloads.GetValueOrDefault(url);
            if (currentDownload != null)
            {
                currentDownload.Add(callback);
                return;
            }

            PendingDownloads[url] = new List<Action<Bitmap, string, bool>> { callback };
            ImageData drawable = null;

            await Task.Factory.StartNew(() =>
            {
                var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                var cacheFile = System.IO.Path.Combine(baseDir, url.GetHashCode().ToString(CultureInfo.InvariantCulture).Replace("-", "N") + ".urlimage");
                if (storage.Exists(cacheFile))
                {
                    try
                    {
                        if (cacheDuration == default(TimeSpan)) cacheDuration = Timeout.InfiniteTimeSpan;
                        if (cacheDuration == Timeout.InfiniteTimeSpan)
                        {
                            drawable = new ImageData(LoadFromStorage(cacheFile, 0, 0), cacheFile);
                        }
                        else
                        {
                            Device.Log.Debug("Refreshing Expired File in Cache: " + cacheFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        Device.Log.Debug("File Cache Exception " + ex);
                    }
                }

                if (drawable == null)
                {
                    byte[] bytes = null;

                    if (cached != null)
                    {
                        bytes = cached.GetBytes();
                    }
                    else if (url.StartsWith("data:"))
                    {
                        string ext;
                        bytes = ImageUtility.DecodeImageFromDataUri(url, out ext);
                        cacheFile = null;
                    }
                    else if (!storage.Exists(url))
                    {
                        try
                        {
                            var uri = Java.Net.URI.Create(url);
                            bytes = string.IsNullOrEmpty(uri.Scheme) ? null : Device.Network.GetBytes(url);
                            if (bytes == null || bytes.Length == 0)
                            {
                                Device.Log.Warn("Image load failed: {0}", url);
                            }
                            else
                            {
                                storage.Save(cacheFile, bytes);
                            }
                        }
                        catch (Exception ex)
                        {
                            Device.Log.Error("Image download failed", ex);
                        }
                    }

                    if (bytes != null && bytes.Length > 0)
                    {
                        drawable = new ImageData(BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, new BitmapFactory.Options
                        {
                            InDensity = (int)DisplayMetricsDensity.Default,
                            InTargetDensity = (int)((int)DisplayMetricsDensity.Default * DroidFactory.DisplayScale),
                        }), cacheFile);
                    }

                    if (!skipCache && drawable != null)
                    {
                        Device.ImageCache.Add(url, drawable);
                    }
                }
            });

            var downloads = PendingDownloads[url];
            PendingDownloads.Remove(url);
            foreach (var iv in downloads)
            {
                iv.Invoke(drawable?.Bitmap, url, false);
            }
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
                    var bytes = iApp.File.Read(url);
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

            var exif = new Android.Media.ExifInterface(fetchPath);
            var orientation = exif.GetAttributeInt(Android.Media.ExifInterface.TagOrientation, 1);
            var matrix = new Matrix();
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