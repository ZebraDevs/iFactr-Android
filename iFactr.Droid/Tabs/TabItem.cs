using System;
using System.ComponentModel;
using iFactr.UI;
using Android.Runtime;
using iFactr.Core;

namespace iFactr.Droid
{
    public class TabItem : ITabItem, INotifyPropertyChanged
    {
        [Preserve]
        public TabItem() { }

        public event EventHandler Selected;

        public string BadgeValue
        {
            get { return string.Empty; }
            set { }
        }

        public string ImagePath
        {
            get { return _imagePath; }
            set
            {
                if (_imagePath == value) return;
                _imagePath = value;
                this.OnPropertyChanged();
            }
        }
        private string _imagePath;

        public Link NavigationLink
        {
            get { return _navigationLink; }
            set
            {
                if (_navigationLink == value) return;
                _navigationLink = value;
                this.OnPropertyChanged();
            }
        }
        private Link _navigationLink;

        public string Title
        {
            get { return _title; }
            set
            {
                if (_title == value) return;
                _title = value;
                this.OnPropertyChanged();
            }
        }
        private string _title;

        public Color TitleColor
        {
            get { return new Color(); }
            set { }
        }

        public Font TitleFont
        {
            get { return Font.PreferredTabFont; }
            set { }
        }

        public IPairable Pair
        {
            get { return _pair; }
            set
            {
                if (_pair != null || value == null) return;
                _pair = value;
                _pair.Pair = this;
            }
        }
        private IPairable _pair;

        public bool Equals(ITabItem other)
        {
            var item = other as UI.TabItem;
            return item?.Equals(this) ?? ReferenceEquals(this, other);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Activate(ITabView tabView)
        {
            if (!OnSelected())
            {
                iApp.Navigate(NavigationLink, tabView);
            }
        }

        public bool OnSelected()
        {
            var selected = Selected;
            if (selected == null) return false;
            selected(Pair, EventArgs.Empty);
            return true;
        }
    }
}