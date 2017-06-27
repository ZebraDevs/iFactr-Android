using System;
using System.Linq;
using Android.Util;
using Android.Views;
using MonoCross;
using MonoCross.Utilities;
using iFactr.UI;
using iFactr.UI.Controls;
using MonoCross.Navigation;
using Size = iFactr.UI.Size;
using View = Android.Views.View;

namespace iFactr.Droid
{
    public static class ElementExtensions
    {
        public const string XmlNamespace = "http://schemas.android.com/apk/lib/ifactr";

        public static Size MeasureView(this IElement view, Size constraints)
        {
            var nativeView = DroidFactory.GetNativeObject<View>(view, nameof(view));
            if (nativeView == null) return new Size();
            nativeView.Measure(View.MeasureSpec.MakeMeasureSpec(GetMeasureSpec(constraints.Width),
                    view.HorizontalAlignment == HorizontalAlignment.Stretch ? MeasureSpecMode.Exactly : MeasureSpecMode.AtMost),
                View.MeasureSpec.MakeMeasureSpec(GetMeasureSpec(constraints.Height), MeasureSpecMode.Unspecified));
            return new Size(nativeView.MeasuredWidth, nativeView.MeasuredHeight);
        }

        private static int GetMeasureSpec(double constraint)
        {
            if (double.IsInfinity(constraint) || constraint == double.MaxValue)
                return ViewGroup.LayoutParams.WrapContent;
            return constraint > int.MaxValue ? int.MaxValue : (int)Math.Ceiling(constraint);
        }

        public static void RequestResize(this IElement element, bool condition = true)
        {
            var gridBase = DroidFactory.GetNativeObject<GridBase>(element.Parent, nameof(element.Parent));
            if (gridBase != null && condition)
            {
                gridBase.ResizeRequested = true;
            }
        }

        public static void InitializeAttributes(this IElement element, IAttributeSet attrs)
        {
            var view = element as View;
            if (view != null && view.Id > 0) element.ID = DroidFactory.MainActivity.Resources.GetResourceEntryName(view.Id);

            if (attrs == null) return;
            const int adj = Element.AutoLayoutIndex + 1;
            var column = attrs.GetAttributeIntValue(XmlNamespace, "column", adj);
            if (column > adj) element.ColumnIndex = column - 1;
            var columnSpan = attrs.GetAttributeIntValue(XmlNamespace, "columnSpan", 1);
            if (columnSpan > 1) element.ColumnSpan = columnSpan;
            var row = attrs.GetAttributeIntValue(XmlNamespace, "row", adj);
            if (row > adj) element.RowIndex = row - 1;
            var rowSpan = attrs.GetAttributeIntValue(XmlNamespace, "rowSpan", 1);
            if (rowSpan > 1) element.RowSpan = rowSpan;
            var hAligns = Enum.GetNames(typeof(HorizontalAlignment)).Select(h => h.ToLower()).ToArray();
            var horizontalAlignment = attrs.GetAttributeListValue(XmlNamespace, "horizontalAlignment", hAligns, 0);
            if (horizontalAlignment > 0) element.HorizontalAlignment = (HorizontalAlignment)horizontalAlignment;
            var vAligns = Enum.GetNames(typeof(VerticalAlignment)).Select(v => v.ToLower()).ToArray();
            var verticalAlignment = attrs.GetAttributeListValue(XmlNamespace, "verticalAlignment", vAligns, 0);
            if (verticalAlignment > 0) element.VerticalAlignment = (VerticalAlignment)verticalAlignment;
            var visibles = Enum.GetNames(typeof(Visibility)).Select(v => v.ToLower()).ToArray();
            var visibility = attrs.GetAttributeListValue(XmlNamespace, "visibility", visibles, 0);
            if (visibility > 0) element.Visibility = (Visibility)visibility;

            string font = attrs.GetAttributeValue(XmlNamespace, "font");
            var fontProp = Device.Reflector.GetProperty(typeof(Font), font);
            if (fontProp != null)
            {
                var value = fontProp.GetValue(Font.PreferredLabelFont);
                Device.Reflector.GetProperty(element.GetType(), "Font")?.SetValue(element, value);
            }
            else if (font != null)
            {
                var defaults = MXContainer.Resolve<IPlatformDefaults>(typeof(IPlatformDefaults));
                var defaultsProp = Device.Reflector.GetProperty(defaults.GetType(), font);
                if (defaultsProp != null)
                {
                    var value = defaultsProp.GetValue(defaults);
                    Device.Reflector.GetProperty(element.GetType(), "Font")?.SetValue(element, value);
                }
            }

            string foregroundColor = attrs.GetAttributeValue(XmlNamespace, "foregroundColor");
            var colorProp = Device.Reflector.GetProperty(typeof(Color), foregroundColor);
            if (colorProp != null)
            {
                var value = colorProp.GetValue(Color.Transparent);
                Device.Reflector.GetProperty(element.GetType(), "ForegroundColor")?.SetValue(element, value);
            }
            else if (foregroundColor != null)
            {
                Device.Reflector.GetProperty(element.GetType(), "ForegroundColor")?.SetValue(element, new Color(foregroundColor));
            }

            string backgroundColor = attrs.GetAttributeValue(XmlNamespace, "backgroundColor");
            colorProp = Device.Reflector.GetProperty(typeof(Color), backgroundColor);
            if (colorProp != null)
            {
                var value = colorProp.GetValue(Color.Transparent);
                Device.Reflector.GetProperty(element.GetType(), "BackgroundColor")?.SetValue(element, value);
            }
            else if (backgroundColor != null)
            {
                Device.Reflector.GetProperty(element.GetType(), "BackgroundColor")?.SetValue(element, new Color(backgroundColor));
            }

            string margin = attrs.GetAttributeValue(XmlNamespace, "margin");
            if (!string.IsNullOrEmpty(margin))
            {
                var padValues = margin.Split(',').Select(p => p.TryParseDouble()).ToList();
                switch (padValues.Count)
                {
                    case 1:
                        element.Margin = new Thickness(padValues[0]);
                        break;
                    case 2:
                        element.Margin = new Thickness(padValues[0], padValues[1]);
                        break;
                    case 4:
                        element.Margin = new Thickness(padValues[0], padValues[1], padValues[2], padValues[3]);
                        break;
                    default:
                        throw new FormatException("Invalid margin format: " + margin);
                }
            }

            var left = ParseMargin(attrs, "marginLeft", (int)element.Margin.Left);
            var top = ParseMargin(attrs, "marginTop", (int)element.Margin.Top);
            var right = ParseMargin(attrs, "marginRight", (int)element.Margin.Right);
            var bottom = ParseMargin(attrs, "marginBottom", (int)element.Margin.Bottom);
            element.Margin = new Thickness(left, top, right, bottom);
        }

        private static int ParseMargin(IAttributeSet attrs, string margin, int currentMargin)
        {
            var marginLeftAttr = attrs.GetAttributeValue(XmlNamespace, margin);
            if (marginLeftAttr == null) return currentMargin;
            var marginLeft = marginLeftAttr.TrimEnd('s', 'd', 'p', 'x').TryParseInt32(currentMargin);
            if (marginLeft != currentMargin) return marginLeft;
            var marginRes = attrs.GetAttributeResourceValue(XmlNamespace, margin, 0);
            if (marginRes > 0) marginLeft = (int)(DroidFactory.MainActivity.Resources.GetDimension(marginRes) / DroidFactory.DisplayScale);
            return marginLeft;
        }
    }
}