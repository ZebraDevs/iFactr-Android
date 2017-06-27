using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MonoCross;
using iFactr.Core.Layers;
using iFactr.UI;
using Object = Java.Lang.Object;
using View = Android.Views.View;
using iFactr.Core;

namespace iFactr.Droid
{
    public class ActionBarAdapter : ArrayAdapter<string>, ActionBar.IOnNavigationListener, ITabView, INotifyPropertyChanged
    {
        #region Constructors

        public ActionBarAdapter() :
            base(DroidFactory.MainActivity, Android.Resource.Layout.SimpleSpinnerDropDownItem)
        {
            Initialize();
        }


        public ActionBarAdapter(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {
            Initialize();
        }

        public ActionBarAdapter(Context context, int textViewResourceId)
            : base(context, textViewResourceId)
        {
            Initialize();
        }

        public ActionBarAdapter(Context context, int resource, int textViewResourceId)
            : base(context, resource, textViewResourceId)
        {
            Initialize();
        }

        public ActionBarAdapter(Context context, int textViewResourceId, string[] objects)
            : base(context, textViewResourceId, objects)
        {
            Initialize();
        }

        public ActionBarAdapter(Context context, int resource, int textViewResourceId, string[] objects)
            : base(context, resource, textViewResourceId, objects)
        {
            Initialize();
        }

        public ActionBarAdapter(Context context, int textViewResourceId, IList<string> objects)
            : base(context, textViewResourceId, objects)
        {
            Initialize();
        }

        public ActionBarAdapter(Context context, int resource, int textViewResourceId, IList<string> objects)
            : base(context, resource, textViewResourceId, objects)
        {
            Initialize();
        }

        private void Initialize()
        {
            if (DroidFactory.MainActivity.ActionBar == null)
            {
                throw new TypeInitializationException(nameof(ActionBarTabView), new NullReferenceException("An action bar must be present to use this tab mode."));
            }
        }

        #endregion

        #region ActionBar.IOnNavigationListener members

        public bool OnNavigationItemSelected(int itemPosition, long itemId)
        {
            if (DroidFactory.Tabs != this) return false;

            var masterStack = (FragmentHistoryStack)PaneManager.Instance.FromNavContext(Pane.Master, itemPosition);
            var tab = TabItems.ElementAt(itemPosition);
            var nativeTab = DroidFactory.GetNativeObject<TabItem>(TabItems.ElementAt(itemPosition), "tab");
            if (itemPosition != SelectedIndex && !PaneManager.Instance.ShouldNavigate(tab.NavigationLink, Pane.Tabs, NavigationType.Tab))
            {
                return false;
            }

            if (masterStack.CurrentView == null)
            {
                SelectedIndex = itemPosition;
                this.OnPropertyChanged("SelectedIndex");
                nativeTab.Activate(this);
            }
            else
            {
                if (itemPosition == SelectedIndex)
                {
                    masterStack.Align(NavigationType.Tab);
                    return true;
                }

                SelectedIndex = itemPosition;
                var navTabs = _model as NavigationTabs;
                if (navTabs != null && navTabs.TabItems[itemPosition].RefreshOnFocus)
                {
                    this.OnPropertyChanged("SelectedIndex");
                    nativeTab.Activate(this);
                    return true;
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

                nativeTab.OnSelected();
                this.OnPropertyChanged("SelectedIndex");
                masterStack.Align(NavigationType.Tab);
                (PaneManager.Instance.FromNavContext(Pane.Detail, 0) as FragmentHistoryStack)?.PopToRoot();
            }
            return true;
        }

        #endregion

        #region IMXView members

        public void Render()
        {
            Clear();
            this.RaiseEvent(nameof(Rendering), EventArgs.Empty);
            DroidFactory.Tabs = this;
            AddAll(TabItems.Select(t => t.Title).ToList());
            var actionBar = DroidFactory.MainActivity.ActionBar;

            if (!TabItems.Any()) return;
            HeaderColor = iApp.Instance.Style.HeaderColor;
            actionBar.Title = null;
            actionBar.Subtitle = null;
            actionBar.NavigationMode = ActionBarNavigationMode.List;
            actionBar.SetListNavigationCallbacks(this, this);
            actionBar.SetSelectedNavigationItem(PaneManager.Instance.CurrentTab > TabItems.Count() - 1 ? 0 : PaneManager.Instance.CurrentTab);
        }

        public Type ModelType => _model?.GetType() ?? typeof(NavigationTabs);

        public object GetModel() { return _model; }

        public void SetModel(object model) { _model = model; }
        private object _model;

        #endregion

        #region Views

        public override View GetDropDownView(int position, View convertView, ViewGroup parent)
        {
            var view = (CheckedTextView)base.GetDropDownView(position, convertView, parent);

            // TODO: Get action bar text color and set to that
            view.SetTextColor((iApp.Instance.Style.HeaderTextColor.IsDefaultColor ? Color.White : iApp.Instance.Style.HeaderTextColor).ToColor());
            if (!iApp.Instance.Style.HeaderColor.IsDefaultColor)
                view.SetBackgroundColor(iApp.Instance.Style.HeaderColor.ToColor());
            return view;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            ViewHolder holder;
            if (convertView == null)
            {
                convertView = DroidFactory.MainActivity.LayoutInflater.Inflate(Resource.Layout.ab_main_view, parent, false);
                convertView.Tag = holder = new ViewHolder
                {
                    Title = convertView.FindViewById<TextView>(Resource.Id.ab_basemaps_title),
                    Subtitle = convertView.FindViewById<TextView>(Resource.Id.ab_basemaps_subtitle),
                };
            }
            else
            {
                holder = (ViewHolder)convertView.Tag;
            }

            var titleView = PaneManager.Instance.FromNavContext(Pane.Master, position)?.CurrentView as IView;
            var title = titleView?.Title;

            var stack = PaneManager.Instance.FromNavContext(Pane.Detail, position);
            titleView = stack?.CurrentView as IView;
            if (titleView != null && stack.FindPane() == Pane.Detail && !string.IsNullOrWhiteSpace(titleView.Title))
            {
                title += " | " + titleView.Title;
            }

            holder.Title.Text = string.IsNullOrEmpty(title) ?
                    TabItems.ElementAtOrDefault(position)?.Title ?? Title
                    : title;
            holder.Subtitle.Text = TabItems.ElementAtOrDefault(position)?.Title ?? string.Empty;
            if (TitleColor.IsDefaultColor)
            {
                var id = DroidFactory.MainActivity.Resources.GetIdentifier("action_bar_title", "id", "android");
                var titleText = DroidFactory.MainActivity.FindViewById<TextView>(id);
                var color = new Android.Graphics.Color(titleText.CurrentTextColor);
                holder.Title.SetTextColor(color);
                holder.Subtitle.SetTextColor(color);
            }
            else
            {
                holder.Title.SetTextColor(TitleColor.ToColor());
                holder.Subtitle.SetTextColor(TitleColor.ToColor());
            }
            holder.Title.Visibility = string.IsNullOrWhiteSpace(holder.Title.Text) ? ViewStates.Invisible : ViewStates.Visible;
            holder.Subtitle.Visibility = string.IsNullOrWhiteSpace(holder.Subtitle.Text) ? ViewStates.Gone : ViewStates.Visible;
            return convertView;
        }

        private class ViewHolder : Object
        {
            public TextView Title { get; set; }
            public TextView Subtitle { get; set; }
        }

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

        public double Height => DroidFactory.MainActivity.ActionBar == null ? 0 : DroidFactory.MainActivity.ActionBar.Height;

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
                NotifyDataSetChanged();
                if (_title == value) return;
                _title = value;
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
                _selectedIndex = value;
                if (_selectedIndex < DroidFactory.MainActivity.ActionBar.NavigationItemCount)
                {
                    DroidFactory.MainActivity.ActionBar.SetSelectedNavigationItem(_selectedIndex);
                }
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

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
    }
}