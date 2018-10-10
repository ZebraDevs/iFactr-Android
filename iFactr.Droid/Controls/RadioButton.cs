using System;
using System.ComponentModel;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using iFactr.UI;
using iFactr.UI.Controls;

namespace iFactr.Droid
{
    public class RadioButton : Android.Widget.RadioButton, IButton, ISwitch, INotifyPropertyChanged
    {
        #region Constructors

        [Preserve]
        public RadioButton()
            : this(DroidFactory.MainActivity)
        { }

        public RadioButton(string title)
            : this()
        {
            Title = title;
        }

        public RadioButton(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public RadioButton(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public RadioButton(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize(attrs);
        }

        [Preserve]
        public RadioButton(Context context, IAttributeSet attrs, int defStyleAttr)
            : base(context, attrs, defStyleAttr)
        {
            Initialize(attrs);
        }

        [Preserve]
        public RadioButton(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes)
            : base(context, attrs, defStyleAttr, defStyleRes)
        {
            Initialize(attrs);
        }

        private void Initialize(IAttributeSet attrs = null)
        {
            CheckedChange += RadioButton_CheckedChange;
            this.InitializeAttributes(attrs);
        }

        #endregion

        #region Methods

        public void NullifyEvents()
        {
            Clicked = null;
            ValueChanged = null;
            Validating = null;
        }

        #endregion

        #region Value

        public event PropertyChangedEventHandler PropertyChanged;

        private void RadioButton_CheckedChange(object sender, CheckedChangeEventArgs e)
        {
            this.RaiseEvent("ValueChanged", new ValueChangedEventArgs<bool>(!Value, Value));
            this.RaiseEvent("Clicked", EventArgs.Empty);
            this.OnPropertyChanged("StringValue");
            this.OnPropertyChanged("Value");
            this.OnPropertyChanged("Checked");
        }

        public event ValueChangedEventHandler<bool> ValueChanged;

        public bool Value
        {
            get { return Checked; }
            set { Checked = value; }
        }

        Link IButton.NavigationLink
        {
            get { return null; }
            set { }
        }

        public string Title
        {
            get { return Text; }
            set
            {
                if (Text == value) return;
                Text = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(Text));
            }

        }
        public event EventHandler Clicked;

        #endregion

        #region Style

        public bool IsEnabled
        {
            get { return Handle != IntPtr.Zero && Enabled; }
            set
            {
                if (Handle == IntPtr.Zero || Enabled == value) return;
                Enabled = value;
                this.OnPropertyChanged();
            }
        }

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
            get { return _backgroundColor; }
            set
            {
                if (_foregroundColor == value || Handle == IntPtr.Zero) return;
                SetTextColor(value.IsDefaultColor ? Android.Graphics.Color.Black : value.ToColor());
                _foregroundColor = value;
                this.OnPropertyChanged();
            }
        }
        private Color _foregroundColor;

        IImage IButton.Image
        {
            get { return null; }
            set { }
        }

        Color ISwitch.TrueColor
        {
            get { return default(Color); }
            set { }
        }

        Color ISwitch.FalseColor
        {
            get { return default(Color); }
            set { }
        }

        /// <summary>
        /// Gets or sets the font to be used when rendering the text.
        /// </summary>
        /// <value>The font.</value>
        public Font Font
        {
            get { return _font; }
            set
            {
                if (_font == value || Handle == IntPtr.Zero) return;
                _font = value;

                if (_font.Size > 0)
                {
                    SetTextSize(ComplexUnitType.Sp, (float)_font.Size);
                }

                var hasName = !string.IsNullOrEmpty(_font.Name);
                if (hasName || _font.Formatting > FontFormatting.Normal)
                {
                    var formatting = (Android.Graphics.TypefaceStyle)_font.Formatting;
                    var typeface = hasName ? Android.Graphics.Typeface.Create(_font.Name, formatting) : Typeface;
                    SetTypeface(typeface, formatting);
                }

                this.OnPropertyChanged();
            }
        }
        private Font _font;

        #endregion

        #region Submission

        public string StringValue => Checked.ToString().ToLower();

        public string SubmitKey
        {
            get { return _submitKey; }
            set
            {
                if (_submitKey == value) return;
                _submitKey = value;
                this.OnPropertyChanged();
            }
        }
        private string _submitKey;

        public bool Validate(out string[] errors)
        {
            var handler = Validating;
            if (handler != null)
            {
                var args = new ValidationEventArgs(SubmitKey, Value, StringValue);
                handler(Pair ?? this, args);

                if (args.Errors.Count > 0)
                {
                    errors = new string[args.Errors.Count];
                    args.Errors.CopyTo(errors, 0);
                    return false;
                }
            }

            errors = null;
            return true;
        }

        public event ValidationEventHandler Validating;

        #endregion

        #region Layout

        public new Visibility Visibility
        {
            get { return _visibility; }
            set
            {
                if (value == _visibility || Handle == IntPtr.Zero) return;
                var oldVisibility = _visibility;
                _visibility = value;
                switch (value)
                {
                    case Visibility.Visible:
                        base.Visibility = ViewStates.Visible;
                        BringToFront();
                        break;
                    case Visibility.Hidden:
                        base.Visibility = ViewStates.Invisible;
                        break;
                    case Visibility.Collapsed:
                        base.Visibility = ViewStates.Gone;
                        break;
                }
                this.OnPropertyChanged();
                this.RequestResize(oldVisibility == Visibility.Collapsed || _visibility == Visibility.Collapsed);
            }
        }
        private Visibility _visibility;

        public Thickness Margin
        {
            get { return _margin; }
            set
            {
                if (_margin == value) return;
                _margin = value;
                this.OnPropertyChanged();
            }
        }
        private Thickness _margin;

        public int ColumnIndex
        {
            get { return _columnIndex; }
            set
            {
                if (value == _columnIndex) return;
                _columnIndex = value;
                this.OnPropertyChanged();
            }
        }
        private int _columnIndex = Element.AutoLayoutIndex;

        public int ColumnSpan
        {
            get { return _columnSpan; }
            set
            {
                if (value == _columnSpan) return;
                _columnSpan = value;
                this.OnPropertyChanged();
            }
        }
        private int _columnSpan = 1;

        public int RowIndex
        {
            get { return _rowIndex; }
            set
            {
                if (value == _rowIndex) return;
                _rowIndex = value;
                this.OnPropertyChanged();
            }
        }
        private int _rowIndex = Element.AutoLayoutIndex;

        public int RowSpan
        {
            get { return _rowSpan; }
            set
            {
                if (_rowSpan == value) return;
                _rowSpan = value;
                this.OnPropertyChanged();
            }
        }
        private int _rowSpan = 1;

        public HorizontalAlignment HorizontalAlignment
        {
            get { return _horizontalAlignment; }
            set
            {
                if (value == _horizontalAlignment) return;
                _horizontalAlignment = value;
                this.OnPropertyChanged();
            }
        }
        private HorizontalAlignment _horizontalAlignment;

        public VerticalAlignment VerticalAlignment
        {
            get { return _verticalAlignment; }
            set
            {
                if (value == _verticalAlignment) return;
                _verticalAlignment = value;
                this.OnPropertyChanged();
            }
        }
        private VerticalAlignment _verticalAlignment;

        public UI.Size Measure(UI.Size constraints)
        {
            return this.MeasureView(constraints);
        }

        /// <summary>
        /// Sets the location and size of the control within its parent grid.
        /// This is called by the underlying grid layout system and should not be used in application logic.
        /// </summary>
        /// <param name="location">The X and Y coordinates of the upper left corner of the control.</param>
        /// <param name="size">The width and height of the control.</param>
        public void SetLocation(Point location, UI.Size size)
        {
            var left = location.X;
            var right = location.X + MeasuredWidth;
            var top = location.Y;
            var bottom = location.Y + MeasuredHeight;

            Layout((int)left, (int)top, (int)right, (int)bottom);
        }

        #endregion

        #region Identity

        public string ID
        {
            get { return _id; }
            set
            {
                if (_id == value) return;
                _id = value;
                this.OnPropertyChanged();
            }
        }
        private string _id;

        object IElement.Parent => Parent;

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

        public MetadataCollection Metadata => _metadata ?? (_metadata = new MetadataCollection());
        private MetadataCollection _metadata;

        public bool Equals(IElement other)
        {
            var control = other as Element;
            return control?.Equals(this) ?? ReferenceEquals(this, other);
        }
    }

    #endregion
}