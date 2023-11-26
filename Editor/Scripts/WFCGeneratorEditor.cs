using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WFCWorldGenerator))]
public class WFCGeneratorEditor : Editor
{
    WFCWorldGenerator generator;
    
    private void OnEnable ()
    {
        generator = (WFCWorldGenerator)target;

        SceneView.duringSceneGui -= SceneGUI;
        SceneView.duringSceneGui += SceneGUI;
    }

    private void OnDisable ()
    {
        SceneView.duringSceneGui -= SceneGUI;

    }
    
    private void SceneGUI(SceneView sceneView)
    {
        if (generator == null)
            return;
        
        var screenRect = new Rect(sceneView.position) { height = sceneView.position.height - 24f };

        var margin = 10;
        var border = 10;
        var padding = 10;
        var panelWidth = 250;
        var panelHeight = 80;
        
        var panelRect = new Rect(screenRect.width-panelWidth-margin, screenRect.height-panelHeight-margin, panelWidth, panelHeight);
        var contentRect = new Rect(border, border, panelRect.width-2*border, panelRect.height - 2*border);
        var paddingRect = new Rect(padding, padding, contentRect.width-2*padding, contentRect.height - 2*padding);

        Handles.BeginGUI();
        GUILayout.BeginArea(panelRect, new GUIStyle("Box"));
        GUILayout.BeginArea(contentRect, new GUIStyle("Box"));
        GUILayout.BeginArea(paddingRect);
        
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("WFC Generator", new GUIStyle("BoldLabel"));
            GUILayout.FlexibleSpace();
        }
        
        using (new GUILayout.HorizontalScope())
        using(new EditorGUI.DisabledScope(Application.IsPlaying(generator)))
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear", GUILayout.Width(100)))
            {
                MethodInfo clearMethod = generator.GetType().GetMethod("ClearModules", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                clearMethod?.Invoke(generator, new object[] { });
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Run", GUILayout.Width(100)))
            {
                MethodInfo clearMethod = generator.GetType().GetMethod("Generate", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                clearMethod?.Invoke(generator, new object[] { });
            }
            GUILayout.FlexibleSpace();
        }
        GUILayout.EndArea(); // paddingRect
        GUILayout.EndArea(); // contentRect
        GUILayout.EndArea(); // panelRect
        Handles.EndGUI();
    }
}