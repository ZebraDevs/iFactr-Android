using System;
using Android.Content;
using Android.Locations;
using Android.Hardware;
using MonoCross;
using iFactr.Integrations;
using iFactr.Core;

namespace iFactr.Droid
{
    public class Compass : ICompass
    {
        public event EventHandler<HeadingEventArgs> HeadingUpdated;

        public bool IsActive { get; private set; }

        /// <summary>
        /// Gets or sets the minimum time interval between location updates, in milliseconds.
        /// </summary>
        public long MinimumTime { get; set; }

        /// <summary>
        ///  Gets or sets minimum distance between location updates, in meters. Location updates will
        ///  occur when this and the Minimum Time threshold has been met.
        ///  the location update
        /// </summary>
        public float MinimumDistance { get; set; }

        private readonly SensorManager _sensorManager;
        private readonly LocationManager _locationManager;
        private readonly CompassListener _compassListener;
        private readonly LocationListener _locationListener;

        public Compass()
        {
            _sensorManager = (SensorManager)DroidFactory.MainActivity.GetSystemService(Context.SensorService);
            _locationManager = (LocationManager)DroidFactory.MainActivity.GetSystemService(Context.LocationService);
            _compassListener = new CompassListener(this);
            _locationListener = new LocationListener();
            _locationListener.Add(_compassListener);
        }

        public void Start()
        {
            if (IsActive) return;
            _sensorManager.RegisterListener(_compassListener, _sensorManager.GetDefaultSensor(SensorType.Accelerometer), SensorDelay.Game);
            _sensorManager.RegisterListener(_compassListener, _sensorManager.GetDefaultSensor(SensorType.MagneticField), SensorDelay.Game);
            try { _locationManager.RequestLocationUpdates(LocationManager.NetworkProvider, MinimumTime, MinimumDistance, _locationListener); }
            catch (Exception e) { iApp.Log.Warn("Network provider failed to initialize.", e); }
            IsActive = true;
        }

        public void Stop()
        {
            _sensorManager.UnregisterListener(_compassListener);
            _locationManager.RemoveUpdates(_locationListener);
            IsActive = false;
        }

        private void OnHeadingUpdated(HeadingData data)
        {
            HeadingUpdated?.Invoke(this, new HeadingEventArgs(data));
        }

        private class CompassListener : Java.Lang.Object, ISensorEventListener, ILocationReceiver
        {
            private readonly Compass _compass;
            private readonly SensorStatus[] _currentAccuracies = new SensorStatus[2];
            private readonly float[] _gravityValues = new float[3];
            private readonly float[] _magneticValues = new float[3];
            private readonly float[] _rotationMatrix = new float[16];
            private readonly float[] _inclinationMatrix = new float[16];
            private readonly float[] _orientation = new float[3];
            private HeadingData _currentHeading;
            private GeomagneticField _geoField;
            private DateTime _lastUpdate = DateTime.UtcNow;

            public CompassListener(Compass comp)
            {
                _compass = comp;
            }

            public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy) { }

            public void OnSensorChanged(SensorEvent e)
            {
                float[] data;
                var type = e.Sensor.Type;
                switch (type)
                {
                    case SensorType.Accelerometer:
                        if (e.Accuracy < _currentAccuracies[0])
                            return;
                    
                        _currentAccuracies[0] = e.Accuracy;
                        data = _gravityValues;
                        break;
                    case SensorType.MagneticField:
                        if (e.Accuracy < _currentAccuracies[1])
                            return;
                    
                        _currentAccuracies[1] = e.Accuracy;
                        data = _magneticValues;
                        break;
                    default:
                        return;
                }

                for (var i = 0; i < 3; i++)
                    data[i] = e.Values[i];

                SensorManager.GetRotationMatrix(_rotationMatrix, _inclinationMatrix, _gravityValues, _magneticValues);
                SensorManager.GetOrientation(_rotationMatrix, _orientation);

                OnHeadingChanged();
            }

            public void OnChanged(Location location)
            {
                _geoField = new GeomagneticField((float)location.Latitude, (float)location.Longitude, (float)location.Altitude, location.Time);
                OnHeadingChanged();
            }

            private void OnHeadingChanged()
            {
                if (_lastUpdate.AddMilliseconds(_compass.MinimumTime) >= DateTime.UtcNow) return;
                _lastUpdate = DateTime.UtcNow;
                var heading = (_orientation[0] * (180.0f / Math.PI) + 360) % 360;
                var data = new HeadingData(heading + (_geoField?.Declination ?? 0), heading);
                if (Math.Abs(data.TrueHeading - _currentHeading.TrueHeading) < .001) return;
                _currentHeading = data;
                _compass.OnHeadingUpdated(_currentHeading);
            }
        }
    }
}