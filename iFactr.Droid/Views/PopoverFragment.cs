using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using iFactr.UI;
using MonoCross.Navigation;
using IMenu = iFactr.UI.IMenu;
using View = Android.Views.View;
using Color = Android.Graphics.Color;
using iFactr.Core;

namespace iFactr.Droid
{
    public sealed class PopoverFragment : DialogFragment, IDialogInterfaceOnKeyListener
    {
        #region Properties and fields

        public static PopoverFragment Instance { get; private set; }

        private readonly FragmentHistoryStack _stack = (FragmentHistoryStack)PaneManager.Instance.FromNavContext(Pane.Popover, 0);

        #endregion

        #region Constructors

        public PopoverFragment()
        {
            Cancelable = false;
        }

        #endregion

        #region Fragment overrides and helpers

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var dialog = base.OnCreateDialog(savedInstanceState);
            dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            dialog.SetOnKeyListener(this);
            return dialog;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle icicle)
        {
            var view = inflater.Inflate(Resource.Layout.popover, null);
            view.FindViewById<Button>(Resource.Id.action).Click += (o, e) =>
            {
                var menu = GetMenu();
                if (menu == null || menu.ButtonCount <= 0) return;
                var button = menu.GetButton(0);
                if (!button.RaiseEvent(nameof(button.Clicked), EventArgs.Empty))
                {
                    DroidFactory.Navigate(button.NavigationLink, _stack.CurrentView);
                }
            };
            view.FindViewById<ImageButton>(Resource.Id.options).Click += Menu.Activated;
            var homeAction = view.FindViewById<ImageButton>(Resource.Id.home);
            homeAction.SetImageDrawable(Resources.GetDrawable(Activity.ApplicationInfo.Icon));
            homeAction.Click += (o, e) => Activity.OnBackPressed();
            UpdateTitle(view);
            return view;
        }

        public override void OnResume()
        {
            base.OnResume();
            var metrics = new DisplayMetrics();
            DroidFactory.MainActivity.WindowManager.DefaultDisplay.GetMetrics(metrics);
            var height = LinearLayout.LayoutParams.MatchParent;
            Dialog.Window.SetLayout((int)(metrics.WidthPixels * .6 + 16 * DroidFactory.DisplayScale), height);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            Instance = this;
            _stack.Align(NavigationType.Tab);
        }

        public override void OnDetach()
        {
            base.OnDetach();
            Instance = null;
        }

        public override void OnAttach(Activity activity)
        {
            Instance = this;
            base.OnAttach(activity);
        }

        public static void Close()
        {
            if (Instance == null) return;
            Instance.Dismiss();
            Instance = null;
        }

        internal static void UpdateTitle(View view = null)
        {
            if (Instance == null) return;
            Button actionButton;
            ImageButton actionMenu;
            TextView title;
            LinearLayout header;

            if (view == null)
            {
                if (Instance.Dialog == null) return;
                title = Instance.Dialog.FindViewById<TextView>(Resource.Id.title);
                if (title == null) return;
                actionButton = Instance.Dialog.FindViewById<Button>(Resource.Id.action);
                actionMenu = Instance.Dialog.FindViewById<ImageButton>(Resource.Id.options);
                header = Instance.Dialog.FindViewById<LinearLayout>(Resource.Id.headerBackground);
            }
            else
            {
                title = view.FindViewById<TextView>(Resource.Id.title);
                if (title == null) return;
                actionButton = view.FindViewById<Button>(Resource.Id.action);
                actionMenu = view.FindViewById<ImageButton>(Resource.Id.options);
                header = view.FindViewById<LinearLayout>(Resource.Id.headerBackground);
            }

            var v = Instance._stack.CurrentView as IView;
            title.Text = v == null ? MXContainer.Instance.App.Title : v.Title;

            if (v != null)
            {
                title.SetTextColor(v.TitleColor.IsDefaultColor ? Color.White : v.TitleColor.ToColor());
                header.SetBackgroundColor(v.HeaderColor.IsDefaultColor ? new Color(34, 34, 34) : v.HeaderColor.ToColor());
            }

            var menu = GetMenu();
            if (menu == null || menu.ButtonCount == 0)
            {
                actionButton.Visibility = ViewStates.Gone;
                actionMenu.Visibility = ViewStates.Gone;
            }
            else if (menu.ButtonCount == 1)
            {
                actionButton.SetTextColor(new Color(v.TitleColor.GetHashCode()));
                actionButton.Text = menu.GetButton(0).Title;
                actionButton.Visibility = ViewStates.Visible;
                actionMenu.Visibility = ViewStates.Gone;
            }
            else
            {
                actionButton.Visibility = ViewStates.Gone;
                actionMenu.Visibility = ViewStates.Visible;
            }
        }

        private static IMenu GetMenu()
        {
            var v = Instance._stack.CurrentView;
            return (v as IListView)?.Menu ?? (v as IGridView)?.Menu;
        }

        #endregion

        #region IDialogInterfaceOnKeyListener members

        public bool OnKey(IDialogInterface dialog, Keycode keyCode, KeyEvent e)
        {
            if (keyCode != Keycode.Back || e.Action != KeyEventActions.Up || e.RepeatCount > 0) return false;
            if (TextBase.CurrentFocus == null)
            {
                Activity.OnBackPressed();
            }
            else
            {
                TextBase.CurrentFocus.Blur(true);
            }
            return true;
        }

        #endregion

        public new static FragmentManager ChildFragmentManager
        {
            get
            {
                if (Build.VERSION.SdkInt > BuildVersionCodes.JellyBean)
                {
                    return ((DialogFragment)Instance).ChildFragmentManager;
                }
                return DroidFactory.MainActivity.FragmentManager;
            }
        }
    }
}