using System.IO;
using Android.Graphics;
using MonoCross;
using MonoCross.Utilities;

namespace iFactr.Droid
{
    public class ImageData : IImageData
    {
        public ImageFileFormat Format { get; }

        public Bitmap Bitmap { get; }

        public string Filename { get; private set; }

        public ImageData(Bitmap bitmap, string filename)
        {
            Bitmap = bitmap;
            Filename = filename;
        }

        public byte[] GetBytes()
        {
            return GetBytes(Format);
        }

        private byte[] GetBytes(ImageFileFormat format)
        {
            if (Bitmap == null) return null;
            var save = new MemoryStream();
            switch (format)
            {
                case ImageFileFormat.JPEG:
                    Bitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, save);
                    break;
                default:
                    Bitmap.Compress(Bitmap.CompressFormat.Png, 100, save);
                    break;
            }
            save.Position = 0;
            return save.GetBuffer();
        }

        public IExifData GetExifData()
        {
            if (!string.IsNullOrWhiteSpace(Filename) && Format == ImageFileFormat.JPEG)
            {
                return new ExifData(Filename);
            }
            return null;
        }

        public void Save(string filePath, ImageFileFormat format)
        {
            if (Filename == null)
            {
                Filename = filePath;
            }
            Device.File.Save(filePath, GetBytes(format));
        }
    }
}