using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using MonoCross.Utilities;
using iFactr.UI;
using iFactr.UI.Controls;
using Size = iFactr.UI.Size;

namespace iFactr.Droid
{
    public class Grid : GridBase, IElement
    {
        #region Constructors

        [Preserve]
        public Grid()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public Grid(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public Grid(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize(attrs);
        }

        protected Grid(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        private void Initialize(IAttributeSet attrs = null)
        {
            this.InitializeAttributes(attrs);
            Click += OnClick;
        }

        #endregion

        #region Click

        private void OnClick(object o, EventArgs e)
        {
            if (this.RaiseEvent(nameof(Clicked), EventArgs.Empty)) return;
            DroidFactory.Navigate(NavigationLink, Parent);
        }

        public event EventHandler Clicked;

        public virtual void NullifyEvents()
        {
            Clicked = null;
        }

        public Link NavigationLink
        {
            get { return _navigationLink; }
            set
            {
                if (value == _navigationLink) return;
                _navigationLink = value;
                OnPropertyChanged();
            }
        }
        private Link _navigationLink;

        #endregion

        #region Layout

        public Color BackgroundColor
        {
            get { return _backgroundColor.IsDefaultColor ? Color.Transparent : _backgroundColor; }
            set
            {
                if (_backgroundColor == value || Handle == IntPtr.Zero) return;
                SetBackgroundColor(value.IsDefaultColor ? Android.Graphics.Color.Transparent : value.ToColor());
                _backgroundColor = value;
                OnPropertyChanged();
            }
        }

        private Color _backgroundColor;

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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
            return this.PerformLayout(new Size(MeasuredWidth, MeasuredHeight / DroidFactory.DisplayScale), constraints);
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
                OnPropertyChanged();
            }
        }
        private string _id;

        object IElement.Parent => ((Android.Views.View)this).Parent;

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