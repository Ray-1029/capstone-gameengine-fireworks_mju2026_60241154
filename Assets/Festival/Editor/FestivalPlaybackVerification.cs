using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LanternFestival;

[InitializeOnLoad]
public static class FestivalPlaybackVerification
{
    const string SessionKey="Festival.AutomatedPlayback";
    static FireworkShowPlayer player;
    static double maxDrift, lastDsp, maxStep;
    static bool stalled, replayed;
    static int checkpoints, errors, driftSamples, phase;
    static double phaseStart, pausedTime;
    static readonly List<string> lines=new List<string>();
    static FestivalPlaybackVerification()
    {
        if(SessionState.GetBool(SessionKey,false)) EditorApplication.update+=Tick;
    }
    public static void Begin()
    {
        EditorSceneManager.OpenScene("Assets/Festival/Scenes/LanternFestival.unity");
        SessionState.SetBool(SessionKey,true);
        EditorApplication.EnterPlaymode();
    }
    static void Log(string text)
    {
        lines.Add(text);
        string folder=player && player.show.paradeVersion>=2?"Verification/Wide":player && player.show.paradeVersion>0?"Verification/Parade":"Verification";
        Directory.CreateDirectory(folder); File.WriteAllLines(folder+"/playback-validation.txt",lines); Debug.Log(text);
    }
    static void Assert(bool condition,string text) { Log((condition?"PASS ":"FAIL ")+text); if(!condition) errors++; }
    static void Tick()
    {
        if(!Application.isPlaying) return;
        if(!player)
        {
            player=UnityEngine.Object.FindFirstObjectByType<FireworkShowPlayer>();
            if(!player) return;
            Log("FULL PLAYBACK VALIDATION — native Unity Play Mode; no time acceleration");
            lastDsp=AudioSettings.dspTime;
            Determinism();
            player.Replay();
        }
        double dsp=AudioSettings.dspTime;
        maxStep=Math.Max(maxStep,dsp-lastDsp); lastDsp=dsp;
        var source=player.GetComponent<AudioSource>();
        double t=player.MusicTime;
        if(t>.4 && t<player.show.music.length-.2 && source.isPlaying)
        {
            double drift=Math.Abs(source.timeSamples/(double)source.clip.frequency-player.AudioClockTime);
            maxDrift=Math.Max(maxDrift,drift);
            driftSamples++;
        }
        if(!replayed && t>30*(checkpoints+1) && checkpoints<6)
        {
            checkpoints++; Log($"CHECKPOINT file={t:F3}s audio={source.timeSamples/(double)source.clip.frequency:F3}s maxDrift={maxDrift:F6}s active={player.ActiveEffectCount}");
            Assert(source.isPlaying && source.timeSamples>0,"Audio source actually advancing at checkpoint "+checkpoints);
        }
        if(!stalled && t>121)
        {
            stalled=true; System.Threading.Thread.Sleep(250);
            Log("Injected one 250 ms main-thread stall; audio and absolute cue clock continue.");
        }
        if(!replayed && t>player.show.music.length+player.show.tailAfterAudio)
        {
            Assert(checkpoints==6,"Reached 30/60/90/120/150/180 s in uninterrupted full-file run");
            Assert(driftSamples>100 && maxDrift<.15,$"No cumulative drift; maximum sampled source/DSP discrepancy {maxDrift:F6}s across {driftSamples} observations (audio buffer tolerance)");
            Assert(player.ActiveEffectCount==0,"Gold tail completed and all live effects released");
            Assert(stalled,"Frame stall recovery exercised");
            player.Replay(); replayed=true; phase=1;
            Assert(player.MusicTime<0 && player.ActiveEffectCount==0 && source.timeSamples==0,"Replay resets music, cue cursor, preroll, and particles together");
        }
        if(phase==1 && t>1.7 && t<10)
        {
            Assert(source.isPlaying && player.ActiveEffectCount>0,"Opening cue runs again after replay");
            player.TogglePause(); pausedTime=player.MusicTime; phaseStart=dsp; phase=2;
            Assert(player.IsPaused&&!source.isPlaying,"Pause stops music and freezes visual clock");
        }
        if(phase==2 && dsp-phaseStart>.3)
        {
            Assert(Math.Abs(player.MusicTime-pausedTime)<.000001,"Paused visual time remains fixed for 300 ms");
            player.TogglePause(); Assert(!player.IsPaused,"Resume schedules audio from preserved sample position");
            phase=3; phaseStart=dsp;
        }
        if(phase==3 && dsp-phaseStart>.6)
        {
            Assert(source.isPlaying && source.timeSamples/(double)source.clip.frequency>pausedTime+.3,"Resume continues audible samples after scheduling lead time");
            Log($"SUMMARY errors={errors}; maximum_editor_update_gap={maxStep:F4}s");
            SessionState.SetBool(SessionKey,false); EditorApplication.update-=Tick;
            if(Application.isBatchMode) EditorApplication.Exit(errors==0?0:2);
            else EditorApplication.isPlaying=false;
        }
    }
    static void Determinism()
    {
        foreach(var prefab in player.show.cues.Select(c=>c.prefab).Distinct())
        {
            var cue=new FireworkCue { prefab=prefab,referenceTime=5,riseTime=prefab.HasBurst?2:0,anchor=prefab.HasBurst?CueAnchor.Burst:CueAnchor.Launch,launchPosition=new Vector3(1,0,70),height=40,scale=1,seed=71 };
            var a=UnityEngine.Object.Instantiate(prefab); a.Prepare(cue);
            var b=UnityEngine.Object.Instantiate(prefab); b.Prepare(cue);
            for(double t=3;t<6.1;t+=.017) a.Evaluate(t);
            a.Evaluate(6.1); b.Evaluate(6.1);
            var pa=new ParticleSystem.Particle[18000]; var pb=new ParticleSystem.Particle[18000];
            int na=a.tails.GetParticles(pa),nb=b.tails.GetParticles(pb);
            bool equal=na==nb&&na>0;
            for(int i=0;i<Math.Min(na,nb);i++) equal &= (pa[i].position-pb[i].position).sqrMagnitude<1e-10;
            Assert(equal,"Absolute-time particles independent of frame history: "+prefab.name);
            UnityEngine.Object.Destroy(a.gameObject); UnityEngine.Object.Destroy(b.gameObject);
        }
    }
}
