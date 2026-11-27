using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LanternFestival
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class FireworkShowPlayer : MonoBehaviour
    {
        public FireworkShowData show;
        public bool playOnStart = true;
        public bool showControls = false;
        [Range(0, 1)] public float volume = .8f;
        public double MusicTime { get; private set; }
        public double AudioClockTime => IsPaused ? MusicTime : AudioSettings.dspTime-origin;
        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }
        public int ActiveEffectCount => active.Count;
        AudioSource source;
        double origin;
        double visualHoldUntil;
        int nextCue;
        List<FireworkCue> ordered = new List<FireworkCue>();
        readonly List<(FireworkCue cue, FireworkEffect effect)> active = new List<(FireworkCue, FireworkEffect)>();
        GUIStyle labelStyle;

        void Start() { source = GetComponent<AudioSource>(); if (playOnStart) Replay(); }
        void OnDisable() { if (source) source.Stop(); ClearEffects(); IsPlaying = false; }

        [ContextMenu("Replay from file start")]
        public void Replay()
        {
            if (!show || !show.music) return;
            source = GetComponent<AudioSource>();
            source.Stop(); source.clip = show.music; source.playOnAwake = false;
            source.loop = false; source.pitch = 1; source.spatialBlend = 0; source.volume = volume;
            source.timeSamples = 0;
            ordered = show.cues.Where(c => c.prefab).OrderBy(c => c.LaunchTime).ToList();
            double earliest = ordered.Count > 0 ? ordered[0].LaunchTime : 0;
            double preRoll = Math.Max(show.preRoll, -earliest + .25);
            origin = AudioSettings.dspTime + preRoll;
            visualHoldUntil=0;
            MusicTime = -preRoll; IsPlaying = true; IsPaused = false; nextCue = 0;
            ClearEffects();
            source.PlayScheduled(origin);
        }

        public void TogglePause()
        {
            if (!IsPlaying) { Replay(); return; }
            if (!IsPaused)
            {
                MusicTime = AudioSettings.dspTime - origin;
                source.Stop(); IsPaused = true;
            }
            else
            {
                double lead = MusicTime < 0 ? -MusicTime : .12;
                origin = AudioSettings.dspTime + lead - Math.Max(0, MusicTime);
                visualHoldUntil=MusicTime>=0?AudioSettings.dspTime+lead:0;
                source.timeSamples = Mathf.Clamp((int)(Math.Max(0,MusicTime)*show.music.frequency),0,show.music.samples-1);
                if (MusicTime < show.music.length) source.PlayScheduled(AudioSettings.dspTime + lead);
                IsPaused = false;
            }
        }

        public void Seek(double seconds, bool play)
        {
            if (!show || !show.music) return;
            if (!source) source = GetComponent<AudioSource>();
            source.Stop(); source.clip = show.music; source.pitch = 1; source.spatialBlend = 0; source.volume = volume;
            ordered = show.cues.Where(c => c.prefab).OrderBy(c => c.LaunchTime).ToList();
            MusicTime = Math.Max(-show.preRoll, Math.Min(show.music.length+show.tailAfterAudio,seconds));
            ClearEffects(); nextCue = 0; EvaluateAt(MusicTime);
            origin = AudioSettings.dspTime + .12 - MusicTime;
            visualHoldUntil=MusicTime>=0?AudioSettings.dspTime+.12:0;
            IsPlaying = true; IsPaused = !play;
            if(play && MusicTime < show.music.length)
            {
                source.timeSamples = Mathf.Clamp((int)(Math.Max(0,MusicTime)*show.music.frequency),0,show.music.samples-1);
                if(MusicTime < 0) { origin=AudioSettings.dspTime-MusicTime; source.PlayScheduled(origin); }
                else source.PlayScheduled(AudioSettings.dspTime+.12);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) Replay();
            if (Input.GetKeyDown(KeyCode.Space)) TogglePause();
            if (Input.GetKeyDown(KeyCode.H)) showControls = !showControls;
            if (!IsPlaying || IsPaused) return;
            if(AudioSettings.dspTime<visualHoldUntil) return;
            MusicTime = AudioSettings.dspTime - origin;
            EvaluateAt(MusicTime);
            if (MusicTime > show.music.length + show.tailAfterAudio) { IsPlaying = false; source.Stop(); }
        }

        public void EvaluateAt(double time)
        {
            if (ordered.Count == 0 && show) ordered = show.cues.Where(c=>c.prefab).OrderBy(c=>c.LaunchTime).ToList();
            while (nextCue < ordered.Count && ordered[nextCue].LaunchTime <= time)
            {
                var cue = ordered[nextCue++];
                double end = cue.HasBurst ? cue.BurstTime + cue.prefab.VisibleDuration : cue.LaunchTime + cue.prefab.VisibleDuration;
                if (time > end) continue;
                var effect = Instantiate(cue.prefab, transform);
                effect.name = cue.label; effect.Prepare(cue);
                active.Add((cue,effect));
            }
            for(int i=active.Count-1;i>=0;i--)
            {
                var item=active[i];
                double end = item.cue.HasBurst ? item.cue.BurstTime+item.effect.VisibleDuration : item.cue.LaunchTime+item.effect.VisibleDuration;
                if(time>end) { RemoveEffect(item.effect); active.RemoveAt(i); }
                else item.effect.Evaluate(time);
            }
        }

        void RemoveEffect(FireworkEffect effect)
        {
            if(!effect) return;
            effect.gameObject.SetActive(false);
            if(Application.isPlaying) Destroy(effect.gameObject); else DestroyImmediate(effect.gameObject);
        }
        public void ClearEffects() { foreach(var item in active) RemoveEffect(item.effect); active.Clear(); }

        void OnGUI()
        {
            if(!showControls || !show) return;
            if(labelStyle==null) labelStyle=new GUIStyle(GUI.skin.label) { fontSize=17, normal={textColor=new Color(1,.84f,.5f)} };
            GUI.Box(new Rect(20,20,540,130),GUIContent.none);
            GUI.Label(new Rect(34,28,420,26), $"LANTERN / {MusicTime:000.000}s / {show.music.length:000.000}s",labelStyle);
            if(GUI.Button(new Rect(34,65,110,32),"Restart [R]")) Replay();
            if(GUI.Button(new Rect(152,65,140,32),IsPaused?"Play [Space]":"Pause [Space]")) TogglePause();
            if(GUI.Button(new Rect(300,65,140,32),"Hide [H]")) showControls=false;
            GUI.Label(new Rect(34,108,510,24),"RMB / arrows: look   Wheel: zoom   C: reset view   Esc: release");
        }
    }
}
