using Android.Runtime;
using Android.Views;
using iFactr.UI;
using System;

namespace iFactr.Droid
{
    public class MenuButton : IMenuButton
    {
        public MenuButton() { }

        [Preserve]
        public MenuButton(string title)
        {
            Title = title;
        }

        public event EventHandler Clicked;

        public string Title { get; }

        public Link NavigationLink { get; set; }

        public Color ForegroundColor { get; set; }

        public IMenuItem Item
        {
            get { return _item; }
            set
            {
                _item = value;
                _item.SetShowAsAction(ShowAsAction);
                LoadImage();
            }
        }
        private IMenuItem _item;

        public string ImagePath
        {
            get { return _imagePath; }
            set
            {
                if (_imagePath == value) return;
                _imagePath = value;
                LoadImage();
            }
        }
        private string _imagePath;

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

        public ShowAsAction ShowAsAction
        {
            get { return _showAsAction; }
            set
            {
                _showAsAction = value;
                Item?.SetShowAsAction(_showAsAction);
            }
        }
        private ShowAsAction _showAsAction;

        public virtual void AddToParent(Android.Views.IMenu parent, int menuId, int index, bool showIfRoom)
        {
            var itemId = menuId * byte.MaxValue + index;
            ShowAsAction = showIfRoom ? ShowAsAction.IfRoom : ShowAsAction.Never;
            Item = parent.Add(menuId, itemId, menuId, Title);
        }

        public bool Equals(IMenuButton other)
        {
            var item = other as UI.MenuButton;
            return item?.Equals(this) ?? ReferenceEquals(this, other);
        }

        protected virtual void LoadImage()
        {
            if (Item != null && _imagePath != null)
            {
                ImageGetter.SetDrawable(_imagePath, (bitmap, url, fromCache) =>
                {
                    if (bitmap != null && url == _imagePath)
                        Item.SetIcon(bitmap);
                });
            }
        }
    }
}