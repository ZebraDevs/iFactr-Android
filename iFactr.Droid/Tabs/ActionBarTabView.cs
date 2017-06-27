using System;
using System.Collections.Generic;
using System.Linq;
using MonoCross;
using iFactr.UI;
using System.ComponentModel;
using MonoCross.Utilities;
using Android.App;
using iFactr.Core.Layers;
using Android.Graphics.Drawables;
using iFactr.Core;

namespace iFactr.Droid
{
    public class ActionBarTabView : Java.Lang.Object, ITabView, INotifyPropertyChanged, ActionBar.ITabListener
    {
        public ActionBarTabView()
        {
            if (DroidFactory.MainActivity.ActionBar == null)
            {
                throw new TypeInitializationException(nameof(ActionBarTabView), new NullReferenceException("An action bar must be present to use this tab mode."));
            }
        }

        #region IMXView members

        public void Render()
        {
            this.RaiseEvent(nameof(Rendering), EventArgs.Empty);
            if (!TabItems.Any()) return;
            DroidFactory.Tabs = this;
            _inFlight = true;
            var actionBar = DroidFactory.MainActivity.ActionBar;
            actionBar.RemoveAllTabs();

            var ig = new ImageGetter();
            foreach (var tab in TabItems)
            {
                var abTab = actionBar.NewTab();
                abTab.SetTabListener(this);
                abTab.SetText(tab.Title);
                abTab.SetIcon(ig.GetDrawable(tab.ImagePath));
                actionBar.AddTab(abTab);
            }
            _inFlight = false;

            HeaderColor = iApp.Instance.Style.HeaderColor;

            actionBar.Title = null;
            actionBar.Subtitle = null;
            if (SelectedIndex > -1)
            {
                iApp.SetNavigationContext(new iLayer.NavigationContext
                {
                    OutputOnPane = iApp.CurrentNavContext.ActivePane,
                    NavigatedActiveTab = SelectedIndex,
                });
                actionBar.SelectTab(actionBar.GetTabAt(SelectedIndex));
            }
            actionBar.NavigationMode = ActionBarNavigationMode.Tabs;
        }

        public Type ModelType => _model?.GetType() ?? typeof(NavigationTabs);

        public object GetModel() { return _model; }

        public void SetModel(object model) { _model = model; }
        private object _model;

        #endregion

        #region IView members

        public IPairable Pair { get; set; }

        public bool Equals(IView other)
        {
            return Pair.Equals(other.Pair);
        }

        public Color HeaderColor
        {
            get { return _headerColor; }
            set
            {
                _headerColor = value;
                (DroidFactory.MainActivity as BaseActivity)?.UpdateHeader(_headerColor);
            }
        }
        private Color _headerColor;

        public double Height => DroidFactory.MainActivity.ActionBar.Height;

        public MetadataCollection Metadata => _metadata ?? (_metadata = new MetadataCollection());
        private MetadataCollection _metadata;

        public PreferredOrientation PreferredOrientations
        {
            get { return _preferredOrientations; }
            set
            {
                if (_preferredOrientations == value) return;
                _preferredOrientations = value;
                this.OnPropertyChanged();
            }
        }
        private PreferredOrientation _preferredOrientations;

        public string Title
        {
            get { return _title; }
            set
            {
                FragmentHistoryStack.SetHomeUp(Pane.Master);
                var actionBar = DroidFactory.MainActivity.ActionBar;
                _title = value;

                var oldTitle = actionBar.Title;
                var oldSubtitle = actionBar.Subtitle;

                var titleView = PaneManager.Instance.FromNavContext(Pane.Master, PaneManager.Instance.CurrentTab)?.CurrentView as IView;
                var title = titleView?.Title;

                var stack = PaneManager.Instance.FromNavContext(Pane.Detail, PaneManager.Instance.CurrentTab);
                titleView = stack?.CurrentView as IView;
                if (titleView != null && stack.FindPane() == Pane.Detail && !string.IsNullOrWhiteSpace(titleView.Title))
                {
                    actionBar.Subtitle = titleView.Title;
                }
                else
                {
                    actionBar.Subtitle = null;
                }

                actionBar.Title = string.IsNullOrEmpty(title) ?
                    TabItems.ElementAtOrDefault(PaneManager.Instance.CurrentTab)?.Title ?? Title
                    : title;

                if (actionBar.Title != oldTitle || actionBar.Subtitle != oldSubtitle)
                    this.OnPropertyChanged();
            }
        }
        private string _title;

        public Color TitleColor
        {
            get { return _titleColor; }
            set
            {
                if (_titleColor == value) return;
                _titleColor = value;
                this.OnPropertyChanged();
            }
        }
        private Color _titleColor;

        public double Width { get; private set; }

        public event EventHandler Rendering;
        public void SetBackground(Color color)
        {
            HeaderColor = color;
        }

        public void SetBackground(string imagePath, ContentStretch stretch)
        {
            if (DroidFactory.MainActivity.ActionBar == null) return;
            ImageGetter.SetDrawable(imagePath, (bitmap, url, fromCache) =>
            {
                if (bitmap != null && url == imagePath)
                    DroidFactory.MainActivity.ActionBar.SetBackgroundDrawable(new BitmapDrawable(ImageGetter.Resources, bitmap));
            });
        }

        #endregion

        #region ITabView members

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (_selectedIndex == value) return;
                if (value < DroidFactory.MainActivity.ActionBar.NavigationItemCount)
                {
                    DroidFactory.MainActivity.ActionBar.GetTabAt(value).Select();
                }
                else _selectedIndex = value;
            }
        }
        private int _selectedIndex = -1;

        public Color SelectionColor
        {
            get { return _selectionColor; }
            set
            {
                if (_selectionColor == value) return;
                _selectionColor = value;
                this.OnPropertyChanged();
            }
        }
        private Color _selectionColor;

        public IEnumerable<ITabItem> TabItems
        {
            get { return _tabItems; }
            set
            {
                _tabItems = value;
                this.OnPropertyChanged();
            }
        }
        private IEnumerable<ITabItem> _tabItems = new List<ITabItem>();
        private bool _inFlight;

        #endregion

        #region ActionBar.ITabListener members

        public void OnTabReselected(ActionBar.Tab tab, FragmentTransaction ft)
        {
            if (_inFlight) return;
            var actionTab = DroidFactory.GetNativeObject<TabItem>(TabItems.ElementAt(tab.Position), "actionTab");
            actionTab.Activate(this);
        }

        public void OnTabSelected(ActionBar.Tab actionTab, FragmentTransaction ft)
        {
            if (_inFlight) return;
            var itemPosition = actionTab.Position;
            var masterStack = (FragmentHistoryStack)PaneManager.Instance.FromNavContext(Pane.Master, itemPosition);
            var tab = TabItems.ElementAt(itemPosition);
            var nativeTab = DroidFactory.GetNativeObject<TabItem>(tab, "tab");
            if (itemPosition != SelectedIndex && !PaneManager.Instance.ShouldNavigate(tab.NavigationLink, Pane.Tabs, NavigationType.Tab))
            {
                DroidFactory.MainActivity.ActionBar.GetTabAt(_selectedIndex).Select();
                return;
            }

            var navTabs = _model as NavigationTabs;
            if (masterStack.CurrentView != null && (navTabs == null || !navTabs.TabItems[itemPosition].RefreshOnFocus))
            {
                if (itemPosition != SelectedIndex)
                {
                    _selectedIndex = itemPosition;
                    this.OnPropertyChanged("SelectedIndex");
                    nativeTab.OnSelected();
                }
                iApp.SetNavigationContext(new iLayer.NavigationContext { OutputOnPane = Pane.Tabs, NavigatedActiveTab = itemPosition, });
                masterStack.Align(NavigationType.Tab);
                return;
            }

            iLayer.NavigationContext context;
            if (masterStack.CurrentLayer != null && (context = masterStack.CurrentLayer.NavContext) != null)
            {
                //Fix layer context if it was modified in a different tab
                context.ClearPaneHistoryOnOutput = false;
                context.NavigatedActivePane = Pane.Tabs;
                context.NavigatedActiveTab = itemPosition;
                context.OutputOnPane = Pane.Master;
            }

            if (itemPosition != iApp.CurrentNavContext.ActiveTab)
            {
                var masterView = (PaneManager.Instance.FromNavContext(Pane.Master, iApp.CurrentNavContext.ActiveTab) as FragmentHistoryStack)?.CurrentView as IView;
                iApp.SetNavigationContext(new iLayer.NavigationContext { OutputOnPane = Pane.Tabs, NavigatedActiveTab = itemPosition, });
                masterView?.RaiseEvent("Deactivated", EventArgs.Empty);
            }

            _selectedIndex = itemPosition;
            this.OnPropertyChanged("SelectedIndex");
            nativeTab.Activate(this);
        }

        public void OnTabUnselected(ActionBar.Tab tab, FragmentTransaction ft) { }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
    }
}