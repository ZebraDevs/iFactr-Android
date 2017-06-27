using System;
using Android.Content;
using iFactr.UI.Controls;
using Point = iFactr.UI.Point;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Size = iFactr.UI.Size;

namespace iFactr.Droid
{
    public class TextBox : TextBase, ITextBox
    {
        #region Constructors

        [Preserve]
        public TextBox()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public TextBox(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public TextBox(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize(attrs);
        }

        [Preserve]
        public TextBox(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize(attrs);
        }

        public TextBox(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        private void Initialize(IAttributeSet attrs = null)
        {
            SetSingleLine(true);
            if (InputType == InputTypes.ClassText)
                SetCompletion();
            this.InitializeAttributes(attrs);
        }

        #endregion

        public override void SetLocation(Point location, Size size)
        {
            if (Math.Abs(MeasuredHeight - size.Height) > .001)
            {
                this.MeasureView(size);
            }

            SetWidth((int)size.Width);

            var left = location.X;
            var right = location.X + size.Width;
            var top = location.Y;
            var bottom = location.Y + MeasuredHeight;

            Layout((int)left, (int)top, (int)right, (int)bottom);
        }
    }
}