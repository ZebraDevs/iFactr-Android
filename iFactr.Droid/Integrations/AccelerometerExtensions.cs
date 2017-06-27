using System.Collections.Generic;
using System.Globalization;
using Android.App;
using Android.Content;
using Android.Hardware;
using iFactr.Core;
using iFactr.Core.Integrations;
using iFactr.Core.Utilities;
using Java.Lang;

namespace iFactr.Droid
{
    public static class AccelerometerExtensions
    {
        public const string Scheme = "accel://";
        private static Accelerometer _accel;
        private static ProgressDialog locationGetter;

        public static void Launch(string url)
        {
            var parameters = HttpUtility.ParseQueryString(url.Substring(Scheme.Length));

            if (parameters == null) return;
            const string CallbackUri = "callback";
            new Accelerometer(parameters.ContainsKey(CallbackUri) ? parameters[CallbackUri] : null, null).Launch();
        }

        public static void Launch(this Accelerometer accelerometer)
        {
            _accel = accelerometer;
            locationGetter = ProgressDialog.Show(DroidFactory.MainActivity, null, "Obtaining acceleration...", true, false);
            var sman = (SensorManager)DroidFactory.MainActivity.GetSystemService(Context.SensorService);
            var c = new AccelerometerSensorEventListener();
            sman.RegisterListener(c, sman.GetDefaultSensor(SensorType.Accelerometer), SensorDelay.Normal);
        }

        private class AccelerometerSensorEventListener : Object, ISensorEventListener
        {
            public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy) { }

            private int mCount;
            private readonly float[] data = new float[3];

            public void OnSensorChanged(SensorEvent e)
            {
                for (int i = 0; i < 3; i++)
                    data[i] += e.Values[i];

                if (mCount++ <= 10) return;
                mCount = 0;

                ((SensorManager)DroidFactory.MainActivity.GetSystemService(Context.SensorService)).
                    UnregisterListener(this);
                if (locationGetter != null)
                {
                    locationGetter.Dismiss();
                    locationGetter = null;
                }

                if (_accel.CallbackUrl != null)
                {
                    DroidFactory.Navigate(_accel, new Dictionary<string, string>
                    {
                        { "X", (data[0] / 10).ToString(CultureInfo.InvariantCulture) },
                        { "Y", (data[1] / 10).ToString(CultureInfo.InvariantCulture) },
                        { "Z", (data[2] / 10).ToString(CultureInfo.InvariantCulture) },
                    });
                }

                _accel = null;
            }
        }
    }
}