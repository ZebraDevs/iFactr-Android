using Android.Runtime;
using iFactr.UI;

namespace iFactr.Droid
{
    public class AndroidDefaults : IPlatformDefaults
    {
        [Preserve]
        public AndroidDefaults() { }

        protected double GetDimension(int resId)
        {
            return DroidFactory.MainActivity.Resources.GetDimension(resId) / DroidFactory.DisplayScale;
        }

        public virtual double CellHeight => GetDimension(Resource.Dimension.CellHeight);

        #region Cell Padding

        public virtual double LeftMargin => GetDimension(Resource.Dimension.LeftMargin);

        public virtual double TopMargin => GetDimension(Resource.Dimension.TopMargin);

        public virtual double RightMargin => GetDimension(Resource.Dimension.RightMargin);

        public virtual double BottomMargin => GetDimension(Resource.Dimension.BottomMargin);

        #endregion

        #region Row/column spacing

        public virtual double SmallHorizontalSpacing => GetDimension(Resource.Dimension.SmallHorizontalSpacing);

        public virtual double LargeHorizontalSpacing => GetDimension(Resource.Dimension.LargeHorizontalSpacing);

        public virtual double SmallVerticalSpacing => GetDimension(Resource.Dimension.SmallVerticalSpacing);

        public virtual double LargeVerticalSpacing => GetDimension(Resource.Dimension.LargeVerticalSpacing);

        #endregion

        #region Fonts

        protected Font NormalFont { get; set; } = new Font("Roboto", 14);
        protected Font MediumFont { get; set; } = new Font("Roboto", 18);

        public virtual Font ButtonFont => MediumFont;

        public virtual Font DateTimePickerFont => NormalFont;

        public virtual Font HeaderFont => MediumFont;

        public virtual Font LabelFont => MediumFont;

        public virtual Font MessageBodyFont => NormalFont;

        public virtual Font MessageTitleFont => MediumFont;

        public virtual Font SectionHeaderFont => NormalFont;

        public virtual Font SectionFooterFont => NormalFont;

        public virtual Font SelectListFont => NormalFont;

        public virtual Font SmallFont => NormalFont;

        public virtual Font TabFont => NormalFont;

        public virtual Font TextBoxFont => NormalFont;

        public virtual Font ValueFont => NormalFont;

        #endregion
    }
}