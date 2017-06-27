using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Android.Widget;
using iFactr.UI;
using Android.Runtime;
using Android.Content;
using Android.Util;
using System;

namespace iFactr.Droid
{
    public class Toolbar : HorizontalScrollView, IToolbar, INotifyPropertyChanged
    {
        private LinearLayout _bar;

        #region Constructors

        [Preserve]
        public Toolbar()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public Toolbar(Context context)
            : base(context)
        {
            Initialize();
        }

        public Toolbar(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize();
        }

        public Toolbar(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize();
        }

        public Toolbar(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            Initialize();
        }

        private void Initialize()
        {
            _bar = new LinearLayout(Context)
            {
                LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent),
            };
            _primaryItems = new List<IToolbarItem>();
            _secondaryItems = new List<IToolbarItem>();
            base.AddView(_bar);
        }

        #endregion

        public new IView Parent { get; set; }

        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                if (_backgroundColor == value || Handle == IntPtr.Zero) return;
                _backgroundColor = value;
                SetBackgroundColor(_backgroundColor.ToColor());
                this.OnPropertyChanged();
            }
        }
        private Color _backgroundColor;

        public Color ForegroundColor
        {
            get { return _foregroundColor; }
            set
            {
                if (_foregroundColor == value) return;
                _foregroundColor = value;
                this.OnPropertyChanged();
            }
        }
        private Color _foregroundColor;

        public IEnumerable<IToolbarItem> PrimaryItems
        {
            get { return _primaryItems; }
            set
            {
                if (_primaryItems != null && _primaryItems.Equivalent(value, true) ||
                    _primaryItems == null && value == null) return;
                _primaryItems = value;
                foreach (var primaryItem in _primaryItems.Where(primaryItem => primaryItem.ForegroundColor.IsDefaultColor))
                    primaryItem.ForegroundColor = ForegroundColor;
                UpdateItems();
                this.OnPropertyChanged();
            }
        }
        private IEnumerable<IToolbarItem> _primaryItems;

        public IEnumerable<IToolbarItem> SecondaryItems
        {
            get { return _secondaryItems; }
            set
            {
                if (_secondaryItems != null && _secondaryItems.Equivalent(value, true) ||
                    _secondaryItems == null && value == null) return;
                _secondaryItems = value;
                foreach (var primaryItem in _primaryItems.Where(primaryItem => primaryItem.ForegroundColor.IsDefaultColor))
                    primaryItem.ForegroundColor = ForegroundColor;
                UpdateItems();
                this.OnPropertyChanged();
            }
        }
        private IEnumerable<IToolbarItem> _secondaryItems;

        public IPairable Pair
        {
            get { return _pair; }
            set
            {
                if (_pair != null || value == null) return;
                _pair = value;
                _pair.Pair = this;
                this.OnPropertyChanged();
            }
        }
        private IPairable _pair;

        internal void UpdateItems()
        {
            _bar.RemoveAllViews();

            if (SecondaryItems != null)
            {
                foreach (var control in SecondaryItems.Select(item => DroidFactory.GetNativeObject<Android.Views.View>(item, "item")).Where(item => item != null))
                    _bar.AddView(control);
            }

            if (PrimaryItems == null || !PrimaryItems.Any()) return;
            {
                _bar.AddView(new Android.Views.View(Context), new LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.MatchParent, 1));
                foreach (var control in PrimaryItems.Select(item => DroidFactory.GetNativeObject<Android.Views.View>(item, "item")).Where(item => item != null))
                    _bar.AddView(control);
            }
        }

        public bool Equals(IToolbar other)
        {
            var toolbar = other as UI.Toolbar;
            return toolbar == null ? ReferenceEquals(this, other) : toolbar.Equals(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}