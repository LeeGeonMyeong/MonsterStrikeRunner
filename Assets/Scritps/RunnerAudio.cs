using System;
using UnityEngine;

public class RunnerAudio : MonoBehaviour
{
    public static RunnerAudio Instance { get; private set; }
    public bool Muted { get; private set; }
    private AudioSource music, effects;
    private AudioClip shot, charged, ready, hit, defeat, hurt, heal, win, lose;
    private const int Rate = 22050;
    private void Awake()
    {
        Instance = this;
        Muted = PlayerPrefs.GetInt("Runner.AudioMuted", 0) == 1;
        music = gameObject.AddComponent<AudioSource>();
        effects = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = effects.playOnAwake = false;
        music.spatialBlend = effects.spatialBlend = 0f;
        music.loop = true; music.volume = 0.15f; effects.volume = 0.45f;
        music.clip = Melody();
        shot = Tone("Shot", 900, 260, .1f);
        charged = Tone("Charged", 220, 70, .28f);
        ready = Chime("Ready", new float[] { 660, 990 }, .09f);
        hit = Tone("Hit", 180, 100, .08f);
        defeat = Tone("Defeat", 300, 70, .18f);
        hurt = Tone("Hurt", 120, 55, .25f);
        heal = Chime("Heal", new float[] { 523, 659, 784 }, .10f);
        win = Chime("Win", new float[] { 523, 659, 784, 1047 }, .14f);
        lose = Chime("Lose", new float[] { 330, 294, 220 }, .18f);
        ApplyMute();
    }
    public void StartMusic() { if (!music.isPlaying) music.Play(); }
    public void ToggleMute()
    {
        Muted = !Muted; PlayerPrefs.SetInt("Runner.AudioMuted", Muted ? 1 : 0);
        PlayerPrefs.Save(); ApplyMute();
    }
    private void ApplyMute() { music.mute = effects.mute = Muted; }
    private void Play(AudioClip clip) { if (!Muted) effects.PlayOneShot(clip); }
    public void Fire(bool full) => Play(full ? charged : shot);
    public void ChargeReady() => Play(ready);
    public void MonsterHit(bool killed) => Play(killed ? defeat : hit);
    public void Hurt() => Play(hurt);
    public void Heal() => Play(heal);
    public void Finish(bool survived) { music.Stop(); Play(survived ? win : lose); }
    private static AudioClip Tone(string name, float from, float to, float seconds)
    {
        float[] data = new float[(int)(Rate * seconds)];
        double phase = 0;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / data.Length;
            phase += 2 * Math.PI * Mathf.Lerp(from, to, t) / Rate;
            float envelope = Mathf.Min(1f, t * 40f) * Mathf.Pow(1f - t, 2f);
            data[i] = (float)(Math.Sin(phase) + .18 * Math.Sin(phase * 2)) * envelope * .6f;
        }
        return Clip(name, data);
    }
    private static AudioClip Chime(string name, float[] notes, float step)
    {
        float[] data = new float[(int)(Rate * step * (notes.Length + 2))];
        for (int n = 0; n < notes.Length; n++) AddNote(data, notes[n], n * step, step * 2.5f, .45f);
        return Clip(name, data);
    }
    private static void AddNote(float[] data, float frequency, float start, float length, float volume)
    {
        int offset = (int)(start * Rate), count = (int)(length * Rate);
        for (int j = 0; j < count && offset + j < data.Length; j++)
        {
            float t = (float)j / Rate;
            float envelope = Mathf.Min(1f, t / .012f) * Mathf.Pow(1f - (float)j / count, 2f);
            data[offset + j] += Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
        }
    }
    private static AudioClip Melody()
    {
        const float beat = .30f;
        int[] melody = { 69, 72, 76, 72, 67, 71, 74, 71, 65, 69, 72, 69, 67, 71, 76, 74,
                         69, 72, 76, 79, 67, 71, 74, 76, 65, 69, 72, 74, 67, 71, 76, 71 };
        float[] data = new float[(int)(Rate * beat * melody.Length)];
        for (int i = 0; i < melody.Length; i++)
        {
            AddNote(data, 440f * Mathf.Pow(2f, (melody[i] - 69) / 12f), i * beat, beat * .85f, .25f);
            if (i % 4 == 0) AddNote(data, 440f * Mathf.Pow(2f, (melody[i] - 24 - 69) / 12f), i * beat, beat * 3f, .20f);
        }
        return Clip("ForestLoop", data);
    }
    private static AudioClip Clip(string name, float[] data)
    {
        for (int i = 0; i < data.Length; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
        AudioClip clip = AudioClip.Create(name, data.Length, 1, Rate, false);
        clip.SetData(data, 0); return clip;
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (music != null) Destroy(music.clip);
        foreach (AudioClip clip in new[] { shot, charged, ready, hit, defeat, hurt, heal, win, lose })
            if (clip != null) Destroy(clip);
    }
}
