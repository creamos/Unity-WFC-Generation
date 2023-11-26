using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[CreateAssetMenu(menuName = "Data/Socket Lookup Table", fileName = "Socket LUT")]
public class SocketLUT : ScriptableObject
{

    [SerializeField]
    private SortedList _sockets;
    public SortedList Sockets {
        get {
            if (_sockets == null)
                _sockets = new SortedList(SocketUtility.WILDCARD);
            return _sockets;
        }
        set {
            _sockets = value;
        }
    }

    public void RemoveSockets(string socketID)
    {
        if (int.TryParse(socketID, out int searchedID)) {

            int indexOfFirst = -1;
            int count = 0;

            for(int i = 0; i < Sockets.Count; ++i) {
                if(SocketUtility.ParseSocketIndex(Sockets[i], out int parsedID)) {

                    if (parsedID == searchedID) {
                        if (count == 0) indexOfFirst = i;
                        count++;
                    }
                    else if (parsedID > searchedID)
                        break;
                }
            }

            Sockets.EnableSorting(false);

            Sockets.RemoveRange(indexOfFirst, count);

            Sockets.EnableSorting(true);

            Sockets.Sort();
        }

    }

    #if UNITY_EDITOR
    public static SocketLUT Editor_GetInstance ()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:"+ typeof(SocketLUT).Name);
        
        string path;

        if (guids.Length == 0) {

            SocketLUT so = ScriptableObject.CreateInstance<SocketLUT>();
            so.Sockets.Add("0s");
            path = "Assets/Socket LUT.asset";

            UnityEditor.AssetDatabase.CreateAsset(so, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            Debug.LogWarning("No instance of SocketLUT were found in the project and one as been created in the Assets folder." +
                " <color=red>Don't forget to move it to an appropriate location.</color>");
            return so;
        } else {

            path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<SocketLUT>(path);
        }

    }
    #endif


    [Serializable]
    public class SortedList
    {
        public event Action OnValueChanged;

        [SerializeField]
        List<string> _content;
        List<string> Content {
            get {
                if (_content == null) _content = new List<string>();
                return _content;
            }
        }

        bool _isSorted = true;

        public SortedList (params string[] content) {
            _content = new List<string>(content);
            OnValueChanged?.Invoke();
        }

        public void EnableSorting(bool state = true)
        {
            _isSorted = state;
        }

        public string this[int key] {
            get => Content[key];
            set {
                Content[key] = value;
                Internal_OnValueChanged();
            }
        }

        public int Count => Content.Count;

        public void Clear () => Content.Clear();

        public void Add (string value) {

            Content.Add(value);
            Internal_OnValueChanged();
        }

        public bool Remove (string item) {

            bool ret = Content.Remove(item);
            Internal_OnValueChanged();
            return ret;
        }

        public void RemoveAt (int index) {

            Content.RemoveAt(index);
            Internal_OnValueChanged();
        }

        public void RemoveRange(int index, int count) {
            Content.RemoveRange(index, count);
            Internal_OnValueChanged();
        }

        public List<string> ToList () => Content.ToList();
        public string[] ToArray() => Content.ToArray();

        public void Sort ()
        {
            if (_isSorted) Content.Sort(new AlphaStringComparer());
        }

        void Internal_OnValueChanged ()
        {
            Sort();
            OnValueChanged?.Invoke();
        }
    }
}