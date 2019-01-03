using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using MonoCross.Utilities;
using iFactr.UI;
using iFactr.UI.Controls;
using Point = iFactr.UI.Point;
using Size = iFactr.UI.Size;

namespace iFactr.Droid
{
    public class Accessory : Android.Widget.Button, IControl
    {
        private static readonly int HitArea = (int)(Cell.StandardCellHeight * DroidFactory.DisplayScale);

        public virtual string FontPath
        {
            get
            {
                if (!System.IO.File.Exists(_fontPath))
                {
                    var info = new System.IO.FileInfo(_fontPath);
                    var font = Device.Resources.GetObject(info.Name.Remove(info.Name.Length - info.Extension.Length)) as byte[];
                    Device.File.Save(_fontPath, font, EncryptionMode.NoEncryption);
                }
                return _fontPath;
            }
            set
            {
                _fontPath = value;
                SetTypeface(Typeface.CreateFromFile(FontPath), TypefaceStyle.Normal);
            }
        }
        private string _fontPath = System.IO.Path.Combine(Device.DataPath, "fonts", "AndroidSymbols.ttf");

        public virtual UI.Color ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                _foregroundColor = value;
                SetTextColor(_foregroundColor.ToColor());
            }
        }
        private UI.Color _foregroundColor = new UI.Color(190, 190, 190);

        public virtual string Glyph { get; set; } = ""; // ⓘ

        [Preserve]
        public Accessory()
            : this(DroidFactory.MainActivity)
        {
            Initialize();
        }

        protected Accessory(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public Accessory(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public Accessory(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize(attrs);
        }

        [Preserve]
        public Accessory(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize(attrs);
        }

        private void Initialize(IAttributeSet attrs = null)
        {
            Focusable = false;
            this.InitializeAttributes(attrs);
            SetBackgroundColor(Android.Graphics.Color.Transparent);
            SetTextColor(_foregroundColor.ToColor());
            SetTypeface(Typeface.CreateFromFile(FontPath), TypefaceStyle.Normal);
            Text = Glyph;
            SetIncludeFontPadding(false);
            SetPadding(0, 0, 0, 0);
            SetMinHeight(0);
            SetMinWidth(0);
            SetWidth(HitArea);
            SetHeight(HitArea);
            SetTextSize(ComplexUnitType.Dip, 22);
        }

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
                        BringToFront();
                        break;
                    case Visibility.Hidden:
                        base.Visibility = ViewStates.Invisible;
                        break;
                    case Visibility.Collapsed:
                        base.Visibility = ViewStates.Gone;
                        break;
                }
                this.RequestResize(oldVisibility == Visibility.Collapsed || _visibility == Visibility.Collapsed);
            }
        }
        private Visibility _visibility;

        public bool IsEnabled
        {
            get { return Handle != IntPtr.Zero && Enabled; }
            set
            {
                if (Handle != IntPtr.Zero && Enabled != value)
                    Enabled = value;
            }
        }

        public Link Link { get; set; }

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

        public bool Equals(IElement other)
        {
            var control = other as Element;
            return control?.Equals(this) ?? ReferenceEquals(this, other);
        }

        public int ColumnIndex { get; set; } = Element.AutoLayoutIndex;

        public int ColumnSpan { get; set; } = 1;

        public HorizontalAlignment HorizontalAlignment
        {
            get;
            set;
        } = HorizontalAlignment.Right;

        public string ID { get; set; }

        object IElement.Parent
        {
            get
            {
                var parent = Parent;
                return (parent as IPairable)?.Pair ?? parent ?? Metadata.Get<object>("Parent");
            }
        }

        public MetadataCollection Metadata => _metadata ?? (_metadata = new MetadataCollection());
        private MetadataCollection _metadata;

        public Thickness Margin { get; set; }

        public int RowIndex { get; set; } = Element.AutoLayoutIndex;

        public int RowSpan { get; set; } = 1;

        public string StringValue => Link?.Address;

        public string SubmitKey
        {
            get;
            set;
        }

        public VerticalAlignment VerticalAlignment
        {
            get;
            set;
        } = VerticalAlignment.Center;

        public event ValidationEventHandler Validating;

        public Size Measure(Size constraints)
        {
            var widthSpec = MeasureSpec.MakeMeasureSpec(HitArea, MeasureSpecMode.Exactly);
            var heightSpec = MeasureSpec.MakeMeasureSpec(HitArea, MeasureSpecMode.Exactly);
            Measure(widthSpec, heightSpec);

            return new Size(MeasuredWidth, MeasuredHeight);
        }

        public void NullifyEvents()
        {
            Validating = null;
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
            var right = location.X + HitArea;
            var top = location.Y;
            var bottom = location.Y + HitArea;

            Layout((int)left, (int)top, (int)right, (int)bottom);
        }

        public bool Validate(out string[] errors)
        {
            errors = null;
            return true;
        }
    }
}