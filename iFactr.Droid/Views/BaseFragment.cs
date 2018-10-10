using Android.App;
using Android.Content.PM;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using iFactr.Core;
using iFactr.Core.Forms;
using iFactr.Core.Layers;
using iFactr.UI;
using iFactr.UI.Controls;
using MonoCross.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using IMenu = Android.Views.IMenu;

namespace iFactr.Droid
{
    /// <summary>
    /// Provides a base implementation of <see cref="IListView"/>, <see cref="IBrowserView"/>, and <see cref="ICanvasView"/>.
    /// </summary>
    public class BaseFragment : Fragment, IView, IHistoryEntry, INotifyPropertyChanged
    {
        public BaseFragment()
        {
            ValidationErrors = new ValidationErrorCollection();
        }

        #region Action buttons

        public void FocusMenu()
        {
            _menu?.Focus();
        }

        public UI.IMenu Menu
        {
            get { return _menu == null ? null : _menu.Pair as UI.Menu ?? (UI.IMenu)_menu; }
            set
            {
                if (value == Menu) return;
                _menu = DroidFactory.GetNativeObject<Menu>(value, "menu");
                _buttons = null;
                Activity?.InvalidateOptionsMenu();
                this.OnPropertyChanged();
            }
        }
        private Menu _menu;
        private MenuButton[] _buttons;

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            if (menu == null) return;
            menu.Clear();

            if (Menu.ButtonCount > 1 && Build.VERSION.SdkInt <= BuildVersionCodes.JellyBean)
            {
                var itemId = (int)OutputPane * byte.MaxValue;
                var item = menu.Add((int)OutputPane, itemId, (int)OutputPane, Device.Resources.GetString("Menu"));
                item.SetShowAsAction(ShowAsAction.Always);
                const string source = "ic_menu_dark.png";
                ImageGetter.SetDrawable(source, (bitmap, url, fromCache) =>
                {
                    if (bitmap != null && url == source)
                    {
                        item.SetIcon(new BitmapDrawable(ImageGetter.Resources, bitmap));
                    }
                });
            }
            else if (_buttons != null && _buttons.Length > 0 && _buttons[0]?.Item != null)
            {
                foreach (var button in _buttons)
                {
                    button.Item = menu.Add((int)OutputPane, button.Item.ItemId, (int)OutputPane, button.Title);
                }
            }
            else
            {
                var showIfRoom = true;
                _buttons = Enumerable.Range(0, Menu.ButtonCount).Select(i =>
                {
                    var menuItem = Menu.GetButton(i);
                    if (menuItem is UI.IMenu) showIfRoom = false;
                    return menuItem as MenuButton ?? menuItem?.Pair as MenuButton;
                }).ToArray();
                var subCount = 0;
                for (int i = 0; i < _buttons.Length; i++)
                {
                    var button = _buttons[i];
                    var menuId = button is IMenu ? subCount++ : (int)OutputPane;
                    button.AddToParent(menu, menuId, i, showIfRoom);
                }
            }
            base.OnCreateOptionsMenu(menu, inflater);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            var index = item.ItemId % byte.MaxValue;
            var active = PaneManager.Instance.FromNavContext(OutputPane).CurrentView as IView;
            if (!Equals(active) || index >= Menu.ButtonCount) return false;

            TextBase.CurrentFocus?.Blur(false);

            var paneId = item.ItemId / byte.MaxValue;
            if (paneId == (int)OutputPane)
            {
                if (Build.VERSION.SdkInt > BuildVersionCodes.JellyBean || Menu.ButtonCount == 1)
                {
                    Droid.Menu.OnClick(Menu, index, this);
                }
                else
                {
                    Droid.Menu.Activated(Menu, EventArgs.Empty);
                }
            }
            else  //Submenus
            {
                var subMenuId = paneId - Enum.GetValues(OutputPane.GetType()).Length;
                var subIndex = 0;
                for (var i = 0; i < Menu.ButtonCount; i++)
                {
                    var sub = Menu.GetButton(i) as UI.IMenu;
                    if (sub == null) continue;
                    if (subMenuId == subIndex)
                    {
                        Droid.Menu.OnClick(sub, index, this);
                        break;
                    }
                    subIndex++;
                }
            }
            return base.OnOptionsItemSelected(item);
        }

        private void homeView_FocusChanged(object sender, Android.Views.View.FocusChangeEventArgs e)
        {
            if (!e.HasFocus || RequestFocusHomeUp) return;
            RequestFocusHomeUp = true;
            View?.RequestFocus();
        }

        internal bool RequestFocusHomeUp { get; set; }

        private void UpdateMenu()
        {
            SetHasOptionsMenu(Menu != null && Menu.ButtonCount > 0 && (OutputPane < Pane.Popover || PopoverActivity.Instance != null));
        }

        #endregion

        #region Fragment overrides

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (savedInstanceState != null)
            {
                OutputPane = (Pane)savedInstanceState.GetInt(nameof(OutputPane));
                _background = savedInstanceState.GetString(nameof(_background));
                _stretch = (ContentStretch)savedInstanceState.GetInt(nameof(_stretch));
            }
            this.RaiseEvent(nameof(Activated), EventArgs.Empty);
        }

        public override void OnPause()
        {
            base.OnPause();

            var homeView = DroidFactory.HomeUpView;
            if (homeView != null)
            {
                homeView.FocusChange -= homeView_FocusChanged;
            }
        }

        public override void OnResume()
        {
            base.OnResume();
            UpdateBackground();
            UpdateMenu();
            ResetOrientation();

            var homeView = DroidFactory.HomeUpView;
            if (homeView != null)
            {
                homeView.FocusChange -= homeView_FocusChanged;
                homeView.FocusChange += homeView_FocusChanged;
                RequestFocusHomeUp = homeView.IsFocused && TextBase.CurrentFocus == null;
            }
        }

        private void UpdateBackground()
        {
            var stack = Stack as FragmentHistoryStack;
            Image target;
            if (stack == null || (target = DroidFactory.MainActivity.FindViewById<Image>(stack.BackgroundId) ??
                PopoverFragment.Instance?.View?.FindViewById<Image>(stack.BackgroundId)) == null) return;

            if (_background == null && iApp.Instance != null)
            {
                _background = iApp.Instance.Style.LayerBackgroundImage ??
                              iApp.Instance.Style.LayerBackgroundColor.HexCode;
            }

            if (string.IsNullOrEmpty(_background))
            {
                target.FilePath = null;
                iApp.Thread.ExecuteOnMainThread(() => { target.SetImageDrawable(null); });
            }
            else if (_background.StartsWith("#"))
            {
                target.Stretch = ContentStretch.Fill;
                target.FilePath = null;
                iApp.Thread.ExecuteOnMainThread(() => { target.SetImageDrawable(new Color(_background).ToColorDrawable()); });
            }
            else
            {
                target.Stretch = _stretch;
                if (_background == iApp.VanityImagePath)
                {
                    target.SetScaleType(ImageView.ScaleType.Center);
                }
                target.FilePath = _background;
            }
        }

        protected void ResetOrientation()
        {
            if (DroidFactory.Instance.LargeFormFactor)
            {
                Activity.RequestedOrientation = ScreenOrientation.Unspecified;
                return;
            }
            switch (PreferredOrientations)
            {
                case PreferredOrientation.Portrait:
                    Activity.RequestedOrientation = DroidFactory.AllowReversePortrait ?
                        ScreenOrientation.SensorPortrait : ScreenOrientation.Portrait;
                    break;
                case PreferredOrientation.Landscape:
                    Activity.RequestedOrientation = ScreenOrientation.SensorLandscape;
                    break;
                default:
                    Activity.RequestedOrientation = ScreenOrientation.Unspecified;
                    break;
            }
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutInt(nameof(OutputPane), (int)OutputPane);
            outState.PutString(nameof(_background), _background);
            outState.PutInt(nameof(_stretch), (int)_stretch);
            base.OnSaveInstanceState(outState);
        }

        #endregion

        #region Submission

        public void Submit(string url)
        {
            Submit(new Link(url));
        }

        public void Submit(Link link)
        {
            if (link.Parameters == null)
            {
                link.Parameters = new Dictionary<string, string>();
            }
            var layer = _model as iLayer;
            var submitValues = layer != null ? layer.GetFieldValues() : GetSubmissionValues();

            var submissionHandler = Submitting;
            if (submissionHandler == null)
            {
                if (Validate())
                {
                    link.Parameters.AddRange(submitValues);
                    DroidFactory.Navigate(link, this);
                }
                else
                {
                    Invalidate();
                    new Alert(iApp.Factory.GetResourceString("ValidationFailure"), string.Empty, AlertButtons.OK).Show();
                }
                return;
            }

            var args = new SubmissionEventArgs(link, ValidationErrors);
            submissionHandler(Pair, args);
            if (args.Cancel) { return; }

            link.Parameters.AddRange(submitValues);
            DroidFactory.Navigate(link, this);
        }

        protected virtual void Invalidate() { Render(); }

        protected virtual bool Validate() { return true; }

        public event SubmissionEventHandler Submitting;

        public virtual IDictionary<string, string> GetSubmissionValues()
        {
            return new Dictionary<string, string>(_submitValues);
        }

        internal void SetSubmitValues(IElementHost parent)
        {
            foreach (var control in parent.Children.OfType<IControl>().Where(c => c.ShouldSubmit()))
            {
                if (!control.Validate(out string[] errors))
                {
                    ValidationErrors[control.SubmitKey] = errors;
                }
                else
                {
                    ValidationErrors.Remove(control.SubmitKey);
                }

                if (control is SelectList selectList && selectList.SelectedItem is SelectListFieldItem item)
                {
                    _submitValues[control.SubmitKey + ".Key"] = item.Key;
                }
                _submitValues[control.SubmitKey] = control.StringValue;
            }
        }

        protected readonly Dictionary<string, string> _submitValues = new Dictionary<string, string>();

        public ValidationErrorCollection ValidationErrors { get; private set; }

        #endregion

        #region IHistoryEntry members

        public Link BackLink
        {
            get { return _backLink; }
            set
            {
                if (_backLink == value) return;
                Title = Title;
                _backLink = value;
                this.OnPropertyChanged();
            }
        }
        private Link _backLink;

        public string StackID
        {
            get { return _stackID; }
            set
            {
                if (_stackID == value) return;
                _stackID = value;
                this.OnPropertyChanged();
            }
        }
        private string _stackID;

        public Pane OutputPane
        {
            get { return _outputPane; }
            set
            {
                if (_outputPane == value) return;
                _outputPane = PaneManager.Instance.FromNavContext(value) == null ? PaneManager.Instance.TopmostPane.OutputOnPane : value;
                if (_outputPane < Pane.Master) _outputPane = Pane.Master;
                this.OnPropertyChanged();
            }
        }
        private Pane _outputPane;

        public PopoverPresentationStyle PopoverPresentationStyle
        {
            get { return _popoverPresentationStyle; }
            set
            {
                if (_popoverPresentationStyle == value) return;
                _popoverPresentationStyle = value;
                this.OnPropertyChanged();
            }
        }
        private PopoverPresentationStyle _popoverPresentationStyle;

        public ShouldNavigateDelegate ShouldNavigate
        {
            get { return _shouldNavigate; }
            set
            {
                if (_shouldNavigate == value) return;
                _shouldNavigate = value;
                this.OnPropertyChanged();
            }
        }
        private ShouldNavigateDelegate _shouldNavigate;

        public IHistoryStack Stack => PaneManager.Instance.FromNavContext(OutputPane);

        public event EventHandler Activated;
        public event EventHandler Deactivated;

        #endregion

        #region IMXView members

        private object _model;

        public virtual Type ModelType => _model?.GetType() ?? typeof(object);

        public object GetModel()
        {
            return _model;
        }

        public virtual void SetModel(object model)
        {
            _model = model;
        }

        public virtual void Render()
        {
            if (string.IsNullOrEmpty(Title))
                Title = iApp.Instance.Title;
            _submitValues.Clear();
            this.RaiseEvent(nameof(Rendering), EventArgs.Empty);
            UpdateMenu();
        }
        public event EventHandler Rendering;

        #endregion

        #region IView members

        public Color HeaderColor
        {
            get { return _headerColor.IsDefaultColor && iApp.Instance != null ? iApp.Instance.Style.HeaderColor : _headerColor; }
            set
            {
                if (_headerColor == value) return;
                _headerColor = value;
                this.OnPropertyChanged();
            }
        }
        private Color _headerColor;

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
                _title = value;
                if (OutputPane == Pane.Popover)
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
                    DroidFactory.Tabs.Title = _title;
                }
                if (_title == value) return;
                this.OnPropertyChanged();
            }
        }
        private string _title;

        public Color TitleColor
        {
            get { return _titleColor.IsDefaultColor && iApp.Instance != null ? iApp.Instance.Style.HeaderTextColor : _titleColor; }
            set
            {
                if (_titleColor == value) return;
                _titleColor = value;
                this.OnPropertyChanged();
            }
        }
        private Color _titleColor;

        public double Width => View?.Width ?? 0;

        public double Height => View?.Height / DroidFactory.DisplayScale ?? 0;

        public IPairable Pair
        {
            get { return _pair ?? this; }
            set
            {
                if (_pair != null) return;
                _pair = value;
                _pair.Pair = this;
            }
        }
        private IPairable _pair;

        public bool Equals(IView other)
        {
            return _pair == null ? base.Equals(other) || other == null : _pair.Equals(other.Pair);
        }

        public void SetBackground(Color color)
        {
            _background = color.ToString();
            UpdateBackground();
        }

        public void SetBackground(string imagePath, ContentStretch stretch)
        {
            _stretch = stretch;
            _background = imagePath;
            UpdateBackground();
        }

        private string _background;
        private ContentStretch _stretch;

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
    }
}