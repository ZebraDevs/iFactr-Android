using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Runtime;
using Android.Hardware;

namespace iFactr.Droid
{
    public class ShakeListener : Java.Lang.Object, ISensorEventListener
    {
        public event EventHandler ShakeOccurred;

        public float RateLimitMilliseconds { get; set; } = 1000;

        public ShakeListener()
        {
            _sensorMgr = (SensorManager)DroidFactory.MainActivity.GetSystemService(Context.SensorService);
        }

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent e)
        {
            if (e.Sensor.Type != SensorType.Accelerometer) return;
            var curTime = TimeSpan.FromTicks(DateTime.UtcNow.Ticks).TotalMilliseconds;
            var diffTime = curTime - _lastUpdate;
            if (diffTime < 100 || curTime - _lastShake < RateLimitMilliseconds) return;
            _lastUpdate = curTime;
            var speed = Math.Abs(e.Values.Sum() - _lastValues.Sum()) / diffTime * 10000;
            _lastValues = e.Values.ToList();

            if (speed < 2000) return;
            _lastShake = curTime;
            ShakeOccurred?.Invoke(this, EventArgs.Empty);
        }

        public bool Start()
        {
            return _sensorMgr.RegisterListener(this, _sensorMgr.GetDefaultSensor(SensorType.Accelerometer), SensorDelay.Ui);
        }

        public void Stop()
        {
            _sensorMgr.UnregisterListener(this);
        }

        private double _lastUpdate;
        private double _lastShake;
        private List<float> _lastValues = new List<float>();
        private readonly SensorManager _sensorMgr;
    }
}