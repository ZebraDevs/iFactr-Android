using System;
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Widget;
using iFactr.Core.Controls;
using iFactr.UI;
using Android.Runtime;
using Android.Util;
using System.Text;

namespace iFactr.Droid
{
    public sealed class RichText : LinearLayout, IRichContentCell
    {
        public Browser Browser { get; private set; }

        public string UserAgent
        {
            get { return Browser?.UserAgent; }
            set
            {
                if (Browser == null) return;
                Browser.UserAgent = value;
            }
        }

        #region Constructors

        [Preserve]
        public RichText()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public RichText(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            Initialize();
        }

        public RichText(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public RichText(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize(attrs);
        }

        [Preserve]
        public RichText(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize(attrs);
        }

        private void Initialize(IAttributeSet attrs = null)
        {
            Orientation = Orientation.Vertical;
            LayoutParameters = new AbsListView.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);

            //TODO: Initialize cell from attrs
        }

        #endregion

        public IPairable Pair
        {
            get { return _pair; }
            set
            {
                if (_pair != null || value == null) return;
                _pair = value;
                _pair.Pair = this;
            }
        }
        private IPairable _pair;

        public bool Equals(ICell other)
        {
            return false;
        }

        public Color BackgroundColor { get; set; }

        public double MaxHeight { get; set; }

        public new IView Parent { get; set; }

        public double MinHeight
        {
            get { return Browser?.MinHeight ?? 0; }
            set
            {
                _minHeight = value;
                if (Browser != null)
                    Browser.MinHeight = value;
            }
        }
        private double _minHeight;

        public string Text
        {
            get
            {
                StringBuilder html = new StringBuilder(_text);
                foreach (var item in Items)
                    html.Append(item.GetHtml());
                return html.ToString();
            }
            set
            {
                _text = value;
                Items.Clear();
            }
        }
        private string _text;

        public List<PanelItem> Items { get; set; } = new List<PanelItem>();

        public Color ForegroundColor { get; set; }

        public void Load()
        {
            RemoveAllViews();

            if (Browser == null)
            {
                Browser = new Browser();
            }
            else
            {
                Browser.LoadFinished -= Browser_LoadFinished;
                ((ViewGroup)Browser.Parent)?.RemoveView(Browser);
            }

            Browser.AttachToView(Parent, true);
            Browser.Items = Items;
            Browser.ForegroundColor = ForegroundColor;
            Browser.BackgroundColor = BackgroundColor;
            Browser.MinHeight = _minHeight;

            AddView(Browser, new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent));
            Browser.LoadFinished += Browser_LoadFinished;
            Browser.LoadContent(Text);
        }

        private void Browser_LoadFinished(object sender, LoadFinishedEventArgs e)
        {
            _ready = true;
            AdjustPopover();
        }
        private bool _ready;
        private int _oldHeight;

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            _oldHeight = MeasuredHeight;
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            AdjustPopover();
        }

        private void AdjustPopover()
        {
            if (!_ready || MeasuredHeight <= _oldHeight || PopoverFragment.Instance == null)
                return;
            var metrics = new DisplayMetrics();
            DroidFactory.MainActivity.WindowManager.DefaultDisplay.GetMetrics(metrics);
            var width = (int)(metrics.WidthPixels * .6 + 16 * DroidFactory.DisplayScale);
            var height = Parent is IBrowserView ? LayoutParams.MatchParent :
                PopoverFragment.Instance.View.Height + MeasuredHeight - _oldHeight + (int)(16 * DroidFactory.DisplayScale);
            PopoverFragment.Instance.Dialog.Window.SetLayout(width, height > metrics.HeightPixels ? LayoutParams.MatchParent : height);
        }

        public MetadataCollection Metadata => _metadata ?? (_metadata = new MetadataCollection());
        private MetadataCollection _metadata;
    }
}