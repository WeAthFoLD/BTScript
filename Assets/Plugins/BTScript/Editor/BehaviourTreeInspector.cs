using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using EGL = UnityEditor.EditorGUILayout;
using GL = UnityEngine.GUILayout;
using System.Text;
using System.Linq;

namespace BTScript {

[CustomEditor(typeof(BehaviourTree))]
public class BehaviourTreeInspector : Editor {

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        var tree = (BehaviourTree) target;
        var script = tree.source;
        if (script) {
            if (script.notCompiled) {
                EGL.HelpBox("Script is not compiled.", MessageType.Error);
            }
            if (script.notUpToDate) {
                EGL.HelpBox("Script is not up to date.", MessageType.Warning);
            }
            if (script.compileOkay) {
                EGL.HelpBox("Script is compiled and up to date.", MessageType.Info);
            }
            if (GL.Button("Compile")) {
                script.Compile();
            }
        }

        if (Application.isPlaying) {
            EGL.Space();
            EGL.LabelField("Conditions");
            GUI.enabled = false;
            foreach (var cond in tree.blackboard) {
                var k = cond.Key;
                var v = cond.Value;
                if (v is float) {
                    EGL.FloatField(k, (float) v);
                } else  if (v is int) {
                    EGL.IntField(k, (int) v);
                } else if (v is bool) {
                    EGL.Toggle(k, (bool) v);
                } else if (v is string) {
                    EGL.TextField(k, (string) v);
                }
            }
            GUI.enabled = true;
            Repaint();
        }
    }
    
}

}