using System;
using System.Collections.Generic;
using System.Globalization;
using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using iFactr.Core;
using iFactr.Core.Utilities;
using Location = iFactr.Core.Integrations.Location;

namespace iFactr.Droid
{
    public static class GpsExtensions
    {
        public const string Scheme = "geoloc://";
        public const string CallbackParam = "Coords";
        private static Location _location;

        private static DismissHandler _handler;
        private static LocationListener _mlocListener;
        private static ProgressDialog _locationGetter;

        public static void Launch(string url)
        {
            var parameters = HttpUtility.ParseQueryString(url.Substring(Scheme.Length));

            if (parameters == null) return;
            const string callbackUri = "callback";
            new Location(parameters.ContainsKey(callbackUri) ? parameters[callbackUri] : null, CallbackParam).Launch();
        }

        public static void Launch(this Location location)
        {
            _location = location;

            var mlocManager = (LocationManager)DroidFactory.MainActivity.GetSystemService(Context.LocationService);
            _mlocListener = new LocationListener();
            _mlocListener.Add(new LocationReceiver());
            try { mlocManager.RequestLocationUpdates(LocationManager.NetworkProvider, 0, 0, _mlocListener); }
            catch (Exception e) { iApp.Log.Warn("Network provider failed to initialize. Defaulting to GPS-only.", e); }
            mlocManager.RequestLocationUpdates(LocationManager.GpsProvider, 0, 0, _mlocListener);
            _locationGetter = ProgressDialog.Show(DroidFactory.MainActivity, null, "Obtaining location...", true, true);
            _locationGetter.CancelEvent += (o, e) => Cleanup();
            _handler = new DismissHandler();
            _handler.SendMessageDelayed(new Message { What = 1, }, 30000);
        }

        private class LocationReceiver : ILocationReceiver
        {
            public void OnChanged(Android.Locations.Location location)
            {
                try
                {
                    if (_location.CallbackUrl == null) return;
                    DroidFactory.Navigate(_location, new Dictionary<string, string>
                    {
                        { "Lat", location.Latitude.ToString(CultureInfo.InvariantCulture) },
                        { "Lon", location.Longitude.ToString(CultureInfo.InvariantCulture) },
                    });
                }
                catch (Exception e)
                {
                    iApp.Log.Error(e);
                }
                finally
                {
                    Cleanup();
                }
            }
        }

        private static void Cleanup()
        {
            _location = null;

            if (_handler != null)
            {
                _handler.RemoveMessages(1);
                _handler = null;
            }
            if (_locationGetter != null)
            {
                _locationGetter.Dismiss();
                _locationGetter = null;
            }
            if (_mlocListener != null)
            {
                _mlocListener.RemoveAllReceivers();
                ((LocationManager)DroidFactory.MainActivity.GetSystemService(Context.LocationService)).RemoveUpdates(_mlocListener);
                _mlocListener = null;
            }
        }

        public class DismissHandler : Handler
        {
            public override void HandleMessage(Message msg)
            {
                DroidFactory.Navigate(_location);
                Cleanup();
            }
        }
    }
}