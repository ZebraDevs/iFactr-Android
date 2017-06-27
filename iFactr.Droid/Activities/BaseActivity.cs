using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using iFactr.Core.Styles;
using MonoCross.Utilities;
using iFactr.UI;
using iFactr.Core;

namespace iFactr.Droid
{
    public class BaseActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            var style = iApp.Instance == null ? new Style() : iApp.Instance.Style;
            UpdateHeader(style.HeaderColor);
            base.OnCreate(savedInstanceState);
            ActionBar?.SetHomeButtonEnabled(true);
            var or = Resources.Configuration.Orientation;
            if (or != CurrentOrientation)
            {
                CurrentOrientation = or;
                DroidFactory.OrientationChanged(CurrentOrientation == Orientation.Portrait ? iApp.Orientation.Portrait : iApp.Orientation.LandscapeLeft);
            }
            if (Alert.Instance != null) Alert.Instance.Show();
        }

        internal void UpdateHeader(Color headerColor)
        {
            try
            {
                UpdateStatus(headerColor);
            }
            catch (MissingMethodException e)
            {
                iApp.Log.Error("Please compile using Android SDK v5.0 or greater", e);
            }

            if (ActionBar == null || headerColor.IsDefaultColor) return;
            Device.Thread.ExecuteOnMainThread(() =>
            {
                ActionBar.SetStackedBackgroundDrawable(headerColor.ToColorDrawable());
                ActionBar.SetBackgroundDrawable(headerColor.ToColorDrawable());
            });
        }

        private void UpdateStatus(Color headerColor)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop || headerColor.IsDefaultColor) return;
            Device.Thread.ExecuteOnMainThread(() =>
            {
                var window = Window;
                window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                window.ClearFlags(WindowManagerFlags.TranslucentStatus);
                // Material Design - 700
                window.SetStatusBarColor(new Android.Graphics.Color(
                    (int)(headerColor.R * .91),
                    (int)(headerColor.G * .91),
                    (int)(headerColor.B * .91)));
            });
        }

        public Orientation CurrentOrientation { get; private set; }

        /// <summary>
        /// Called when an activity you launched exits, giving you the requestCode you started it with,
        /// the resultCode it returned, and any additional data from it. The resultCode will be <see cref="Result.Canceled"/>
        /// if the activity explicitly returned that, didn't return any result, or crashed during its operation.<br/>
        /// You will receive this call immediately before OnResume() when your activity is re-starting.
        /// </summary>
        /// <param name="requestCode">The integer request code originally supplied to <seealso cref="Activity.StartActivityForResult(Intent, int)"/>, allowing you to identify who this result came from.</param>
        /// <param name="resultCode">The <see cref="Result"/> returned by the child activity through its <see cref="Activity.SetResult(Result)"/>.</param>
        /// <param name="data">An Intent, which can return result data to the caller (various data can be attached to Intent "extras").</param>
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (resultCode != Result.Ok) return;

            switch (requestCode)
            {
                case CameraExtensions.ImageResult:
                    CameraExtensions.OnCameraResult();
                    break;
                case CameraExtensions.GalleryResult:
                    CameraExtensions.OnGalleryResult(data.Data);
                    break;
                case VideoRecordingExtensions.VideoResult:
                    VideoRecordingExtensions.OnNewVideoResult(data.Data);
                    break;
                case VideoRecordingExtensions.VideoGalleryResult:
                    VideoRecordingExtensions.OnVideoResult(data.Data);
                    break;
                case VideoPlaybackExtensions.VideoPlayerResult:
                    VideoPlaybackExtensions.OnVideoPlaybackResult();
                    break;
            }
        }

        /// <summary>
        /// Called when the activity has detected the user's press of the back key. The default implementation
        /// simply finishes the current activity, but you can override this to do whatever you want. 
        /// </summary>
        public override void OnBackPressed()
        {
            var pane = PaneManager.Instance.TopmostPane.OutputOnPane;
            var displayArea = (FragmentHistoryStack)PaneManager.Instance.FromNavContext(pane);

            var previousView = displayArea.Views.ElementAtOrDefault(displayArea.Views.Count() - 2);
            var viewAtt = previousView == null ? null : Device.Reflector.GetCustomAttribute<StackBehaviorAttribute>(previousView.GetType(), true);

            if ((pane == Pane.Master || pane == Pane.Detail && !FragmentHistoryStack.IsIndependentDetail)
                && PaneManager.Instance.FromNavContext(Pane.Master).Views.Count(v => !(v is VanityFragment)) == 1)
            {
                Finish();
            }
            else if (displayArea.CanGoBack() ||
                (displayArea.CurrentView as IHistoryEntry)?.BackLink == null &&
                (previousView == null || viewAtt != null && (viewAtt.Options & StackBehaviorOptions.HistoryShy) == StackBehaviorOptions.HistoryShy))
            {
                displayArea.HandleBackLink((displayArea.CurrentView as IHistoryEntry)?.BackLink, pane);
            }
        }
    }
}