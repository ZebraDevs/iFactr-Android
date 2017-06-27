using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using iFactr.UI;
using Color = iFactr.UI.Color;

namespace iFactr.Droid
{
    public class CanvasFragment : BaseFragment, ICanvasView
    {
        private readonly DrawingView _canvas;
        private LinearLayout _container;

        public CanvasFragment()
        {
            _canvas = new DrawingView { Parent = this, };
        }

        public override void OnResume()
        {
            base.OnResume();

            //Lock orientation so that the canvas doesn't rotate
            switch (((BaseActivity)DroidFactory.MainActivity).CurrentOrientation)
            {
                case Android.Content.Res.Orientation.Landscape:
                    Activity.RequestedOrientation = ScreenOrientation.SensorLandscape;
                    break;
                case Android.Content.Res.Orientation.Portrait:
                    Activity.RequestedOrientation = ScreenOrientation.SensorPortrait;
                    break;
                default:
                    Activity.RequestedOrientation = ScreenOrientation.Unspecified;
                    break;
            }
            if (View != null) View.RequestFocus();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            ResetOrientation();
            _canvas.CleanUp();
        }

        public override Android.Views.View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var parent = ((Android.Views.View)_canvas).Parent;
            if (parent != null)
            {
                ((ViewGroup)parent).RemoveView(_canvas);
            }
            _container = new LinearLayout(Activity) { Orientation = Orientation.Vertical, };
            _container.AddView(_canvas, new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, 0) { Weight = 1, });

            var bar = DroidFactory.GetNativeObject<Android.Views.View>(_toolbar, "Toolbar");
            if (bar != null)
            {
                if (bar.Parent != _container && bar.Parent != null)
                    ((ViewGroup)bar.Parent).RemoveView(bar);
                _container.AddView(bar, new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.WrapContent));
                ((Toolbar)bar).UpdateItems();
            }
            Render();

            return _container;
        }

        #region Properties

        public Color StrokeColor
        {
            get { return _canvas.StrokeColor; }
            set
            {
                if (_canvas.StrokeColor == value) return;
                _canvas.StrokeColor = value;
                this.OnPropertyChanged();
            }
        }

        public double StrokeThickness
        {
            get { return _canvas.StrokeThickness; }
            set
            {
                if (_canvas.StrokeThickness == value) return;
                _canvas.StrokeThickness = value;
                this.OnPropertyChanged();
            }
        }

        public IToolbar Toolbar
        {
            get { return _toolbar; }
            set
            {
                if (_toolbar == value) return;
                var control = DroidFactory.GetNativeObject<HorizontalScrollView>(_toolbar, "value");
                if (control != null && control.Parent != null)
                    ((ViewGroup)control.Parent).RemoveView(control);

                _toolbar = value;
                control = DroidFactory.GetNativeObject<HorizontalScrollView>(_toolbar, "value");
                var toolbar = control as Toolbar;
                if (toolbar != null)
                    toolbar.Parent = this;
                this.OnPropertyChanged();

                if (control == null || _container == null) return;

                if (control.Parent != null)
                {
                    ((ViewGroup)control.Parent).RemoveView(control);
                }

                _container.AddView(control, new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.WrapContent));
            }
        }
        private IToolbar _toolbar;

        #endregion

        #region ICanvas members

        void IView.SetBackground(string imagePath, ContentStretch stretch)
        {
            _canvas.SetBackground(imagePath, stretch);
        }

        public void Load(string fileName)
        {
            _canvas.Load(fileName);
        }

        public void Clear()
        {
            _canvas.Clear();
        }

        public void Save(bool compositeBackground)
        {
            _canvas.Save(compositeBackground);
        }

        public void Save(string fileName)
        {
            _canvas.Save(fileName, false);
        }

        public void Save(string fileName, bool compositeBackground)
        {
            _canvas.Save(fileName, compositeBackground);
        }

        public event SaveEventHandler DrawingSaved
        {
            add { _canvas.DrawingSaved += value; }
            remove { _canvas.DrawingSaved -= value; }
        }

        #endregion
    }
}