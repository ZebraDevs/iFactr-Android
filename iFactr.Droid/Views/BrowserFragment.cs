using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using MonoCross.Utilities;
using View = Android.Views.View;
using iFactr.Core;

namespace iFactr.Droid
{
    public class BrowserFragment : BaseFragment, UI.IBrowserView
    {
        public event EventHandler<UI.LoadFinishedEventArgs> LoadFinished;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (_browser == null)
            {
                _browser = new RichText
                {
                    Parent = this,
                };
                _browser.Load();
            }

            var viewParent = ((View)_browser).Parent as ViewGroup;
            if (viewParent != null)
            {
                _browser.Browser.LoadFinished -= ClientLoadFinished;
                viewParent.RemoveView(_browser);
            }

            _browser.Browser.AttachToView(this);
            _browser.Browser.LoadFinished += ClientLoadFinished;

            UserAgent = _agent;

            if (_url != null)
            {
                Load(_url);
                _url = null;
            }
            else if (_doc != null)
            {
                LoadFromString(_doc);
                _doc = null;
            }

            var control = DroidFactory.GetNativeObject<View>(_toolbar, "value");
            if (control != null)
            {
                ((ViewGroup)control.Parent)?.RemoveView(control);
                _browser.AddView(control, new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.WrapContent));
            }

            return _browser;
        }

        public override void OnResume()
        {
            base.OnResume();
            if (View == null) return;
            if (PopoverFragment.Instance == null)
            {
                View.LayoutParameters = new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MatchParent, FrameLayout.LayoutParams.MatchParent);
                ((RichText)View).Browser.LayoutParameters = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.MatchParent);
            }
            View.RequestFocus();
        }

        private void ClientLoadFinished(object sender, UI.LoadFinishedEventArgs e)
        {
            UpdateDefaultControls();
            LoadFinished?.Invoke(Pair ?? this, e);
        }

        public bool CanGoBack => _browser != null && _browser.Browser.CanGoBack;

        public bool CanGoForward => _browser != null && _browser.Browser.CanGoForward;

        public bool EnableDefaultControls
        {
            get { return _toolbar != null; }
            set
            {
                if (EnableDefaultControls == value) return;
                var control = DroidFactory.GetNativeObject<View>(_toolbar, "value");
                ((ViewGroup)control?.Parent)?.RemoveView(control);

                if (value)
                {
                    _toolbar = new Toolbar { Parent = this };
                    _backButton = new ToolbarButton { Title = Device.Resources.GetString("GoBack"), /* ImagePath = "backIcon.png"*/ };
                    _backButton.Clicked += (o, e) => GoBack();
                    _backButton.Enabled = _browser != null && CanGoBack;

                    _forwardButton = new ToolbarButton { Title = Device.Resources.GetString("GoForward"), /* ImagePath = "forwardIcon.png" */};
                    _forwardButton.Clicked += (o, e) => GoForward();
                    _forwardButton.Enabled = _browser != null && CanGoForward;

                    _toolbar.PrimaryItems = new[] { _backButton };
                    _toolbar.SecondaryItems = new[] { _forwardButton };
                }
                this.OnPropertyChanged();
            }
        }

        public string UserAgent
        {
            get { return _browser?.UserAgent ?? _agent; }
            set
            {
                _agent = value;
                if (_browser != null)
                {
                    _browser.UserAgent = _agent;
                }
            }
        }

        private Toolbar _toolbar;
        private ToolbarButton _backButton;
        private ToolbarButton _forwardButton;
        private RichText _browser;
        private string _url, _doc, _agent;

        public void GoBack()
        {
            if (_browser == null) return;
            _browser.Browser.GoBack();
            UpdateDefaultControls();
        }

        public void GoForward()
        {
            if (_browser == null) return;
            _browser.Browser.GoForward();
            UpdateDefaultControls();
        }

        public void LaunchExternal(string url)
        {
            Parameter.CheckUrl(url);
            DroidFactory.HandleUrl(new UI.Link(url) { RequestType = UI.RequestType.NewWindow }, false, this);
        }

        public void Load(string url)
        {
            var browser = GetModel() as Core.Layers.Browser;
            if (browser != null)
            {
                // Ensure that the current URL is passed to iFactr Browser
                browser.Url = url;
            }

            if (_browser != null)
            {
                _browser.Browser.Load(url);
                UpdateDefaultControls();
            }
            else
            {
                _url = url;
            }
        }

        public void LoadFromString(string html)
        {
            if (_browser == null)
            {
                _doc = html;
                return;
            }
            _browser.Browser.LoadFromString(html);
            UpdateDefaultControls();
        }

        public void Refresh()
        {
            if (_browser == null) return;
            _browser.Browser.Refresh();
            UpdateDefaultControls();
        }

        private void UpdateDefaultControls()
        {
            if (!EnableDefaultControls) return;
            _backButton.Enabled = CanGoBack;
            _forwardButton.Enabled = CanGoForward;
        }
    }
}