using Android.Runtime;
using Android.Views;
using Android.Widget;
using iFactr.UI;
using System;

namespace iFactr.Droid
{
    public class ToolbarButton : ButtonBase, IToolbarButton
    {
        [Preserve]
        public ToolbarButton()
        {
            _button.Click += OnClick;
        }

        public string ImagePath
        {
            get { return _imagePath; }
            set
            {
                if (_imagePath == value) return;
                if (value == null)
                {
                    LongClick -= OnLongClick;
                }
                else if (_imagePath == null)
                {
                    LongClick += OnLongClick;
                }
                _imagePath = value;

                ImageGetter.SetDrawable(_imagePath, (bitmap, url) =>
                {
                    if (url == _imagePath)
                        _button.SetCompoundDrawables(bitmap, null, null, null);
                });

                this.OnPropertyChanged();
            }
        }
        private string _imagePath;

        private void OnLongClick(object o, LongClickEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Title))
                Toast.MakeText(Context, Title, ToastLength.Short).Show();
        }

        private void OnClick(object sender, EventArgs e)
        {
            if (this.RaiseEvent("Clicked", EventArgs.Empty)) return;
            var toolContainer = Parent as LinearLayout;
            var view = toolContainer?.Parent as Toolbar;
            DroidFactory.Navigate(NavigationLink, view?.Parent);
        }

        public bool Equals(IToolbarButton other)
        {
            var item = other as UI.ToolbarButton;
            return item?.Equals(this) ?? ReferenceEquals(this, other);
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);

            var width = right - left;
            var height = bottom - top;
            var widthSpec = MeasureSpec.MakeMeasureSpec(width, MeasureSpecMode.Exactly);
            var heightSpec = MeasureSpec.MakeMeasureSpec(height, MeasureSpecMode.Exactly);
            _button.Measure(widthSpec, heightSpec);
            _button.Layout(-Padding, -Padding, width + Padding, height + Padding);
        }
    }
}