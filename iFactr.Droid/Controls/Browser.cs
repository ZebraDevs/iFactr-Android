using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Runtime;
using Android.Webkit;
using iFactr.Core.Controls;
using MonoCross.Utilities;
using iFactr.UI;
using iFactr.Core;

namespace iFactr.Droid
{
    public class Browser : WebView
    {
        #region Constructors

        [Preserve]
        public Browser() : this(DroidFactory.MainActivity) { }

        protected Browser(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

        public Browser(Context context) : base(context) { }

        [Preserve]
        public Browser(Context context, Android.Util.IAttributeSet attrs) : base(context, attrs) { }

        [Preserve]
        public Browser(Context context, Android.Util.IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle) { }

        public Browser(Context context, Android.Util.IAttributeSet attrs, int defStyle, bool privateBrowsing) : base(context, attrs, defStyle, privateBrowsing) { }

        #endregion

        public bool ErrorOccured { get; set; }

        public Color ForegroundColor { get; set; }

        public Color BackgroundColor { get; set; }

        public string UserAgent
        {
            get { return Settings.UserAgentString; }
            set
            {
                Settings.UserAgentString = value ?? (_originalUserAgent ??
                    (_originalUserAgent = Settings.UserAgentString));
            }
        }
        private static string _originalUserAgent;

        public void AttachToView(IView parentView, bool newBrowserView = false)
        {
            SetWebViewClient(_client = new HtmlTextWebViewClient(parentView, newBrowserView));
            if (parentView != null) SetWebChromeClient(new HtmlTextWebChomeClient(parentView));
            Settings.JavaScriptEnabled = true;
            SetVerticalScrollbarOverlay(true);
        }

        public double MinHeight
        {
            get { return _minHeight; }
            set
            {
                if (value < 0 || value == _minHeight) return;
                _minHeight = value;
                SetMinimumHeight((int)_minHeight);
                ((Android.Views.View)Parent)?.SetMinimumHeight((int)_minHeight);
            }
        }
        private double _minHeight;

        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();
            ((Android.Views.View)Parent).SetMinimumHeight((int)_minHeight);
        }

        public IEnumerable<PanelItem> Items
        {
            get { return _client?.Items; }
            set
            {
                if (_client == null) return;
                _client.Items = value;
            }
        }

        private HtmlTextWebViewClient _client;

        /// <summary>
        /// Gets a value indicating whether the browser can navigate back a page in the browsing history.
        /// </summary>
        public new bool CanGoBack => base.CanGoBack();

        /// <summary>
        /// Gets a value indicating whether the browser can navigate forward a page in the browsing history.
        /// </summary>
        public new bool CanGoForward => base.CanGoForward();

        /// <summary>
        /// Loads the specified URL in the browser.
        /// </summary>
        /// <param name="url">The URL to load.</param>
        public void Load(string url)
        {
            ErrorOccured = false;
            Parameter.CheckUrl(url);
            var uri = url;
            if (!uri.StartsWith("javascript:") && !uri.StartsWith("data:"))
            {
                DroidFactory.Instance.ActivateLoadTimer();
                uri = new UriBuilder(uri).Uri.ToString();
            }

            SetBackgroundDrawable(null);
            LoadUrl(uri);
        }

        /// <summary>
        /// Reads the specified string as HTML and loads the result in the browser.
        /// </summary>
        /// <param name="html">The HTML to load.</param>
        public void LoadFromString(string html)
        {
            ErrorOccured = false;
            SetBackgroundDrawable(null);
            LoadData(html, "text/html", "utf-8");
        }

        /// <summary>
        /// Reads the specified string as HTML and loads the result in the browser.
        /// </summary>
        /// <param name="htmlContent">The HTML content to load into an empty DOM.</param>
        public void LoadContent(string htmlContent)
        {
            ErrorOccured = false;
            if (htmlContent == null) return;
            var backgroundColor = BackgroundColor.IsDefaultColor ? "background:transparent;" : "background:" + "#" + BackgroundColor.HexCode.Substring(3) + ";";
            var textColor = ";color:" + (ForegroundColor.IsDefaultColor ? "#000" : "#" + ForegroundColor.HexCode.Substring(3));
            const string body = "<html><head><meta name=\"viewport\" content=\"initial-scale=1.0, user-scalable=no\"/></head><body style=\"-webkit-text-size-adjust:none;{0}margin:10px 15px 15px;font-family:helvetica,arial,sans-serif;font-size:16{1}\">{2}</body></html>";
            LoadDataWithBaseURL(iApp.Factory.ApplicationPath, string.Format(body, backgroundColor, textColor, htmlContent), "text/html", "utf-8", null);
            SetBackgroundColor(BackgroundColor.ToColor());
        }

        /// <summary>
        /// Refreshes the contents of the browser.
        /// </summary>
        public void Refresh()
        {
            ErrorOccured = false;
            iApp.Factory.BeginBlockingUserInput();
            Reload();
        }

        public event EventHandler<LoadFinishedEventArgs> LoadFinished;

        private class HtmlTextWebViewClient : WebViewClient
        {
            private readonly bool _newBrowser;

            private readonly IView _parentView;

            public IEnumerable<PanelItem> Items { get; set; }

            public HtmlTextWebViewClient(IView parent, bool newBrowser)
            {
                _parentView = parent;
                _newBrowser = newBrowser;
            }

            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                url = url.Replace(Device.ApplicationPath, string.Empty);
                var links = Items?.OfType<UI.Link>();
                var link = links?.FirstOrDefault(l => l.Address == url) ?? new UI.Link(url);
                if (DroidFactory.HandleUrl(link, _newBrowser, _parentView))
                {
                    return true;
                }
                iApp.Factory.BeginBlockingUserInput();
                return false;
            }

            public override void OnPageFinished(WebView view, string url)
            {
                base.OnPageFinished(view, url);

                if (view.OriginalUrl == null)
                {
                    _parentView.Title = iApp.Instance.Title;
                    var browser = (Browser)view;
                    browser.LoadContent(Device.Resources.GetString("FailedNavigation"));
                    browser.ErrorOccured = true;
                }
                iApp.Factory.StopBlockingUserInput();
                Events.RaiseEvent(view, nameof(LoadFinished), new LoadFinishedEventArgs(url));
            }
        }

        private class HtmlTextWebChomeClient : WebChromeClient
        {
            private readonly IView _parentView;

            public HtmlTextWebChomeClient(IView parent)
            {
                if (parent == null) throw new ArgumentNullException(nameof(parent));
                _parentView = parent;
            }

            public override void OnReceivedTitle(WebView view, string title)
            {
                base.OnReceivedTitle(view, title);
                if (title != null && !title.StartsWith("about:"))
                {
                    _parentView.Title = ((Browser)view).ErrorOccured ? iApp.Instance.Title : title;
                }
            }
        }
    }
}