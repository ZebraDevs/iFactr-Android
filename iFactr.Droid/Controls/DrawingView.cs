using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Views;
using Android.Graphics;
using Android.Widget;
using MonoCross;
using MonoCross.Utilities;
using iFactr.UI;
using Path = Android.Graphics.Path;
using Android.Runtime;
using View = Android.Views.View;
using iFactr.Core;

namespace iFactr.Droid
{
    public class DrawingView : View, IPairable, INotifyPropertyChanged
    {
        private Canvas ForegroundMutator => _foregroundMutator ?? (_foregroundMutator = new Canvas(_mutableForeground));
        private Canvas _foregroundMutator;

        private readonly Paint _bitmapPaint = new Paint(PaintFlags.AntiAlias);
        private Bitmap _scaledBackground;

        private Size _canvasSize;
        private UI.Point _currentTouch;
        private Path _currentStroke;

        private readonly Paint _strokeBrush = new Paint
        {
            AntiAlias = true,
            Dither = true,
            StrokeJoin = Paint.Join.Round,
            StrokeCap = Paint.Cap.Round,
        };

        #region Canvas properties

        /// <summary>
        /// Gets or sets the number of pixels that a stroke must exceed before it is registered by the canvas.
        /// </summary>
        public float TouchTolerance
        {
            get { return _touchTolerance; }
            set
            {
                if (value == _touchTolerance) return;
                _touchTolerance = value;
                this.OnPropertyChanged();
            }
        }
        private float _touchTolerance;

        /// <summary>
        /// Gets or sets the color of the strokes when drawing.
        /// </summary>
        public UI.Color StrokeColor
        {
            get { return _strokeBrush.Color.ToColor(); }
            set
            {
                var val = (value.IsDefaultColor ? UI.Color.Black : value).ToColor();
                if (_strokeBrush.Color == val) return;
                _strokeBrush.Color = val;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the thickness of the strokes when drawing.
        /// </summary>
        public double StrokeThickness
        {
            get { return _strokeBrush.StrokeWidth; }
            set
            {
                var val = (float)(value * DroidFactory.DisplayScale);
                if (_strokeBrush.StrokeWidth == val) return;
                _strokeBrush.StrokeWidth = val;
                this.OnPropertyChanged();
            }
        }

        #endregion

        #region Constructors

        [Preserve]
        public DrawingView()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public DrawingView(Context context) :
            base(context)
        {
            Initialize();
        }

        [Preserve]
        public DrawingView(Context context, Android.Util.IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
        }

        [Preserve]
        public DrawingView(Context context, Android.Util.IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
            Initialize();
        }

        public DrawingView(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        private void Initialize()
        {
            StrokeColor = UI.Color.Black;
            StrokeThickness = 4;
            TouchTolerance = 4;

            _currentStroke = new Path();
        }

        #endregion

        #region Drawing overrides

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if (_backgroundPath != null && _backgroundImage == null)
            {
                SetBackground(ImageGetter.LoadFromStorage(_backgroundPath, canvas.Width, canvas.Height));
            }

            if (_foregroundPath != null)
            {
                SetForeground(ImageGetter.LoadFromStorage(_foregroundPath, canvas.Width, canvas.Height));
                _foregroundPath = null;
            }
            else if (_mutableForeground == null)
            {
                SetForeground(null);
            }

            DrawBackground(canvas);

            if (_mutableForeground == null || _mutableForeground.IsRecycled) return;
            var left = (_canvasSize.Width - _mutableForeground.Width) / 2;
            var top = (_canvasSize.Height - _mutableForeground.Height) / 2;
            canvas.DrawBitmap(_mutableForeground, (float)left, (float)top, _bitmapPaint);
            canvas.DrawPath(_currentStroke, _strokeBrush);
        }

        private void DrawBackground(Canvas canvas)
        {
            if (_backgroundPrepared)
            {
                var bitmap = _scaledBackground ?? _backgroundImage;
                if (bitmap == null || bitmap.IsRecycled) return;

                var left = (_canvasSize.Width - bitmap.Width) / 2;
                var top = (_canvasSize.Height - bitmap.Height) / 2;
                canvas.DrawBitmap(bitmap, (float)left, (float)top, _bitmapPaint);
                return;
            }
            _backgroundPrepared = true;

            if (_backgroundImage == null) return;
            float backgroundWidth = _backgroundImage.Width;
            float backgroundHeight = _backgroundImage.Height;
            var scaled = false;

            if (backgroundWidth > _canvasSize.Width)
            {
                backgroundHeight = backgroundHeight * ((float)_canvasSize.Width / backgroundWidth);
                backgroundWidth = (float)_canvasSize.Width;
                scaled = true;
            }

            if (backgroundHeight > _canvasSize.Height)
            {
                backgroundWidth = backgroundWidth * ((float)_canvasSize.Height / backgroundHeight);
                backgroundHeight = (float)_canvasSize.Height;
                scaled = true;
            }

            if (scaled)
            {
                Recycle(_scaledBackground);
                _scaledBackground = Bitmap.CreateScaledBitmap(_backgroundImage, (int)backgroundWidth, (int)backgroundHeight, false);
            }

            DrawBackground(canvas);
        }
        private bool _backgroundPrepared;

        #endregion

        #region Handle touch

        public override bool OnTouchEvent(MotionEvent motionEvent)
        {
            var x = motionEvent.GetX();
            var y = motionEvent.GetY();

            switch (motionEvent.Action)
            {
                case MotionEventActions.Down:
                    _strokeBrush.SetStyle(Paint.Style.Stroke);
                    Invalidate();
                    break;
                case MotionEventActions.Move:
                    TouchMove(x, y);
                    Invalidate();
                    break;
                case MotionEventActions.Up:
                    TouchUp();
                    Invalidate();
                    x = y = 0;
                    break;
            }
            _currentTouch = new UI.Point(x, y);
            return true;
        }

        private void TouchMove(float x, float y)
        {
            var dx = (float)Math.Abs(x - _currentTouch.X);
            var dy = (float)Math.Abs(y - _currentTouch.Y);

            // add movement to stroke if outside the area of tolerence (a square with side length of TouchTolerance * 2, centered at currentTouch)
            if (!(dx >= TouchTolerance) && !(dy >= TouchTolerance))
            {
                return;
            }

            if (_currentStroke.IsEmpty)
            {
                _currentStroke.MoveTo((float)_currentTouch.X, (float)_currentTouch.Y);
            }

            _currentStroke.QuadTo((float)_currentTouch.X,
                (float)_currentTouch.Y,
                (float)(x + _currentTouch.X) / 2,
                (float)(y + _currentTouch.Y) / 2);
        }

        private void TouchUp()
        {
            var left = (_canvasSize.Width - _mutableForeground.Width) / 2;
            var top = (_canvasSize.Height - _mutableForeground.Height) / 2;

            if (_currentStroke.IsEmpty)
            {
                _strokeBrush.SetStyle(Paint.Style.Fill);
                ForegroundMutator.DrawCircle((float)(_currentTouch.X - left), (float)(_currentTouch.Y - top), (float)StrokeThickness / 2, _strokeBrush);
            }
            else
            {
                _currentStroke.LineTo((float)_currentTouch.X, (float)_currentTouch.Y);
                // Commit the path to our offscreen canvas
                var m = new Matrix();
                m.SetTranslate((float)-left, (float)-top);
                _currentStroke.Transform(m);
                ForegroundMutator.DrawPath(_currentStroke, _strokeBrush);
            }
            // Reset the stroke so that it is not redrawn in OnDraw.
            _currentStroke.Reset();
        }

        #endregion

        public void SetForeground(Bitmap foreground)
        {
            if (_mutableForeground == foreground && foreground != null) return;
            _foregroundMutator = null;
            Recycle(_mutableForeground);
            _mutableForeground = foreground == null ? Bitmap.CreateBitmap((int)_canvasSize.Width, (int)_canvasSize.Height, Bitmap.Config.Argb8888) : foreground.Copy(Bitmap.Config.Argb8888, true);
            Invalidate();
            this.OnPropertyChanged();
        }
        private Bitmap _mutableForeground;


        public void SetBackground(Bitmap background)
        {
            if (_backgroundImage == background) return;
            Recycle(_backgroundImage);
            _backgroundImage = background;
            _backgroundPrepared = false;
            Invalidate();
            this.OnPropertyChanged();
        }

        private Bitmap _backgroundImage;
        private string _foregroundPath;
        private string _backgroundPath;

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            _canvasSize = new Size(w, h);

            Bitmap foreground = null;
            try
            {
                var view = DroidFactory.MainActivity.FindViewById<ImageView>(((FragmentHistoryStack)((BaseFragment)Parent).Stack).BackgroundId);
                if (view == null) return;
                if (_backgroundPath != null && _backgroundImage == null)
                {
                    SetBackground(ImageGetter.LoadFromStorage(_backgroundPath, view.MeasuredWidth, view.MeasuredHeight));
                }

                if (_foregroundPath != null)
                {
                    foreground = ImageGetter.LoadFromStorage(_foregroundPath, view.MeasuredWidth, view.MeasuredHeight);
                    SetForeground(null);
                    _foregroundPath = null;
                }
                else
                {
                    foreground = _mutableForeground;
                    SetForeground(Bitmap.CreateBitmap((int)_canvasSize.Width, (int)_canvasSize.Height, Bitmap.Config.Argb8888));
                    this.OnPropertyChanged("MutableForeground");
                }
            }
            catch (Exception ex)
            {
                iApp.Log.Error(ex);
            }

            if (foreground == null) return;

            var foregroundWidth = foreground.Width;
            var foregroundHeight = foreground.Height;

            var scaled = false;

            if (foregroundWidth > _canvasSize.Width)
            {
                foregroundHeight = (int)(foregroundHeight * (_canvasSize.Width / foregroundWidth));
                foregroundWidth = (int)_canvasSize.Width;
                scaled = true;
            }

            if (foregroundHeight > _canvasSize.Height)
            {
                foregroundWidth = (int)(foregroundWidth * (_canvasSize.Height / foregroundHeight));
                foregroundHeight = (int)_canvasSize.Height;
                scaled = true;
            }

            if (scaled)
            {
                var fore = foreground;
                foreground = Bitmap.CreateScaledBitmap(foreground, foregroundWidth, foregroundHeight, false);
                fore.Recycle();
            }

            var left = foregroundWidth > _canvasSize.Width ? 0 : ((float)_canvasSize.Width - foregroundWidth) / 2;
            var top = foregroundHeight > _canvasSize.Height ? 0 : ((float)_canvasSize.Height - foregroundHeight) / 2;

            ForegroundMutator.DrawBitmap(foreground, left, top, _bitmapPaint);
            Recycle(foreground);
        }

        public void SetBackground(string imagePath, ContentStretch stretch)
        {
            ImageGetter.SetDrawable(imagePath, (drawable, url, fromCache) =>
            {
                if (drawable == null)
                {
                    SetBackground(null);
                    _backgroundPath = imagePath;
                    Invalidate();
                }
                else if (url == imagePath)
                {
                    SetBackground(drawable);
                }
            });
        }

        /// <summary>
        /// Clears the canvas of all foreground content.
        /// </summary>
        public void Clear()
        {
            try
            {
                SetForeground(null);
            }
            catch (Exception ex)
            {
                iApp.Log.Error(ex);
            }
        }

        /// <summary>
        /// Loads the specified file into the canvas.
        /// </summary>
        /// <param name="fileName">The full path of the file to load.</param>
        public void Load(string fileName)
        {
            _foregroundPath = fileName;
            Invalidate();
        }

        /// <summary>
        /// Saves the current drawing to the temp directory with a randomly generated file name.
        /// </summary>
        /// <param name="compositeBackground">Whether to include the background as part of the saved image.</param>
        public void Save(bool compositeBackground)
        {
            Save(System.IO.Path.Combine(DroidFactory.Instance.TempPath, Guid.NewGuid() + ".png"), compositeBackground);
        }

        /// <summary>
        /// Saves the current drawing to the specified file.
        /// </summary>
        /// <param name="fileName">The full path of the file in which to save the image.</param>
        public void Save(string fileName)
        {
            Save(fileName, false);
        }

        /// <summary>
        /// Saves the current drawing to the specified file.
        /// </summary>
        /// <param name="fileName">The full path of the file in which to save the image.</param>
        /// <param name="compositeBackground">Whether to include the background as part of the saved image.</param>
        public async void Save(string fileName, bool compositeBackground)
        {
            if (_mutableForeground == null)
            {
                // Nothing to save
                return;
            }

            iApp.Factory.ActivateLoadTimer("Saving...");

            await Task.Factory.StartNew(() =>
            {
                byte[] retval;
                if (compositeBackground && _backgroundImage != null)
                {
                    var image = _scaledBackground ?? _backgroundImage;
                    var left = ((float)_canvasSize.Width - image.Width) / 2;
                    var top = ((float)_canvasSize.Height - image.Height) / 2;

                    var b = _backgroundPath == null ? _backgroundImage.Copy(_backgroundImage.GetConfig(), true) : _backgroundImage;
                    var canvas = new Canvas(b);

                    canvas.DrawBitmap(_mutableForeground,
                        new Rect((int)left, (int)top, image.Width + (int)left, image.Height + (int)top),
                        new RectF(0, 0, b.Width, b.Height),
                        _bitmapPaint);

                    var stream = new MemoryStream();
                    b.Compress(Bitmap.CompressFormat.Png, 100, stream);
                    Recycle(b);

                    retval = stream.GetBuffer();
                }
                else
                {
                    //TODO: Background color composition
                    var stream = new MemoryStream();
                    _mutableForeground.Compress(Bitmap.CompressFormat.Png, 100, stream);
                    retval = stream.GetBuffer();
                }

                iApp.File.EnsureDirectoryExists(fileName);
                iApp.File.Save(fileName, retval, EncryptionMode.NoEncryption);
            });

            this.RaiseEvent("DrawingSaved", new SaveEventArgs(fileName));
            iApp.Factory.StopBlockingUserInput();
        }

        /// <summary>
        /// Occurs when the current drawing has been saved to disk.
        /// </summary>
        public event SaveEventHandler DrawingSaved;

        public new IView Parent { get; set; }

        public IPairable Pair
        {
            get { return _pair; }
            set
            {
                if (_pair != null) return;
                _pair = value;
                _pair.Pair = this;
            }
        }
        private IPairable _pair;

        public event PropertyChangedEventHandler PropertyChanged;

        internal void CleanUp()
        {
            if (!_cleaned) return;
            _cleaned = true;

            if (_backgroundPath != null && _backgroundImage != null)
            {
                _backgroundImage.Recycle();
                _backgroundImage = null;
            }

            Recycle(_scaledBackground, _mutableForeground);
        }

        private bool _cleaned;

        ~DrawingView()
        {
            CleanUp();
        }

        private void Recycle(params Bitmap[] bitmaps)
        {
            var b = bitmaps.Where(bitmap => bitmap != null).ToList();

            if (!b.Any())
                return;

            foreach (var bitmap in b)
                bitmap.Recycle();

            Java.Lang.JavaSystem.Gc();
            GC.Collect(0);
        }
    }
}