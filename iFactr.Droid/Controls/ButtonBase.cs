using System;
using System.ComponentModel;
using Android.Text;
using Android.Views;
using iFactr.UI;
using Color = iFactr.UI.Color;
using Android.Runtime;
using Button = Android.Widget.Button;
using Android.Content;
using iFactr.Core;
using MonoCross.Navigation;

namespace iFactr.Droid
{
    public abstract class ButtonBase : Android.Widget.FrameLayout, IPairable, INotifyPropertyChanged
    {
        protected Button _button;
        protected static readonly int Padding = (int)(DroidFactory.DisplayScale * 4);

        #region Constructors

        public ButtonBase()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public ButtonBase(Context context)
            : base(context)
        {
            Initialize();
        }

        public ButtonBase(Context context, Android.Util.IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize();
        }

        public ButtonBase(Context context, Android.Util.IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize();
        }

        protected ButtonBase(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        #endregion

        #region Methods

        private void Initialize()
        {
            _button = MXContainer.Resolve<Button>(GetType().Name, Context) ?? new Button(Context);
            AddView(_button, new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent, GravityFlags.Fill));
            _button.Gravity = GravityFlags.Center;
            _button.SetPadding(4 * Padding, 2 * Padding, 4 * Padding, 0);
            _button.SetSingleLine(true);
            _button.Ellipsize = TextUtils.TruncateAt.End;
            _button.TextChanged += TextChanged;

            ForegroundColor = iApp.Instance.Style.TextColor;
        }

        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            this.OnPropertyChanged("StringValue");
            this.OnPropertyChanged("Title");
        }

        #endregion

        public virtual void NullifyEvents()
        {
            Clicked = null;
        }

        #region Click

        public event EventHandler Clicked;

        public Link NavigationLink
        {
            get { return _navigationLink; }
            set
            {
                if (value == _navigationLink) return;
                _navigationLink = value;
                this.OnPropertyChanged();
            }
        }
        private Link _navigationLink;

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

        #endregion

        #region Value

        public event PropertyChangedEventHandler PropertyChanged;

        public string Title
        {
            get { return _button.Handle == IntPtr.Zero ? null : _button.Text; }
            set { if (_button.Handle != IntPtr.Zero) _button.Text = value; }
        }

        #endregion

        #region Style

        public Color ForegroundColor
        {
            get { return _foregroundColor; }
            set
            {
                if (_foregroundColor == value || Handle == IntPtr.Zero) return;
                _button.SetTextColor(value.IsDefaultColor ? Android.Graphics.Color.Black : value.ToColor());
                _foregroundColor = value;
                this.OnPropertyChanged();
            }
        }
        private Color _foregroundColor;

        #endregion
    }
}