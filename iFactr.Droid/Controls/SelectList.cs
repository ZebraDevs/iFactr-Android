using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using iFactr.Core.Forms;
using iFactr.UI;
using iFactr.UI.Controls;
using System.Collections.Generic;
using Android.Util;
using Color = iFactr.UI.Color;
using Point = iFactr.UI.Point;
using Size = iFactr.UI.Size;
using View = Android.Views.View;

namespace iFactr.Droid
{
    public class SelectList : Spinner, ISelectList, AdapterView.IOnItemSelectedListener, INotifyPropertyChanged
    {
        #region Constructors

        [Preserve]
        public SelectList()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public SelectList(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public SelectList(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize(attrs);
        }

        [Preserve]
        public SelectList(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize(attrs);
        }

        protected SelectList(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        private void Initialize(IAttributeSet attrs = null)
        {
            OnItemSelectedListener = this;
            Adapter = new SelectListAdapter(Context, Android.Resource.Layout.SimpleSpinnerItem)
            {
                Parent = this,
                DropDownViewResource = Android.Resource.Layout.SimpleSpinnerDropDownItem,
            };

            this.InitializeAttributes(attrs);
            Touch += SelectList_Touch;
        }

        #endregion

        #region Methods

        private void SelectList_Touch(object sender, TouchEventArgs e)
        {
            e.Handled = false;
            if (e.Event.Action != MotionEventActions.Up || _focused) return;
            TextBase.CurrentFocus?.Blur(true);
            _focused = true;
            GotFocus?.Invoke(this, EventArgs.Empty);
        }
        private bool _focused;

        public override void OnWindowFocusChanged(bool hasWindowFocus)
        {
            base.OnWindowFocusChanged(hasWindowFocus);
            if (!_focused || !hasWindowFocus) return;
            _focused = false;
            LostFocus?.Invoke(this, EventArgs.Empty);
        }

        public void ShowList()
        {
            PerformClick();
        }

        public void NullifyEvents()
        {
            Validating = null;
            SelectionChanged = null;
        }

        public override void Invalidate()
        {
            base.Invalidate();
            this.RequestResize();
        }

        #endregion

        #region Value

        public event PropertyChangedEventHandler PropertyChanged;

        public IEnumerable Items
        {
            get { return _items; }
            set
            {
                var val = value.Cast<object>().ToList();
                if (val.Count == _items.Count && _items.Equivalent(val, true))
                {
                    return;
                }

                var selection = SelectedItem;
                _items.Clear();

                foreach (var item in value)
                {
                    _items.Add(item);
                }

                Adapter.Clear();
                Adapter.AddAll(_items);

                SelectedItem = selection;
                Invalidate();
                this.OnPropertyChanged();
            }
        }
        private readonly List<object> _items = new List<object>();

        private new SelectListAdapter Adapter
        {
            get { return base.Adapter as SelectListAdapter; }
            set { base.Adapter = value; }
        }

        public int SelectedIndex
        {
            get { return _items.Any() ? Math.Max(SelectedItemPosition, 0) : -1; }
            set
            {
                if (value == SelectedIndex) return;
                if (value < 0) value = 0;
                _oldItem = SelectedItem;
                SetSelection(value, true);
            }
        }

        public new object SelectedItem
        {
            get { return SelectedIndex > -1 && SelectedIndex < Items.Count() ? Items.ElementAt(SelectedIndex) : null; }
            set
            {
                var index = Items.Count() > 0 ? Items.IndexOf(value) : SelectedIndex;
                if (index == SelectedIndex) return;
                if (index == -1) index = 0;
                _oldItem = SelectedItem;
                SetSelection(index, true);
            }
        }
        private object _oldItem;

        public override void SetSelection(int position)
        {
            SetSelection(position, true);
        }

        public override void SetSelection(int position, bool animate)
        {
            if (position == SelectedIndex && Tag == null)
                Tag = position;
            base.SetSelection(position, animate);
        }

        public void OnItemSelected(AdapterView parent, View view, int position, long id)
        {
            if (Tag != null && (int)Tag != position)
            {
                var handler = SelectionChanged;
                if (handler != null && _oldItem != SelectedItem)
                {
                    handler(Pair ?? this, new ValueChangedEventArgs<object>(_oldItem, SelectedItem));
                    _oldItem = SelectedItem;
                }
                this.OnPropertyChanged("SelectedIndex");
                this.OnPropertyChanged("StringValue");
                this.OnPropertyChanged("SelectedItem");
            }
            else
            {
                _oldItem = null;
            }
            Tag = position;
        }
        public event ValueChangedEventHandler<object> SelectionChanged;

        public void OnNothingSelected(AdapterView parent) { }

        #endregion

        #region Submission

        public string StringValue => SelectedItem?.ToString();

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
                var args = new ValidationEventArgs(SubmitKey, SelectedItem, StringValue);
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

        public event EventHandler GotFocus;

        public event EventHandler LostFocus;

        public event ValidationEventHandler Validating;

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

        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                if (_backgroundColor == value) return;
                _backgroundColor = value;
                Invalidate();
                Adapter.NotifyDataSetChanged();
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
                Invalidate();
                Adapter.NotifyDataSetChanged();
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
                Invalidate();
                Adapter.NotifyDataSetChanged();
                this.OnPropertyChanged();
            }
        }
        private Font _font = Font.PreferredSelectListFont;

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

        Size IElement.Measure(Size constraints)
        {
            return this.MeasureView(constraints);
        }

        void IElement.SetLocation(Point location, Size size)
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

        private void SetTextStyle(TextView view)
        {
            if (view.Handle == IntPtr.Zero) return;
            if (Font.Size > 0)
            {
                view.SetTextSize(ComplexUnitType.Sp, (float)Font.Size);
            }

            if (!string.IsNullOrEmpty(Font.Name))
            {
                var format = (Android.Graphics.TypefaceStyle)Font.Formatting;
                view.SetTypeface(Android.Graphics.Typeface.Create(Font.Name, format), format);
            }

            if (!ForegroundColor.IsDefaultColor)
            {
                view.SetTextColor(ForegroundColor.ToColor());
            }

            if (!BackgroundColor.IsDefaultColor)
            {
                view.SetBackgroundColor(BackgroundColor.ToColor());
            }
        }

        private class SelectListAdapter : ArrayAdapter
        {
            #region Constructors

            protected SelectListAdapter(IntPtr javaReference, JniHandleOwnership transfer)
                : base(javaReference, transfer)
            {
            }

            public SelectListAdapter(Context context, int textViewResourceId)
                : base(context, textViewResourceId)
            {
            }

            public SelectListAdapter(Context context, int resource, int textViewResourceId)
                : base(context, resource, textViewResourceId)
            {
            }

            #endregion

            public SelectList Parent { get; set; }

            public int DropDownViewResource
            {
                get { return _dropDownViewResource; }
                set { SetDropDownViewResource(_dropDownViewResource = value); }
            }
            private int _dropDownViewResource;

            public override View GetView(int position, View convertView, ViewGroup parent)
            {
                var view = (TextView)base.GetView(position, convertView, parent);
                Parent.SetTextStyle(view);
                return view;
            }

            public override View GetDropDownView(int position, View convertView, ViewGroup parent)
            {
                var view = (TextView)base.GetDropDownView(position, convertView, parent);
                Parent.SetTextStyle(view);
                return view;
            }
        }
    }
}