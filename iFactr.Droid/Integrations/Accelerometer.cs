using System;
using Android.Content;
using Android.Hardware;
using iFactr.Integrations;

namespace iFactr.Droid
{
    public class Accelerometer : IAccelerometer
    {
        public event EventHandler<AccelerometerEventArgs> ValuesUpdated;

        public bool IsActive { get; private set; }

        private readonly SensorManager _manager;
        private readonly AccelerometerListener _listener;

        public Accelerometer()
        {
            _manager = (SensorManager)DroidFactory.MainActivity.GetSystemService(Context.SensorService);
            _listener = new AccelerometerListener(this);
        }

        public void Start()
        {
            if (IsActive) return;
            _manager.RegisterListener(_listener, _manager.GetDefaultSensor(SensorType.Accelerometer), SensorDelay.Normal);
            IsActive = true;
        }

        public void Stop()
        {
            _manager.UnregisterListener(_listener);
            IsActive = false;
        }

        private void OnValuesUpdated(AccelerometerData data)
        {
            ValuesUpdated?.Invoke(this, new AccelerometerEventArgs(data));
        }

        private class AccelerometerListener : Java.Lang.Object, ISensorEventListener
        {
            private readonly Accelerometer _accelerometer;

            public AccelerometerListener(Accelerometer accel)
            {
                _accelerometer = accel;
            }

            public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy) { }

            public void OnSensorChanged(SensorEvent e)
            {
                if (e.Sensor.Type == SensorType.Accelerometer)
                {
                    _accelerometer.OnValuesUpdated(new AccelerometerData(e.Values[0], e.Values[1], e.Values[2]));
                }
            }
        }
    }
}