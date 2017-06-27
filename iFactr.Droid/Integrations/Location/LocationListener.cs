using System.Collections.Generic;
using Android.Locations;
using Android.OS;
using Java.Lang;

namespace iFactr.Droid
{
    public class LocationListener : Object, ILocationListener
    {
        private const int TwoMinutes = 120000;
        private readonly List<ILocationReceiver> _receivers = new List<ILocationReceiver>();
        private Location _currentLocation;

        public void Add(ILocationReceiver receiver)
        {
            _receivers.Add(receiver);
        }

        public void RemoveAllReceivers()
        {
            _receivers.Clear();
        }

        void ILocationListener.OnLocationChanged(Location location)
        {
            if (!IsBetterLocation(location, _currentLocation)) return;
            _currentLocation = location;

            foreach (var receiver in new List<ILocationReceiver>(_receivers))
            {
                receiver.OnChanged(location);
            }
        }

        void ILocationListener.OnProviderDisabled(string provider) { }
        void ILocationListener.OnProviderEnabled(string provider) { }
        void ILocationListener.OnStatusChanged(string provider, Availability status, Bundle extras) { }

        /// <summary>
        /// Determines whether one Location reading is better than the current Location fix
        /// </summary>
        /// <param name="location">The new Location that you want to evaluate</param>
        /// <param name="currentBestLocation">The current Location fix, to which you want to compare the new one</param>
        /// <returns></returns>
        protected bool IsBetterLocation(Location location, Location currentBestLocation)
        {
            if (currentBestLocation == null)
            {
                // A new location is always better than no location
                return true;
            }

            // Check whether the new location fix is newer or older
            var timeDelta = location.Time - currentBestLocation.Time;
            var isSignificantlyNewer = timeDelta > TwoMinutes;
            var isSignificantlyOlder = timeDelta < -TwoMinutes;
            var isNewer = timeDelta > 0;

            if (isSignificantlyNewer)
            {
                // If it's been more than two minutes since the current location, use the new location because the user has likely moved
                return true;
            }
            if (isSignificantlyOlder)
            {
                // If the new location is more than two minutes older, it must be worse
                return false;
            }

            // Check whether the new location fix is more or less accurate
            var accuracyDelta = (int)(location.Accuracy - currentBestLocation.Accuracy);
            var isLessAccurate = accuracyDelta > 0;
            var isMoreAccurate = accuracyDelta < 0;
            var isSignificantlyLessAccurate = accuracyDelta > 200;

            // Check if the old and new location are from the same provider
            var isFromSameProvider = location.Provider == currentBestLocation.Provider;

            // Determine location quality using a combination of timeliness and accuracy
            return isMoreAccurate ||
                isNewer && !isLessAccurate ||
                isNewer && !isSignificantlyLessAccurate && isFromSameProvider;
        }
    }
}