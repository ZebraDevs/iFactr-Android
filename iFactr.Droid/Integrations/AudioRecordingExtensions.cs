using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Media;
using MonoCross;
using MonoCross.Utilities;
using iFactr.UI;
using iFactr.Core;

namespace iFactr.Droid
{
    public static class AudioRecordingExtensions
    {
        public const string Scheme = "voicerecording://";
        public const string CallbackParam = "AudioId";
        private static MicAccess _access;

        public static void Launch(Link link)
        {
            var callbackUri = link.Parameters.GetValueOrDefault("callback");
            _access = new MicAccess();
            var id = Guid.NewGuid().ToString();
            new AlertDialog.Builder(DroidFactory.MainActivity)
                .SetTitle(iApp.Factory.GetResourceString("Recording") ?? "Recording...")
                .SetPositiveButton(iApp.Factory.GetResourceString("StopRecording") ?? "Stop Recording", (o, e) =>
                {
                    _access.StopRecording();
                    _access = null;
                    if (callbackUri != null)
                    {
                        DroidFactory.Navigate(new Link(callbackUri, new Dictionary<string, string>
                        {
                            { CallbackParam, id },
                        }));
                    }
                })
                .SetCancelable(false)
                .Show();
            _access.StartRecordingFromMic(Path.Combine(DroidFactory.Instance.TempPath, "Images", id));
        }

        public class MicAccess
        {
            public void StartRecordingFromMic(string fileName)
            {
                // set some default values for recording settings
                _mic.SetAudioSource(AudioSource.Mic);
                _mic.SetOutputFormat(OutputFormat.Default);
                _mic.SetAudioEncoder(AudioEncoder.Default);

                // define a filename and location for the output file
                Device.File.EnsureDirectoryExistsForFile(fileName);
                _mic.SetOutputFile(fileName);

                // prepare and start recording
                _mic.Prepare();
                _mic.Start();
            }

            public void StopRecording()
            {
                // stop recording
                _mic.Stop();

                // prepare object for GC by calling dispose
                _mic.Release();
                _mic.Dispose();
                _mic = null;
            }

            public MicAccess()
            {
                _mic = new MediaRecorder();
            }

            private MediaRecorder _mic;
        }
    }
}