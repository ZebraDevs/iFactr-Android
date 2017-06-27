using System;
using System.ComponentModel;
using Android.App;
using Android.Graphics;
using Android.Views;
using iFactr.UI;
using iFactr.UI.Controls;
using Button = Android.Widget.Button;
using Color = iFactr.UI.Color;
using Point = iFactr.UI.Point;
using Android.Runtime;
using Android.Content;
using Android.Util;
using MonoCross.Utilities;
using Size = iFactr.UI.Size;

namespace iFactr.Droid
{
    public class TimePicker : Button, ITimePicker, INotifyPropertyChanged
    {
        private TimePickerDialog _timePicker;

        #region Constructors

        [Preserve]
        public TimePicker()
            : base(DroidFactory.MainActivity, null, Android.Resource.Attribute.SpinnerStyle)
        {
            Initialize();
        }

        public TimePicker(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public TimePicker(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize(attrs);
        }

        [Preserve]
        public TimePicker(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize(attrs);
        }

        protected TimePicker(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        private void Initialize(IAttributeSet attrs = null)
        {
            Focusable = false;
            Font = Font.PreferredDateTimePickerFont;
            this.InitializeAttributes(attrs);
            Click += (o, e) => ShowPicker();
        }

        #endregion

        #region Methods

        public void ShowPicker()
        {
            TextBase.CurrentFocus?.Blur(true);
            var now = Time ?? DateTime.Now;
            _timePicker = new TimePickerDialog(DroidFactory.MainActivity, OnTimeChanged, now.Hour, now.Minute,
                Android.Text.Format.DateFormat.Is24HourFormat(DroidFactory.MainActivity));
            _timePicker.Show();
        }

        private void OnTimeChanged(object sender, TimePickerDialog.TimeSetEventArgs e)
        {
            var date = Time?.Date ?? DateTime.Today;
            Time = date.AddHours(e.HourOfDay).AddMinutes(e.Minute);
        }

        public void NullifyEvents()
        {
            Validating = null;
            TimeChanged = null;
        }

        #endregion

        #region Value

        public new virtual string Text
        {
            get { return Handle == IntPtr.Zero ? null : base.Text; }
            set
            {
                if (Handle != IntPtr.Zero)
                    base.Text = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event ValueChangedEventHandler<DateTime?> TimeChanged;

        public DateTime? Time
        {
            get { return _value; }
            set
            {
                if (_value == value) return;
                var oldVal = _value;
                _value = value;

                Device.Thread.ExecuteOnMainThread(() => { Text = value?.ToString(TimeFormat ?? "t") ?? string.Empty; });
                (Parent as GridBase)?.SetSubmission(SubmitKey, StringValue);
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(StringValue));
                this.RaiseEvent(nameof(TimeChanged), new ValueChangedEventArgs<DateTime?>(oldVal, value));
            }
        }
        private DateTime? _value;

        public string TimeFormat
        {
            get { return _valueFormat; }
            set
            {
                if (_valueFormat == value) return;
                _valueFormat = value;
                Text = Time?.ToString(TimeFormat ?? "t") ?? string.Empty;
                this.OnPropertyChanged();
            }
        }
        private string _valueFormat;

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
                this.OnPropertyChanged(nameof(Enabled));
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
            get { return _foregroundColor; }
            set
            {
                if (_foregroundColor == value || Handle == IntPtr.Zero) return;
                SetTextColor(value.IsDefaultColor ? Android.Graphics.Color.Black : value.ToColor());
                _foregroundColor = value;
                this.OnPropertyChanged();
            }
        }
        private Color _foregroundColor;

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
                    SetTextSize(ComplexUnitType.Sp, (float)_font.Size);
                if (!string.IsNullOrEmpty(_font.Name))
                    SetTypeface(Typeface.Create(_font.Name, (TypefaceStyle)_font.Formatting), (TypefaceStyle)_font.Formatting);
                this.OnPropertyChanged();
            }
        }
        private Font _font;

        #endregion

        #region Submission

        public string StringValue => Text;

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
                var args = new ValidationEventArgs(SubmitKey, Time, StringValue);
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

        public Size Measure(Size constraints)
        {
            if (constraints.Width > 0 && !double.IsInfinity(constraints.Width))
                SetWidth((int)constraints.Width);
            this.MeasureView(constraints);
            return new Size(constraints.Width, MeasuredHeight);
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
            var bottom = location.Y + MeasuredHeight;

            SetWidth((int)size.Width);
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

        #endregion
    }
}