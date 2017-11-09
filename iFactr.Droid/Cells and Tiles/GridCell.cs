using Android.Content;
using Android.Runtime;
using iFactr.UI;
using System;
using System.Linq;
using iFactr.UI.Controls;
using iFactr.UI.Instructions;
using iFactr.Core;

namespace iFactr.Droid
{
    public class GridCell : GridBase, IGridCell, ILayoutInstruction
    {
        #region Constructors

        [Preserve]
        public GridCell()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public GridCell(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            Initialize();
        }

        public GridCell(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public GridCell(Context context, Android.Util.IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize();
        }

        private void Initialize()
        {
            Padding = new Thickness(Thickness.LeftMargin, Thickness.TopMargin, Thickness.RightMargin, Thickness.BottomMargin);
            SelectionColor = iApp.Instance.Style.SelectionColor;
            SelectionStyle = SelectionStyle.Default;
        }

        #endregion

        #region Properties

        public Link AccessoryLink
        {
            get { return _accessory?.Link; }
            set
            {
                if (_accessory == null)
                {
                    _accessory = new Accessory
                    {
                        Margin = new Thickness(0, -Thickness.TopMargin, -Thickness.RightMargin, -Thickness.BottomMargin),
                        ColumnSpan = 1,
                        RowIndex = 0,
                    };
                    _accessory.Click += Accessory_Click;
                }
                _accessory.RequestResize(Children.Contains(_accessory) != (value != null));
                _accessory.Link = value;
            }
        }
        private Accessory _accessory;

        private void Accessory_Click(object sender, EventArgs e)
        {
            if (!Pair.RaiseEvent(nameof(AccessorySelected), EventArgs.Empty))
            {
                DroidFactory.Navigate(AccessoryLink, Parent);
            }
        }
        public event EventHandler AccessorySelected;

        public Link NavigationLink { get; set; }

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

        public MetadataCollection Metadata => _metadata ?? (_metadata = new MetadataCollection());
        private MetadataCollection _metadata;

        public Color SelectionColor
        {
            get { return SelectionStyle > SelectionStyle.IndicatorOnly && !_selectionColor.IsDefaultColor ? _selectionColor : new Color(); }
            set
            {
                if (_selectionColor == value) return;
                _selectionColor = value;
                OnPropertyChanged();
            }
        }
        private Color _selectionColor;

        public SelectionStyle SelectionStyle
        {
            get { return _selectionStyle; }
            set
            {
                _selectionStyle = value;
                OnPropertyChanged();
            }
        }
        private SelectionStyle _selectionStyle;

        #endregion

        public void Deselect()
        {
            SetBackgroundColor(BackgroundColor.ToColor());
            var labels = Children.OfType<ILabel>().Select(f => DroidFactory.GetNativeObject<Label>(f, "label"));
            foreach (var label in labels)
            {
                label.SetTextColor(label.ForegroundColor.ToColor());
            }
        }

        public void Highlight()
        {
            if (SelectionColor.IsDefaultColor)
            {
                Deselect();
                return;
            }

            SetBackgroundColor(SelectionColor.ToColor());
            var labels = Children.OfType<ILabel>().Select(f => DroidFactory.GetNativeObject<Label>(f, "label"));
            foreach (var label in labels)
            {
                label.SetTextColor(label.HighlightColor.ToColor());
            }
        }

        public void Select()
        {
            var listViewFragment = Parent as ListViewFragment;
            if (listViewFragment != null) listViewFragment.SelectedIndex = Metadata.Get<int>("Index");

            if (!this.RaiseEvent(nameof(Selected), EventArgs.Empty))
            {
                DroidFactory.Navigate(NavigationLink, Parent);
            }
        }
        public new event EventHandler Selected;

        public void NullifyEvents()
        {
            AccessorySelected = null;
            Selected = null;
        }

        public bool Equals(ICell other)
        {
            var control = other as Cell;
            return control?.Equals(this) ?? ReferenceEquals(this, other);
        }

        public void Layout()
        {
            (Pair as ILayoutInstruction)?.Layout();

            if (AccessoryLink == null && Children.Contains(_accessory))
            {
                RemoveChild(_accessory);
                Columns.RemoveAt(Columns.Count - 1);
            }
            else if (AccessoryLink != null && !Children.Contains(_accessory))
            {
                AddChild(_accessory);
                Columns.Add(Column.AutoSized);
                _accessory.ColumnIndex = Columns.Count - 1;
                _accessory.RowSpan = Math.Max(1, Rows.Count);
            }
        }
    }
}