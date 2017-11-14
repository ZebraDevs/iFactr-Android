using System;
using Android.Content;
using Android.Locations;
using iFactr.Integrations;
using iFactr.Core;

namespace iFactr.Droid
{
    public class GeoLocation : IGeoLocation
    {
        public event EventHandler<GeoLocationEventArgs> LocationUpdated;

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

        private readonly LocationManager _manager;
        private readonly LocationListener _listener;

        public GeoLocation()
        {
            _manager = (LocationManager)DroidFactory.MainActivity.GetSystemService(Context.LocationService);
            _listener = new LocationListener();
            _listener.Add(new LocationReceiver(this));
        }

        public void Start()
        {
            if (IsActive) return;
            try { _manager.RequestLocationUpdates(LocationManager.NetworkProvider, MinimumTime, MinimumDistance, _listener); }
            catch (Exception e) { iApp.Log.Warn("Network provider failed to initialize. Defaulting to GPS-only.", e); }
            _manager.RequestLocationUpdates(LocationManager.GpsProvider, MinimumTime, MinimumDistance, _listener);
            IsActive = true;
        }

        public void Stop()
        {
            _manager.RemoveUpdates(_listener);
            IsActive = false;
        }

        private void OnLocationUpdated(GeoLocationData data)
        {
            LocationUpdated?.Invoke(this, new GeoLocationEventArgs(data));
        }

        private class LocationReceiver : Java.Lang.Object, ILocationReceiver
        {
            private readonly GeoLocation _geoLocation;

            public LocationReceiver(GeoLocation geo)
            {
                _geoLocation = geo;
            }

            public void OnChanged(Location location)
            {
                _geoLocation.OnLocationUpdated(new GeoLocationData(location.Latitude, location.Longitude));
            }
        }
    }
}