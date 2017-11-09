using System.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using MonoCross.Utilities;
using iFactr.UI;
using MonoCross.Navigation;
using iFactr.Core;
using iFactr.Core.Targets;

namespace iFactr.Droid
{
    public class iFactrActivity : BaseActivity
    {
        public const string InitKey = "__initView";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            var splash = DroidFactory.MainActivity as SplashActivity;
            if (splash != null) DroidFactory.Instance.ViewOutputting -= splash.OnViewOutputting;
            DroidFactory.MainActivity = this;
            base.OnCreate(savedInstanceState);

            if (DroidFactory.TheApp == null)
            {
                Device.Log.Warn($"The app has not been set. Please set {nameof(TargetFactory)}.{nameof(TargetFactory.TheApp)} to this to your app instance.");
                SetContentView(Resource.Layout.main);
            }
            else
            {
                SetContentView(DroidFactory.TheApp.FormFactor != FormFactor.Fullscreen ? Resource.Layout.main : Resource.Layout.fullscreen);
            }

            if (FindViewById(Resource.Id.master_fragment) == null)
            {
                var layoutName = DroidFactory.TheApp.FormFactor != FormFactor.Fullscreen
                    ? nameof(Resource.Layout.main)
                    : nameof(Resource.Layout.fullscreen);
                Device.Log.Fatal($"Master view id @+id/master_fragment not found in layout resource: {layoutName}");
            }

            if (PaneManager.Instance.Any())
            {
                if (DroidFactory.Tabs == null)
                    (PaneManager.Instance.FromNavContext(Pane.Master, 0) as FragmentHistoryStack)?.Align(NavigationType.Tab);
                else DroidFactory.Tabs.Render();

                (PaneManager.Instance.FromNavContext(Pane.Detail, 0) as FragmentHistoryStack)?.Align(NavigationType.Tab);
                var popover = PaneManager.Instance.FromNavContext(Pane.Popover, 0) as FragmentHistoryStack;
                if (popover != null && popover.Views.Any(v => !(v is VanityFragment)))
                    popover.Align(NavigationType.Tab);
            }
            else
            {
                PaneManager.Instance.AddStack(new FragmentHistoryStack(Pane.Master, 0), new iApp.AppNavigationContext { ActivePane = Pane.Master, });

                var detail = FindViewById<FrameLayout>(Resource.Id.detail_fragment);
                if (detail != null)
                {
                    var detailStack = new FragmentHistoryStack(Pane.Detail, 0);
                    detailStack.PushView(new VanityFragment { OutputPane = Pane.Detail, });
                    PaneManager.Instance.AddStack(detailStack, detailStack.Context);
                }

                PaneManager.Instance.AddStack(new FragmentHistoryStack(Pane.Popover, 0), new iApp.AppNavigationContext { ActivePane = Pane.Popover, });
            }

            if (!iApp.Session.ContainsKey(InitKey)) return;
            var view = iApp.Session[InitKey] as IMXView;
            iApp.Session.SafeKeys.Remove(InitKey);
            iApp.Session.Remove(InitKey);
            if (view == null) return;
            PaneManager.Instance.DisplayView(view);
            var entry = view as IHistoryEntry;
            if (entry != null)
            {
                var stack = (FragmentHistoryStack)PaneManager.Instance.FindStack(view);
                entry.OutputPane = stack.Context.ActivePane;
            }
            Events.RaiseEvent(DroidFactory.Instance, nameof(iApp.Factory.ViewOutputted), new ViewOutputEventArgs(null, view));
        }

        /// <summary>
        /// This hook is called whenever an item in the options menu is selected. The default implementation 
        /// simply returns false to have the normal processing happen (calling the item's Runnable or sending 
        /// a message to its Handler as appropriate). You can use this method for any items for which you would 
        /// like to do processing without those other facilities.<br/>
        /// Derived classes should call through to the base class for it to perform the default menu handling.
        /// </summary>
        /// <param name="item">The menu item that was selected.</param>
        /// <returns><c>false</c> to allow normal menu processing to proceed, <c>true</c> to consume it here.</returns>
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item == null || item.ItemId != Android.Resource.Id.Home) return base.OnOptionsItemSelected(item);
            var masterStack = PaneManager.Instance.FromNavContext(Pane.Master, PaneManager.Instance.CurrentTab);
            var masterView = masterStack?.CurrentView as IHistoryEntry;
            var back = masterView?.BackLink;
            if (back != null && back.Action == ActionType.None) return false;

            Link currentTabLink;
            if ((ActionBar?.DisplayOptions & ActionBarDisplayOptions.HomeAsUp) == ActionBarDisplayOptions.HomeAsUp)
            {
                DroidFactory.HideKeyboard(false);
                masterStack?.HandleBackLink(back, Pane.Master);
            }
            else if ((currentTabLink = DroidFactory.Tabs?.TabItems?.ElementAtOrDefault(PaneManager.Instance.CurrentTab)?.NavigationLink) == null)
            {
                if (masterView != null) masterView.OutputPane = Pane.Tabs;
                DroidFactory.Navigate(new Link(iApp.Instance.NavigateOnLoad));
            }
            else
            {
                DroidFactory.Navigate(currentTabLink, DroidFactory.Tabs);
            }
            return true;
        }
    }
}