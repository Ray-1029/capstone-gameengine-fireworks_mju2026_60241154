using UnityEditor;
using UnityEngine;
using LanternFestival;

[CustomPropertyDrawer(typeof(FireworkCue))]
public sealed class FireworkCueDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty p,GUIContent label)
    {
        if(!p.isExpanded) return 25;
        return 25+13*EditorGUIUtility.singleLineHeight+13*5+58;
    }
    public override void OnGUI(Rect rect,SerializedProperty p,GUIContent label)
    {
        var name=p.FindPropertyRelative("label").stringValue;
        var time=p.FindPropertyRelative("referenceTime").doubleValue;
        p.isExpanded=EditorGUI.Foldout(new Rect(rect.x,rect.y,rect.width,22),p.isExpanded,$"{time:000.000}s  {name}",true);
        if(!p.isExpanded) return;
        EditorGUI.indentLevel++; float y=rect.y+27;
        foreach(var field in new[]{("label","Cue name"),("referenceTime","Reference time / file seconds"),("anchor","Align to: Burst or Launch"),("riseTime","Rise time (seconds)"),("launchPosition","Launch position (world)"),("height","Height (ground: 0 = prefab)"),("scale","Size multiplier"),("brightness","Brightness multiplier"),("prefab","Firework prefab"),("seed","Deterministic seed")})
        {
            EditorGUI.PropertyField(new Rect(rect.x,y,rect.width,EditorGUIUtility.singleLineHeight),p.FindPropertyRelative(field.Item1),new GUIContent(field.Item2));
            y+=EditorGUIUtility.singleLineHeight+5;
        }
        double rise=p.FindPropertyRelative("riseTime").floatValue;
        bool burst=p.FindPropertyRelative("anchor").enumValueIndex==0;
        double launch=burst?time-rise:time, explosion=burst?time:time+rise;
        var prefab=p.FindPropertyRelative("prefab").objectReferenceValue as FireworkEffect;
        string result=prefab && !prefab.HasBurst?$"Launch {launch:F3}s  /  no aerial burst":$"Launch {launch:F3}s  /  first flash {explosion:F3}s";
        EditorGUI.HelpBox(new Rect(rect.x+16,y,rect.width-16,35),result,MessageType.Info); y+=41;
        EditorGUI.PropertyField(new Rect(rect.x,y,rect.width,52),p.FindPropertyRelative("musicalEvidence"),new GUIContent("Audio evidence"));
        EditorGUI.indentLevel--;
    }
}

[CustomEditor(typeof(FireworkShowPlayer))]
public sealed class FireworkShowPlayerInspector : Editor
{
    double preview=66.1813;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var player=(FireworkShowPlayer)target;
        EditorGUILayout.HelpBox("RMB drag / arrows = look; wheel = zoom; C = reset view; Esc = release mouse. R = replay; Space = pause; H = controls. All cue times refer to the original audio file.",MessageType.Info);
        if(GUILayout.Button("Select editable cue timeline")) Selection.activeObject=player.show;
        preview=EditorGUILayout.DoubleField("Preview file time (s)",preview);
        if(GUILayout.Button(Application.isPlaying?"Seek and play":"Preview particles at this time")) { player.Seek(preview,Application.isPlaying); SceneView.RepaintAll(); }
        if(GUILayout.Button("Clear preview")) player.ClearEffects();
        if(Application.isPlaying && GUILayout.Button("Replay from start")) player.Replay();
    }
}
