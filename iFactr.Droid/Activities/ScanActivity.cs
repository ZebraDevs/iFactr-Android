using Android.OS;
using iFactr.Core.Utilities;
using iFactr.Scanning;
using MonoCross.Navigation;

namespace iFactr.Droid
{
    public class ScanActivity : iFactrActivity
    {
        public IScanner ScanInstance { get { return _scanner; } }
        private IScanner _scanner;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (_scanner == null)
            {
                _scanner = MXContainer.Resolve<IScanner>(this);
                if (_scanner == null)
                {
                    Device.Log.Error("Failed to initialize scanner from type registry.");
                }
                else
                {
                    Scanner.Initalize(_scanner);
                }
            }
        }

        protected override void OnStop()
        {
            ScanInstance?.StopScan();
            base.OnStop();
        }

        protected override void OnDestroy()
        {
            ScanInstance?.TermScanner();
            base.OnDestroy();
        }
    }
}