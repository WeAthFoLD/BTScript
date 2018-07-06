using BTScript;
using UnityEditor;

using GL  = UnityEngine.GUILayout;
using EGL = UnityEditor.EditorGUILayout;

[CustomEditor(typeof(BehaviourScript))]
class BehaviourScriptInspector : Editor {

    public override void OnInspectorGUI() {
        var script = (BehaviourScript) target;

        if (script.compileOkay) {
            EGL.HelpBox("The script is compiled and up to date!", MessageType.Info);
        }
        if (script.notUpToDate) {
            EGL.HelpBox("The compiled script is not up to date.", MessageType.Warning);
        }
        if (script.notCompiled) {
            EGL.HelpBox("The script is not yet compiled.", MessageType.Error);
        }

        if (GL.Button("Compile")) {
            script.Compile();
        }

        if (GL.Button("Print AST")) {
            script.PrintAST();
        }
    }

}