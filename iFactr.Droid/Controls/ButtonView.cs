using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using iFactr.UI;
using iFactr.UI.Controls;
using System;
using Color = iFactr.UI.Color;
using Point = iFactr.UI.Point;
using Size = iFactr.UI.Size;

namespace iFactr.Droid
{
    public class ButtonView : ButtonBase, IButton
    {
        #region Constructors

        [Preserve]
        public ButtonView()
        {
            Initialize();
        }

        public ButtonView(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public ButtonView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize(attrs);
        }

        [Preserve]
        public ButtonView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize(attrs);
        }

        protected ButtonView(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        private void Initialize(IAttributeSet attrs = null)
        {
            Font = Font.PreferredButtonFont;
            this.InitializeAttributes(attrs);
            _button.Click += OnClick;
        }

        #endregion

        #region Methods

        private void OnClick(object o, EventArgs e)
        {
            TextBase.CurrentFocus?.Blur(true);
            if (this.RaiseEvent(nameof(Clicked), EventArgs.Empty)) return;
            var view = Parent as GridBase;
            DroidFactory.Navigate(NavigationLink, view?.Parent);
        }

        public override void NullifyEvents()
        {
            base.NullifyEvents();
            Validating = null;
        }

        #endregion

        #region Style

        public bool IsEnabled
        {
            get { return _button != null && _button.Handle != IntPtr.Zero && _button.Enabled; }
            set
            {
                if (_button == null || _button.Handle == IntPtr.Zero || _button.Enabled == value) return;
                _button.Enabled = value;
                this.OnPropertyChanged();
            }
        }

        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                if (value == _backgroundColor || Handle == IntPtr.Zero) return;
                SetBackgroundColor(value.ToColor());
                _backgroundColor = value;
                this.OnPropertyChanged();
            }
        }
        private Color _backgroundColor;

        public IImage Image
        {
            get { return _image; }
            set
            {
                if (_image == value) return;
                _image = value;
                ImageGetter.SetDrawable(_image?.FilePath, (bitmap, url, fromCache) =>
                {
                    if (bitmap != null && url == _image?.FilePath)
                        _button.SetCompoundDrawables(bitmap, null, null, null);
                });
                this.OnPropertyChanged();
            }
        }
        private IImage _image;

        /// <summary>
        /// Gets or sets the font to be used when rendering the text.
        /// </summary>
        /// <value>The font.</value>
        public Font Font
        {
            get { return _font; }
            set
            {
                if (_font == value) return;
                _font = value;
                if (_font.Size > 0)
                    _button.SetTextSize(ComplexUnitType.Sp, (float)_font.Size);
                if (!string.IsNullOrEmpty(_font.Name))
                    _button.SetTypeface(Typeface.Create(_font.Name, (TypefaceStyle)_font.Formatting), (TypefaceStyle)_font.Formatting);
                this.OnPropertyChanged();
            }
        }
        private Font _font;

        #endregion

        #region Layout

        public new Visibility Visibility
        {
            get { return _visibility; }
            set
            {
                try
                {
                    if (value == _visibility || Handle == IntPtr.Zero) return;
                    var oldVisibility = _visibility;
                    _visibility = value;
                    switch (value)
                    {
                        case Visibility.Visible:
                            base.Visibility = ViewStates.Visible;
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
                catch (ObjectDisposedException) { /*GULP!!*/ }
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

        public Size Measure(Size constraints)
        {
            return this.MeasureView(constraints);
        }

        /// <summary>
        /// Sets the location and size of the control within its parent grid.
        /// This is called by the underlying grid layout system and should not be used in application logic.
        /// </summary>
        /// <param name="location">The X and Y coordinates of the upper left corner of the control.</param>
        /// <param name="size">The width and height of the control.</param>
        public void SetLocation(Point location, Size size)
        {
            var left = location.X;
            var right = location.X + size.Width;
            var top = location.Y;
            var bottom = location.Y + size.Height;

            Layout((int)left, (int)top, (int)right, (int)bottom);

            var widthSpec = MeasureSpec.MakeMeasureSpec((int)size.Width, MeasureSpecMode.Exactly);
            var heightSpec = MeasureSpec.MakeMeasureSpec((int)size.Height, MeasureSpecMode.Exactly);
            _button.Measure(widthSpec, heightSpec);
            _button.Layout(-Padding, -Padding, (int)size.Width + Padding, (int)size.Height + Padding);
        }

        #endregion

        #region Submission

        public string StringValue => _button.Text;

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

        public event ValidationEventHandler Validating;

        public bool Validate(out string[] errors)
        {
            var handler = Validating;
            if (handler != null)
            {
                var args = new ValidationEventArgs(SubmitKey, StringValue, StringValue);
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

        object IElement.Parent
        {
            get
            {
                var parent = Parent;
                return (parent as IPairable)?.Pair ?? parent ?? Metadata.Get<object>("Parent");
            }
        }

        public MetadataCollection Metadata => _metadata ?? (_metadata = new MetadataCollection());
        private MetadataCollection _metadata;

        public bool Equals(IElement other)
        {
            return (other as Element)?.Equals(this) ?? ReferenceEquals(this, other);
        }

        #endregion
    }
}