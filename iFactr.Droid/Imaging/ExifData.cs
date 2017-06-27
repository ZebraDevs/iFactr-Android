using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Android.Media;
using MonoCross;

namespace iFactr.Droid
{
    public class ExifData : IExifData
    {
        public ExifData(string filename)
        {
            var exif = new ExifInterface(filename);
            _rawData = new Dictionary<string, object>
            {
                {ExifInterface.TagAperture, exif.GetAttributeDouble(ExifInterface.TagAperture,1)},
                {ExifInterface.TagDatetime, exif.GetAttribute(ExifInterface.TagDatetime)},
                {ExifInterface.TagExposureTime, exif.GetAttribute(ExifInterface.TagExposureTime)},
                {ExifInterface.TagFlash, exif.GetAttribute(ExifInterface.TagFlash)},
                {ExifInterface.TagFocalLength, exif.GetAttributeDouble(ExifInterface.TagFocalLength, 0)},
                {ExifInterface.TagGpsAltitude, exif.GetAttribute(ExifInterface.TagGpsAltitude)},
                {ExifInterface.TagGpsAltitudeRef, exif.GetAttribute(ExifInterface.TagGpsAltitudeRef)},
                {ExifInterface.TagGpsDatestamp, exif.GetAttribute(ExifInterface.TagGpsDatestamp)},
                {ExifInterface.TagGpsLatitude, exif.GetAttribute(ExifInterface.TagGpsLatitude)},
                {ExifInterface.TagGpsLatitudeRef, exif.GetAttribute(ExifInterface.TagGpsLatitudeRef)},
                {ExifInterface.TagGpsLongitude, exif.GetAttribute(ExifInterface.TagGpsLongitude)},
                {ExifInterface.TagGpsLongitudeRef, exif.GetAttribute(ExifInterface.TagGpsLongitudeRef)},
                {ExifInterface.TagGpsProcessingMethod, exif.GetAttribute(ExifInterface.TagGpsProcessingMethod)},
                {ExifInterface.TagGpsTimestamp, exif.GetAttribute(ExifInterface.TagGpsTimestamp)},
                {ExifInterface.TagImageLength, exif.GetAttribute(ExifInterface.TagImageLength)},
                {ExifInterface.TagImageWidth, exif.GetAttribute(ExifInterface.TagImageWidth)},
                {ExifInterface.TagIso, exif.GetAttribute(ExifInterface.TagIso)},
                {ExifInterface.TagMake, exif.GetAttribute(ExifInterface.TagMake)},
                {ExifInterface.TagModel, exif.GetAttribute(ExifInterface.TagModel)},
                {ExifInterface.TagOrientation, exif.GetAttributeInt(ExifInterface.TagOrientation,-1)},
                {ExifInterface.TagWhiteBalance, exif.GetAttributeInt(ExifInterface.TagWhiteBalance,0)},
            };
        }

        public ExifData(Dictionary<string, object> rawData)
        {
            _rawData = rawData;
        }

        public double Aperture { get { return GetTagValue<double>(ExifInterface.TagAperture); } }
        public int ColorSpace { get { return GetTagValue<int>(ExifInterface.TagWhiteBalance); } }

        public DateTime DateTime
        {
            get
            {
                DateTime retval;
                DateTime.TryParse(GetTagValue<string>(ExifInterface.TagDatetime), out retval);
                return retval;
            }
        }

        public DateTime DateTimeDigitized { get { return DateTime; } }
        public DateTime DateTimeOriginal { get { return DateTime; } }
        public double DPIHeight { get { return GetTagValue<double>(); } }
        public double DPIWidth { get { return GetTagValue<double>(); } }
        public string ExposureProgram { get { return GetTagValue<string>(); } }
        public double ExposureTime { get { return GetTagValue<double>(); } }
        public int Flash { get { return GetTagValue<int>(); } }
        public double FNumber { get { return GetTagValue<double>(ExifInterface.TagAperture); } }
        public double FocalLength { get { return GetTagValue<double>(); } }
        public string Manufacturer { get { return GetTagValue<string>(ExifInterface.TagMake); } }
        public string Model { get { return GetTagValue<string>(ExifInterface.TagModel); } }
        public int Orientation { get { return GetTagValue<int>(); } }
        public double PixelHeight { get { return GetTagValue<double>(); } }
        public double PixelWidth { get { return GetTagValue<double>(); } }
        public double ShutterSpeed { get { return GetTagValue<double>(); } }
        public double XResolution { get { return GetTagValue<double>(); } }
        public double YResolution { get { return GetTagValue<double>(); } }

        private readonly Dictionary<string, object> _rawData;

        public IDictionary<string, object> GetRawData()
        {
            return _rawData.ToDictionary(k => k.Key, v => v.Value);
        }

        private T GetTagValue<T>([CallerMemberName] string propertyName = null)
        {
            return propertyName == null || !_rawData.ContainsKey(propertyName) ? default(T) : (T)_rawData[propertyName];
        }
    }
}