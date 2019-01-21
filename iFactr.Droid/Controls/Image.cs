using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using iFactr.UI;
using iFactr.UI.Controls;
using MonoCross;
using MonoCross.Utilities;
using System;
using System.ComponentModel;
using Point = iFactr.UI.Point;
using Size = iFactr.UI.Size;

namespace iFactr.Droid
{
    public class Image : ImageView, IImage, INotifyPropertyChanged
    {
        #region Constructors

        [Preserve]
        public Image()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public Image(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public Image(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize(attrs);
        }

        [Preserve]
        public Image(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize(attrs);
        }

        protected Image(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        [Preserve]
        public Image(ImageCreationOptions options)
            : this()
        {
            Options = options;
        }

        [Preserve]
        public Image(string filePath)
            : this()
        {
            FilePath = filePath;
        }

        public Image(int resourceId)
            : this()
        {
            base.SetImageDrawable(Context.GetDrawable(resourceId));
        }

        private void Initialize(IAttributeSet attrs = null)
        {
            this.InitializeAttributes(attrs);
            string file = attrs?.GetAttributeValue(ElementExtensions.XmlNamespace, "filePath");
            if (file != null)
            {
                FilePath = file;
            }
        }

        #endregion

        private void Image_Click(object sender, EventArgs e)
        {
            var click = _clicked;
            if (IsEnabled && click != null)
            {
                click(Pair, e);
            }
            else
            {
                (Parent as IGridCell)?.Select();
            }
        }

        public void NullifyEvents()
        {
            _clicked = null;
            Loaded = null;
            Validating = null;
        }

        #region Value

        public override float Alpha
        {
            get { return Handle == IntPtr.Zero ? 0 : base.Alpha; }
            set
            {
                if (Handle == IntPtr.Zero || Math.Abs(Alpha - value) < .003) return;
                base.Alpha = value;
                this.OnPropertyChanged();
            }
        }

        public bool IsEnabled
        {
            get { return Handle != IntPtr.Zero && Enabled; }
            set
            {
                if (Handle == IntPtr.Zero || Enabled == value) return;
                Enabled = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(Enabled));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageCreationOptions Options { get; set; }

        public string FilePath
        {
            get { return _filePath; }
            set
            {
                if (_filePath == value) return;
                SetImageDrawable(null);
                _filePath = value;

                if (_filePath != null)
                {
                    ImageGetter.SetDrawable(_filePath, (d, url) =>
                    {
                        if (url != _filePath) return;
                        SetImageDrawable(d);
                        Loaded?.Invoke(this, EventArgs.Empty);
                        this.RequestResize();
                    }, Options);
                }
                else
                {
                    this.RequestResize();
                }

                this.OnPropertyChanged(nameof(StringValue));
                this.OnPropertyChanged();
            }
        }
        private string _filePath;

        #endregion

        #region Submission

        public string StringValue => FilePath;

        public string SubmitKey
        {
            get { return _submitKey; }
            set
            {
                if (_submitKey == value) return;
                _submitKey = value;
                this.OnPropertyChanged();
            }
        }
        private string _submitKey;

        public event ValidationEventHandler Validating;

        public bool Validate(out string[] errors)
        {
            var handler = Validating;
            if (handler != null)
            {
                var args = new ValidationEventArgs(SubmitKey, FilePath, StringValue);
                handler(Pair ?? this, args);

                if (args.Errors.Count > 0)
                {
                    errors = new string[args.Errors.Count];
                    args.Errors.CopyTo(errors, 0);
                    return false;
                }
            }

            errors = null;
            return true;
        }

        #endregion

        #region Layout

        public new Visibility Visibility
        {
            get { return _visibility; }
            set
            {
                if (value == _visibility || Handle == IntPtr.Zero) return;
                var oldVisibility = _visibility;
                _visibility = value;
                switch (value)
                {
                    case Visibility.Visible:
                        base.Visibility = ViewStates.Visible;
                        break;
                    case Visibility.Hidden:
                        base.Visibility = ViewStates.Invisible;
                        break;
                    case Visibility.Collapsed:
                        base.Visibility = ViewStates.Gone;
                        break;
                }
                this.OnPropertyChanged();
                this.RequestResize(oldVisibility == Visibility.Collapsed || _visibility == Visibility.Collapsed);
            }
        }
        private Visibility _visibility;

        public Thickness Margin
        {
            get { return _margin; }
            set
            {
                if (_margin == value) return;
                _margin = value;
                this.OnPropertyChanged();
            }
        }
        private Thickness _margin;

        public int ColumnIndex
        {
            get { return _columnIndex; }
            set
            {
                if (value == _columnIndex) return;
                _columnIndex = value;
                this.OnPropertyChanged();
            }
        }
        private int _columnIndex = Element.AutoLayoutIndex;

        public int ColumnSpan
        {
            get { return _columnSpan; }
            set
            {
                if (value == _columnSpan) return;
                _columnSpan = value;
                this.OnPropertyChanged();
            }
        }
        private int _columnSpan = 1;

        public int RowIndex
        {
            get { return _rowIndex; }
            set
            {
                if (value == _rowIndex) return;
                _rowIndex = value;
                this.OnPropertyChanged();
            }
        }
        private int _rowIndex = Element.AutoLayoutIndex;

        public int RowSpan
        {
            get { return _rowSpan; }
            set
            {
                if (_rowSpan == value) return;
                _rowSpan = value;
                this.OnPropertyChanged();
            }
        }
        private int _rowSpan = 1;

        public HorizontalAlignment HorizontalAlignment
        {
            get { return _horizontalAlignment; }
            set
            {
                if (value == _horizontalAlignment) return;
                _horizontalAlignment = value;
                this.OnPropertyChanged();
            }
        }
        private HorizontalAlignment _horizontalAlignment;

        public VerticalAlignment VerticalAlignment
        {
            get { return _verticalAlignment; }
            set
            {
                if (value == _verticalAlignment) return;
                _verticalAlignment = value;
                this.OnPropertyChanged();
            }
        }
        private VerticalAlignment _verticalAlignment;

        public Size Dimensions
        {
            get
            {
                var d = Drawable as BitmapDrawable;
                if (d?.Bitmap == null && FilePath != null && FilePath.StartsWith("/"))
                {
                    var orientation = 0;
                    if (FilePath.EndsWith(".jpg") || FilePath.EndsWith(".jpeg"))
                    {
                        var exif = new Android.Media.ExifInterface(FilePath);
                        orientation = exif.GetAttributeInt(Android.Media.ExifInterface.TagOrientation, 1);
                    }
                    var options = new BitmapFactory.Options { InJustDecodeBounds = true, };
                    using (BitmapFactory.DecodeFile(FilePath, options))
                        switch (orientation)
                        {
                            case 6:
                            case 8:
                                return new Size(options.OutHeight, options.OutWidth);
                            default:
                                return new Size(options.OutWidth, options.OutHeight);
                        }
                }
                if (d == null) return new Size(1, 1);

                ImageData cachedImage;
                var bitmap = d.Bitmap ?? ((cachedImage = (ImageData)Device.ImageCache.Get(_filePath)) != null ? cachedImage.Bitmap : null);
                return bitmap != null ? new Size(bitmap.Width, bitmap.Height) : new Size(d.IntrinsicWidth, d.IntrinsicHeight);
            }
        }

        public ContentStretch Stretch
        {
            get { return _stretch; }
            set
            {
                switch (value)
                {
                    case ContentStretch.None:
                        SetScaleType(ScaleType.CenterInside);
                        break;
                    case ContentStretch.Fill:
                        SetScaleType(ScaleType.FitXy);
                        break;
                    case ContentStretch.Uniform:
                        SetScaleType(ScaleType.FitCenter);
                        break;
                    case ContentStretch.UniformToFill:
                        SetScaleType(ScaleType.CenterCrop);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("value");
                }
                if (_stretch == value) return;
                _stretch = value;
                this.OnPropertyChanged();
            }
        }
        private ContentStretch _stretch;

        public Size Measure(Size constraints)
        {
            if (string.IsNullOrEmpty(FilePath))
                return new Size();
            var originalSize = Dimensions;
            if (originalSize == new Size(1, 1))
                return originalSize;

            if (constraints.Height < .001)
            {
                return new Size(MeasuredWidth, MeasuredHeight);
            }

            Size imageSize;
            var ratio = originalSize.Width / originalSize.Height;
            if (_stretch != ContentStretch.Fill)
            {
                double width = originalSize.Width, height = originalSize.Height;

                if (constraints.Height > 0 && originalSize.Height > constraints.Height)
                {
                    height = constraints.Height;
                    width = height * ratio;
                }

                if (constraints.Width > 0 && width > constraints.Width)
                {
                    width = constraints.Width;
                    height = width / ratio;
                }

                double scale;
                switch (_stretch)
                {
                    case ContentStretch.Uniform:
                        scale = Math.Min(constraints.Width / width, constraints.Height / height);
                        break;
                    case ContentStretch.UniformToFill:
                        scale = Math.Max(constraints.Width / width, constraints.Height / height);
                        break;
                    default:
                        scale = 1;
                        break;
                }

                imageSize = new Size(width * scale, height * scale);
            }
            else
            {
                imageSize = constraints;
            }

            if (imageSize.Height > int.MaxValue)
            {
                imageSize.Height = imageSize.Width / ratio;
            }

            if (imageSize.Width > int.MaxValue)
            {
                imageSize.Width = imageSize.Height * ratio;
            }

            this.MeasureView(imageSize);
            return imageSize;
        }

        /// <summary>
        /// Sets the location and size of the control within its parent grid.
        /// This is called by the underlying grid layout system and should not be used in application logic.
        /// </summary>
        /// <param name="location">The X and Y coordinates of the upper left corner of the control.</param>
        /// <param name="size">The width and height of the control.</param>
        public void SetLocation(Point location, Size size)
        {
            var left = location.X;
            var top = location.Y;
            var right = left + size.Width;
            var bottom = top + size.Height;

            Layout((int)left, (int)top, (int)right, (int)bottom);
        }

        #endregion

        #region Identity

        public string ID
        {
            get { return _id; }
            set
            {
                if (_id == value) return;
                _id = value;
                this.OnPropertyChanged();
            }
        }
        private string _id;

        object IElement.Parent
        {
            get
            {
                var parent = Parent;
                return (parent as IPairable)?.Pair ?? parent ?? Metadata.Get<object>("Parent");
            }
        }

        public IPairable Pair
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
        private IPairable _pair;

        public MetadataCollection Metadata => _metadata ?? (_metadata = new MetadataCollection());
        private MetadataCollection _metadata;

        public bool Equals(IElement other)
        {
            var control = other as Element;
            return control?.Equals(this) ?? ReferenceEquals(this, other);
        }

        #endregion

        public event EventHandler Clicked
        {
            add
            {
                if (_clicked == null)
                {
                    Click += Image_Click;
                }

                _clicked += value;
            }
            remove { _clicked -= value; }
        }
        private event EventHandler _clicked;

        public event EventHandler Loaded;

        public IImageData GetImageData()
        {
            return Device.ImageCache.Get(FilePath);
        }
    }
}