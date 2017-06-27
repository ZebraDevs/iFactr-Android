using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Widget;
using iFactr.UI;
using System;
using System.ComponentModel;
using Color = iFactr.UI.Color;

namespace iFactr.Droid
{
    public class HeaderView : FrameLayout, ISectionHeader, INotifyPropertyChanged
    {
        private TextView _header;

        #region Constructors

        [Preserve]
        public HeaderView()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public HeaderView(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public HeaderView(Context context, Android.Util.IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize();
        }

        [Preserve]
        public HeaderView(Context context, Android.Util.IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize();
        }

        public HeaderView(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            Initialize();
        }

        private void Initialize()
        {
            _header = new TextView(Context, null, Android.Resource.Attribute.ListSeparatorTextViewStyle);
            _header.SetTypeface(Typeface.DefaultBold, TypefaceStyle.Bold);
            _header.SetPadding((int)(Thickness.LeftMargin * DroidFactory.DisplayScale), 0, 0, 0);
            base.AddView(_header, new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent));
        }

        #endregion

        #region Style

        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                if (_backgroundColor == value || _header.Handle == IntPtr.Zero) return;
                _backgroundColor = value;
                if (!_backgroundColor.IsDefaultColor)
                {
                    _header.SetBackgroundColor(_backgroundColor.ToColor());
                }
                this.OnPropertyChanged();
            }
        }
        private Color _backgroundColor;

        public Color ForegroundColor
        {
            get { return _foregroundColor; }
            set
            {
                if (_foregroundColor == value || _header.Handle == IntPtr.Zero) return;
                _foregroundColor = value;
                if (!_foregroundColor.IsDefaultColor)
                {
                    _header.SetTextColor(_foregroundColor.ToColor());
                }
                this.OnPropertyChanged();
            }
        }
        private Color _foregroundColor;

        public Font Font
        {
            get { return _font; }
            set
            {
                if (_font == value) return;
                _font = value;
                if (_font.Size > 0)
                    _header.SetTextSize(Android.Util.ComplexUnitType.Sp, (float)_font.Size);
                if (!string.IsNullOrEmpty(_font.Name))
                    _header.SetTypeface(Typeface.Create(_font.Name, (TypefaceStyle)_font.Formatting), (TypefaceStyle)_font.Formatting);
                this.OnPropertyChanged();
            }
        }
        private Font _font;

        #endregion

        public string Text
        {
            get { return _header.Text; }
            set { _header.Text = value; }
        }

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

        public event PropertyChangedEventHandler PropertyChanged;
    }
}