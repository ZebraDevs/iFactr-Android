using System.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using MonoCross.Utilities;
using iFactr.UI;
using iFactr.Core;

namespace iFactr.Droid
{
    [Activity(WindowSoftInputMode = SoftInput.AdjustPan)]
    public class PopoverActivity : BaseActivity
    {
        public static Activity Instance { get; set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            Instance = this;
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.popover_fullscreen);

            var actionBar = ActionBar;
            if (actionBar != null)
            {
                actionBar.NavigationMode = ActionBarNavigationMode.Standard;
                UpdateTitle();
            }
            Stack.Align(NavigationType.Tab);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Instance = null;
        }

        internal static void UpdateTitle()
        {
            if (Instance?.ActionBar == null) return;

            FragmentHistoryStack.SetHomeUp(Pane.Popover);

            var style = Stack.CurrentLayer == null ? iApp.Instance.Style : Stack.CurrentLayer.LayerStyle;
            ((PopoverActivity)Instance).UpdateHeader(style.HeaderColor);

            var popoverView = Stack.CurrentView as IView;
            var title = popoverView == null ? iApp.Instance.Title : popoverView.Title;
            Instance.ActionBar.Title = title;
        }

        public static void Close(bool updateContext)
        {
            if (Instance == null) return;
            Instance.Finish();
            Instance = null;
            Stack.PopToRoot();
            if (!updateContext) return;
            var pane = PaneManager.Instance.FromNavContext(PaneManager.Instance.TopmostPane.OutputOnPane) as FragmentHistoryStack;
            pane?.Align(NavigationType.Tab);
        }

        private static FragmentHistoryStack Stack => PaneManager.Instance.FromNavContext(Pane.Popover, 0) as FragmentHistoryStack;

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
            if (item.ItemId != Android.Resource.Id.Home) return base.OnOptionsItemSelected(item);

            var previousView = Stack.Views.ElementAtOrDefault(Stack.Views.Count() - 2);
            var viewAtt = previousView == null ? null : Device.Reflector.GetCustomAttribute<StackBehaviorAttribute>(previousView.GetType(), true);

            var backLink = (Stack.CurrentView as IHistoryEntry)?.BackLink;
            if ((ActionBar.DisplayOptions & ActionBarDisplayOptions.HomeAsUp) == ActionBarDisplayOptions.HomeAsUp)
            {
                DroidFactory.HideKeyboard(false);
                Stack.HandleBackLink(backLink, Pane.Popover);
            }
            else if (Stack.CanGoBack() ||
                backLink == null && (previousView == null || viewAtt != null && (viewAtt.Options & StackBehaviorOptions.HistoryShy) == StackBehaviorOptions.HistoryShy))
            {
                DroidFactory.HideKeyboard(false);
                Close(false);
            }
            return true;
        }
    }
}