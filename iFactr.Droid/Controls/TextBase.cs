using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using iFactr.UI;
using iFactr.UI.Controls;
using MonoCross.Utilities;

namespace iFactr.Droid
{
    public abstract class TextBase : EditText, INotifyPropertyChanged, IElement
    {
        #region Constructors

        [Preserve]
        public TextBase() : base(DroidFactory.MainActivity) { Initialize(); }

        public TextBase(Context context) : base(context) { Initialize(); }

        public TextBase(Context context, Android.Util.IAttributeSet attrs) : base(context, attrs) { Initialize(); }

        public TextBase(Context context, Android.Util.IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle) { Initialize(); }

        public TextBase(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

        private void Initialize()
        {
            Blur(false);
            Click += (o, e) => Focus();
            Font = Font.PreferredTextBoxFont;
            Gravity = GravityFlags.Left;
        }

        #endregion

        #region Methods

        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();
            var view = Parent as GridBase;
            var historyEntry = view?.Parent as IHistoryEntry;
            if (historyEntry != null) historyEntry.Deactivated += View_Deactivated;
            if (this == CurrentFocus)
            {
                Focus();
            }
        }

        private void View_Deactivated(object sender, EventArgs e)
        {
            var entry = (IHistoryEntry)sender;
            entry.Deactivated -= View_Deactivated;
            if (CurrentFocus == this) CurrentFocus = null;
            Blur(true, false);
        }

        /// <summary>
        /// This method is called when the text is changed, in case any subclasses would like to know.
        /// Within text, the lengthAfter characters beginning at start have just replaced old text that
        /// had length lengthBefore. It is an error to attempt to make changes to text from this callback.
        /// </summary>
        /// <param name="text">The text the TextView is displaying</param>
        /// <param name="start">The offset of the start of the range of the text that was modified</param>
        /// <param name="before">The length of the former text that has been replaced</param>
        /// <param name="after">The length of the replacement modified text</param>
        protected override void OnTextChanged(Java.Lang.ICharSequence text, int start, int before, int after)
        {
            base.OnTextChanged(text, start, before, after);
            if (Text != _currentValue && !(_expression == null || _expression.IsMatch(Text)))
            {
                EditableText.Replace(0, Text.Length, _currentValue ?? string.Empty);
            }
            if (Text == _currentValue) return;
            var old = _currentValue;
            _currentValue = Text;

            this.OnPropertyChanged(nameof(Text));
            this.OnPropertyChanged(nameof(StringValue));

            (Parent as GridBase)?.SetSubmission(SubmitKey, StringValue);
            OnTextChanged(old, _currentValue);
        }
        private string _currentValue = string.Empty;

        public new virtual string Text
        {
            get { return base.Text; }
            set
            {
                if (value == _currentValue) return;
                if (_expression != null && (value == null || !_expression.IsMatch(value)))
                {
                    Device.Log.Info($"{ID}: {value} did not match pattern {_expression}");
                    return;
                }
                Device.Thread.ExecuteOnMainThread(() =>
                {
                    if (Handle == IntPtr.Zero) return;
                    base.Text = value;
                });
            }
        }

        public override bool IsFocused => _isFocused && CurrentFocus == this;
        private bool _isFocused;

        protected override void OnFocusChanged(bool gainFocus, FocusSearchDirection direction, Rect previouslyFocusedRect)
        {
            if (gainFocus && CurrentFocus != this)
            {
                Focus();
            }

            base.OnFocusChanged(gainFocus, direction, previouslyFocusedRect);
        }

        public override bool OnKeyPreIme(Keycode keyCode, KeyEvent e)
        {
            if (e.KeyCode == Keycode.Back && e.Action == KeyEventActions.Up)
            {
                Blur(true);
                if (PopoverFragment.Instance == null)
                {
                    CurrentFocus = null;
                }
            }
            return base.OnKeyPreIme(keyCode, e);
        }

        internal static TextBase CurrentFocus { get; set; }

        public override void OnEditorAction(ImeAction actionCode)
        {
            ReturnKeyPressed?.Invoke(Pair ?? this, new EventHandledEventArgs());
            if (KeyboardReturnType > KeyboardReturnType.Next)
            {
                Blur(true);
            }
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Enter)
            {
                EventHandler<EventHandledEventArgs> rkp;
                if ((rkp = ReturnKeyPressed) != null)
                {
                    var args = new EventHandledEventArgs();
                    rkp(Pair ?? this, args);
                    if (args.IsHandled) return true;
                }
                if (KeyboardReturnType > KeyboardReturnType.Next)
                {
                    Blur(true);
                }
            }

            try
            {
                return base.OnKeyDown(keyCode, e);
            }
            catch (Exception ex)
            {
                Device.Log.Error(ex);
                return false;
            }
        }

        public event EventHandler<EventHandledEventArgs> ReturnKeyPressed;
        public event EventHandler GotFocus;
        public event EventHandler LostFocus;

        public void Focus()
        {
            try
            {
                if (CurrentFocus != this)
                {
                    CurrentFocus?.Blur(false);
                    CurrentFocus = this;
                    if (!IsFocused)
                    {
                        _isFocused = true;
                        this.RaiseEvent(nameof(GotFocus), EventArgs.Empty);
                        this.OnPropertyChanged(nameof(IsFocused));
                    }
                }
                Device.Thread.ExecuteOnMainThread(() =>
                {
                    Focusable = true;
                    FocusableInTouchMode = true;
                    DroidFactory.ShowKeyboard(this);
                });
            }
            catch (ObjectDisposedException)
            {
                _isFocused = false;
                /*GULLPPPP!!*/
            }
        }

        public void Blur(bool closeKeyboard, bool fireLostFocus = true)
        {
            try
            {
                if (closeKeyboard)
                {
                    var imm = (InputMethodManager)Context.GetSystemService(Context.InputMethodService);
                    imm.HideSoftInputFromWindow(WindowToken, HideSoftInputFlags.None);
                }

                if (IsFocused)
                {
                    _isFocused = false;
                    if (fireLostFocus) this.RaiseEvent(nameof(LostFocus), EventArgs.Empty);
                    this.OnPropertyChanged(nameof(IsFocused));
                }

                Device.Thread.ExecuteOnMainThread(() =>
                {
                    // Set CurrentFocus before calling ClearFocus(), since
                    // gainFocus is passed as true to OnFocusChanged.
                    var focusRef = CurrentFocus;
                    CurrentFocus = this;
                    ClearFocus();
                    CurrentFocus = focusRef == this ? null : focusRef;
                    Focusable = false;
                    FocusableInTouchMode = false;
                });
            }
            catch (ObjectDisposedException)
            {
                _isFocused = false;
                /*GULLPPPP!!*/
            }
        }

        public virtual void NullifyEvents()
        {
            Validating = null;
            TextChanged = null;
            GotFocus = null;
            LostFocus = null;
            ReturnKeyPressed = null;
        }

        #endregion

        #region Value

        public string Placeholder
        {
            get { return Hint; }
            set
            {
                if (Hint == value) return;
                Hint = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(Hint));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public new event ValueChangedEventHandler<string> TextChanged;

        protected virtual void OnTextChanged(string oldValue, string newValue)
        {
            TextChanged?.Invoke(this, new ValueChangedEventArgs<string>(oldValue, newValue));
        }

        #endregion

        #region Style

        public string Expression
        {
            get { return _expression?.ToString(); }
            set
            {
                if (_expression == null && value == null || _expression != null && _expression.ToString() == value)
                    return;
                _expression = value == null ? null : new Regex(value);
                this.OnPropertyChanged();
            }
        }

        private Regex _expression;

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

        public UI.KeyboardType KeyboardType
        {
            get { return _keyboardType; }
            set
            {
                if (_keyboardType == value) return;

                if (_keyboardType == UI.KeyboardType.Email)
                    InputExtras &= ~InputTypes.TextVariationWebEmailAddress;

                _keyboardType = value;

                if (_keyboardType == UI.KeyboardType.Email)
                    InputExtras |= InputTypes.TextVariationWebEmailAddress;

                SetCompletion();
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(InputType));
            }
        }

        private UI.KeyboardType _keyboardType;

        public KeyboardReturnType KeyboardReturnType
        {
            get { return _keyboardReturnType; }
            set
            {
                if (_keyboardReturnType == value) return;
                _keyboardReturnType = value;
                switch (_keyboardReturnType)
                {
                    case KeyboardReturnType.Next:
                        SetImeActionLabel(Device.Resources.GetString("Next"), ImeAction.Next);
                        break;
                    case KeyboardReturnType.Done:
                        SetImeActionLabel(Device.Resources.GetString("Done"), ImeAction.Done);
                        break;
                    case KeyboardReturnType.Go:
                        SetImeActionLabel(Device.Resources.GetString("Go"), ImeAction.Go);
                        break;
                    case KeyboardReturnType.Search:
                        SetImeActionLabel(Device.Resources.GetString("SearchHint"), ImeAction.Search);
                        break;
                    default:
                        SetImeActionLabel((string)null, ImeAction.Unspecified);
                        break;
                }
                this.OnPropertyChanged();
            }
        }

        private KeyboardReturnType _keyboardReturnType;

        public new virtual UI.TextAlignment TextAlignment
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

        public TextCompletion TextCompletion
        {
            get { return _textCompletion; }
            set
            {
                if (_textCompletion == value) return;
                _textCompletion = value;
                SetCompletion();
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(InputType));
            }
        }
        private TextCompletion _textCompletion;

        protected virtual void SetCompletion()
        {
            var extras = InputExtras;
            if ((_textCompletion & TextCompletion.AutoCapitalize) == TextCompletion.AutoCapitalize)
            {
                extras |= InputTypes.TextFlagCapSentences;
            }

            if ((_textCompletion & TextCompletion.OfferSuggestions) != TextCompletion.OfferSuggestions)
            {
                extras |= InputTypes.TextFlagNoSuggestions;
            }

            InputType = InputTypes.ClassText | extras;

            if (_keyboardType == UI.KeyboardType.PIN)
            {
                var focused = Focusable;
                KeyListener = Android.Text.Method.DigitsKeyListener.GetInstance("0123456789+-., /*:");
                Focusable = focused;
            }
        }

        protected InputTypes InputExtras { get; set; }

        public UI.Color BackgroundColor
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

        private UI.Color _backgroundColor;

        public UI.Color ForegroundColor
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

        private UI.Color _foregroundColor;

        public UI.Color PlaceholderColor
        {
            get { return _placeholderColor; }
            set
            {
                if (_placeholderColor == value || Handle == IntPtr.Zero) return;
                _placeholderColor = value;
                if (!_placeholderColor.IsDefaultColor)
                {
                    SetHintTextColor(_placeholderColor.ToColor());
                }
                this.OnPropertyChanged();
            }
        }

        private UI.Color _placeholderColor;

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
                    SetTextSize(Android.Util.ComplexUnitType.Sp, (float)_font.Size);
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

        #endregion

        #region Layout

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

        /// <summary>
        /// Calculates and returns an appropriate width and height value for the contents of the control.
        /// This is called by the underlying grid layout system and should not be used in application logic.
        /// </summary>
        /// <param name="constraints">The size that the control is limited to.</param>
        /// <returns>The label size, given a width constraint and a measured height.</returns>
        public virtual Size Measure(Size constraints)
        {
            return this.MeasureView(constraints);
        }

        /// <summary>
        /// Sets the location and size of the control within its parent grid.
        /// This is called by the underlying grid layout system and should not be used in application logic.
        /// </summary>
        /// <param name="location">The X and Y coordinates of the upper left corner of the control.</param>
        /// <param name="size">The width and height of the control.</param>
        public virtual void SetLocation(UI.Point location, Size size)
        {
            var left = location.X;
            var right = location.X + size.Width;
            var top = location.Y;
            var bottom = top + size.Height;

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