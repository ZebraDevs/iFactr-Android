using System;
using System.ComponentModel;
using System.Globalization;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using iFactr.UI;
using iFactr.UI.Controls;
using Android.Runtime;
using Color = iFactr.UI.Color;
using Point = iFactr.UI.Point;
using Android.Content;
using Android.Util;
using Size = iFactr.UI.Size;

namespace iFactr.Droid
{
    public class Slider : SeekBar, ISlider, INotifyPropertyChanged
    {
        private TrackerDrawable _sliderDrawable;
        private double _oldValue;

        #region Constructors

        [Preserve]
        public Slider()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public Slider(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public Slider(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize(attrs);
        }

        [Preserve]
        public Slider(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize(attrs);
        }

        protected Slider(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }
        private void Initialize(IAttributeSet attrs = null)
        {
            this.InitializeAttributes(attrs);
            _sliderDrawable = new TrackerDrawable(this);
            ProgressChanged += Slider_ProgressChanged;
            StopTrackingTouch += (o, e) => Toast.MakeText(Context, StringValue, ToastLength.Short).Show();
        }

        #endregion

        public void NullifyEvents()
        {
            Validating = null;
            ValueChanged = null;
        }

        #region Value

        public event PropertyChangedEventHandler PropertyChanged;

        private void Slider_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (Parent is GridBase)
                ((GridBase)Parent).SetSubmission(SubmitKey, StringValue);
            this.RaiseEvent("ValueChanged", new ValueChangedEventArgs<double>(_oldValue, Value));
            _oldValue = Value;
            this.OnPropertyChanged("Value");
            this.OnPropertyChanged("Progress");
        }

        public double Value
        {
            get { return Progress + MinValue; }
            set { Progress = (int)(value - MinValue); }
        }

        public double MaxValue
        {
            get { return _maxValue; }
            set
            {
                if (_maxValue == value) return;
                _maxValue = value;
                Max = (int)(_maxValue - MinValue);
                this.OnPropertyChanged();
                this.OnPropertyChanged("Max");
            }
        }
        private double _maxValue = 100;

        public double MinValue
        {
            get { return _minValue; }
            set
            {
                if (_minValue == value) return;
                _minValue = value;
                Max = (int)(_maxValue - _minValue);
                Value = Progress + _minValue;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(Max));
            }
        }
        private double _minValue;

        public event ValueChangedEventHandler<double> ValueChanged;

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
                this.OnPropertyChanged("Enabled");
            }
        }

        public Color MaximumTrackColor
        {
            get { return _sliderDrawable.MaximumColor; }
            set
            {
                if (_sliderDrawable.MaximumColor == value || value.IsDefaultColor) return;
                _sliderDrawable.MaximumColor = value;
                ProgressDrawable = _sliderDrawable;
                this.OnPropertyChanged();
                this.OnPropertyChanged("ProgressDrawable");
            }
        }

        public Color MinimumTrackColor
        {
            get { return _sliderDrawable.MinimumColor; }
            set
            {
                if (_sliderDrawable.MinimumColor == value || value.IsDefaultColor) return;
                _sliderDrawable.MinimumColor = value;
                ProgressDrawable = _sliderDrawable;
                this.OnPropertyChanged();
                this.OnPropertyChanged("ProgressDrawable");
            }
        }

        #endregion

        #region Submission

        public string StringValue => Value.ToString(CultureInfo.InvariantCulture);

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
            var args = new ValidationEventArgs(SubmitKey, Value, StringValue);
            if (this.RaiseEvent("Validating", args) && args.Errors.Count > 0)
            {
                errors = new string[args.Errors.Count];
                args.Errors.CopyTo(errors, 0);
                return false;
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
        private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Stretch;

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

        #endregion

        private class TrackerDrawable : Drawable
        {
            private readonly Paint _paint;
            private readonly Rect _rect;
            private readonly SeekBar _parent;
            private readonly double _scale;

            public Color MinimumColor { get; set; }

            public Color MaximumColor { get; set; }

            public TrackerDrawable(SeekBar parent)
            {
                _parent = parent;
                _rect = new Rect();
                _paint = new Paint { StrokeWidth = 1, };
                _paint.SetStyle(Paint.Style.Fill);
                _scale = DroidFactory.DisplayScale;
                MinimumColor = new Color("33B5E5");
                MaximumColor = new Color("C2C2C2");
            }

            public override void Draw(Canvas canvas)
            {
                var width = _parent.Width - _parent.PaddingLeft - _parent.PaddingRight;
                var height = _parent.Height - _parent.PaddingTop - _parent.PaddingBottom;
                var percent = (int)((_parent.Progress / (double)_parent.Max) * width);
                var center = height / 2;
                var lineHeight = (int)(2 * _scale);

                #region Before slider

                _paint.SetARGB(MinimumColor.A, MinimumColor.R, MinimumColor.G, MinimumColor.B);

                _rect.Left = 0;
                _rect.Top = center - lineHeight;
                _rect.Right = percent;
                _rect.Bottom = center + lineHeight;

                canvas.DrawRect(_rect, _paint);

                #endregion

                #region After slider

                lineHeight = Math.Max((int)(_scale / 2), 1);
                _paint.SetARGB(MaximumColor.A, MaximumColor.R, MaximumColor.G, MaximumColor.B);

                _rect.Left = percent;
                _rect.Top = center - lineHeight;
                _rect.Right = width;
                _rect.Bottom = center + lineHeight;

                canvas.DrawRect(_rect, _paint);

                #endregion
            }

            public override void SetAlpha(int alpha)
            {
                _paint.Alpha = alpha;
            }

            public override void SetColorFilter(ColorFilter cf)
            {
                _paint.SetColorFilter(cf);
            }

            public override int Opacity => (int)Format.Opaque;
        }
    }
}