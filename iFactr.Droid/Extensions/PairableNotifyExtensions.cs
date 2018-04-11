using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using iFactr.UI;

namespace iFactr.Droid
{
    public static class PairableNotifyExtensions
    {
        public static void OnPropertyChanged(this IPairable obj, [CallerMemberName] string propertyName = null)
        {
            var jObject = obj as Java.Lang.Object;
            if (obj is INotifyPropertyChanged && (jObject == null || jObject.Handle != IntPtr.Zero))
            {
                obj.RaiseEvent(nameof(INotifyPropertyChanged.PropertyChanged), new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}