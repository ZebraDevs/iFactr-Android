using System.Linq;
using iFactr.UI;
using iFactr.UI.Controls;
using iFactr.UI.Instructions;

namespace iFactr.Droid
{
    public class AndroidInstructor : UniversalInstructor
    {
        protected override void OnLayout(ILayoutInstruction element)
        {
            base.OnLayout(element);
            var pairable = element as IPairable;

            var contentCell = pairable?.Pair as ContentCell ?? pairable as ContentCell;
            if (contentCell != null) OnLayoutContentCell(contentCell);

            var headerCell = pairable?.Pair as HeaderedControlCell ?? pairable as HeaderedControlCell;
            if (headerCell != null) OnLayoutHeaderedCell(headerCell);
        }

        private static void OnLayoutContentCell(ContentCell contentCell)
        {
            if (!string.IsNullOrEmpty(contentCell?.SubtextLabel.Text) && contentCell.MinHeight == Cell.StandardCellHeight)
            {
                contentCell.MinHeight = Cell.StandardCellHeight * 4 / 3;
            }
        }

        // if controls.count < 3 && all are image or switch then set beside
        private static void OnLayoutHeaderedCell(HeaderedControlCell cell)
        {
            var grid = ((IPairable)cell).Pair as IGridBase;
            if (grid == null)
                return;

            var controls = grid.Children.Where(c => c != cell.Header).ToList();

            var first = controls.FirstOrDefault();
            if (controls.Count == 0 || controls.Count > 2 || !(first is ISwitch) && !(first is IImage)) return;

            grid.Columns.Clear();
            grid.Rows.Clear();
            grid.Columns.Add(Column.OneStar);
            grid.Columns.Add(Column.AutoSized);

            grid.Rows.Add(Row.AutoSized);
            grid.Rows.Add(Row.AutoSized);

            if (!string.IsNullOrEmpty(cell.Header.Text))
            {
                cell.Header.Font = Font.PreferredHeaderFont.Size > 0 ? Font.PreferredHeaderFont : Font.PreferredLabelFont;
                cell.Header.Lines = 1;
                cell.Header.VerticalAlignment = VerticalAlignment.Center;
                cell.Header.HorizontalAlignment = HorizontalAlignment.Left;
                cell.Header.RowIndex = 0;
                cell.Header.ColumnIndex = 0;
                first.Margin = new Thickness(Thickness.LargeHorizontalSpacing, 0, 0, 0);
            }
            else first.Margin = new Thickness();

            first.VerticalAlignment = VerticalAlignment.Center;
            first.RowIndex = 0;
            first.ColumnIndex = 1;
        }
    }
}