using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using MonoCross;
using MonoCross.Utilities;
using iFactr.UI;
using iFactr.UI.Controls;
using Point = iFactr.UI.Point;
using Size = iFactr.UI.Size;
using Android.App;
using MonoCross.Utilities.Storage;

namespace iFactr.Droid
{
    public class AnimationView : SurfaceView, IImage, INotifyPropertyChanged, ISurfaceHolderCallback
    {
        private readonly Paint _paint = new Paint(PaintFlags.FilterBitmap);
        private Movie _gifMovie;
        private float _movieWidth, _movieHeight;
        private long _movieStart;

        #region Constructors

        [Preserve]
        public AnimationView()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public AnimationView(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public AnimationView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize(attrs);
        }

        [Preserve]
        public AnimationView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize(attrs);
        }

        protected AnimationView(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }


        [Preserve]
        public AnimationView(ImageCreationOptions options)
            : this()
        {
            Options = options;
        }

        [Preserve]
        public AnimationView(string filePath)
            : this()
        {
            FilePath = filePath;
        }

        [Preserve]
        public AnimationView(int resourceId)
            : this()
        {
            var inputStream = Context.Resources.OpenRawResource(resourceId);
            SetAnimationStream(inputStream);
        }

        private void Initialize(IAttributeSet attrs = null)
        {
            Focusable = true;
            Holder.AddCallback(this);
            SetZOrderOnTop(true);
            Holder.SetFormat(Format.Transparent);

            if (attrs == null)
            {
                _gifMovie = null;
                _movieWidth = 0;
                _movieHeight = 0;
            }
            else
            {
                this.InitializeAttributes(attrs);
                string filePath;
                var resourceId = attrs.GetAttributeResourceValue("http://schemas.android.com/apk/res/android", "src", 0);
                if (resourceId > 0)
                {
                    var inputStream = Context.Resources.OpenRawResource(resourceId);
                    SetAnimationStream(inputStream);
                }
                else if (!string.IsNullOrEmpty((filePath = attrs.GetAttributeValue(ElementExtensions.XmlNamespace, "file_path"))))
                {
                    FilePath = filePath;
                }
            }
        }

        #endregion

        private void Image_Click(object sender, EventArgs e)
        {
            var click = _clicked;
            if (IsEnabled && click != null)
            {
                click(sender, e);
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
            DurationLapsed = null;
        }

        public void SetAnimationStream(Stream inputStream)
        {
            if (inputStream == null)
            {
                _gifMovie = null;
                _movieWidth = 0;
                _movieHeight = 0;
            }
            else
            {
                _gifMovie = Movie.DecodeStream(inputStream);
                if (_gifMovie != null)
                {
                    _movieWidth = _gifMovie.Width();
                    _movieHeight = _gifMovie.Height();
                }
                else
                {
                    _movieWidth = 0;
                    _movieHeight = 0;
                }

                Loaded?.Invoke(this, EventArgs.Empty);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageCreationOptions Options { get; set; }

        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                if (_filePath == value) return;
                _filePath = value;
                this.OnPropertyChanged();
                if (_filePath == null) return;
                Stream inputStream;
                int res = ((AndroidFile)Device.File).ResourceFromFileName(_filePath);
                if (res > 0)
                {
                    inputStream = Context.Resources.OpenRawResource(res);
                }
                else
                {
                    inputStream = new MemoryStream(Device.File.Read(_filePath));
                }
                SetAnimationStream(inputStream);
            }
        }
        private string _filePath;

        private readonly object _lock = new object();
        private async Task AnimateGif(object token)
        {
            var tk = (CancellationToken)token;
            while (!tk.IsCancellationRequested)
            {
                var now = TimeSpan.FromTicks(DateTime.UtcNow.Ticks).TotalMilliseconds;
                if (_movieStart == 0)
                {
                    _movieStart = Convert.ToInt64(now);
                }

                int dur = 0;
                var holder = Holder;
                var canvas = holder.LockCanvas();
                if (canvas == null) return;

                var xScale = canvas.Width / _movieWidth;
                var yScale = canvas.Height / _movieHeight;
                float scale, xOffset = 0, yOffset = 0;
                if (xScale > yScale)
                {
                    scale = xScale;
                    yOffset = (_movieHeight - canvas.Height / xScale) / -2;
                }
                else
                {
                    scale = yScale;
                    xOffset = (_movieWidth - canvas.Width / yScale) / -2;
                }

                canvas.DrawColor(Android.Graphics.Color.Transparent, PorterDuff.Mode.Clear);
                canvas.Scale(scale, scale);
                var elapsed = now - _movieStart;

                lock (_lock)
                {
                    if (_gifMovie != null)
                    {
                        dur = _gifMovie.Duration();
                        _gifMovie.SetTime((int)Math.Min(elapsed, dur));
                        _gifMovie.Draw(canvas, xOffset, yOffset, _paint);
                    }
                }

                int[] animationLocation = new int[2];
                int[] parentLocation = new int[2];
                GetLocationOnScreen(animationLocation);
                ((base.Parent as GridBase)?.Parent as Fragment)?.View.GetLocationOnScreen(parentLocation);
                var offset = animationLocation[1] - parentLocation[1];
                if (offset < 0)
                {
                    var clearPaint = new Paint();
                    clearPaint.SetXfermode(new PorterDuffXfermode(PorterDuff.Mode.Clear));
                    canvas.DrawRect(0, 0, canvas.Width / scale, -offset / scale, clearPaint);
                }

                holder.UnlockCanvasAndPost(canvas);

                var then = TimeSpan.FromTicks(DateTime.UtcNow.Ticks).TotalMilliseconds;
                var runtime = 1000.0 / 60 - then + now;
                if (runtime > 0)
                {
                    await Task.Delay((int)runtime, tk).ContinueWith(tsk => { }); ;
                }

                if (elapsed < dur || _durationElapsed) continue;
                _durationElapsed = true;
                DurationLapsed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Cleanup()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            lock (_lock)
            {
                if (_gifMovie != null)
                {
                    _gifMovie.Dispose();
                    _gifMovie = null;
                }
            }
            _filePath = null;

            Device.Thread.ExecuteOnMainThread(() =>
            {
                ((ViewGroup)base.Parent)?.RemoveView(this);
            });
        }

        public void Restart()
        {
            _movieStart = Convert.ToInt64(TimeSpan.FromTicks(DateTime.UtcNow.Ticks).TotalMilliseconds);
            _durationElapsed = false;
        }

        private bool _durationElapsed;

        public event EventHandler DurationLapsed;

        public bool IsEnabled { get; set; }

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

        public Size Dimensions => new Size(_movieWidth, _movieHeight);

        public ContentStretch Stretch
        {
            get { return _stretch; }
            set
            {
                if (_stretch == value) return;
                _stretch = value;
                this.OnPropertyChanged();
            }
        }
        private ContentStretch _stretch;

        public Size Measure(Size constraints)
        {
            var originalSize = Dimensions;

            if (constraints.Height < .001)
            {
                return originalSize;
            }

            Size imageSize;
            var ratio = originalSize.Width / originalSize.Height;
            if (_stretch == ContentStretch.None)
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

                imageSize = new Size(width, height);
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

        public new object Parent => base.Parent ?? Metadata.Get<object>("Parent");

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
            throw new NotImplementedException();
        }

        public void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
        {
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            _cts = new CancellationTokenSource();
            Task.Factory.StartNew(AnimateGif, _cts.Token, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            if (_cts == null) return;
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        private CancellationTokenSource _cts;
    }
}