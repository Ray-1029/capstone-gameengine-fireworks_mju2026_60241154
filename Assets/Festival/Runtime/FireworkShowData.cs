using System;
using System.Collections.Generic;
using UnityEngine;

namespace LanternFestival
{
    public enum CueAnchor { Burst, Launch }
    public enum FireworkShape { Chrysanthemum, Willow, RedBurst, Fan, Comet, Star, MultiBreak, Glitter, DoubleRing, Palm, Peony, Crossette, FallingLeaves }

    [Serializable]
    public class FireworkCue
    {
        public string label;
        [TextArea] public string musicalEvidence;
        [Tooltip("Seconds from the original audio file start, including any source silence.")]
        public double referenceTime;
        public CueAnchor anchor = CueAnchor.Burst;
        [Min(0)] public float riseTime = 2;
        public Vector3 launchPosition;
        [Min(0)] public float height = 32;
        [Range(0.2f, 2.2f)] public float scale = 1;
        [Range(.1f,1.5f)] public float brightness = 1;
        public FireworkEffect prefab;
        public int seed = 1;
        public double LaunchTime => anchor == CueAnchor.Burst ? referenceTime - riseTime : referenceTime;
        public double BurstTime => anchor == CueAnchor.Burst ? referenceTime : referenceTime + riseTime;
        public bool HasBurst => prefab && prefab.HasBurst;
    }

    [CreateAssetMenu(menuName = "Lantern Festival/Show Data")]
    public class FireworkShowData : ScriptableObject
    {
        public AudioClip music;
        [Min(0)] public float preRoll = 3.2f;
        [Min(0)] public float tailAfterAudio = 9;
        [TextArea] public string analysisNotes;
        public List<FireworkCue> cues = new List<FireworkCue>();
        [HideInInspector] public int paradeVersion;
    }
}
