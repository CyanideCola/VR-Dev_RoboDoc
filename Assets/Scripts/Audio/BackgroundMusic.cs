using UnityEngine;

// A quiet loop under the whole game. If the AudioManager has a Sound called "music" with a clip,
// that track is used; otherwise a soft pad is generated, so the game is never silent while a real
// track is being arranged. It lives in its own object, so it carries on across scene loads.
public static class BackgroundMusic
{
    const string SoundName = "music";
    const float Volume = 0.22f;

    static AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Start()
    {
        if (source) return;

        var go = new GameObject("Background Music");
        Object.DontDestroyOnLoad(go);
        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;          // everywhere, not from a point in the room
        source.volume = Volume;

        var clip = NamedClip() ?? BuildPad();
        if (!clip) return;
        source.clip = clip;
        source.Play();
        source.time = clip.length * 0.5f;  // start mid-phrase, so it is never obviously the same loop
    }

    static AudioClip NamedClip()
    {
        var manager = AudioManager.instance;
        if (!manager) manager = Object.FindAnyObjectByType<AudioManager>();
        if (!manager || manager.sounds == null) return null;
        foreach (var s in manager.sounds)
            if (s.name == SoundName && s.clip) return s.clip;
        return null;
    }

    // Four slow chords, each breathing in and out, with a little warmth and a slow vibrato.
    static AudioClip BuildPad()
    {
        const int rate = 22050;
        const float bar = 6f;              // seconds per chord
        float[][] chords =
        {
            new[] { 110.00f, 164.81f, 220.00f },   // A2  E3  A3
            new[] {  87.31f, 174.61f, 261.63f },   // F2  F3  C4
            new[] {  98.00f, 146.83f, 196.00f },   // G2  D3  G3
            new[] { 110.00f, 196.00f, 261.63f },   // A2  G3  C4
        };

        int total = Mathf.RoundToInt(rate * bar * chords.Length);
        var data = new float[total];
        for (int i = 0; i < total; i++)
        {
            float t = (float)i / rate;
            int at = Mathf.Min((int)(t / bar), chords.Length - 1);
            float local = t - at * bar;
            float breathe = Mathf.Sin(Mathf.PI * Mathf.Clamp01(local / bar));   // silent at both ends
            float vibrato = 1f + 0.0015f * Mathf.Sin(2f * Mathf.PI * 0.15f * t);

            float v = 0f;
            foreach (float hz in chords[at]) v += Voice(hz * vibrato, t);
            data[i] = v * breathe * 0.2f;
        }

        var clip = AudioClip.Create("Ambient Pad", total, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // A sine with a touch of second harmonic: warm, and no clicks at the loop point
    static float Voice(float hz, float t)
    {
        return Mathf.Sin(2f * Mathf.PI * hz * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * hz * t);
    }
}
