using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SocketLUT))]
public class SocketLUTEditor :
#if NAUGHTY_ATTR
    NaughtyAttributes.Editor.NaughtyInspector
#else
    Editor
#endif
{
    SocketLUT targ;

    public override void OnInspectorGUI ()
    {
        targ = (SocketLUT)target;

        string[] sockets = targ.Sockets.ToArray();

        if (sockets.Length == 0)
            targ.Sockets.Add(SocketUtility.WILDCARD);

        if (GUILayout.Button("Add Symetrical Socket"))
            AddSymetricalSocket();

        if (GUILayout.Button("Add Flippable Socket"))
            AddFlippableSocket();

        if (GUILayout.Button("Add Vertical Socket"))
            AddVerticalSocket();


        string indexToRemove = null;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Defined Sockets", EditorStyles.boldLabel);

        using (new EditorGUI.ChangeCheckScope()) {


            using (new EditorGUILayout.VerticalScope(new GUIStyle("Box"))) {

                int currentIndex;
                int lastIndex = -1;

                for (int i = 0; i < sockets.Length; i++) {

                    string socket = sockets[i];
                    string socketLabel = (SocketUtility.IsWildCard(socket) ? "?" : socket) + "\t " + SocketDesc(socket);
                    SocketUtility.ParseSocketIndex(socket, out currentIndex);

                    if (currentIndex != lastIndex) {
                        var rect = EditorGUILayout.GetControlRect();
                        rect.y += 8f;
                        
                        // horizontal line
                        rect.height = 1f;
                        EditorGUI.DrawRect(rect, Color.grey);
                    }

                    using (new EditorGUILayout.HorizontalScope()) {
                        EditorGUILayout.LabelField(socketLabel);
                        if (!SocketUtility.IsWildCard(socket) && currentIndex != lastIndex && GUILayout.Button("-") && SocketUtility.ParseSocketIndex(socket, out int id))
                            indexToRemove = id.ToString();
                    }

                    lastIndex = currentIndex;
                }
            }

            if (!string.IsNullOrEmpty(indexToRemove)) {

                Undo.RegisterCompleteObjectUndo(targ, $"Remove Socket ({indexToRemove})");
                targ.RemoveSockets(indexToRemove);
                EditorUtility.SetDirty(targ);
            }

        }

        Undo.FlushUndoRecordObjects();
    }

    private static string SocketDesc (string socket)
    {

        if (!SocketUtility.IsValid(socket))
            return "INVALID";


        int id, angle;

        SocketUtility.ParseSocketIndex(socket, out id);
        SocketUtility.ParseSocketLastDigit(socket, out angle);

        if(SocketUtility.IsWildCard(socket))
            return $"Default Wildcard Socket";

        else if (SocketUtility.IsDirectional(socket))
            return $"Vertical Socket {id} - {angle * 90}";

        else if (SocketUtility.IsSymmetric(socket))
            return $"Symmetrical Socket {id}";

        else if (SocketUtility.IsFlippable(socket)) {
            if (SocketUtility.IsFlipped(socket))
                return $"Flippable Socket {id} - Flipped";
            else
                return $"Flippable Socket {id}";
        } else
            return "INVALID";
    }

    private static int firstAvailableID (string[] sockets)
    {
        int i = 0;
        int testedID = 0;
        int socketID = 0;

        while (socketID <= testedID && i < sockets.Length) {
            if (SocketUtility.ParseSocketIndex(sockets[i], out socketID) && testedID == socketID)
                testedID++;

            i++;
        }

        return testedID;
    }

    private void AddSymetricalSocket ()
    {
        int id = firstAvailableID(targ.Sockets.ToArray());


        Undo.RegisterCompleteObjectUndo(targ, $"Add Symetrical Socket ({id})");
        targ.Sockets.Add($"{id}s");
        EditorUtility.SetDirty(targ);
    }


    private void AddFlippableSocket ()
    {
        int id = firstAvailableID(targ.Sockets.ToArray());


        Undo.RegisterCompleteObjectUndo(targ, $"Add Flippable Socket ({id})");
        targ.Sockets.Add($"{id}");
        targ.Sockets.Add($"{id}f");
        EditorUtility.SetDirty(targ);
    }


    private void AddVerticalSocket ()
    {
        int id = firstAvailableID(targ.Sockets.ToArray());


        Undo.RegisterCompleteObjectUndo(targ, $"Add Vertical Socket ({id})");
        targ.Sockets.Add($"{id}_0");
        targ.Sockets.Add($"{id}_1");
        targ.Sockets.Add($"{id}_2");
        targ.Sockets.Add($"{id}_3");
        EditorUtility.SetDirty(targ);
    }
}
