using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using iFactr.UI;
using iFactr.UI.Controls;
using MonoCross.Navigation;
using MonoCross.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using View = Android.Views.View;

namespace iFactr.Droid
{
    public class ListViewFragment : BaseFragment, IListView, AdapterView.IOnItemSelectedListener, AdapterView.IOnItemClickListener, AdapterView.IOnItemLongClickListener, AbsListView.IRecyclerListener
    {
        public ListView List { get; private set; }

        #region Constructors

        // Default constructor required for restoring view from Java
        public ListViewFragment()
            : this(ListViewStyle.Default)
        { }

        public ListViewFragment(ListViewStyle style)
        {
            Sections = new SectionCollection();
            Style = style;
        }

        #endregion

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // restore index and position
            if (savedInstanceState != null)
            {
                _index = savedInstanceState.GetInt(nameof(_index), -1);
                _top = savedInstanceState.GetInt(nameof(_top), -1);
                SelectedIndex = savedInstanceState.GetInt(nameof(SelectedIndex), -1);
            }
        }
        private int _top;
        private int _index;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var layout = new LinearLayout(Activity) { Orientation = Orientation.Vertical, };
            var search = DroidFactory.GetNativeObject<SearchBox>(SearchBox, nameof(SearchBox));
            AttachSearchView(search, layout);

            List = MXContainer.Resolve<ListView>(Activity) ?? new ListView(Activity);
            List.OnItemSelectedListener = this;
            List.OnItemClickListener = this;
            List.OnItemLongClickListener = this;
            List.DescendantFocusability = DescendantFocusability.BeforeDescendants;
            List.ScrollingCacheEnabled = false;
            List.Adapter = new CellAdapter(this);
            List.ScrollStateChanged += (o, e) =>
            {
                if (e.ScrollState == ScrollState.Idle)
                {
                    var cell = List.GetChildAt(0);
                    _index = List.FirstVisiblePosition;
                    _top = cell?.Top ?? 0;
                }
                List.DescendantFocusability = DescendantFocusability.BeforeDescendants;
                if (_touchScroll) DroidFactory.HideKeyboard(true);
                if (e.ScrollState == ScrollState.Idle) _touchScroll = true;
                RequestFocusHomeUp = false;
            };
            List.SetRecyclerListener(this);
            SeparatorColor = _separatorColor;
            layout.AddView(List, new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.MatchParent));
            if (search == null || search.FocusRequested)
            {
                layout.RequestFocus();
            }
            else
            {
                List.RequestFocus();
            }

            return layout;
        }

        public override void OnResume()
        {
            base.OnResume();
            if (Sections.Any() && (_index > 0 || _top < -1))
            {
                int sectionIndex, cellIndex;
                CellAdapter.GetSectionAndIndex(Sections, _index, out sectionIndex, out cellIndex);
                ScrollToCell(sectionIndex, cellIndex, false, _top);
            }
            SelectedIndex = _selectedIndex;
            if (PopoverFragment.Instance == null)
                return;
            var metrics = new DisplayMetrics();
            DroidFactory.MainActivity.WindowManager.DefaultDisplay.GetMetrics(metrics);
            var width = (int)(metrics.WidthPixels * .6 + 16 * DroidFactory.DisplayScale);
            var height = LinearLayout.LayoutParams.WrapContent;
            PopoverFragment.Instance.Dialog.Window.SetLayout(width, height);
        }

        /// <summary>
        /// Renders this instance.
        /// </summary>
        public override void Render()
        {
            base.Render();
            ReloadSections();
        }

        public override IDictionary<string, string> GetSubmissionValues()
        {
            foreach (var cell in GetVisibleCells().OfType<IElementHost>())
            {
                SetSubmitValues(cell);
            }

            return base.GetSubmissionValues();
        }

        public void OnMovedToScrapHeap(View view)
        {
            SetSubmitValues(view as IElementHost);
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            // save index and top position
            if (List != null)
            {
                if (TextBase.CurrentFocus != null)
                {
                    var cell = (View)TextBase.CurrentFocus.Parent;
                    _index = List.FirstVisiblePosition + List.IndexOfChild(cell);
                }
                else if (SelectedIndex >= List.FirstVisiblePosition && SelectedIndex <= List.LastVisiblePosition)
                {
                    var cell = GetVisibleCells().ElementAt(SelectedIndex - List.FirstVisiblePosition) as View;
                    _index = SelectedIndex;
                    _top = cell?.Top ?? 0;
                }
            }

            outState.PutInt(nameof(SelectedIndex), SelectedIndex);
            outState.PutInt(nameof(_index), _index);
            outState.PutInt(nameof(_top), _top);
            base.OnSaveInstanceState(outState);
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
                if (!_separatorColor.IsDefaultColor && List != null)
                {
                    List.Divider = _separatorColor.ToColorDrawable();
                    List.DividerHeight = 1;
                }
                if (_separatorColor != oldValue)
                    this.OnPropertyChanged();
            }
        }
        private Color _separatorColor;

        public ListViewStyle Style { get; }

        public ISearchBox SearchBox
        {
            get { return _searchBox; }
            set
            {
                if (_searchBox == value) return;

                if (_searchBox != null)
                {
                    var box = DroidFactory.GetNativeObject<View>(_searchBox, nameof(_searchBox));
                    ((ViewGroup)box.Parent)?.RemoveView(box);
                }

                _searchBox = value;

                if (_searchBox != null)
                {
                    AttachSearchView(DroidFactory.GetNativeObject<View>(_searchBox, nameof(_searchBox)));
                }

                this.OnPropertyChanged();
            }
        }
        private ISearchBox _searchBox;

        private void AttachSearchView(View searchBox, LinearLayout layout = null)
        {
            if (searchBox == null) return;
            ((ViewGroup)searchBox.Parent)?.RemoveView(searchBox);
            searchBox.LayoutParameters = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.WrapContent);
            (layout ?? View as LinearLayout)?.AddView(searchBox, 0);
        }

        public SectionCollection Sections { get; }

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
            SelectedIndex = -1;
            Device.Thread.ExecuteOnMainThread(() =>
            {
                (List?.Adapter as BaseAdapter)?.NotifyDataSetChanged();
            });
        }

        public IEnumerable<ICell> GetVisibleCells()
        {
            var first = List.FirstVisiblePosition;
            var last = List.LastVisiblePosition;
            for (var i = first; i <= last; i++)
            {
                var view = List.GetChildAt(i - first);
                var cell = RetrieveCell(view);
                if (cell != null) yield return cell.Pair as ICell ?? cell;
                else if (view != null) yield return new CustomItemContainer(view);
            }
        }

        private int GetPosition(int section, int index)
        {
            var position = 0;
            for (var i = 0; i <= section; i++)
            {
                var s = Sections[i];
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
            ScrollToCell(section, index, animated, 0);
        }

        public void ScrollToCell(int section, int index, bool animated, int top)
        {
            var position = GetPosition(section, index);
            if (List == null)
            {
                _touchScroll = false;
                _index = position;
                return;
            }
            Scroll(position, top, animated);
        }

        public void ScrollToEnd(bool animated)
        {
            Scroll(List.Count - 1, 0, animated);
        }

        public void ScrollToHome(bool animated)
        {
            Scroll(0, 0, animated);
        }

        private void Scroll(int position, int offset, bool animated)
        {
            _touchScroll = false;
            if (List == null)
            {
                _index = 0;
                return;
            }
            if (animated)
            {
                Device.Thread.ExecuteOnMainThread(() => List.SmoothScrollToPositionFromTop(position, offset, 250));
            }
            else
            {
                Device.Thread.ExecuteOnMainThread(() =>
                {
                    List.SetSelectionFromTop(position, offset);
                    List.ClearChoices();
                });
            }
        }
        private bool _touchScroll = true;

        public void OnItemSelected(AdapterView parent, View view, int position, long id)
        {
            Device.Log.Platform($"Cell selected at position {position}");
            SelectedIndex = position;
        }

        public void OnNothingSelected(AdapterView parent)
        {
            List.DescendantFocusability = DescendantFocusability.BeforeDescendants;
        }

        public void OnItemClick(AdapterView parent, View view, int position, long id)
        {
            Device.Log.Platform($"Cell clicked at position {position}");
            var cell = RetrieveCell(view) as IGridCell;
            List.DescendantFocusability = Events.HasEvent(cell, nameof(cell.Selected)) ? DescendantFocusability.AfterDescendants : DescendantFocusability.BeforeDescendants;
            cell?.Select();
        }

        public bool OnItemLongClick(AdapterView parent, View view, int position, long id)
        {
            Device.Log.Platform($"Cell long-clicked at position {position}");
            var args = new EventHandledEventArgs();
            var cell = RetrieveCell(view);
            LongClicked?.Invoke(cell?.Pair ?? cell, args);
            return args.IsHandled;
        }

        private ICell RetrieveCell(View view)
        {
            var cell = view as ICell;
            while (cell == null && view != null)
            {
                view = view.Parent as View;
                cell = view as ICell;
            }
            return cell;
        }

        public event EventHandler<EventHandledEventArgs> LongClicked;

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (List != null)
                {
                    Device.Thread.ExecuteOnMainThread(() =>
                    {
                        var currentSelectedCell = List.GetChildAt(_selectedIndex - List.FirstVisiblePosition) as GridCell;

                        if (value == -1)
                        {
                            _index = -1;
                            _top = -1;
                            List.ClearChoices();
                        }
                        else
                        {
                            _index = value;
                            _top = List.GetChildAt(value - List.FirstVisiblePosition)?.Top ?? _top;
                            List.SetSelectionFromTop(value, _top);
                        }

                        if (_selectedIndex == value) return;
                        _selectedIndex = value;
                        this.OnPropertyChanged();

                        currentSelectedCell?.Deselect();
                        currentSelectedCell = List.GetChildAt(_selectedIndex - List.FirstVisiblePosition) as GridCell;
                        currentSelectedCell?.Highlight();
                    });
                }
                else if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    this.OnPropertyChanged();
                }
            }
        }
        private int _selectedIndex = -1;
    }
}