using System;
using System.ComponentModel;
using Android.Widget;
using iFactr.UI;
using Android.Runtime;
using Android.Content;
using Android.Util;
using Android.Text;

namespace iFactr.Droid
{
    public class SearchBox : SearchView, ISearchBox, INotifyPropertyChanged
    {
        #region Constructors

        [Preserve]
        public SearchBox()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public SearchBox(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public SearchBox(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public SearchBox(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize();
        }

        private void Initialize()
        {
            SetIconifiedByDefault(false);
            QueryTextChange += OnQueryTextChange;
            QueryTextSubmit += (o, e) => DroidFactory.HideKeyboard(true);
        }

        #endregion

        public TextCompletion TextCompletion
        {
            get { return _textCompletion; }
            set
            {
                if (_textCompletion == value) return;
                _textCompletion = value;
                SetCompletion();
            }
        }
        private TextCompletion _textCompletion;

        public string Text
        {
            get
            {
                return Query;
            }
            set
            {
                if (Query == value) return;
                SetQuery(value, true);
                this.OnPropertyChanged();
                this.OnPropertyChanged("Query");
            }
        }

        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                if (_backgroundColor == value || Handle == IntPtr.Zero) return;
                SetBackgroundColor(value.IsDefaultColor ? Android.Graphics.Color.Transparent : value.ToColor());
                _backgroundColor = value;
                this.OnPropertyChanged();
            }
        }
        private Color _backgroundColor;

        public Color ForegroundColor
        {
            get { return _foregroundColor; }
            set
            {
                if (_foregroundColor == value || Handle == IntPtr.Zero) return;
                int id = Context.Resources.GetIdentifier("android:id/search_src_text", null, null);
                var textView = FindViewById<TextView>(id);
                textView.SetTextColor(value.ToColor());
                _foregroundColor = value;
                this.OnPropertyChanged();
            }
        }
        private Color _foregroundColor;

        public Color BorderColor
        {
            get { return new Color(); }
            set { }
        }

        public string Placeholder
        {
            get
            {
                return _hint;
            }
            set
            {
                if (_hint == value) return;
                _hint = value;
                SetQueryHint(_hint);
                this.OnPropertyChanged();
            }
        }
        private string _hint;

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

        public void Focus()
        {
            FocusRequested = true;
            DroidFactory.ShowKeyboard(this);
        }

        public bool FocusRequested { get; set; }

        private void OnQueryTextChange(object sender, QueryTextChangeEventArgs queryTextChangeEventArgs)
        {
            this.RaiseEvent(nameof(SearchPerformed), new SearchEventArgs(queryTextChangeEventArgs.NewText));
        }

        private void SetCompletion()
        {
            var inputType = InputTypes.ClassText;
            if ((_textCompletion & TextCompletion.AutoCapitalize) == TextCompletion.AutoCapitalize)
            {
                inputType |= InputTypes.TextFlagCapSentences;
            }

            if ((_textCompletion & TextCompletion.OfferSuggestions) == TextCompletion.OfferSuggestions)
            {
                inputType |= InputTypes.TextFlagAutoComplete;
            }
            else
            {
                inputType |= InputTypes.TextFlagNoSuggestions;
            }
            SetInputType(inputType);

            this.OnPropertyChanged("TextCompletion");
            this.OnPropertyChanged("InputType");
        }

        public event SearchEventHandler SearchPerformed;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}