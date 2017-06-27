using System;
using Android.App;
using Android.Content;
using iFactr.UI;

namespace iFactr.Droid
{
    public class Alert : AlertDialog, IAlert
    {
        public static Alert Instance { get; private set; }

        public event AlertResultEventHandler Dismissed;

        public Link CancelLink { get; set; }

        public Link OKLink { get; set; }
        public string Message { get; }
        public string Title { get; }

        public AlertButtons Buttons { get; }

        public Alert(string message, string title, AlertButtons buttons)
            : base(DroidFactory.MainActivity)
        {
            SetTitle(Title = title);
            SetMessage(Message = message);
            Buttons = buttons;
            DismissEvent += Alert_DismissEvent;

            switch (buttons)
            {
                case AlertButtons.OKCancel:
                    SetButton(DroidFactory.Instance.GetResourceString("OK"), Handler);
                    SetButton2(DroidFactory.Instance.GetResourceString("Cancel"), Handler);
                    break;
                case AlertButtons.YesNo:
                    SetButton(DroidFactory.Instance.GetResourceString("Yes"), Handler);
                    SetButton2(DroidFactory.Instance.GetResourceString("No"), Handler);
                    break;
                default:
                    SetButton(DroidFactory.Instance.GetResourceString("OK"), Handler);
                    break;
            }
        }

        private void Alert_DismissEvent(object sender, EventArgs e)
        {
            if (Buttons == AlertButtons.YesNo)
                _result += 2;
            var handler = Dismissed;
            if (handler != null)
            {
                handler(this, new AlertResultEventArgs(_result));
            }
            else switch (_result)
                {
                    case AlertResult.OK:
                    case AlertResult.Yes:
                        DroidFactory.Navigate(OKLink);
                        break;
                    case AlertResult.Cancel:
                    case AlertResult.No:
                        DroidFactory.Navigate(CancelLink);
                        break;
                }
            Instance = null;
            _result = AlertResult.Cancel;
        }

        private void Handler(object sender, DialogClickEventArgs e)
        {
            _result = (AlertResult)(Math.Abs(e.Which) - 1);
        }

        private AlertResult _result = AlertResult.Cancel;

        public new virtual void Show()
        {
            if (Instance == null)
            {
                Instance = this;
                base.Show();
            }
            else
            {
                Instance.Dismissed -= Alert_DismissEvent;
                Instance.Dismiss();
                Instance = null;
                new Alert(Message, Title, Buttons)
                {
                    CancelLink = CancelLink,
                    OKLink = OKLink,
                    Dismissed = Dismissed,
                }.Show();
            }
        }
    }
}