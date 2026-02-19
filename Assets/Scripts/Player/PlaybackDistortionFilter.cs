using UnityEngine;

/// <summary>
/// ECHOES - Playback Distortion Filter
/// Attached to the playback AudioSource child of SoundRecorderDevice.
/// Routes OnAudioFilterRead to SoundRecorderDevice.ApplyPlaybackDistortion()
/// to create eerie ghost-radio effects on recorded clip playback.
/// 
/// Effects applied:
/// - Pitch wobble: subtle random pitch variation
/// - Bit crush: reduced sample precision for grainy feel
/// - Ring modulation: low-frequency sine multiplication for eerie undertone
/// - Noise overlay: layered static that fades in/out
/// </summary>
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class PlaybackDistortionFilter : MonoBehaviour
{
    [HideInInspector]
    public SoundRecorderDevice recorderDevice;

    void OnAudioFilterRead(float[] data, int channels)
    {
        // Safety: recorderDevice may be null during initialization or after destroy
        if (recorderDevice == null) return;

        // Safety: only process if device is actively playing
        if (!recorderDevice.IsPlayingClip) return;

        recorderDevice.ApplyPlaybackDistortion(data, channels);
    }
}