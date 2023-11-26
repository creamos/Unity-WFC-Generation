using System.Linq;
using UnityEditor;
using UnityEngine;

public class PrototypeBuilder : EditorWindow
{
    [SerializeField]
    Module[] modules;

    bool[] folded;

    [MenuItem("Tools/WFC/Prototype Builder")]
    public static void ShowWindow ()
    {
        var window = GetWindow<PrototypeBuilder>(false, "Prototype Builder");
        window.titleContent.image = Resources.Load<Texture>("AnimationTool/Gear Icon");
        window.Init();
    }

    public void Init ()
    {
        modules = new Module[0];
        folded = new bool[0];
    }

    private void OnGUI ()
    {

        SerializedObject so = new SerializedObject(this);
        so.Update();


        using (new EditorGUI.ChangeCheckScope()) {

            if (GUILayout.Button("Extract Data")) {
                ExtractData();
                EditorUtility.SetDirty(this);
            }


            if (modules.Contains(null)) {
                modules = new Module[0];
                folded = new bool[0];
            }

            if (modules.Length > 0) {
                using (new EditorGUILayout.VerticalScope(new GUIStyle("Box"))) {
                    EditorGUILayout.LabelField($"Modules are loaded : {modules.Length}", new GUIStyle("label") { alignment = TextAnchor.MiddleCenter });
                }
            }

            using (new EditorGUI.DisabledGroupScope(modules == null || modules.Length == 0)) {

                if (GUILayout.Button("Bake Data")) {
                    var prototypeLUT = PrototypeLUT.Editor_GetInstance();
                    prototypeLUT.Populate(BakePrototypes(modules));
                    EditorUtility.SetDirty(prototypeLUT);
                    EditorUtility.SetDirty(this);
                }
            }

            DrawPrototypesPanel();
        }
        so.ApplyModifiedProperties();

    }

    int _selectedProto = -1;
    int _selectedSocket = 0;
    Vector2 _protoScrollPos, _connectionScrollPos;
    private void DrawPrototypesPanel ()
    {
        var headerStyle = new GUIStyle("label") { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        var boldLabelStyle = new GUIStyle("label") { fontStyle = FontStyle.Bold };
        var descStyle = new GUIStyle("Box") { alignment = TextAnchor.LowerCenter, fixedHeight = 200 };
        var boxStyle = new GUIStyle("Box");

        PrototypeLUT pLUT = PrototypeLUT.Editor_GetInstance();

        using (var protoScrollScope = new EditorGUILayout.ScrollViewScope(_protoScrollPos, new GUIStyle("Box"))) {
            _protoScrollPos = protoScrollScope.scrollPosition;

            for (int i = 0; i < pLUT.Count; i++) {
                var proto = pLUT[i];
                HighlightedButton(ref _selectedProto, i, $"{proto.prefab.name} : {proto.angle * 90}");
            }
        }

        if (_selectedProto >= 0 && _selectedProto < pLUT.Count) {
            Prototype proto = pLUT[_selectedProto];

            using (new EditorGUILayout.VerticalScope(descStyle)) {
                EditorGUILayout.LabelField($"{proto.prefab.name} : {proto.angle * 90}", headerStyle);
                EditorGUILayout.Space();


                using (var panel = new EditorGUILayout.HorizontalScope()) {

                    var widthOption = GUILayout.MinWidth(panel.rect.width / 2f);

                    using (new EditorGUILayout.VerticalScope(boxStyle, widthOption)) {

                        EditorGUILayout.LabelField("Sockets :", boldLabelStyle);

                        HighlightedButton(ref _selectedSocket, 0, string.Format("-X : {0} connections", proto.xPrevConstraints.Length));
                        HighlightedButton(ref _selectedSocket, 1, string.Format("+X : {0} connections", proto.xNextConstraints.Length));
                        HighlightedButton(ref _selectedSocket, 2, string.Format("-Y : {0} connections", proto.yPrevConstraints.Length));
                        HighlightedButton(ref _selectedSocket, 3, string.Format("+Y : {0} connections", proto.yNextConstraints.Length));
                        HighlightedButton(ref _selectedSocket, 4, string.Format("-Z : {0} connections", proto.zPrevConstraints.Length));
                        HighlightedButton(ref _selectedSocket, 5, string.Format("+Z : {0} connections", proto.zNextConstraints.Length));

                    }

                    string socketName = "";
                    int[] selectedConnections = null;

                    switch (_selectedSocket) {
                    case 0:
                        selectedConnections = proto.xPrevConstraints;
                        socketName = "-X"; break;
                    case 1:
                        selectedConnections = proto.xNextConstraints;
                        socketName = "+X"; break;
                    case 2:
                        selectedConnections = proto.yPrevConstraints;
                        socketName = "-Y"; break;
                    case 3:
                        selectedConnections = proto.yNextConstraints;
                        socketName = "+Y"; break;
                    case 4:
                        selectedConnections = proto.zPrevConstraints;
                        socketName = "-Z"; break;
                    case 5:
                        selectedConnections = proto.zNextConstraints;
                        socketName = "+Z"; break;
                    };

                    using (new EditorGUILayout.VerticalScope(boxStyle, widthOption)) {

                        if (selectedConnections != null) {

                            EditorGUILayout.LabelField($"Connections on socket {socketName}", headerStyle, widthOption);

                            using (var connectionScrollScope = new EditorGUILayout.ScrollViewScope(_connectionScrollPos)) {
                                _connectionScrollPos = connectionScrollScope.scrollPosition;

                                foreach (int protoID in selectedConnections) {
                                    var connectableProto = pLUT[protoID];
                                    HighlightedButton(ref _selectedProto, protoID, $"{connectableProto.prefab.name} : {connectableProto.angle * 90}");
                                }
                            }
                        }

                    }
                }
            }
        }
    }

    private bool HighlightedButton (ref int currentID, int buttonID, string message)
    {
        var oldColor = GUI.backgroundColor;
        if (buttonID == currentID) {
            GUI.backgroundColor = new Color(1, .7f, 0);
        }

        bool clicked;
        if (clicked = GUILayout.Button(message))
            currentID = buttonID;

        GUI.backgroundColor = oldColor;

        return clicked;
    }

    private void ExtractData ()
    {
        modules = FindObjectsOfType<ModuleSockets>()
            .Where(modSocket => PrefabUtility.IsOutermostPrefabInstanceRoot(modSocket.gameObject))
            .SelectMany(
            modSocket => new int[] { 0, 1, 2, 3 },
            (modSocket, angle) => {

                string rotateVerticalSocket (string socket, int angle)
                {
                    if (SocketUtility.IsValid(socket)
                    && SocketUtility.ParseSocketIndex(socket, out int id)
                    && !SocketUtility.IsWildCard(socket)) {
                        
                        if (SocketUtility.IsSymmetric(socket))
                            return socket;
                        if (SocketUtility.IsFlippable(socket) )
                            if (angle % 2 == 0)
                                return socket;
                            else
                                return SocketUtility.IsFlipped(socket) ? $"{id}" : $"{id}f";

                        if (SocketUtility.IsDirectional(socket)
                            && SocketUtility.ParseSocketLastDigit(socket, out int baseAngle))
                            return $"{id}_{(baseAngle + angle) % 4}";

                            return SocketUtility.WILDCARD;
                    }

                    else
                        return SocketUtility.WILDCARD;
                }

                string rotateSideSocket (ModuleSockets mod, ModuleSide side, int angle)
                {
                    int rotated = (int)(side + 4 - angle) % 4;
                    return (ModuleSide)rotated switch {
                        ModuleSide.XPrev => mod.xPrev,
                        ModuleSide.XNext => mod.xNext,
                        ModuleSide.ZPrev => mod.zPrev,
                        ModuleSide.ZNext => mod.zNext,
                        _ => SocketUtility.WILDCARD
                    };
                }

                Module mod = new() {
                    Prefab = AssetDatabase.LoadAssetAtPath<GameObject>( PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot( modSocket.gameObject)),
                    angle = angle,
                    weight = modSocket.weight,
                    xPrev = rotateSideSocket(modSocket, ModuleSide.XPrev, angle),
                    xNext = rotateSideSocket(modSocket, ModuleSide.XNext, angle),
                    yPrev = rotateVerticalSocket(modSocket.yPrev, angle),
                    yNext = rotateVerticalSocket(modSocket.yNext, angle),
                    zPrev = rotateSideSocket(modSocket, ModuleSide.ZPrev, angle),
                    zNext = rotateSideSocket(modSocket, ModuleSide.ZNext, angle)
                };

                return mod;
            }).Distinct(new Module.ModuleComparer()).ToArray();

        if (folded == null || folded.Length != modules.Length)
            folded = new bool[modules.Length];

    }

    private Prototype[] BakePrototypes (Module[] modules)
    {
        Prototype[] prototypes = new Prototype[modules.Length];
        for (int i = 0; i < modules.Length; i++) {
            var currentModule = modules[i];
            prototypes[i] = new Prototype {
                prefab = currentModule.Prefab,
                angle = currentModule.angle,
                weight = currentModule.weight,
                sockets = new string[] { currentModule.xPrev, currentModule.xNext, currentModule.yPrev, currentModule.yNext, currentModule.zPrev, currentModule.zNext },
                xPrevConstraints = currentModule.ConnectingWith(modules, ModuleSide.XPrev),
                xNextConstraints = currentModule.ConnectingWith(modules, ModuleSide.XNext),
                yPrevConstraints = currentModule.ConnectingWith(modules, ModuleSide.YPrev),
                yNextConstraints = currentModule.ConnectingWith(modules, ModuleSide.YNext),
                zPrevConstraints = currentModule.ConnectingWith(modules, ModuleSide.ZPrev),
                zNextConstraints = currentModule.ConnectingWith(modules, ModuleSide.ZNext)
            };
        }

        return prototypes;
    }
}