using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Graphics;
using Android.Util;
using Android.Views.InputMethods;
using Android.Widget;
using MonoCross;
using iFactr.Core.Layers;
using iFactr.Core.Targets.Settings;
using MonoCross.Utilities;
using MonoCross.Utilities.ImageComposition;
using MonoCross.Utilities.Resources;
using iFactr.Integrations;
using iFactr.UI;
using iFactr.UI.Controls;
using iFactr.UI.Instructions;
using MonoCross.Navigation;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using IMenu = iFactr.UI.IMenu;
using Path = System.IO.Path;
using Uri = Android.Net.Uri;
using View = Android.Views.View;
using iFactr.Core;

namespace iFactr.Droid
{
    public class DroidFactory : Core.Native.NativeFactory
    {
        #region Properties
        public static Activity MainActivity
        {
            get { return PopoverActivity.Instance ?? AndroidDevice.Instance?.Context; }
            set
            {
                value.RequestWindowFeature(WindowFeatures.IndeterminateProgress);
                if (!IsInitialized)
                {
                    Device.Initialize(new AndroidDevice(value));
                    Initialize(new DroidFactory());
                }
                else
                {
                    AndroidDevice.Instance.Context = value;
                }

                var metrics = new DisplayMetrics();
                MainActivity.WindowManager.DefaultDisplay.GetMetrics(metrics);
                DisplayScale = metrics.Density;
            }
        }

        #region Tabs

        public static ITabView Tabs
        {
            get { return _tabs; }
            set
            {
                if (value != _tabs)
                {
                    (PaneManager.Instance.FromNavContext(Pane.Popover, 0) as FragmentHistoryStack)?.PopToRoot();
                }

                for (var index = 0; index < value.TabItems.Count(); index++)
                {
                    var stack = PaneManager.Instance.FromNavContext(Pane.Master, index) as FragmentHistoryStack;
                    var context = new iApp.AppNavigationContext { ActivePane = Pane.Master, ActiveTab = index, };
                    if (stack != null && stack.ID == context.ToString())
                    {
                        var newTab = value.TabItems.ElementAtOrDefault(index);
                        var oldTab = _tabs?.TabItems.ElementAtOrDefault(index);
                        if (oldTab != newTab)
                        {
                            ((IList)stack.Views).Clear();
                            if (index == value.SelectedIndex)
                            {
                                (PaneManager.Instance.FromNavContext(Pane.Detail, 0) as FragmentHistoryStack)?.PopToRoot();
                            }
                        }
                        continue;
                    }
                    stack = new FragmentHistoryStack(Pane.Master, index);
                    PaneManager.Instance.AddStack(stack, context);
                }
                _tabs = value;
                Device.Reflector.GetProperty(typeof(PaneManager), "Tabs").SetValue(PaneManager.Instance, _tabs);
            }
        }
        private static ITabView _tabs;

        public TabMode TabMode
        {
            get { return _tabMode; }
            set
            {
                _tabMode = value;
                switch (_tabMode)
                {
                    case TabMode.Dropdown:
                        Register<ITabView>(typeof(ActionBarAdapter));
                        break;
                    case TabMode.SlidingTabs:
                        Register<ITabView>(typeof(ActionBarTabView));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_tabMode), _tabMode, null);
                }
            }
        }
        private TabMode _tabMode;

        #endregion

        /// <summary>
        /// Allows implementation of ICustomItem in container
        /// </summary>
        public Func<ICustomItem, View, iLayer, View> CustomItemRequested { get; set; }

        public static View HomeUpView
        {
            get
            {
                View homeView = MainActivity.FindViewById<ImageView>(Android.Resource.Id.Home);
                if (homeView == null) return null;

                var parent = homeView;
                while (!((Java.Lang.Object)parent.Parent).Class.SimpleName.Contains("ActionBarView"))
                {
                    parent = (View)parent.Parent;
                }
                return parent;
            }
        }

        #endregion

        /// <summary>
        /// Private default constructor to use with singleton
        /// </summary>
        private DroidFactory()
        {
            ((AndroidResources)Device.Resources).Assembly = GetType().Assembly;

            if (Device.File.Exists("vanity.png"))
                iApp.VanityImagePath = "vanity.png";

            base.Style.HeaderColor = MainActivity.Resources.GetColor(Resource.Color.HeaderColor).ToColor();
            base.Style.HeaderTextColor = MainActivity.Resources.GetColor(Resource.Color.HeaderTextColor).ToColor();
            base.Style.SectionHeaderColor = MainActivity.Resources.GetColor(Resource.Color.SectionHeaderColor).ToColor();
            base.Style.SectionHeaderTextColor = MainActivity.Resources.GetColor(Resource.Color.SectionHeaderTextColor).ToColor();
            base.Style.SelectionColor = MainActivity.Resources.GetColor(Resource.Color.SelectionColor).ToColor();
            base.Style.LayerBackgroundColor = MainActivity.Resources.GetColor(Resource.Color.LayerBackgroundColor).ToColor();
            base.Style.LayerItemBackgroundColor = MainActivity.Resources.GetColor(Resource.Color.LayerItemBackgroundColor).ToColor();
            base.Style.SeparatorColor = MainActivity.Resources.GetColor(Resource.Color.SeparatorColor).ToColor();
            base.Style.TextColor = MainActivity.Resources.GetColor(Resource.Color.TextColor).ToColor();
            base.Style.SubTextColor = MainActivity.Resources.GetColor(Resource.Color.SubTextColor).ToColor();
            base.Style.SecondarySubTextColor = MainActivity.Resources.GetColor(Resource.Color.SecondarySubTextColor).ToColor();
        }

        public static T GetNativeObject<T>(object obj, string objName)
            where T : class
        {
            if (obj == null)
                return null;

            var customItem = obj as CustomItemContainer;
            if (customItem != null)
            {
                return customItem.CustomItem as T;
            }

            var pairable = obj as IPairable;
            if (obj is T || pairable?.Pair is T)
            {
                return GetNativeObject(obj, objName, typeof(T)) as T;
            }

            return null;
        }

        public new static DroidFactory Instance
        {
            get
            {
                if (!IsInitialized)
                    Initialize(new DroidFactory());
                return MXContainer.Instance as DroidFactory;
            }
        }

        internal static bool HandleUrl(Link link, bool createBrowserView, IMXView fromView)
        {
            if (link?.Address == null) return false;

            var queryIndex = link.Address.IndexOf('?');
            if (queryIndex > 0)
            {
                var queryString = link.Address.Substring(queryIndex).Split('#')[0];
                link.Parameters = HttpUtility.ParseQueryString(queryString).ToDictionary(k => k.Key, v => v.Value);
            }

            if (link.Address.StartsWith("app:"))
            {
                link.Address = link.Address.Substring(4);
            }

            if (link.Address.StartsWith(MailToExtensions.Scheme))
            {
                MailToExtensions.Launch(link);
            }
            else if (link.Address.StartsWith(TelephoneExtensions.Scheme) || link.Address.StartsWith(TelephoneExtensions.CallToScheme))
            {
                TelephoneExtensions.Launch(link);
            }
            else if (link.Address.StartsWith(CameraExtensions.Scheme))
            {
                CameraExtensions.Launch(link);
            }
            else if (link.Address.StartsWith(AudioPlaybackExtensions.Scheme))
            {
                AudioPlaybackExtensions.Launch(link);
            }
            else if (link.Address.StartsWith(AudioRecordingExtensions.Scheme))
            {
                AudioRecordingExtensions.Launch(link);
            }
            else if (link.Address.StartsWith(VideoPlaybackExtensions.Scheme))
            {
                VideoPlaybackExtensions.Launch(link);
            }
            else if (link.Address.StartsWith(VideoRecordingExtensions.Scheme))
            {
                VideoRecordingExtensions.Launch(link);
            }
            else if (link.Address.StartsWith("javascript:"))
            {
                return false;
            }
            else if (link.Address.StartsWith("http") && link.RequestType != RequestType.NewWindow)
            {
                if (!createBrowserView && string.IsNullOrEmpty(link.ConfirmationText)) return false;
                Navigate(link, fromView);
            }
            else if (iApp.Instance.NavigationMap.MatchUrl(link.Address)?.Controller != null)
            {
                Navigate(link, fromView);
            }
            else
            {
                if (link.Address.StartsWith("/"))
                {
                    link.Address = "file://" + link.Address;
                }

                var externalIntent = Intent.ParseUri(link.Address, IntentUriType.Scheme);
                externalIntent.AddFlags(ActivityFlags.NewTask);

                if (MainActivity.PackageManager.QueryIntentActivities(externalIntent, PackageInfoFlags.MatchDefaultOnly).Count > 0)
                {
                    MainActivity.StartActivity(externalIntent);
                    return true;
                }

                // Launch external app by package name
                externalIntent = MainActivity.PackageManager.GetLaunchIntentForPackage(link.Address);
                if (MainActivity.PackageManager.QueryIntentActivities(externalIntent, PackageInfoFlags.MatchDefaultOnly).Count > 0)
                {
                    MainActivity.StartActivity(externalIntent);
                    return true;
                }

                Device.Log.Error("Unable to handle url: " + link.Address);
                return false;
            }
            return true;
        }

        internal static void RefreshTitles()
        {
            var actionBar = MainActivity.ActionBar;
            if (actionBar == null) return;

            Device.Thread.ExecuteOnMainThread(() => { FragmentHistoryStack.SetHomeUp(Pane.Master); });
            var master = PaneManager.Instance.FromNavContext(Pane.Master, 0);
            var detail = PaneManager.Instance.FromNavContext(Pane.Detail, 0);
            var masterView = master?.CurrentView as IView;
            var detailView = detail?.CurrentView as IView;

            var title = masterView == null ? iApp.Instance?.Title : masterView.Title;
            var subtitle = detailView?.Title;
            if (string.IsNullOrWhiteSpace(subtitle)) { subtitle = null; }

            actionBar.NavigationMode = ActionBarNavigationMode.Standard;

            var view = detail ?? master;
            Device.Thread.ExecuteOnMainThread(() =>
            {
                var c = (view?.CurrentView as IView)?.TitleColor;
                if (!c.HasValue || c.Value.IsDefaultColor)
                {
                    actionBar.Title = title;
                    actionBar.Subtitle = subtitle;
                }
                else
                {
                    if (title == null)
                    {
                        actionBar.TitleFormatted = null;
                    }
                    else
                    {
                        ISpannable text = new SpannableString(title);
                        text.SetSpan(new ForegroundColorSpan(c.Value.ToColor()), 0, text.Length(), SpanTypes.InclusiveInclusive);
                        actionBar.TitleFormatted = text;
                    }

                    if (subtitle == null)
                    {
                        actionBar.SubtitleFormatted = null;
                    }
                    else
                    {
                        ISpannable text = new SpannableString(subtitle);
                        text.SetSpan(new ForegroundColorSpan(c.Value.ToColor()), 0, text.Length(), SpanTypes.InclusiveInclusive);
                        actionBar.SubtitleFormatted = text;
                    }
                }
            });

            UI.Color headerColor;

            if (view?.CurrentLayer != null)
            {
                headerColor = view.CurrentLayer.LayerStyle.HeaderColor;
            }
            else if (view?.CurrentView is IView)
            {
                headerColor = ((IView)view.CurrentView).HeaderColor;
            }
            else if (iApp.Instance != null)
            {
                headerColor = iApp.Instance.Style.HeaderColor;
            }
            else
            {
                return;
            }

            var baseActivity = MainActivity as BaseActivity;
            if (baseActivity != null)
            {
                baseActivity.UpdateHeader(headerColor);
            }
            else
            {
                actionBar.SetBackgroundDrawable(headerColor.ToColorDrawable());
            }
        }

        public static void OrientationChanged(iApp.Orientation orientation)
        {
            if (IsInitialized)
                Instance.OnOrientationChanged(orientation);
        }

        #region TargetFactory members

        /// <summary>
        /// Called when an <see cref="iLayer" /> instance is ready to be outputted.  Override this method in a subclass
        /// in order to handle layer types that cannot be handled by the available abstract objects.
        /// </summary>
        /// <param name="layer">The layer to be outputted.</param>
        /// <returns>
        ///   <c>true</c> if layer output was handled and the factory should not attempt to output it as a controller; otherwise <c>false</c>.
        /// </returns>
        protected override bool OnOutputLayer(iLayer layer)
        {
            var browser = layer as Core.Layers.Browser;
            if (browser == null || browser.Url.StartsWith("http") || browser.Url.StartsWith("data"))
            {
                return false;
            }
            return base.OnOutputLayer(layer) || HandleUrl(new Link(browser.Url), true, null);
        }

        protected override object OnGetCustomItem(ICustomItem item, iLayer layer, IListView view, object recycledCell)
        {
            if (CustomItemRequested == null)
                return base.OnGetCustomItem(item, layer, view, recycledCell);
            var custom = recycledCell as CustomItemContainer;
            return CustomItemRequested(item, custom?.CustomItem as View, layer);
        }

        protected override void OnSetDefinitions()
        {
            Register<IPlatformDefaults>(typeof(AndroidDefaults));
            Register<ITimer>(typeof(Timer));
            Register<IAlert>(typeof(Alert));
            Register<IImageData>(typeof(ImageData));
            Register<IExifData>(typeof(ExifData));

            Register<IGeoLocation>(typeof(GeoLocation));
            Register<ICompass>(typeof(Compass));
            Register<IAccelerometer>(typeof(Accelerometer));

            Register<ICanvasView>(typeof(CanvasFragment));
            Register<IBrowserView>(typeof(BrowserFragment));
            Register<IListView>(typeof(ListViewFragment));
            Register<IGridView>(typeof(GridFragment));
            Register<ITabView>(typeof(ActionBarAdapter));

            Register<IGridCell>(typeof(GridCell));
            Register<IRichContentCell>(typeof(RichText));

            Register<IImage>(typeof(Image));
            Register<ILabel>(typeof(Label));
            Register<IButton>(typeof(ButtonView));
            Register<IDatePicker>(typeof(DatePickerView));
            Register<ITimePicker>(typeof(TimePicker));
            Register<ITextBox>(typeof(TextBox));
            Register<ISearchBox>(typeof(SearchBox));
            Register<ITextArea>(typeof(TextArea));
            Register<ISelectList>(typeof(SelectList));
            Register<ISlider>(typeof(Slider));
            Register<ISwitch>(typeof(Switch));
            Register<IPasswordBox>(typeof(PasswordBox));

            Register<ISectionHeader>(typeof(HeaderView));
            Register<ISectionFooter>(typeof(FooterView));

            Register<ITabItem>(typeof(TabItem));

            Register<IMenu>(typeof(Menu));
            Register<IMenuButton>(typeof(MenuButton));

            Register<IToolbar>(typeof(Toolbar));
            Register<IToolbarButton>(typeof(ToolbarButton));
            Register<IToolbarSeparator>(typeof(ToolbarSeparator));
            RegisterSingleton<Type>((object)typeof(PopoverActivity), "Popover");
            Register<Android.Widget.Button>(typeof(Android.Widget.Button), nameof(ButtonView));

            #region Custom control registration for Android factory

            Register<IGridBase>(typeof(Grid));
            Register<ISwitch>(typeof(CheckBox), "CheckBox");
            Register<ISwitch>(typeof(RadioButton), "RadioButton");
            Register<IImage>(typeof(AnimationView), "AnimationView", filePath =>
            {
                var file = filePath.ElementAtOrDefault(0)?.ToString();
                var view = string.IsNullOrEmpty(file) ? new AnimationView() : new AnimationView(file);
                view.DurationLapsed += (sender, args) => { view.Restart(); };
                return view;
            });

            #endregion
        }

        protected override void OnBeginBlockingUserInput()
        {
            TextBase.CurrentFocus?.Blur(true, false);
        }

        [Obsolete]
        protected override void ShouldNavigate(Core.Controls.Link link, Pane pane, Action handler)
        {
            iApp.Thread.ExecuteOnMainThread(() =>
            {
                if (PaneManager.Instance.ShouldNavigate(link, pane, NavigationType.Forward))
                {
                    base.ShouldNavigate(link, pane, handler);
                }
                else
                {
                    OnHideLoadIndicator();
                }
            });
        }

        public override string TempPath => MainActivity.CacheDir.AbsolutePath;

        public virtual string TempImagePath => TempPath.AppendPath("Images");

        public override string DeviceId => Android.Provider.Settings.Secure.GetString(MainActivity.ContentResolver, Android.Provider.Settings.Secure.AndroidId);

        public override ICompositor Compositor => _compositor ?? (_compositor = new AndroidCompositor());

        public override ISettings Settings => _settings ?? (_settings = new BasicSettingsDictionary());

        public override MobileTarget Target => MobileTarget.Android;

        protected override double GetLineHeight(Font font)
        {
            return 1;
        }

        public override bool LargeFormFactor =>
            (MainActivity.Resources.Configuration.ScreenLayout & ScreenLayout.SizeMask) >= ScreenLayout.SizeLarge;

        public override Instructor Instructor
        {
            get { return _instructor ?? (_instructor = new AndroidInstructor()); }
            set { _instructor = value; }
        }
        private Instructor _instructor;

        /// <summary>
        /// Gets a value for scaling calulations on absolute pixels
        /// </summary>
        /// <returns>A value for scaling a pixel value into a display-independent pixel</returns>
        public override double GetDisplayScale()
        {
            return DisplayScale;
        }

        public static double DisplayScale { get; private set; }

        public static bool AllowReversePortrait { get; set; } = true;

        #region Load indicators

        private bool _immediateLoadIndicatorVisible;
        private ProgressDialog _loadIndicator;

        protected override void OnShowImmediateLoadIndicator()
        {
            MainActivity.SetProgressBarIndeterminateVisibility(_immediateLoadIndicatorVisible = true);
        }

        protected override void OnShowLoadIndicator(string title)
        {
            iApp.Thread.ExecuteOnMainThread(() =>
            {
                if (_loadIndicator == null)
                {
                    _loadIndicator = ProgressDialog.Show(MainActivity, string.Empty, title, true);
                }
                else
                {
                    _loadIndicator.SetMessage(title);
                }
            });
        }

        protected override void OnHideLoadIndicator()
        {
            MainActivity.SetProgressBarIndeterminateVisibility(_immediateLoadIndicatorVisible = false);
            try
            {
                _loadIndicator?.Dismiss();
            }
            catch {; }
            finally { _loadIndicator = null; }
        }

        #endregion

        #endregion

        #region Image helpers

        /// <summary>
        /// Stores an image to filesystem, overwriting if a file already exists.
        /// </summary>
        /// <returns>
        /// The image ID.
        /// </returns>
        /// <param name='imageData'>
        /// The image data.
        /// </param>
        /// <param name="extension">
        /// The image file's extension.
        /// </param>
        /// <param name='imageId'>
        /// An optional image identifier. If null, a GUID will be assigned as the image ID.
        /// </param>
        public static string StoreImage(Bitmap imageData, string extension, string imageId = null)
        {
            if (null == imageId)
            {
                imageId = Guid.NewGuid().ToString();
            }

            var path = Path.Combine(Instance.TempImagePath, imageId + "." + extension);
            iApp.File.Delete(path);
            var save = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            Bitmap.CompressFormat format;
            switch (extension)
            {
                case "jpg":
                case "jpeg":
                    format = Bitmap.CompressFormat.Jpeg;
                    break;
                case "png":
                    format = Bitmap.CompressFormat.Png;
                    break;
                default:
                    return null;
            }
            imageData.Compress(format, 100, save);
            return imageId;
        }

        internal string StoreImage(Uri imageData, string extension, string imageId = null)
        {
            if (null == imageId)
                imageId = Guid.NewGuid().ToString();
            var bitmapStream = MainActivity.ContentResolver.OpenInputStream(imageData);
            iApp.File.Save(Path.Combine(TempImagePath, imageId + "." + extension), bitmapStream, EncryptionMode.NoEncryption);
            bitmapStream.Dispose();
            return imageId;
        }

        /// <summary>
        /// Retrieves a full file path for an image ID.
        /// </summary>
        /// <returns>
        /// The full path to the image file for the specified ID.
        /// </returns>
        /// <param name='imageId'>
        /// An image identifier. Returns null if no image is found for the given ID.
        /// </param>
        public override string RetrieveImage(string imageId)
        {
            var info = new DirectoryInfo(TempImagePath);
            return info.EnumerateFiles().FirstOrDefault(file => file.Name.Contains(imageId))?.FullName;
        }

        public static Bitmap LoadBitmapFromView(View v)
        {
            var b = Bitmap.CreateBitmap(v.Width, v.Height, Bitmap.Config.Argb8888);
            var c = new Canvas(b);
            v.Layout(0, 0, v.LayoutParameters.Width, v.LayoutParameters.Height);
            v.Draw(c);
            return b;
        }

        #endregion

        #region Keyboard

        public static void HideKeyboard(bool fireLostFocus)
        {
            var tb = TextBase.CurrentFocus;
            var view = tb ?? PopoverFragment.Instance?.View?.FindViewById(Resource.Id.popover_fragment) ??
                       MainActivity.FindViewById(Resource.Id.popover_fragment) ??
                       MainActivity.FindViewById(Resource.Id.master_fragment);
            if (view == null) return;

            var hideKeyboard = new Action(() =>
            {
                var imm = (InputMethodManager)MainActivity.GetSystemService(Context.InputMethodService);
                imm.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);
            });

            if (tb != null)
            {
                tb.Blur(false, false);
                tb.Post(hideKeyboard);
            }
            else hideKeyboard();
        }

        public static void ShowKeyboard(View responder)
        {
            if (responder == null || responder.Visibility != ViewStates.Visible || !responder.Enabled) return;
            responder.RequestFocus();
            if (MainActivity.Resources.Configuration.Keyboard != Android.Content.Res.KeyboardType.Nokeys) return;
            responder.Post(() =>
            {
                if (responder != TextBase.CurrentFocus) return;
                var imm = (InputMethodManager)MainActivity.GetSystemService(Context.InputMethodService);
                imm.ShowSoftInput(responder, ShowFlags.Implicit);
            });
        }

        #endregion

        #region Navigation

        public new static void Navigate(string uri)
        {
            if (Instance._immediateLoadIndicatorVisible) return;
            iApp.Thread.ExecuteOnMainThread(() =>
            {
                Navigate(new Link(uri), PaneManager.Instance.FromNavContext(PaneManager.Instance.TopmostPane).CurrentView);
            });
        }

        public static void Navigate(Link link, Pane pane)
        {
            iApp.Thread.ExecuteOnMainThread(() =>
            {
                Navigate(link, PaneManager.Instance.FromNavContext(pane).CurrentView);
            });
        }

        public static void Navigate(Link link, IMXView view = null)
        {
            if (Instance._immediateLoadIndicatorVisible) return;
            iApp.Navigate(link, view);
        }

        #endregion
    }
}