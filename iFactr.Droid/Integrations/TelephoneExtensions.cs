using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.Widget;
using MonoCross.Utilities;
using iFactr.UI;

namespace iFactr.Droid
{
    public static class TelephoneExtensions
    {
        public const string Scheme = "tel:";
        public const string CallToScheme = "callto:";

        public static void Launch(Link link)
        {
            var intent = new Intent(Intent.ActionDial, Uri.Parse(link.Address));
            if (DroidFactory.MainActivity.PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly).Count > 0)
            {
                DroidFactory.MainActivity.StartActivity(intent);
            }
            else
            {
                Device.Log.Error("Unable to handle url: " + link.Address);
                Toast.MakeText(DroidFactory.MainActivity, Device.Resources.GetString("FailedNavigation"), ToastLength.Short).Show();
            }
        }
    }
}