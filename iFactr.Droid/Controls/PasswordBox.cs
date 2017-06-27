using System;
using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Text.Method;
using Android.Util;
using iFactr.UI;
using iFactr.UI.Controls;
using Android.Runtime;

namespace iFactr.Droid
{
    public class PasswordBox : TextBox, IPasswordBox
    {
        #region Constructors

        [Preserve]
        public PasswordBox()
            : base(DroidFactory.MainActivity)
        {
            Initialize();
        }

        public PasswordBox(Context context)
            : base(context)
        {
            Initialize();
        }

        [Preserve]
        public PasswordBox(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize();
        }

        [Preserve]
        public PasswordBox(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize();
        }

        public PasswordBox(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        private void Initialize()
        {
            InputExtras = InputTypes.TextVariationPassword;
            SetCompletion();
        }

        #endregion

        protected override void OnTextChanged(string oldValue, string newValue)
        {
            base.OnTextChanged(oldValue, newValue);
            this.OnPropertyChanged(nameof(Password));
            PasswordChanged?.Invoke(this, new ValueChangedEventArgs<string>(oldValue, newValue));
        }

        protected override void SetCompletion()
        {
            base.SetCompletion();
            TransformationMethod = new PasswordTransformationMethod();
            SetTypeface(Typeface.Monospace, TypefaceStyle.Normal);
        }

        public override void NullifyEvents()
        {
            base.NullifyEvents();
            PasswordChanged = null;
        }

        public string Password
        {
            get { return Text; }
            set
            {
                if (Text == value) return;
                Text = value ?? string.Empty;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(Text));
            }
        }

        public event ValueChangedEventHandler<string> PasswordChanged;
    }
}