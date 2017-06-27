using System;
using System.Collections.Generic;
using Android.Media;
using iFactr.UI;

namespace iFactr.Droid
{
    public static class AudioPlaybackExtensions
    {
        public const string Scheme = "audio://";
        private static readonly AudioAccess Access = new AudioAccess();
        private static string _callback;

        static AudioPlaybackExtensions()
        {
            Access.OnComplete += (sender, args) =>
            {
                Access.Stop();
                if (_callback == null) return;
                DroidFactory.Navigate(_callback);
                _callback = null;
            };
        }

        public static void Launch(Link link)
        {
            _callback = link.Parameters.GetValueOrDefault("callback");
            string uri = link.Parameters.GetValueOrDefault("source");

            if (link.Parameters.ContainsKey("command"))
            {
                switch (link.Parameters["command"])
                {
                    case "stop":
                        Access.Stop();
                        break;
                    case "pause":
                        Access.TogglePause();
                        break;
                    default:
                        Access.Start(uri);
                        break;
                }
            }
            else
            {
                Access.Start(uri);
            }
        }

        public class AudioAccess
        {
            public EventHandler OnComplete;

            public void Start(string audioFilePath)
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
                _player.Completion += OnComplete;
                _player.Start();
            }

            public void TogglePause()
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

            public void Stop()
            {
                if (_player == null) return;

                if (_player.IsPlaying)
                {
                    _player.Stop();
                }

                _player.Release();
                _player = null;
            }

            private MediaPlayer _player;
        }
    }
}