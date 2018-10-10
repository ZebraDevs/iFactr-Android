using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using iFactr.Core;
using iFactr.UI;
using iFactr.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Size = iFactr.UI.Size;

namespace iFactr.Droid
{
    public class GridFragment : BaseFragment, IGridView
    {
        private ScrollView _verticalContainer;
        private HorizontalScrollView _horizontalContainer;
        private GridBase _grid;

        public override Android.Views.View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            _verticalContainer = new ContainerScrollView(Activity);
            _horizontalContainer = new ContainerHorizontalScrollView(Activity) { FillViewport = true, };

            _grid = new GridBase(Activity) { Parent = this, };
            _verticalContainer.AddView(_horizontalContainer, new ScrollView.LayoutParams(ScrollView.LayoutParams.MatchParent, ScrollView.LayoutParams.MatchParent));
            _horizontalContainer.AddView(_grid, new HorizontalScrollView.LayoutParams(HorizontalScrollView.LayoutParams.MatchParent, HorizontalScrollView.LayoutParams.MatchParent));

            if (_padding != new Thickness())
                _grid.Padding = _padding;

            _grid.Columns.AddRange(_columns);
            _grid.Rows.AddRange(_rows);
            foreach (var control in _controls)
            {
                _grid.AddChild(control);
            }

            HorizontalScrollingEnabled = _horizontallyScrolling;
            VerticalScrollingEnabled = _verticallyScrolling;

            return _verticalContainer;
        }

        public override void OnResume()
        {
            base.OnResume();
            View?.RequestFocus();
        }

        public override void OnPause()
        {
            base.OnPause();
            _columns = _grid.Columns;
            _rows = _grid.Rows;
            _controls = new List<IElement>(_grid.Children);
            _padding = _grid.Padding;
        }

        public ColumnCollection Columns => _grid == null ? _columns : _grid.Columns;
        private ColumnCollection _columns = new ColumnCollection();

        public RowCollection Rows => _grid == null ? _rows : _grid.Rows;
        private RowCollection _rows = new RowCollection();

        public Thickness Padding
        {
            get
            {
                return _grid?.Padding ?? _padding;
            }
            set
            {
                if (_grid == null)
                    _padding = value;
                else
                {
                    _grid.Padding = value;
                    _grid.SetPadding((int)_padding.Left, (int)_padding.Top, (int)_padding.Right, (int)_padding.Bottom);
                }
                this.OnPropertyChanged();
            }
        }
        private Thickness _padding;

        public IEnumerable<IElement> Children => _grid == null ? _controls : _grid.Children;

        public void AddChild(IElement control)
        {
            if (_grid == null)
            {
                _controls.Add(control);
            }
            else
            {
                _grid.AddChild(control);
            }
            this.OnPropertyChanged(nameof(Children));
        }

        public void RemoveChild(IElement control)
        {
            if (_grid == null)
            {
                _controls.Remove(control);
            }
            else
            {
                _grid.RemoveChild(control);
            }
            this.OnPropertyChanged(nameof(Children));
        }

        public bool HorizontalScrollingEnabled
        {
            get { return _horizontallyScrolling; }
            set
            {
                if (_horizontalContainer != null)
                {
                    _horizontalContainer.HorizontalScrollBarEnabled = value;
                    _grid.LayoutParameters.Width = value ? ScrollView.LayoutParams.WrapContent : ScrollView.LayoutParams.MatchParent;
                    if (value)
                    {
                        _grid.MinWidth = -1;
                        _grid.MaxWidth = double.PositiveInfinity;
                    }
                    else
                    {
                        _grid.MinWidth = _grid.MaxWidth = -1;
                    }
                }
                if (value == _horizontallyScrolling) return;
                _horizontallyScrolling = value;
                this.OnPropertyChanged();
            }
        }
        private bool _horizontallyScrolling;

        public bool VerticalScrollingEnabled
        {
            get { return _verticallyScrolling; }
            set
            {
                if (_verticalContainer != null)
                {
                    _verticalContainer.VerticalScrollBarEnabled = value;
                    _grid.LayoutParameters.Height = value ? ScrollView.LayoutParams.WrapContent : ScrollView.LayoutParams.MatchParent;
                    if (value)
                    {
                        _grid.MinHeight = -1;
                        _grid.MaxHeight = double.PositiveInfinity;
                    }
                    else
                    {
                        _grid.MinHeight = _grid.MaxHeight = -1;
                    }
                }
                if (value == _verticallyScrolling) return;
                _verticallyScrolling = value;
                this.OnPropertyChanged();
            }
        }
        private bool _verticallyScrolling;

        private List<IElement> _controls = new List<IElement>();

        /// <summary>
        /// Renders this instance.
        /// </summary>
        public override void Render()
        {
            base.Render();

            if (_grid != null)
            {
                VerticalScrollingEnabled = _verticallyScrolling;
                HorizontalScrollingEnabled = _horizontallyScrolling;
                _grid.Invalidate();
            }

            _verticalContainer?.ScrollTo(_x, _y);
            _horizontalContainer?.ScrollTo(_x, _y);
            System.GC.Collect(0);
        }

        protected override bool Validate()
        {
            var retval = true;
            foreach (var child in Children.OfType<IControl>())
            {
                string[] errors;
                if (!child.Validate(out errors))
                {
                    foreach (var error in errors)
                    {
                        iApp.Log.Error(error);
                    }
                    retval = false;
                }
            }
            return retval;
        }

        public override IDictionary<string, string> GetSubmissionValues()
        {
            SetSubmitValues(this);
            return base.GetSubmissionValues();
        }

        protected override void Invalidate()
        {
            this.PerformLayout(new Size(), new Size(Width, double.PositiveInfinity));
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            // save index and top position
            if (_verticalContainer == null)
            {
                outState.PutInt(nameof(_x), _x);
                outState.PutInt(nameof(_y), _y);
            }
            else
            {
                outState.PutInt(nameof(_x), _horizontalContainer.ScrollX);
                outState.PutInt(nameof(_y), _verticalContainer.ScrollY);
            }

            base.OnSaveInstanceState(outState);
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // restore index and position
            if (savedInstanceState != null)
            {
                _x = savedInstanceState.GetInt(nameof(_x), -1);
                _y = savedInstanceState.GetInt(nameof(_y), -1);
            }
            else if (_verticalContainer != null)
            {
                _x = _horizontalContainer.ScrollX;
                _y = _verticalContainer.ScrollY;
            }
        }
        private int _x;
        private int _y;

        private class ContainerScrollView : ScrollView
        {
            #region Constructors

            public ContainerScrollView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

            public ContainerScrollView(Context context) : base(context) { }

            public ContainerScrollView(Context context, IAttributeSet attrs) : base(context, attrs) { }

            public ContainerScrollView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) { }

            public ContainerScrollView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes) { }

            #endregion

            public override bool OnTouchEvent(MotionEvent e)
            {
                return VerticalScrollBarEnabled && base.OnTouchEvent(e);
            }

            public override bool OnInterceptTouchEvent(MotionEvent ev)
            {
                return VerticalScrollBarEnabled && base.OnInterceptTouchEvent(ev);
            }
        }

        private class ContainerHorizontalScrollView : HorizontalScrollView
        {
            #region Constructors

            public ContainerHorizontalScrollView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

            public ContainerHorizontalScrollView(Context context) : base(context) { }

            public ContainerHorizontalScrollView(Context context, IAttributeSet attrs) : base(context, attrs) { }

            public ContainerHorizontalScrollView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) { }

            public ContainerHorizontalScrollView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes) { }

            #endregion

            public override bool OnTouchEvent(MotionEvent e)
            {
                return HorizontalScrollBarEnabled && base.OnTouchEvent(e);
            }

            public override bool OnInterceptTouchEvent(MotionEvent ev)
            {
                return HorizontalScrollBarEnabled && base.OnInterceptTouchEvent(ev);
            }
        }
    }
}