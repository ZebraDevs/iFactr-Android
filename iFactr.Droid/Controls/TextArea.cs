using System;
using Android.Content;
using Android.Text;
using iFactr.UI.Controls;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Size = iFactr.UI.Size;
using TextAlignment = iFactr.UI.TextAlignment;

namespace iFactr.Droid
{
    public class TextArea : TextBase, ITextArea
    {
        #region Constructors

        [Preserve]
        public TextArea()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public TextArea(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public TextArea(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize(attrs);
        }

        [Preserve]
        public TextArea(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize(attrs);
        }

        public TextArea(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        private void Initialize(IAttributeSet attrs = null)
        {
            InputExtras = InputTypes.TextFlagMultiLine;
            VerticalAlignment = UI.VerticalAlignment.Stretch;
            this.InitializeAttributes(attrs);
            this.RequestResize();
        }

        #endregion

        public new int MinLines
        {
            get { return _minLines; }
            set
            {
                if (_minLines == value) return;
                _minLines = value;
                SetMinLines(_minLines);
                this.OnPropertyChanged();
            }
        }
        private int _minLines;

        public new int MaxLines
        {
            get { return _maxLines; }
            set
            {
                if (_maxLines == value) return;
                _maxLines = value;
                SetMaxLines(_maxLines);
                this.OnPropertyChanged();
            }
        }
        private int _maxLines;

        public override Size Measure(Size constraints)
        {
            return this.MeasureView(constraints);
        }

        protected override void OnTextChanged(string oldValue, string newValue)
        {
            this.RequestResize(_lines != LineCount);
            _lines = LineCount;
            base.OnTextChanged(oldValue, newValue);
        }
        private int _lines;

        public override TextAlignment TextAlignment
        {
            get { return base.TextAlignment; }
            set
            {
                base.TextAlignment = value;
                Gravity |= GravityFlags.Top;
            }
        }
    }
}