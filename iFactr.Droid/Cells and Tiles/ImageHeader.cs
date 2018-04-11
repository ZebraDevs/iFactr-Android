using System;
using Android.Content;
using Android.Runtime;
using iFactr.UI;

namespace iFactr.Droid
{
    public class ImageHeader : GridBase, ISectionHeader
    {
        private Image _image;

        public ImageHeader() :
            base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public ImageHeader(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            Initialize();
        }

        public ImageHeader(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public ImageHeader(Context context, Android.Util.IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize();
        }

        private void Initialize()
        {
            Rows.Add(Row.AutoSized);
            Columns.Add(Column.AutoSized);

            _image = new Image
            {
                RowSpan = 1,
                ColumnSpan = 1,
                Stretch = ContentStretch.None,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            AddChild(_image);
        }

        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }
        public Font Font { get; set; }

        public string Text
        {
            get { return Path; }
            set { }
        }

        public string Path
        {
            get { return _image.FilePath; }
            set { _image.FilePath = value; }
        }

        public new IPairable Pair
        {
            get { return _pair; }
            set
            { 
                if (_pair != null || value == null) return;
                _pair = value;
                _pair.Pair = this;
                this.OnPropertyChanged();
            }
        }
    }
}