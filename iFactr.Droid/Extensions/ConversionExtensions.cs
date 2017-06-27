using System;
using Android.Graphics.Drawables;
using Android.Text;
using Android.Views;
using iFactr.UI;
using KeyboardType = iFactr.UI.KeyboardType;

namespace iFactr.Droid
{
    public static class ConversionExtensions
    {
        public static ColorDrawable ToColorDrawable(this Color color)
        {
            return new ColorDrawable(color.ToColor());
        }

        public static Color ToColor(this Android.Graphics.Color color)
        {
            return new Color(color.A, color.R, color.G, color.B);
        }

        public static Android.Graphics.Color ToColor(this Color color)
        {
            return new Android.Graphics.Color(color.R, color.G, color.B, color.A);
        }
        
        public static UI.TextAlignment ToTextAlignment(this GravityFlags alignment)
        {
            if ((alignment & GravityFlags.Left) == GravityFlags.Left)
            {
                return UI.TextAlignment.Left;
            }
            if ((alignment & GravityFlags.Right) == GravityFlags.Right)
            {
                return UI.TextAlignment.Right;
            }
            if ((alignment & GravityFlags.FillHorizontal) == GravityFlags.FillHorizontal)
            {
                return UI.TextAlignment.Justified;
            }
            if ((alignment & GravityFlags.CenterHorizontal) == GravityFlags.CenterHorizontal)
            {
                return UI.TextAlignment.Center;
            }
            return UI.TextAlignment.Left;
        }

        public static GravityFlags ToTextAlignment(this UI.TextAlignment alignment)
        {
            switch (alignment)
            {
                case UI.TextAlignment.Left:
                    return GravityFlags.Left;
                case UI.TextAlignment.Center:
                    return GravityFlags.CenterHorizontal;
                case UI.TextAlignment.Right:
                    return GravityFlags.Right;
                case UI.TextAlignment.Justified:
                    return GravityFlags.FillHorizontal;
            }
            throw new ArgumentOutOfRangeException(nameof(alignment));
        }
    }
}