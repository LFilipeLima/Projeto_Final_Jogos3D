using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ArchiveAudio : MonoBehaviour
{
    public enum Cue { Shot, Pulse, Gunner, Heavy, Document, Series }

    private static ArchiveAudio current;
    private static bool muted;
    public static bool Muted => muted;
    private AudioClip[] clips;
    private AudioSource local;

    private void Awake()
    {
        current = this;
        local = gameObject.AddComponent<AudioSource>();
        local.playOnAwake = false;
        var names = System.Enum.GetNames(typeof(Cue));
        clips = new AudioClip[names.Length];
        for (int i = 0; i < names.Length; i++)
            clips[i] = Resources.Load<AudioClip>("ArchiveAudio/" + names[i]);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            muted = !muted;
    }

    public static void Play(Cue cue, AudioSource position = null)
    {
        if (!current || muted) return;
        AudioClip clip = current.clips[(int)cue];
        if (clip) (position ? position : current.local).PlayOneShot(clip, cue == Cue.Shot ? 0.38f : 0.75f);
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }
}
