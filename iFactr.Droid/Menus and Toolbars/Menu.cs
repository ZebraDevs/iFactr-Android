using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Views;
using iFactr.UI;
using IMenu = iFactr.UI.IMenu;
using Android.Runtime;
using MonoCross.Utilities;
using MonoCross.Navigation;
using iFactr.Core;

namespace iFactr.Droid
{
    public class Menu : Java.Lang.Object, IMenu, IDialogInterfaceOnClickListener
    {
        [Preserve]
        public Menu() { }

        [Preserve]
        public Menu(params IMenuButton[] buttons)
        {
            _buttons = buttons.ToList();
        }

        #region MonoView members

        public string ImagePath
        {
            get { return null; }
            set { }
        }

        public string Title
        {
            get { return null; }
            set { }
        }

        public Color BackgroundColor { get; set; }

        public Color ForegroundColor { get; set; }

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

        public bool Equals(IMenu other)
        {
            var menu = other as UI.Menu;
            return menu?.Equals(this) ?? ReferenceEquals(this, other);
        }

        #endregion

        #region IMenu members

        public Color SelectionColor
        {
            get;
            set;
        }

        public int ButtonCount => _buttons.Count;
        private readonly List<IMenuButton> _buttons = new List<IMenuButton>();

        public void Add(IMenuButton menuItem)
        {
            if (menuItem != null) _buttons.Add(menuItem);

            var button = menuItem?.Pair as MenuButton ?? menuItem as MenuButton;
            if (button == null) return;
            button.ForegroundColor = ForegroundColor;
        }

        public IMenuButton GetButton(int index)
        {
            if (index < 0 || index >= ButtonCount) return null;
            return _buttons[index];
        }

        public void Focus()
        {
            var homeView = DroidFactory.HomeUpView;
            if (homeView == null) return;
            var actionBarView = (ViewGroup)homeView.Parent;
            var hasTabs = PaneManager.Instance.FromNavContext(Pane.Master, 1) != null;
            if (actionBarView.ChildCount <= (hasTabs ? 2 : 1)) return;
            var actionMenu = actionBarView.GetChildAt(actionBarView.ChildCount - 1);
            actionMenu.RequestFocus();
        }

        #endregion

        public void OnClick(IDialogInterface dialog, int which)
        {
            IMXView view = null;
            for (var p = Pane.Popover; p > Pane.Tabs; p--)
            {
                var check = PaneManager.Instance.FromNavContext(p)?.CurrentView;
                var menu = (check as IListView)?.Menu ?? (check as IGridView)?.Menu ?? (check as IBrowserView)?.Menu;
                if (!Equals(menu?.Pair, this) && menu != this) continue;
                view = check;
                break;
            }
            OnClick(this, which, view);
        }

        public static void OnClick(IMenu menu, int index, IMXView view)
        {
            if (menu.ButtonCount <= 0) return;
            var button = menu.GetButton(index);
            if (!button.RaiseEvent("Clicked", EventArgs.Empty) && (button.Pair == null || !button.Pair.RaiseEvent("Clicked", EventArgs.Empty)))
                DroidFactory.Navigate(button.NavigationLink, view);
            Device.Log.Platform($"Clicked menu item \"{button.Title}\"");
        }

        public static void Activated(object sender, EventArgs eventArgs)
        {
            var menu = sender as IMenu;
            if (menu == null)
            {
                var popover = PaneManager.Instance.FromNavContext(Pane.Popover)?.CurrentView;
                menu = (popover as IListView)?.Menu ?? (popover as IGridView)?.Menu ?? (popover as IBrowserView)?.Menu;
            }
            if (menu == null || menu.ButtonCount <= 0) return;
            var items = new string[menu.ButtonCount];
            for (var i = 0; i < menu.ButtonCount; i++)
                items[i] = menu.GetButton(i).Title;
            var builder = new AlertDialog.Builder(DroidFactory.MainActivity)
                .SetItems(items, DroidFactory.GetNativeObject<Menu>(menu, nameof(menu)));
            builder.Create().Show();
        }
    }
}