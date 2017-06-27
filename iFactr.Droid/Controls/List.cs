using System;
using System.Collections.Generic;
using Android.Animation;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using iFactr.Core.Utilities;
using iFactr.UI;
using iFactr.UI.Controls;
using Size = iFactr.UI.Size;
using View = Android.Views.View;

namespace iFactr.Droid
{
    public class List : ListView, IElement, AdapterView.IOnItemSelectedListener, AdapterView.IOnItemClickListener, AdapterView.IOnItemLongClickListener
    {
        #region Constructors

        public List(ListViewStyle style)
            : this()
        {
            Style = style;
        }


        [Preserve]
        public List()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public List(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public List(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize(attrs);
        }

        [Preserve]
        public List(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize(attrs);
        }

        protected List(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            Initialize();
        }

        private void Initialize(IAttributeSet attrs = null)
        {
            this.InitializeAttributes(attrs);

            SelectedIndex = -1;
            OnItemSelectedListener = this;
            OnItemClickListener = this;
            OnItemLongClickListener = this;
            DescendantFocusability = DescendantFocusability.BeforeDescendants;
            ScrollStateChanged += OnScrollStateChanged;
            SeparatorColor = _separatorColor;
        }

        #endregion

        private void OnScrollStateChanged(object o, AbsListView.ScrollStateChangedEventArgs e)
        {
            DescendantFocusability = DescendantFocusability.BeforeDescendants;
            DroidFactory.HideKeyboard();
            var fragment = Parent as BaseFragment;
            if (fragment != null) fragment.RequestFocusHomeUp = false;
        }

        public ColumnMode ColumnMode
        {
            get { return _columnMode; }
            set
            {
                if (_columnMode == value) return;
                _columnMode = value;
                this.OnPropertyChanged();
            }
        }
        private ColumnMode _columnMode;

        public Color SeparatorColor
        {
            get { return _separatorColor; }
            set
            {
                var oldValue = _separatorColor;
                _separatorColor = value;
                if (!_separatorColor.IsDefaultColor)
                {
                    Divider = _separatorColor.ToColorDrawable();
                    DividerHeight = 1;
                }
                if (_separatorColor != oldValue)
                    this.OnPropertyChanged();
            }
        }
        private Color _separatorColor;

        public ListViewStyle Style { get; }

        public SectionCollection Sections { get; } = new SectionCollection();

        public CellDelegate CellRequested
        {
            get { return _cellRequested; }
            set
            {
                if (_cellRequested == value) return;
                _cellRequested = value;
                this.OnPropertyChanged();
            }
        }
        private CellDelegate _cellRequested;

        public ItemIdDelegate ItemIdRequested
        {
            get { return _itemIdRequested; }
            set
            {
                if (_itemIdRequested == value) return;
                _itemIdRequested = value;
                this.OnPropertyChanged();
            }
        }
        private ItemIdDelegate _itemIdRequested;

        public void ReloadSections()
        {
            (Adapter as BaseAdapter)?.NotifyDataSetChanged();
        }

        public IEnumerable<ICell> GetVisibleCells()
        {
            var first = FirstVisiblePosition;
            var last = LastVisiblePosition;
            for (var i = first; i <= last; i++)
            {
                var view = GetChildAt(i - first);
                var cell = view as ICell;
                if (cell != null) yield return cell.Pair as ICell ?? cell;
                else if (view != null) yield return new CustomItemContainer(view);
            }
        }

        internal static int GetPosition(SectionCollection sections, int section, int index)
        {
            var position = 0;
            for (var i = 0; i <= section; i++)
            {
                var s = sections[i];
                if (!string.IsNullOrEmpty(s.Header?.Text))
                    position++;
                if (i == section) break;
                position += s.ItemCount;
                if (!string.IsNullOrEmpty(s.Footer?.Text))
                    position++;
            }
            return position + index;
        }

        public void ScrollToCell(int section, int index, bool animated)
        {
            var position = GetPosition(Sections, section, index);

            if (animated)
            {
                SmoothScrollToPosition(position);
            }
            else
            {
                SetSelection(position);
            }
        }

        public void ScrollToEnd(bool animated)
        {
            var objectAnimator = ObjectAnimator.OfFloat(this, "scrollY", 0, Height).SetDuration(animated ? 250 : 0);
            objectAnimator.Start();
        }

        public void ScrollToHome(bool animated)
        {
            var objectAnimator = ObjectAnimator.OfInt(this, "scrollY", 0, 0).SetDuration(animated ? 250 : 0);
            objectAnimator.Start();
        }

        public void OnItemSelected(AdapterView parent, View view, int position, long id)
        {
            var oldValue = SelectedIndex;
            SelectedIndex = position;
            SelectionChanged?.Invoke(Pair ?? this, new ValueChangedEventArgs<object>(oldValue, position));
        }

        public void OnNothingSelected(AdapterView parent)
        {
            DescendantFocusability = DescendantFocusability.BeforeDescendants;
        }

        public void OnItemClick(AdapterView parent, View view, int position, long id)
        {
            var cell = view as IGridCell;
            DescendantFocusability = Events.HasEvent(cell, nameof(Selected)) ? DescendantFocusability.AfterDescendants : DescendantFocusability.BeforeDescendants;
            cell?.Select();
        }

        public bool OnItemLongClick(AdapterView parent, View view, int position, long id)
        {
            var args = new EventHandledEventArgs();
            var cell = view as ICell;
            LongClicked?.Invoke(cell?.Pair ?? cell, args);
            return args.IsHandled;
        }

        public event ValueChangedEventHandler<object> SelectionChanged;
        public event EventHandler<EventHandledEventArgs> LongClicked;

        public int SelectedIndex { get; internal set; }

        #region Layout

        public new Visibility Visibility
        {
            get { return _visibility; }
            set
            {
                if (value == _visibility) return;
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

        /// <summary>
        /// Calculates and returns an appropriate width and height value for the contents of the control.
        /// This is called by the underlying grid layout system and should not be used in application logic.
        /// </summary>
        /// <param name="constraints">The size that the control is limited to.</param>
        /// <returns>The label size, given a width constraint and a measured height.</returns>
        public Size Measure(Size constraints)
        {
            return this.MeasureView(constraints, true);
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

        public new IView Parent
        {
            get { return _parent; }
            set
            {
                if (value is IGridView || value is IListView)
                    _parent = value;
            }
        }
        private IView _parent;

        internal void SetSubmission(string id, string value)
        {
            if (id == null || Parent == null) return;
            var values = GetSubmissions();
            if (values != null) values[id] = value;
        }

        internal IDictionary<string, string> GetSubmissions()
        {
            return (Parent as IListView)?.GetSubmissionValues() ??
                   (Parent as IGridView)?.GetSubmissionValues();
        }

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