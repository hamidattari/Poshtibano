using Poshtibano.Common;
using System;

namespace Poshtibano.Desk.Services.Media
{
    /// <summary>
    /// Manages the state of audio/video streams.
    /// </summary>
    public class AudioVideoStateManager
    {
        public MediaStreamState LocalAudioState { get; private set; } = MediaStreamState.Idle;
        public MediaStreamState LocalWebcamState { get; private set; } = MediaStreamState.Idle;

        public MediaStreamState RemoteAudioState { get; private set; } = MediaStreamState.Idle;
        public MediaStreamState RemoteWebcamState { get; private set; } = MediaStreamState.Idle;

        // Permissions
        public bool LocalAudioPermissionGranted { get; private set; }
        public bool LocalWebcamPermissionGranted { get; private set; }
        public bool RemoteAudioPermissionGranted { get; private set; }
        public bool RemoteWebcamPermissionGranted { get; private set; }

        // Events
        public event Action<MediaStreamState> OnLocalAudioStateChanged;
        public event Action<MediaStreamState> OnLocalWebcamStateChanged;
        public event Action<MediaStreamState> OnRemoteAudioStateChanged;
        public event Action<MediaStreamState> OnRemoteWebcamStateChanged;

        public AudioVideoStateManager()
        {
            Console.WriteLine($"[{DateTime.Now}] 📊 AudioVideoStateManager created");
        }

        // Local Audio
        public void SetLocalAudioState(MediaStreamState state)
        {
            if (LocalAudioState != state)
            {
                Console.WriteLine($"[{DateTime.Now}] 🎤 Local audio:  {LocalAudioState} → {state}");
                LocalAudioState = state;
                OnLocalAudioStateChanged?.Invoke(state);
            }
        }

        public void SetLocalAudioPermission(bool granted)
        {
            LocalAudioPermissionGranted = granted;
        }

        // Local Webcam
        public void SetLocalWebcamState(MediaStreamState state)
        {
            if (LocalWebcamState != state)
            {
                Console.WriteLine($"[{DateTime.Now}] 📷 Local webcam: {LocalWebcamState} → {state}");
                LocalWebcamState = state;
                OnLocalWebcamStateChanged?.Invoke(state);
            }
        }

        public void SetLocalWebcamPermission(bool granted)
        {
            LocalWebcamPermissionGranted = granted;
        }

        // Remote Audio
        public void SetRemoteAudioState(MediaStreamState state)
        {
            if (RemoteAudioState != state)
            {
                Console.WriteLine($"[{DateTime.Now}] 🎤 Remote audio: {RemoteAudioState} → {state}");
                RemoteAudioState = state;
                OnRemoteAudioStateChanged?.Invoke(state);
            }
        }

        public void SetRemoteAudioPermission(bool granted)
        {
            RemoteAudioPermissionGranted = granted;
        }

        // Remote Webcam
        public void SetRemoteWebcamState(MediaStreamState state)
        {
            if (RemoteWebcamState != state)
            {
                Console.WriteLine($"[{DateTime.Now}] 📷 Remote webcam: {RemoteWebcamState} → {state}");
                RemoteWebcamState = state;
                OnRemoteWebcamStateChanged?.Invoke(state);
            }
        }

        public void SetRemoteWebcamPermission(bool granted)
        {
            RemoteWebcamPermissionGranted = granted;
        }

        // Reset all
        public void Reset()
        {
            LocalAudioState = MediaStreamState.Idle;
            LocalWebcamState = MediaStreamState.Idle;
            RemoteAudioState = MediaStreamState.Idle;
            RemoteWebcamState = MediaStreamState.Idle;

            LocalAudioPermissionGranted = false;
            LocalWebcamPermissionGranted = false;
            RemoteAudioPermissionGranted = false;
            RemoteWebcamPermissionGranted = false;

            Console.WriteLine($"[{DateTime.Now}] 🔄 AudioVideoStateManager reset");
        }
    }
}