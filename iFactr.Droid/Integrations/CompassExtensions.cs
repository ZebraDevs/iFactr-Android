using System.Collections.Generic;
using System.Globalization;
using Android.App;
using Android.Content;
using Android.Hardware;
using iFactr.Core;
using iFactr.Core.Integrations;
using iFactr.Core.Utilities;
using Java.Lang;
using Math = System.Math;

namespace iFactr.Droid
{
    public static class CompassExtensions
    {
        public const string CallbackParam = "Bearing";
        public const string Scheme = "compass://";
        internal static Compass _compass;
        private static ProgressDialog locationGetter;

        public static void Launch(string url)
        {
            var parameters = HttpUtility.ParseQueryString(url.Substring(Scheme.Length));

            if (parameters == null) return;
            const string CallbackUri = "callback";
            new Compass(parameters.ContainsKey(CallbackUri) ? parameters[CallbackUri] : null, CallbackParam).Launch();
        }

        public static void Launch(this Compass compass)
        {
            _compass = compass;
            locationGetter = ProgressDialog.Show(DroidFactory.MainActivity, null, "Obtaining bearing...", true, false);
            var sman = (SensorManager)DroidFactory.MainActivity.GetSystemService(Context.SensorService);
            var c = new CompassSensorEventListener();
            sman.RegisterListener(c, sman.GetDefaultSensor(SensorType.Accelerometer), SensorDelay.Normal);
            sman.RegisterListener(c, sman.GetDefaultSensor(SensorType.MagneticField), SensorDelay.Normal);
        }

        private class CompassSensorEventListener : Object, ISensorEventListener
        {
            private readonly float[] mGData = new float[3];
            private readonly float[] mMData = new float[3];
            private readonly float[] mR = new float[16];
            private readonly float[] mI = new float[16];
            private readonly float[] mOrientation = new float[3];

            public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
            {
            }

            private int mCount;
            public void OnSensorChanged(SensorEvent e)
            {
                float[] data;
                var type = e.Sensor.Type;
                switch (type)
                {
                    case SensorType.Accelerometer:
                        data = mGData;
                        break;
                    case SensorType.MagneticField:
                        data = mMData;
                        break;
                    default:
                        return;
                }

                for (var i = 0; i < 3; i++)
                    data[i] = e.Values[i];

                SensorManager.GetRotationMatrix(mR, mI, mGData, mMData);
                SensorManager.GetOrientation(mR, mOrientation);

                if (mCount++ <= 50) return;
                const float rad2deg = (float)(180.0f / Math.PI);
                mCount = 0;

                ((SensorManager)DroidFactory.MainActivity.GetSystemService(Context.SensorService)).UnregisterListener(this);
                if (locationGetter != null)
                {
                    locationGetter.Dismiss();
                    locationGetter = null;
                }
                if (_compass.CallbackUrl != null)
                {
                    DroidFactory.Navigate(_compass, new Dictionary<string, string> { { _compass.CallbackParam, (((mOrientation[0] * rad2deg) + 360) % 360).ToString(CultureInfo.InvariantCulture) } });
                }
                _compass = null;
            }
        }
    }
}