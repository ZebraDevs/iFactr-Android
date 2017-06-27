using Android.App;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using MonoCross;
using MonoCross.Utilities;
using iFactr.UI;
using iFactr.Core;

namespace iFactr.Droid
{
    public abstract class SplashActivity : Activity
    {
        private bool _animationFinished;
        private AnimationView _animation;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            if (!DroidFactory.IsInitialized)
            {
                DroidFactory.MainActivity = this;
                DroidFactory.MainActivity.Window.AddFlags(WindowManagerFlags.Fullscreen);
                DroidFactory.Instance.ViewOutputting += OnViewOutputting;

                #region Splash setup

                SetContentView(Resource.Layout.splash);

                _animation = FindViewById<AnimationView>(Resource.Id.logo_motion);

                var imm = (InputMethodManager)GetSystemService(InputMethodService);
                imm.HideSoftInputFromWindow(_animation.WindowToken, HideSoftInputFlags.None);

                _animation.DurationLapsed += (o, e) =>
                {
                    _animationFinished = true;
                    Device.Thread.ExecuteOnMainThread(() =>
                    {
                        var staticLogoImageView = FindViewById(Resource.Id.logo_static) as ImageView;
                        if (staticLogoImageView == null) return;

                        // Detach from view and release GIF resource
                        _animation.Cleanup();
                        staticLogoImageView.Visibility = ViewStates.Visible;
                    });

                    if (iApp.Session.ContainsKey(iFactrActivity.InitKey) && _animationFinished)
                    {
                        StartiFactrActivity();
                    }
                };

                #endregion
            }
            else StartiFactrActivity();
        }

        protected abstract void StartApp();

        public virtual void OnViewOutputting(object sender, ViewOutputCancelEventArgs args)
        {
            args.Cancel = true;
            iApp.Factory.StopBlockingUserInput();
            if (args.View == null) return;
            iApp.Session[iFactrActivity.InitKey] = args.View;
            if (!iApp.Session.SafeKeys.Contains(iFactrActivity.InitKey))
                iApp.Session.SafeKeys.Add(iFactrActivity.InitKey);
            if (iApp.Session.ContainsKey(iFactrActivity.InitKey) && _animationFinished)
                StartiFactrActivity();
        }

        private void StartiFactrActivity()
        {
            StartApp();
            Finish();
        }

        protected override void OnDestroy()
        {
            _animation?.Cleanup();
            base.OnDestroy();
        }

        public override void OnBackPressed() { }
    }
}