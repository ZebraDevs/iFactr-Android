using Android.Locations;

namespace iFactr.Droid
{
    public interface ILocationReceiver
    {
        void OnChanged(Location location);
    }
}