using System;
using System.ComponentModel;
using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Views;
using Android.Widget;
using iFactr.UI;
using iFactr.UI.Controls;
using Color = iFactr.UI.Color;
using Math = System.Math;
using Point = iFactr.UI.Point;
using Android.Runtime;
using TextAlignment = iFactr.UI.TextAlignment;
using Android.Util;
using MonoCross.Utilities;
using Size = iFactr.UI.Size;
using iFactr.Core;

namespace iFactr.Droid
{
    /// <summary>
    /// A <see cref="TextView"/> implementation of <see cref="ILabel"/>. 
    /// </summary>
    public class Label : TextView, ILabel, INotifyPropertyChanged
    {
        // Our ellipse string
        private const string Ellipsis = "...";

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Label"/> class.
        /// </summary>
        [Preserve]
        public Label()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public Label(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public Label(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize(attrs);
        }

        [Preserve]
        public Label(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize(attrs);
        }

        public Label(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        private void Initialize(IAttributeSet attrs = null)
        {
            Font = Font.PreferredLabelFont;
            ForegroundColor = iApp.Instance.Style.TextColor;
            Gravity = GravityFlags.Left;

            this.InitializeAttributes(attrs);
            TextChanged += Label_TextChanged;
        }

        #endregion

        /// <summary>
        /// Resets the invocation list of all events within the class.
        /// </summary>
        public void NullifyEvents()
        {
            Validating = null;
            ValueChanged = null;
        }

        #region Value

        public event PropertyChangedEventHandler PropertyChanged;

        public new virtual string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                Device.Thread.ExecuteOnMainThread(() =>
                {
                    if (Handle != IntPtr.Zero)
                        base.Text = _text;
                });
            }
        }
        private string _text = string.Empty;

        /// <summary>
        /// Handles the TextChanged event of the Label control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TextChangedEventArgs"/> instance containing the event data.</param>
        private void Label_TextChanged(object sender, TextChangedEventArgs e)
        {
            var old = StringValue;
            var ellipsized = (Text ?? string.Empty).EndsWith(Ellipsis);
            var infinitySize = new Size(double.PositiveInfinity, double.PositiveInfinity);

            if (!ellipsized && StringValue != Text)
            {
                _constraints = infinitySize;
                StringValue = Text;
                (Parent as GridBase)?.SetSubmission(SubmitKey, StringValue);
            }

            this.RequestResize();

            if (old == StringValue) return;
            this.RaiseEvent("ValueChanged", new ValueChangedEventArgs<string>(old, StringValue));
            this.OnPropertyChanged("Text");
            this.OnPropertyChanged("StringValue");
        }

        /// <summary>
        /// Occurs when the label's text value has changed.
        /// </summary>
        public event ValueChangedEventHandler<string> ValueChanged;

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

        public new TextAlignment TextAlignment
        {
            get { return Gravity.ToTextAlignment(); }
            set
            {
                if (TextAlignment == value) return;
                Gravity = value.ToTextAlignment();
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(Gravity));
            }
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

        /// <summary>
        /// Gets or sets the color of the text.
        /// </summary>
        /// <value>The color of the foreground.</value>
        public Color ForegroundColor
        {
            get { return _foregroundColor.IsDefaultColor ? Color.Black : _foregroundColor; }
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
        /// Gets or sets the color of the text when the label is in a cell that is being highlighted.
        /// </summary>
        /// <value>The color of the foreground.</value>
        public new Color HighlightColor
        {
            get { return _highlightColor.IsDefaultColor ? Color.White : _highlightColor; }
            set
            {
                if (_highlightColor == value || Handle == IntPtr.Zero) return;
                SetHighlightColor(value.IsDefaultColor ? Android.Graphics.Color.White : value.ToColor());
                _highlightColor = value;
                this.OnPropertyChanged();
            }
        }
        private Color _highlightColor;

        public bool HasHighlight
        {
            get { return _hasHighlight; }
            set
            {
                if (_hasHighlight == value) return;
                _hasHighlight = value;
                SetTextColor(_hasHighlight ? HighlightColor.ToColor() : ForegroundColor.ToColor());
            }
        }
        private bool _hasHighlight;

        /// <summary>
        /// Gets or sets the maximum number of lines of text that the label is allowed to display.
        /// A value equal to or less than 0 means that there is no limit.
        /// </summary>
        /// <value>The maximum number of lines.</value>
        public int Lines
        {
            get { return _maxLineCount; }
            set
            {
                if (_maxLineCount == value) return;
                this.RequestResize(value < _measuredLines);
                _maxLineCount = value;
                this.OnPropertyChanged();
            }
        }
        private int _maxLineCount;
        private int _measuredLines = 1;

        #endregion

        #region Submission

        /// <summary>
        /// Gets the string representation of the control's current value.
        /// </summary>
        /// <value>The string value.</value>
        public string StringValue { get; private set; }

        /// <summary>
        /// Gets or sets the identifier to use when submitting control values.
        /// If an identifier is not set, the control will not be submitted.
        /// </summary>
        /// <value>The submit identifier.</value>
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

        /// <summary>
        /// Fires the <see cref="Validating" /> event and returns a value indicating whether or not validation has passed.
        /// </summary>
        /// <param name="errors">When the method returns, an array of validation errors that have occurred.</param>
        /// <returns><c>true</c> if validation has passed; otherwise, <c>false</c>.</returns>
        public bool Validate(out string[] errors)
        {
            var handler = Validating;
            if (handler != null)
            {
                var args = new ValidationEventArgs(SubmitKey, Text, StringValue);
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

        /// <summary>
        /// Occurs when the control is being validated.
        /// </summary>
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
        private VerticalAlignment _verticalAlignment = VerticalAlignment.Stretch;

        /// <summary>
        /// Calculates and returns an appropriate width and height value for the contents of the control.
        /// This is called by the underlying grid layout system and should not be used in application logic.
        /// </summary>
        /// <param name="constraints">The size that the control is limited to.</param>
        /// <returns>The label size, given a width constraint and a measured height.</returns>
        public Size Measure(Size constraints)
        {
            if (string.IsNullOrEmpty(StringValue))
                return new Size();

            if (_constraints == constraints)
                return new Size(MeasuredWidth, MeasuredHeight);

            _constraints = constraints;
            var height = _constraints.Height > int.MaxValue ? int.MaxValue : (int)Math.Ceiling(_constraints.Height);
            var width = _constraints.Width > int.MaxValue ? int.MaxValue : (int)Math.Ceiling(_constraints.Width);
            var measuredWidth = 0;

            // Do not resize if the view does not have dimensions or there is no text
            if (string.IsNullOrEmpty(StringValue) || height <= 0 || width <= 0 || TextSize <= 0)
            {
                if (Text != string.Empty)
                    SetText(string.Empty, BufferType.Normal);
                _measuredLines = 0;
            }
            else
            {
                // modified: make a copy of the original TextPaint object for measuring
                // (apparently the object gets modified while measuring, see also the
                // docs for TextView.getPaint() (which states to access it read-only)
                var paint = new TextPaint(Paint) { TextSize = TextSize, };
                // Measure using a static layout
                var layout = new StaticLayout(StringValue, paint, width, Layout.Alignment.AlignNormal, 1.0f, 0, true);

                float totalWidth = 0;

                // If we had reached our minimum text size and still don't fit, append an ellipsis
                if (layout.Height > height || _maxLineCount > 0 && layout.LineCount > _maxLineCount)
                {
                    var ellipsisWidth = Paint.MeasureText(Ellipsis);
                    var addEllipsis = false;
                    int lastLineIndex;

                    if (height == int.MaxValue)
                    {
                        lastLineIndex = Math.Max(layout.LineCount - 1, 0);
                    }
                    else
                    {
                        lastLineIndex = Math.Max(layout.GetLineForVertical(height) - 1, 0);
                        if (lastLineIndex < layout.LineCount - 1)
                        {
                            addEllipsis = true;
                        }
                    }

                    if (_maxLineCount > 0 && lastLineIndex > _maxLineCount - 1)
                    {
                        lastLineIndex = _maxLineCount - 1;
                        addEllipsis = true;
                    }

                    _measuredLines = lastLineIndex + 1;
                    var ellipsizeIndex = layout.GetLineEnd(lastLineIndex);
                    var lastLineStartIndex = layout.GetLineStart(lastLineIndex);
                    ellipsizeIndex = lastLineStartIndex + (int)Math.Ceiling((ellipsizeIndex - lastLineStartIndex) * 1.4);
                    if (StringValue.Length < ellipsizeIndex)
                    {
                        ellipsizeIndex = StringValue.Length;
                        addEllipsis = false;
                    }
                    var text = StringValue.Substring(lastLineStartIndex, ellipsizeIndex - lastLineStartIndex).TrimEnd();
                    totalWidth = Paint.MeasureText(text) + (addEllipsis ? ellipsisWidth : 0);

                    // Trim characters off until we have enough room to draw the ellipsis
                    while (width < totalWidth && text != string.Empty)
                    {
                        addEllipsis = true;
                        text = StringValue.Substring(lastLineStartIndex, --ellipsizeIndex - lastLineStartIndex);
                        totalWidth = Paint.MeasureText(text) + ellipsisWidth;
                    }

                    if (addEllipsis)
                    {
                        text = StringValue.Substring(0, ellipsizeIndex).TrimEnd() + Ellipsis;
                        if (text != Text)
                        {
                            SetText(text, BufferType.Normal);
                        }
                    }
                }
                else if (Text != StringValue)
                {
                    SetText(StringValue, BufferType.Normal);
                    _measuredLines = layout.LineCount;
                }
                else
                {
                    _measuredLines = layout.LineCount;
                }

                // Some devices try to auto adjust line spacing, so force default line spacing
                // and invalidate the layout as a side effect
                SetLineSpacing(0, 1.0f);

                for (var i = 0; i < _measuredLines; i++)
                {
                    var lineWidth = layout.GetLineWidth(i);
                    if (lineWidth > totalWidth)
                    {
                        totalWidth = lineWidth;
                    }
                }
                measuredWidth = (int)Math.Ceiling(totalWidth);
            }

            var size = this.MeasureView(constraints);
            return new Size(measuredWidth, size.Height);
        }
        private Size _constraints;

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

        /// <summary>
        /// Gets the abstract or native object that is paired with this instance.
        /// This property is used internally by the framework and user-defined controls, and it should not be used in application logic.
        /// </summary>
        /// <value>The paired abstract or native object.</value>
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

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><c>true</c> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <c>false</c>.</returns>
        public bool Equals(IElement other)
        {
            var control = other as Element;
            return control?.Equals(this) ?? ReferenceEquals(this, other);
        }

        #endregion
    }
}