using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Media;
using MonoCross.Utilities;
using iFactr.UI;
using iFactr.Core;

namespace iFactr.Droid
{
    public static class AudioRecordingExtensions
    {
        public const string Scheme = "voicerecording://";
        public const string CallbackParam = "AudioId";
        private static string _callback, _callbackId;
        private static AlertDialog _recordingDialog;
        private static MediaRecorder _mic;

        public static void Launch(Link link)
        {
            _callback = link.Parameters.GetValueOrDefault("callback");
            _callbackId = Guid.NewGuid().ToString();

            if (!link.Parameters.GetValueOrDefault("headless").TryParseBoolean())
            {
                _recordingDialog = new AlertDialog.Builder(DroidFactory.MainActivity)
                    .SetTitle(iApp.Factory.GetResourceString("Recording") ?? "Recording...")
                    .SetPositiveButton(iApp.Factory.GetResourceString("StopRecording") ?? "Stop Recording", (o, e) =>
                    {
                        Stop();
                        _recordingDialog = null;
                    })
                    .SetCancelable(false)
                    .Show();
            }

            if (link.Parameters.ContainsKey("command"))
            {
                switch (link.Parameters["command"])
                {
                    case "stop":
                        Stop();
                        break;
                    default:
                        Start(Path.Combine(DroidFactory.Instance.TempPath, "Images", _callbackId) + ".3gp");
                        break;
                }
            }
            else
            {
                Start(Path.Combine(DroidFactory.Instance.TempPath, "Images", _callbackId) + ".3gp");
            }
        }

        public static void Start(string fileName)
        {
            if (_mic != null)
            {
                Stop();
            }
            _mic = new MediaRecorder();

            // set some default values for recording settings
            _mic.SetAudioSource(AudioSource.Mic);
            _mic.SetOutputFormat(OutputFormat.ThreeGpp);
            _mic.SetAudioEncoder(AudioEncoder.AmrNb);

            // define a filename and location for the output file
            Device.File.EnsureDirectoryExistsForFile(fileName);
            _mic.SetOutputFile(fileName);

            // prepare and start recording
            _mic.Prepare();
            _mic.Start();
        }

        public static void Stop()
        {
            _recordingDialog?.Dismiss();
            _recordingDialog = null;

            // stop recording
            try
            {
                _mic.Stop();
            }
            catch (Exception e)
            {
                Device.Log.Error(e);
            }

            // prepare object for GC by calling dispose
            _mic.Release();
            _mic.Dispose();
            _mic = null;

            if (_callback != null)
            {
                DroidFactory.Navigate(new Link(_callback, new Dictionary<string, string>
                {
                    { CallbackParam, _callbackId },
                }));
            }
        }
    }
}