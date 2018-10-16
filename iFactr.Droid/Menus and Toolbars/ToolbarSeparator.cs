using System;
using System.ComponentModel;
using Android.Widget;
using iFactr.UI;
using Android.Runtime;

namespace iFactr.Droid
{
    public class ToolbarSeparator : Android.Views.View, IToolbarSeparator, INotifyPropertyChanged
    {
        [Preserve]
        public ToolbarSeparator()
            : base(DroidFactory.MainActivity)
        {
            base.LayoutParameters = new LinearLayout.LayoutParams(1, LinearLayout.LayoutParams.MatchParent);
        }

        /// <summary>
        /// Gets or sets the color of the separator.
        /// </summary>
        /// <value>The color of the foreground.</value>
        public Color ForegroundColor
        {
            get { return _foregroundColor; }
            set
            {
                if (_foregroundColor == value || Handle == IntPtr.Zero) return;
                _foregroundColor = value;
                SetBackgroundColor(_foregroundColor.ToColor());
                this.OnPropertyChanged();
            }
        }
        private Color _foregroundColor;

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

        public bool Equals(IToolbarSeparator other)
        {
            var item = other as UI.ToolbarSeparator;
            return item?.Equals(this) ?? ReferenceEquals(this, other);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}