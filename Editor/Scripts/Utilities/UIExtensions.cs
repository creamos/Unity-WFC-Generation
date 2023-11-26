using UnityEditor;
using UnityEngine.UIElements;

public static class UIExtensions
{
    public static string FilePath (this EditorWindow win) => AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(win));

    public static VisualElement LoadUXML (this EditorWindow _, string path)
    {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{path}.uxml");
        return visualTree.Instantiate();
    }
    public static StyleSheet LoadUSS (this EditorWindow _, string path)
    {
        return AssetDatabase.LoadAssetAtPath<StyleSheet>($"{path}.uss");
    }

}
