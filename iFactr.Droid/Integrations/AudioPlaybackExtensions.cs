using System;
using System.Collections.Generic;
using Android.Media;
using iFactr.UI;

namespace iFactr.Droid
{
    public static class AudioPlaybackExtensions
    {
        public const string Scheme = "audio://";
        private static string _callback;
        private static MediaPlayer _player;

        public static void Launch(Link link)
        {
            _callback = link.Parameters.GetValueOrDefault("callback");
            var uri = link.Parameters.GetValueOrDefault("source");

            if (link.Parameters.ContainsKey("command"))
            {
                switch (link.Parameters["command"])
                {
                    case "stop":
                        Stop();
                        break;
                    case "pause":
                        TogglePause();
                        break;
                    default:
                        Start(uri);
                        break;
                }
            }
            else
            {
                Start(uri);
            }
        }

        public static void Start(string audioFilePath)
        {
            if (audioFilePath == null) return;

            // first stop any active audio on MediaPlayer instance
            if (_player != null) { Stop(); }

            _player = new MediaPlayer();
            if (_player == null)
            {
                throw new Exception("Could not load MediaPlayer");
            }

            _player.SetDataSource(new Java.IO.FileInputStream(audioFilePath).FD);
            _player.Prepare();
            _player.Completion += (sender, args) =>
            {
                Stop();
                if (_callback == null) return;
                DroidFactory.Navigate(_callback);
                _callback = null;
            };
            _player.Start();
        }

        public static void TogglePause()
        {
            if (_player == null) return;
            if (_player.IsPlaying)
            {
                _player.Pause();
            }
            else
            {
                _player.Start();
            }
        }

        public static void Stop()
        {
            if (_player == null) return;
            if (_player.IsPlaying)
            {
                _player.Stop();
            }

            _player.Release();
            _player = null;
        }
    }
}