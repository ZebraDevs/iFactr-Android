using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using iFactr.UI;
using Color = Android.Graphics.Color;
using Uri = Android.Net.Uri;

namespace iFactr.Droid
{
    public static class VideoPlaybackExtensions
    {
        public const string Scheme = "video://";
        public const int VideoPlayerResult = 16;
        private static string _callback;

        public static void Launch(Link link)
        {
            _callback = link.Parameters.GetValueOrDefault("callback");
            if (!link.Parameters.ContainsKey("source")) return;
            var source = Uri.Parse(link.Parameters["source"]);
            var intent = new Intent(DroidFactory.MainActivity, typeof(VideoViewActivity));
            intent.PutExtra("source", source.ToString());
            DroidFactory.MainActivity.StartActivityForResult(intent, VideoPlayerResult);
        }

        public static void OnVideoPlaybackResult()
        {
            if (_callback == null) return;
            DroidFactory.Navigate(_callback);
            _callback = null;
        }

        [Activity(NoHistory = true, ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
        private class VideoViewActivity : Activity
        {
            protected override void OnCreate(Bundle bundle)
            {
                base.OnCreate(bundle);
                SetResult(Result.Ok);
                RequestWindowFeature(WindowFeatures.NoTitle);
                Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);

                var layout = new LinearLayout(this);
                layout.SetBackgroundColor(Color.Black);
                layout.SetGravity(GravityFlags.Center);

                var view = new VideoView(this);
                if (Intent.Extras == null)
                {
                    Finish();
                    return;
                }
                var videoUri = Intent.Extras.GetString("source");
                if (videoUri == null)
                {
                    Finish();
                    return;
                }

                view.SetVideoPath(videoUri);
                view.Completion += (o, e) => Finish();

                if (Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb)
                    view.SystemUiVisibility = StatusBarVisibility.Hidden;

                var controller = new FullscreenMediaController(this, view);
                view.SetMediaController(controller);

                layout.AddView(view);
                SetContentView(layout);

                view.Start();
            }

            private class FullscreenMediaController : MediaController
            {
                private readonly VideoView _video;

                public FullscreenMediaController(Context context, VideoView video)
                    : base(context)
                {
                    _video = video;
                }

                public override void Show()
                {
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb)
                        _video.SystemUiVisibility = StatusBarVisibility.Visible;
                    base.Show();
                }

                public override void Hide()
                {
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb)
                        _video.SystemUiVisibility = StatusBarVisibility.Hidden;
                    base.Hide();
                }
            }
        }
    }
}