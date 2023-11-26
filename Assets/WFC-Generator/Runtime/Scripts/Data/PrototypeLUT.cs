using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Prototype Lookup Table", fileName = "Prototype LUT")]
public class PrototypeLUT : ScriptableObject
{
    [SerializeField] private Prototype[] _prototypes;

    public Prototype GetPrototype(int i) => _prototypes[i];
    public int Count => _prototypes?.Length ?? 0;

    public Prototype this[int key]
    {
        get => _prototypes[key];
    }

    public void Populate(Prototype[] prototypes) =>
        _prototypes = prototypes;

    public IEnumerator GetEnumerator()
    {
        return _prototypes.GetEnumerator();
    }

#if UNITY_EDITOR
    public static PrototypeLUT Editor_GetInstance()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(PrototypeLUT).Name);

        string path;

        if (guids.Length == 0)
        {
            PrototypeLUT so = ScriptableObject.CreateInstance<PrototypeLUT>();
            path = "Assets/Prototype LUT.asset";

            UnityEditor.AssetDatabase.CreateAsset(so, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            Debug.LogWarning(
                "No instance of PrototypeLUT were found in the project and one as been created in the Assets folder." +
                " <color=red>Don't forget to move it to an appropriate location.</color>");
            return so;
        }
        else
        {
            path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<PrototypeLUT>(path);
        }
    }

    public event Action OnValueChanged;
    private void OnValidate ()
    {
        OnValueChanged?.Invoke();
    }
#endif
}

[Serializable]
public struct Prototype
{
    public GameObject prefab;
    public int angle;
    public float weight;
    public ModuleFlag flag;
    
    public string[] sockets;

    public int[] xPrevConstraints;
    public int[] xNextConstraints;
    public int[] yPrevConstraints;
    public int[] yNextConstraints;
    public int[] zPrevConstraints;
    public int[] zNextConstraints;

    public int[] GetConstraints (Vector3Int direction)
    {
        if (direction == -Vector3Int.right) return xPrevConstraints;
        if (direction == Vector3Int.right) return xNextConstraints;
        if (direction == -Vector3Int.up) return yPrevConstraints;
        if (direction == Vector3Int.up) return yNextConstraints;
        if (direction == -Vector3Int.forward) return zPrevConstraints;
        if (direction == Vector3Int.forward) return zNextConstraints;
        return new int[0];
    }
    public int[] GetConstraints(int socketID)
    {
        return socketID switch {
            0 => xPrevConstraints,
            1 => xNextConstraints,
            2 => yPrevConstraints,
            3 => yNextConstraints,
            4 => zPrevConstraints,
            5 => zNextConstraints,
            _ => new int[0]
        };
    }

    public string DisplayName (bool withAngle = true)
    {
        return $"{prefab.name.Replace('_', ' ')}" + (withAngle ? $" - {angle*90}" : "");
    }
}