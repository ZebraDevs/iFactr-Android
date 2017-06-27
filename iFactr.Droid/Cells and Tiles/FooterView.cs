using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using iFactr.UI;
using System;
using System.ComponentModel;
using MonoCross.Utilities;
using Color = iFactr.UI.Color;

namespace iFactr.Droid
{
    public class FooterView : TextView, ISectionFooter, INotifyPropertyChanged
    {
        #region Constructors

        [Preserve]
        public FooterView()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public FooterView(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public FooterView(Context context, Android.Util.IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize();
        }

        [Preserve]
        public FooterView(Context context, Android.Util.IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize();
        }

        public FooterView(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            Initialize();
        }

        private void Initialize()
        {
            base.Gravity = GravityFlags.CenterHorizontal;
        }

        #endregion

        #region Style

        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                if (_backgroundColor == value || Handle == IntPtr.Zero) return;
                SetBackgroundColor(value.IsDefaultColor ? Android.Graphics.Color.Transparent : value.ToColor());
                _backgroundColor = value;
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
                try
                {
                    SetTextColor(value.IsDefaultColor ? Android.Graphics.Color.Black : value.ToColor());
                    _foregroundColor = value;
                    this.OnPropertyChanged();
                }
                catch (Exception e)
                {
                    Device.Log.Warn($"SetTextColor threw an {e} exception");
                }
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
                    SetTextSize(Android.Util.ComplexUnitType.Sp, (float)_font.Size);
                if (!string.IsNullOrEmpty(_font.Name))
                    SetTypeface(Typeface.Create(_font.Name, (TypefaceStyle)_font.Formatting), (TypefaceStyle)_font.Formatting);
                this.OnPropertyChanged();
            }
        }
        private Font _font;

        #endregion

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