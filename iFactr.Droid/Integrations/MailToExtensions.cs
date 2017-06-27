using System.Collections.Generic;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.Text;
using Android.Widget;
using MonoCross;
using iFactr.UI;
using MailTo = iFactr.Core.Integrations.MailTo;
using iFactr.Core;

namespace iFactr.Droid
{
    public static class MailToExtensions
    {
        public const string Scheme = "mailto:";
        public const string MimeType = "MIME";
        public const string DefaultMimeType = "message/rfc822";

        public static void Launch(Link link)
        {
            var mailTo = MailTo.ParseUrl(link.Address);
            var mimetype = link.Parameters.GetValueOrDefault(MimeType, DefaultMimeType);
            var emailIntent = new Intent(Intent.ActionSend);
            emailIntent.SetType(mimetype);
            emailIntent.PutExtra(Intent.ExtraEmail, mailTo.EmailTo.ToArray());
            emailIntent.PutExtra(Intent.ExtraSubject, mailTo.EmailSubject);
            emailIntent.PutExtra(Intent.ExtraText, Html.FromHtml(mailTo.EmailBody));
            foreach (var attachment in mailTo.EmailAttachments)
                emailIntent.PutExtra(Intent.ExtraStream, Uri.Parse(attachment.Filename));

            if (DroidFactory.MainActivity.PackageManager.QueryIntentActivities(emailIntent, PackageInfoFlags.MatchDefaultOnly).Count > 0)
            {
                DroidFactory.MainActivity.StartActivity(emailIntent);
            }
            else
            {
                iApp.Log.Error("Unable to handle mailto.");
                Toast.MakeText(DroidFactory.MainActivity, iApp.Factory.GetResourceString("NoService") ?? "Service unavailable.", ToastLength.Short).Show();
            }
        }
    }
}