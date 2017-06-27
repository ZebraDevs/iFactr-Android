using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Android.Views;
using Android.Widget;
using iFactr.Core.Layers;
using iFactr.UI;
using View = Android.Views.View;
using iFactr.Core;

namespace iFactr.Droid
{
    public class CellAdapter : BaseAdapter<ICell>
    {
        #region Fields and properties

        public ListViewFragment Parent { get; }

        private readonly List<int> _ids = new List<int>();
        private readonly List<int> _headerIndices = new List<int>();
        private readonly List<int> _footerIndices = new List<int>();

        private int _itemCount;

        public override int Count => _itemCount;

        #endregion

        #region Constructors

        public CellAdapter(ListViewFragment parent)
        {
            Parent = parent;
            NotifyDataSetChanged();
        }

        #endregion

        #region Methods

        public override void NotifyDataSetChanged()
        {
            var oldCount = _itemCount;
            _itemCount = 0;
            _headerIndices.Clear();
            _footerIndices.Clear();
            _ids.Clear();

            //Reserve ids for headers and footers
            _ids.Add(0);
            _ids.Add(1);

            foreach (var section in Parent.Sections.ToList())
            {
                if (!string.IsNullOrEmpty(section.Header?.Text))
                {
                    _headerIndices.Add(_itemCount);
                    _itemCount++;
                }

                _itemCount += section.ItemCount;

                if (string.IsNullOrEmpty(section.Footer?.Text)) continue;
                _footerIndices.Add(_itemCount);
                _itemCount++;
            }

            //Ensure a view type exists for dynamic views
            if (_itemCount == _headerIndices.Count + _footerIndices.Count) _itemCount++;
            //Ensure a view type exists for dynamic headers
            if (!_headerIndices.Any()) _itemCount++;
            //Ensure a view type exists for dynamic footers
            if (!_footerIndices.Any()) _itemCount++;

            if (oldCount > 0 && _itemCount > oldCount)
            {
                Parent.List.Adapter = new CellAdapter(Parent);
            }
            else
            {
                base.NotifyDataSetChanged();
            }
        }

        private ICell GetCell(int sectionIndex, int cellIndex, ICell cell)
        {
            //Skip headers and invalid sections
            if (sectionIndex == -1 || cellIndex == -1)
                return null;

            var section = Parent.Sections[sectionIndex];

            //Skip footers
            if (section.ItemCount <= cellIndex)
                return null;

            var sectionHandler = section.CellRequested;
            CellDelegate listHandler;

            ICell retval = null;
            if (sectionHandler != null)
            {
                retval = sectionHandler.Invoke(cellIndex, cell == null ? null : cell.Pair as ICell ?? cell);
            }
            else if ((listHandler = Parent.CellRequested) != null)
            {
                retval = listHandler.Invoke(sectionIndex, cellIndex, cell == null ? null : cell.Pair as ICell ?? cell);
            }

            return retval;
        }

        public static void GetSectionAndIndex(SectionCollection sections, int position, out int sectionIndex, out int cellIndex)
        {
            var sectionIX = 0;
            if (sections != null)
            {
                foreach (var section in sections.ToList())
                {
                    if (!string.IsNullOrEmpty(section.Header?.Text))
                        position--;

                    if (position < section.ItemCount)
                    {
                        sectionIndex = sectionIX;
                        cellIndex = position;
                        return;
                    }

                    position -= section.ItemCount;

                    if (!string.IsNullOrEmpty(section.Footer?.Text))
                    {
                        if (position == 0)
                        {
                            sectionIndex = sectionIX;
                            cellIndex = section.ItemCount;
                            return;
                        }
                        position--;
                    }
                    sectionIX++;
                }
            }

            sectionIndex = -1;
            cellIndex = -1;
        }

        #endregion

        #region Adapter overrides

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            int sectionIndex, cellIndex;
            GetSectionAndIndex(Parent.Sections, position, out sectionIndex, out cellIndex);

            var layer = Parent.GetModel() as iLayer;
            var sectionIndexProperty = typeof(iLayerItem).GetProperty("SectionIndex", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var outOfSync = layer != null && (layer.Items.Count == 0 || layer.Items.Count > 1 && layer.Items.All(i => (int)sectionIndexProperty.GetValue(i) == 0));

            var count = _itemCount;
            if (!_headerIndices.Any()) count--;
            if (!_footerIndices.Any()) count--;

            if (!Parent.Sections.Any() || outOfSync || position >= count)
            {
                return new View(DroidFactory.MainActivity);
            }

            object cell = GetCell(sectionIndex, cellIndex, convertView as ICell) ??
                (convertView == null ? null : GetCell(sectionIndex, cellIndex, new CustomItemContainer(convertView)));

            if (cell == null)
            {
                if (sectionIndex > -1)
                {
                    var section = Parent.Sections[sectionIndex];
                    if (_headerIndices.Contains(position) && section.Header != null) return DroidFactory.GetNativeObject<View>(section.Header, "Header");
                    if (_footerIndices.Contains(position) && section.Footer != null) return DroidFactory.GetNativeObject<View>(section.Footer, "Footer");
                }

                if (convertView == null)
                {
                    iApp.Log.Error("View not found for item in section {0}, cell {1}", sectionIndex, cellIndex);
                    return new View(DroidFactory.MainActivity);
                }
            }

            var custom = cell as CustomItemContainer;
            if (custom != null)
            {
                cell = custom.CustomItem;
            }

            var pairable = cell as IPairable;
            if (cell is IGridBase || pairable?.Pair is IGridBase)
            {
                var grid = DroidFactory.GetNativeObject<GridBase>(cell, "IGridBase");
                grid.Parent = Parent;
            }

            if (cell is IGridCell || pairable?.Pair is IGridCell)
            {
                var grid = DroidFactory.GetNativeObject<GridCell>(cell, "IGridCell");
                grid.Metadata["Index"] = position;
                if (position == Parent.SelectedIndex)
                {
                    grid.Highlight();
                }
                else
                {
                    grid.Deselect();
                }
                return grid;
            }

            if (cell is IRichContentCell || pairable?.Pair is IRichContentCell)
            {
                var richText = DroidFactory.GetNativeObject<RichText>(cell, "IRichContentCell");
                richText.Parent = Parent;
                richText.Load();
                return richText;
            }

            if (cell is View || pairable?.Pair is View)
            {
                var nativeView = DroidFactory.GetNativeObject<View>(cell, "cell");
                if (nativeView != null)
                {
                    return nativeView;
                }
            }

            iApp.Log.Warn("Native view not found for custom item in section {0}, cell {1}", sectionIndex, cellIndex);
            return new View(DroidFactory.MainActivity);
        }

        public override long GetItemId(int position)
        {
            var handler = Parent.ItemIdRequested;
            int sectionIX, cellIX;
            GetSectionAndIndex(Parent.Sections, position, out sectionIX, out cellIX);
            return handler == null || cellIX == -1 ? position : handler(sectionIX, cellIX);
        }

        public override int GetItemViewType(int position)
        {
            if (_headerIndices.Contains(position))
                return 0;
            if (_footerIndices.Contains(position))
                return 1;

            int sectionIX, cellIX;
            GetSectionAndIndex(Parent?.Sections, position, out sectionIX, out cellIX);

            var layer = Parent?.GetModel() as iLayer;
            var prop = typeof(iLayerItem).GetProperty("SectionIndex", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var outOfSync = layer != null && (layer.Items.Count == 0 || layer.Items.Count > 1 && layer.Items.All(i => (int)prop.GetValue(i) == 0));

            if (outOfSync || sectionIX < 0 || cellIX < 0 || Parent.Sections.Count <= sectionIX)
            {
                return -1;
            }

            int type;
            var section = Parent.Sections[sectionIX];
            var sectionHandler = section.ItemIdRequested;
            if (sectionHandler != null && cellIX < section.ItemCount)
            {
                type = sectionHandler.Invoke(cellIX);
            }
            else
            {
                var viewHandler = Parent.ItemIdRequested;
                if (viewHandler != null && cellIX < section.ItemCount)
                {
                    type = viewHandler.Invoke(sectionIX, cellIX);
                }
                else return -1;
            }

            //Skip reserved header and footer indices.
            type += 2;

            if (!_ids.Contains(type) && _ids.Count < _itemCount)
            {
                _ids.Add(type);
            }
            return _ids.IndexOf(type);
        }

        public override int ViewTypeCount => _itemCount;

        public override bool AreAllItemsEnabled()
        {
            return false;
        }

        public override bool IsEnabled(int position)
        {
            return GetItemViewType(position) > 1;
        }

        public override ICell this[int position]
        {
            get
            {
                int sectionIX, cellIX;
                GetSectionAndIndex(Parent?.Sections, position, out sectionIX, out cellIX);
                return GetCell(sectionIX, cellIX, null);
            }
        }

        #endregion
    }
}