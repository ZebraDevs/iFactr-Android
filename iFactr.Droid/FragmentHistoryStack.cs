using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.OS;
using iFactr.Core.Layers;
using iFactr.Core.Styles;
using MonoCross.Utilities;
using iFactr.UI;
using iFactr.Core;
using MonoCross.Navigation;
using System.Reflection;

namespace iFactr.Droid
{
    public class FragmentHistoryStack : IHistoryStack
    {
        #region iFactr fragment properties

        /// <summary>
        /// Gets the view's background id.
        /// </summary>
        public int BackgroundId { get; private set; }

        public TransitionDirection AnimationDirection
        {
            get
            {
                var style = CurrentLayer == null ? iApp.Instance.Style : CurrentLayer.LayerStyle;
                switch (Context.ActivePane)
                {
                    case Pane.Detail:
                        return style.DetailTransitionDirection;
                    case Pane.Popover:
                        return style.PopoverTransitionDirection;
                    default:
                        return style.MasterTransitionDirection;
                }
            }
        }

        private TransitionDirection OppositeDirection
        {
            get { return (TransitionDirection)Math.Abs(3 - (int)AnimationDirection); }
        }

        public Transition Animation
        {
            get
            {
                var style = CurrentLayer == null ? iApp.Instance.Style : CurrentLayer.LayerStyle;
                switch (Context.ActivePane)
                {
                    case Pane.Detail:
                        return style.DetailTransitionAnimation;
                    case Pane.Popover:
                        return style.PopoverTransitionAnimation;
                    default:
                        return style.MasterTransitionAnimation;
                }
            }
        }

        #endregion

        #region Constructors

        public FragmentHistoryStack(Pane pane, int tab)
        {
            Context = new iApp.AppNavigationContext { ActivePane = pane, ActiveTab = tab, };
            switch (pane)
            {
                case Pane.Master:
                    BackgroundId = Resource.Id.master_background;
                    break;
                case Pane.Detail:
                    BackgroundId = Resource.Id.detail_background;
                    break;
                case Pane.Popover:
                    BackgroundId = Resource.Id.popover_background;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pane), "Cannot create stacked view: " + pane);
            }
        }

        #endregion

        #region Portable IHistoryStack members

        public string ID => Context.ToString();

        /// <summary>
        /// Gets the history stack's context.
        /// </summary>
        public iApp.AppNavigationContext Context { get; }

        public IEnumerable<IMXView> Views => _views;
        private readonly List<IMXView> _views = new List<IMXView>();

        /// <summary>
        /// The view currently onscreen.
        /// </summary>
        public IMXView CurrentView => _views.Count > 0 ? _views[_views.Count - 1] : null;

        /// <summary>
        /// The layer currently onscreen.
        /// </summary>
        public iLayer CurrentLayer => CurrentView?.GetModel() as iLayer;

        public void InsertView(int index, IMXView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (index > Views.Count() - 1)
            {
                PushView(view);
            }
            else
            {
                _views.Insert(index, view);
            }
        }

        #endregion

        #region Fragment IHistoryStack members

        public IMXView PopView()
        {
            if (_views.Count > 1)
                return PopToView(_views[_views.Count - 2]).FirstOrDefault();
            if (Context.ActivePane == Pane.Master)
                ReplaceView(CurrentView, new VanityFragment());
            else if (Context.ActivePane == Pane.Detail && !IsIndependentDetail)
                PaneManager.Instance.FromNavContext(Pane.Master, iApp.CurrentNavContext.ActiveTab).PopView();
            else
                PopToRoot();

            return null;
        }

        public IMXView[] PopToRoot()
        {
            var root = Views.FirstOrDefault();
            if (Context.ActivePane == Pane.Master)
                return PopToView(root);

            if (Context.ActivePane == Pane.Popover)
            {
                if (PopoverFragment.Instance == null && PopoverActivity.Instance == null)
                    return PopToView(root);

                if (!(root is VanityFragment))
                {
                    _views.Insert(0, new VanityFragment { OutputPane = Pane.Popover });
                }

                for (var pane = Pane.Detail; pane > Pane.Tabs; pane--)
                {
                    var stack = PaneManager.Instance.FromNavContext(pane, iApp.CurrentNavContext.ActiveTab);
                    if (stack == null || stack.CurrentView is VanityFragment) continue;
                    iApp.CurrentNavContext.ActivePane = pane;
                    break;
                }

                return PopToView(Views.FirstOrDefault());
            }

            if (root is VanityFragment)
            {
                return PopToView(root);
            }

            ReplaceView(Views.FirstOrDefault(), new VanityFragment { OutputPane = Pane.Detail });

            if (Views.Count() > 1) return PopToView(Views.FirstOrDefault());
            Align(NavigationType.Forward);
            return new[] { root };
        }

        public IMXView[] PopToView(IMXView view)
        {
            if (view == null || Equals(view, CurrentView)) return null;

            if (!this.Contains(view))
            {
                throw new ArgumentException("View not found in stack.");
            }

            var layer = CurrentLayer;

            // Perform pop on Views collection
            var removed = new List<IMXView>();
            var entry = view as IHistoryEntry;
            IMXView current = null;

            while (((current = _views.LastOrDefault()) as IHistoryEntry)?.StackID != entry?.StackID && entry != null && !Equals(current, view))
            {
                _views.Remove(current);
                removed.Add(current);
            }

            if (removed.Count <= 0)
            {
                return removed.ToArray();
            }

            Align(NavigationType.Back);

            layer?.Unload();

            var iview = removed.First() as IPairable;
            if (iview is IHistoryEntry)
            {
                iview.RaiseEvent("Deactivated", EventArgs.Empty);
            }
            return removed.ToArray();
        }

        public void PushView(IMXView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            _views.Add(view);
            Align(NavigationType.Forward);
        }

        public void ReplaceView(IMXView currentView, IMXView newView)
        {
            var i = _views.IndexOf(currentView);
            var oldView = i < 0 ? null : _views[i];
            if (oldView == null) return;

            if (newView == null) _views.RemoveAt(i);
            else _views[i] = newView;
            if (i < _views.Count - 1) return;

            (oldView.GetModel() as iLayer)?.Unload();
            var frag = oldView as IView;
            frag?.RaiseEvent("Deactivated", EventArgs.Empty);
            Align(NavigationType.Forward);
        }

        /// <summary>
        /// Synchronize the rendered stack with the <see cref="Views"/> collection
        /// </summary>
        public void Align(NavigationType navType)
        {
            var view = _views.LastOrDefault();
            if (view == null) return;
            _views.RemoveAll(v => Device.Reflector.IsAssignableFrom(typeof(LoginLayer), v.ModelType) && !Equals(v, view));

            var monoView = view as IView;
            if (monoView?.Pair is Fragment)
            {
                view = DroidFactory.GetNativeObject<Fragment>(monoView, "view") as IMXView;
            }

            var fragment = view as Fragment;
            if (fragment == null)
            {
                _views.Remove(view);
            }
            else
            {
                #region Popover initialization

                FragmentManager popoverManager = null;
                if (Context.ActivePane == Pane.Popover)
                {
                    #region Popover teardown

                    if (view is VanityFragment)
                    {
                        DroidFactory.HideKeyboard(false);
                        PopoverActivity.Close(true);
                        PopoverFragment.Close();
                        return;
                    }

                    #endregion

                    if (PopoverFragment.Instance != null)
                    {
                        popoverManager = PopoverFragment.ChildFragmentManager;
                    }
                    else if (PopoverActivity.Instance == null)
                    {
                        IHistoryEntry frag;
                        if (Build.VERSION.SdkInt > BuildVersionCodes.JellyBean && iApp.Factory.LargeFormFactor &&
                                ((frag = view as IHistoryEntry ?? (view as IPairable)?.Pair as IHistoryEntry) == null ||
                                frag.PopoverPresentationStyle != PopoverPresentationStyle.FullScreen))
                        {
                            var name = Java.Lang.Class.FromType(typeof(PopoverFragment)).Name;
                            var dialog = (DialogFragment)Fragment.Instantiate(DroidFactory.MainActivity, name);
                            dialog.Show(DroidFactory.MainActivity.FragmentManager, null);
                        }
                        else DroidFactory.MainActivity.StartActivity(MXContainer.Resolve<Type>("Popover"));
                        return;
                    }
                }

                #endregion

                iApp.CurrentNavContext.ActivePane = Context.ActivePane;
                iApp.CurrentNavContext.ActiveLayer = view.GetModel() as iLayer;

                var activity = PopoverActivity.Instance ?? DroidFactory.MainActivity;
                try
                {
                    var transaction = (popoverManager ?? activity.FragmentManager).BeginTransaction();
                    transaction.Replace(FragmentId, fragment);
                    transaction.CommitAllowingStateLoss();
                }
                catch (Exception e)
                {
                    iApp.Log.Error(e);
                }
            }

            #region Update screen titles

            if (Context.ActivePane == Pane.Popover)
            {
                PopoverFragment.UpdateTitle();
                var titleUpdater = PopoverActivity.Instance?.GetType().GetMethod("UpdateTitle", BindingFlags.Static | BindingFlags.NonPublic);
                titleUpdater?.Invoke(null, null);
            }
            else if (DroidFactory.Tabs == null || !DroidFactory.Tabs.TabItems.Any())
            {
                DroidFactory.RefreshTitles();
            }
            else
            {
                DroidFactory.Tabs.Title = monoView?.Title ?? iApp.Instance.Title;
            }

            #endregion
        }

        public static void SetHomeUp(Pane updatedPane)
        {
            var activity = PopoverActivity.Instance ?? DroidFactory.MainActivity;
            if (activity.ActionBar == null) return;
            var targetStack = PaneManager.Instance.FromNavContext(updatedPane, Math.Max(PaneManager.Instance.CurrentTab, 0));
            if (targetStack == null)
            {
                throw new InvalidOperationException($"No history stack for Pane.{updatedPane} found! Did you mean to use an iFactrActivity?");
            }
            var view = targetStack.CurrentView;
            var link = (view as IHistoryEntry)?.BackLink;
            IHistoryStack detailStack;
            var showBack = (link == null || link.Action != ActionType.None) && (view != null) && !(view.GetModel() is LoginLayer)
                            && targetStack.Views.Count(v => !(v is VanityFragment)) > 1;

            activity.ActionBar.SetDisplayHomeAsUpEnabled(showBack ||
                updatedPane < Pane.Popover && (detailStack = PaneManager.Instance.FromNavContext(Pane.Detail)) != null &&
                detailStack.Views.Any(v => !(v is VanityFragment)) && IsIndependentDetail);
        }

        internal int FragmentId
        {
            get
            {
                switch (Context.ActivePane)
                {
                    case Pane.Master:
                        return Resource.Id.master_fragment;
                    case Pane.Detail:
                        return Resource.Id.detail_fragment;
                    case Pane.Popover:
                        return Resource.Id.popover_fragment;
                }
                return 0;
            }
        }

        public static bool IsIndependentDetail
        {
            get
            {
                var detailStack = PaneManager.Instance.FromNavContext(Pane.Detail);
                if (detailStack == null) return false;
                var masterStack = PaneManager.Instance.FromNavContext(Pane.Master);
                return detailStack.CurrentView != null && masterStack.CurrentLayer != null && masterStack.CurrentLayer.DetailLink != null &&
                    masterStack.CurrentLayer.DetailLink.Address != PaneManager.Instance.GetNavigatedURI(detailStack.CurrentView)
                    || detailStack.CanGoBack();
            }
        }

        #endregion

        #region Obsolete IHistoryStack members

        /// <summary>
        /// Clears the history stack through the given layer.
        /// </summary>
        [Obsolete("Use PopToView instead.")]
        public void PopToLayer(iLayer layer)
        {
            var view = _views.FirstOrDefault(v => v.GetModel().Equals(layer));
            if (view != null) PopToView(view);
        }

        /// <summary>
        /// Gets the last layer pushed onto the history stack.
        /// </summary>
        /// <returns>The <see cref="IMXView"/> on the top of the history stack.</returns>
        /// <remarks>This can be used to get information about the previous Layer.</remarks>
        [Obsolete("Use Views instead.")]
        public iLayer Peek()
        {
            return History.LastOrDefault();
        }

        /// <summary>
        /// Pushes the <see cref="IHistoryStack.CurrentView"/> onto the History to make way for another view.
        /// </summary>
        /// <remarks>If the CurrentDisplay is associated with a LoginLayer, it will not be pushed to the stack history.</remarks>
        [Obsolete]
        public void PushCurrent()
        {
        }

        /// <summary>
        /// Clears the history and current display.
        /// </summary>
        /// <remarks>If this is a popover stack, the popover is closed. If this is a detail stack, it will show the vanity panel.</remarks>
        [Obsolete("Use PopToRoot instead.")]
        public void Clear(iLayer layer)
        {
            if (layer == null)
            {
                PopToRoot();
            }
            else
            {
                PopToLayer(layer);
            }
        }

        /// <summary>
        /// A stack of layers that used to be in the pane.
        /// </summary>
        [Obsolete("Use Views instead.")]
        public IEnumerable<iLayer> History
        {
            get { return _views.Select(v => v.GetModel() as iLayer).Take(_views.Count - 1); }
        }

        #endregion
    }
}