using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;

public class Prototyper : EditorWindow
{
    [SerializeField]
    PrototypeLUT _pLUT;

    [SerializeField]
    VisualTreeAsset _template;
    [SerializeField]
    StyleSheet _windowSS, _activeButtonSS;

    [SerializeField]
    VisualElement _connectionsListContainer;
    [SerializeField]
    VisualElement _prototypeListContainer;

    [SerializeField]
    int _selectedPrototype = -1;


    [MenuItem("Tools/WFC/Prototyper")]
    public static void OpenWindow ()
    {
        Prototyper wnd = GetWindow<Prototyper>();
        wnd.titleContent = new GUIContent("Prototyper");
    }

    const string
        PrototypesListContainer = "prototypes-list",
        ConnectionsListContainer = "connections-list",

        SelectedPrototypePanel = "selected-prototype-panel",
        ErrorPanel = "error-panel",

        BakingButton = "bake-button",
        SocketXPrevButton = "socket-xPrev",
        SocketXNextButton = "socket-xNext",
        SocketYPrevButton = "socket-yPrev",
        SocketYNextButton = "socket-yNext",
        SocketZPrevButton = "socket-zPrev",
        SocketZNextButton = "socket-zNext",
        ConnectionXPrevButton = "connection-xPrev",
        ConnectionXNextButton = "connection-xNext",
        ConnectionYPrevButton = "connection-yPrev",
        ConnectionYNextButton = "connection-yNext",
        ConnectionZPrevButton = "connection-zPrev",
        ConnectionZNextButton = "connection-zNext",

        PrototypeCountLabel = "prototypes-count",
        PrototypeIDLabel = "prototype-id",
        PrototypeNameLabel = "prototype-name",
        PrototypeFlagLabel = "prototype-flag",
        PrototypeWeightLabel = "prototype-weight",
        PrototypeAngleLabel = "prototype-angle",
        SelectedSocketLabel = "selected-socket",
        ErrorLabel = "error-label",

        PrototypePreviewImage = "prototype-preview",

        ButtonXClass = "button-x",
        ButtonYClass = "button-y",
        ButtonZClass = "button-z";
        
        

    public void CreateGUI ()
    {
        // Root VisualElement object
        VisualElement root = rootVisualElement;

        // Import UXML
        _template.CloneTree(root);
        //root.styleSheets.Add(_windowSS);

        _prototypeListContainer = root.Q<VisualElement>(PrototypesListContainer);
        _connectionsListContainer = root.Q<VisualElement>(ConnectionsListContainer);

        _pLUT ??= PrototypeLUT.Editor_GetInstance();

        _pLUT.OnValueChanged += LoadPrototypesList;

        LoadPrototypesList();

        root.Q<Button>(BakingButton).clicked += BakeData;

        root.Q<Button>(SocketXPrevButton).clicked += () => ChangeSelectedSocket(0);
        root.Q<Button>(SocketXNextButton).clicked += () => ChangeSelectedSocket(1);
        root.Q<Button>(SocketYPrevButton).clicked += () => ChangeSelectedSocket(2);
        root.Q<Button>(SocketYNextButton).clicked += () => ChangeSelectedSocket(3);
        root.Q<Button>(SocketZPrevButton).clicked += () => ChangeSelectedSocket(4);
        root.Q<Button>(SocketZNextButton).clicked += () => ChangeSelectedSocket(5);


        rootVisualElement.Q<Button>(ConnectionXPrevButton).clicked += () => ChangeSelectedSocket(0);
        rootVisualElement.Q<Button>(ConnectionXNextButton).clicked += () => ChangeSelectedSocket(1);
        rootVisualElement.Q<Button>(ConnectionYPrevButton).clicked += () => ChangeSelectedSocket(2);
        rootVisualElement.Q<Button>(ConnectionYNextButton).clicked += () => ChangeSelectedSocket(3);
        rootVisualElement.Q<Button>(ConnectionZPrevButton).clicked += () => ChangeSelectedSocket(4);
        rootVisualElement.Q<Button>(ConnectionZNextButton).clicked += () => ChangeSelectedSocket(5);

        UpdateShownPrototypeData(_selectedPrototype);
    }

    private void OnDestroy ()
    {
        _pLUT.OnValueChanged -= LoadPrototypesList;
    }



    #region Visual Update
    private void LoadPrototypesList ()
    {
        rootVisualElement.Q<Label>(PrototypeCountLabel).text = _pLUT.Count.ToString();

        _selectedPrototype = -1;

        _prototypeListContainer.Query<Button>().ForEach(btn => btn.RemoveFromHierarchy());

        Action OnButton (int id) => () => OnPrototypeButtonClicked(id);
        for (int protoID = 0; protoID < _pLUT.Count; protoID++) {
            Prototype proto = _pLUT[protoID];
            var button = new Button() { text = proto.DisplayName() };
            button.clicked += OnButton(protoID);
            _prototypeListContainer.Add(button);
        }
        UpdateShownPrototypeData(_selectedPrototype);
    }
    private void OnPrototypeButtonClicked (int protoID)
    {
        if (_selectedPrototype >= 0) {

            var prevButton = _prototypeListContainer.Query<Button>().AtIndex(_selectedPrototype);

            prevButton.SetEnabled(true);
            prevButton.styleSheets.Remove(_activeButtonSS);

        }

        var button = _prototypeListContainer.Query<Button>().AtIndex(protoID);
        button.SetEnabled(false);
        button.styleSheets.Add(_activeButtonSS);
        _selectedPrototype = protoID;

        UpdateShownPrototypeData(protoID);
    }
    private void UpdateShownPrototypeData (int id)
    {
        Label idLabel = rootVisualElement.Q<Label>(PrototypeIDLabel);
        Label nameLabel = rootVisualElement.Q<Label>(PrototypeNameLabel);
        Label flagLabel = rootVisualElement.Q<Label>(PrototypeFlagLabel);
        Label weightLabel = rootVisualElement.Q<Label>(PrototypeWeightLabel);
        Label angleLabel = rootVisualElement.Q<Label>(PrototypeAngleLabel);
        VisualElement preview = rootVisualElement.Q<VisualElement>(PrototypePreviewImage);

        VisualElement panel = rootVisualElement.Q<VisualElement>(SelectedPrototypePanel);

        panel.visible = id >= 0 && id < _pLUT.Count;

        HideErrorPanel();

        if (id >= 0) {
            Prototype proto = _pLUT[id];

            if (proto.sockets == null || proto.sockets.Length != 6) {
                ShowErrorPanel("This Prototype isn't correctely generated. You may need to rebake data.");
            } else {

                idLabel.text = $"#{id}";
                nameLabel.text = proto.DisplayName(false);
                string flag = Enum.GetName(typeof(ModuleFlag), proto.flag);
                flagLabel.text = !string.IsNullOrEmpty(flag) ? flag : proto.flag==0?"None":"Multiple";
                weightLabel.text = $"Weight : {proto.weight}";
                angleLabel.text = $"Angle : {proto.angle * 90}";
                preview.style.backgroundImage = new StyleBackground(AssetPreview.GetAssetPreview(proto.prefab));

                rootVisualElement.Q<Button>(SocketXPrevButton).text = proto.sockets[0];
                rootVisualElement.Q<Button>(SocketXNextButton).text = proto.sockets[1];
                rootVisualElement.Q<Button>(SocketYPrevButton).text = proto.sockets[2];
                rootVisualElement.Q<Button>(SocketYNextButton).text = proto.sockets[3];
                rootVisualElement.Q<Button>(SocketZPrevButton).text = proto.sockets[4];
                rootVisualElement.Q<Button>(SocketZNextButton).text = proto.sockets[5];

                rootVisualElement.Q<Button>(ConnectionXPrevButton).text = $"{proto.xPrevConstraints.Length} connections";
                rootVisualElement.Q<Button>(ConnectionXNextButton).text = $"{proto.xNextConstraints.Length} connections";
                rootVisualElement.Q<Button>(ConnectionYPrevButton).text = $"{proto.yPrevConstraints.Length} connections";
                rootVisualElement.Q<Button>(ConnectionYNextButton).text = $"{proto.yNextConstraints.Length} connections";
                rootVisualElement.Q<Button>(ConnectionZPrevButton).text = $"{proto.zPrevConstraints.Length} connections";
                rootVisualElement.Q<Button>(ConnectionZNextButton).text = $"{proto.zNextConstraints.Length} connections";

            }
        } else {

            idLabel.text = "";
            nameLabel.text = "";
            flagLabel.text = "";
            weightLabel.text = $"Weight : ";
            angleLabel.text = $"Angle : ";
            preview.style.backgroundImage = new StyleBackground(AssetPreview.GetAssetPreview(null));

            rootVisualElement.Q<Button>(SocketXPrevButton).text = "";
            rootVisualElement.Q<Button>(SocketXNextButton).text = "";
            rootVisualElement.Q<Button>(SocketYPrevButton).text = "";
            rootVisualElement.Q<Button>(SocketYNextButton).text = "";
            rootVisualElement.Q<Button>(SocketZPrevButton).text = "";
            rootVisualElement.Q<Button>(SocketZNextButton).text = "";

            rootVisualElement.Q<Button>(ConnectionXPrevButton).text = "";
            rootVisualElement.Q<Button>(ConnectionXNextButton).text = "";
            rootVisualElement.Q<Button>(ConnectionYPrevButton).text = "";
            rootVisualElement.Q<Button>(ConnectionYNextButton).text = "";
            rootVisualElement.Q<Button>(ConnectionZPrevButton).text = "";
            rootVisualElement.Q<Button>(ConnectionZNextButton).text = "";
        }

        ChangeSelectedSocket(0);
    }
    private void ChangeSelectedSocket (int socketID)
    {
        void SetupLabel (Label label, string text, bool toggleX, bool toggleY, bool toggleZ)
        {
            label.EnableInClassList(ButtonXClass, toggleX);
            label.EnableInClassList(ButtonYClass, toggleY);
            label.EnableInClassList(ButtonZClass, toggleZ);
            label.text = text;
        }


        Action OnButton (int id) => () => OnPrototypeButtonClicked(id);
        _connectionsListContainer.Query<Button>().ToList().ForEach((button) => button.RemoveFromHierarchy());

        if (_selectedPrototype >= 0) {

            Label socketLabel = rootVisualElement.Q<Label>(SelectedSocketLabel);

            switch (socketID) {
            case 0:
                SetupLabel(socketLabel, "-X", true, false, false);
                break;

            case 1:
                SetupLabel(socketLabel, "+X", true, false, false);
                break;


            case 2:
                SetupLabel(socketLabel, "-Y", false, true, false);
                break;

            case 3:
                SetupLabel(socketLabel, "+Y", false, true, false);
                break;


            case 4:
                SetupLabel(socketLabel, "-Z", false, false, true);
                break;

            case 5:
                SetupLabel(socketLabel, "+Z", false, false, true);
                break;

            default:
                Debug.LogWarning("Non existing socket id");
                break;
            }


            Prototype proto = _pLUT[_selectedPrototype];
            var constraints = proto.GetConstraints(socketID);

            for (int constraintID = 0; constraintID < constraints.Length; constraintID++) {
                int protoID = constraints[constraintID];
                var button = new Button() { text = _pLUT[protoID].DisplayName() };

                if(protoID == _selectedPrototype) {
                    button.styleSheets.Add(_activeButtonSS);
                    button.SetEnabled(false);
                } else {
                    button.clicked += OnButton(constraints[constraintID]);
                }
                _connectionsListContainer.Add(button);
            }
        }
    }
    #endregion


    #region Error Panel
    void HideErrorPanel ()
    {
        rootVisualElement.Q<VisualElement>(SelectedPrototypePanel)
            .style.display = DisplayStyle.Flex;

        rootVisualElement.Q<VisualElement>(ErrorPanel)
            .style.display = DisplayStyle.None;
    }
    void ShowErrorPanel (string message)
    {
        rootVisualElement.Q<VisualElement>(SelectedPrototypePanel)
            .style.display = DisplayStyle.None;

        rootVisualElement.Q<VisualElement>(ErrorPanel)
            .style.display = DisplayStyle.Flex;

        rootVisualElement.Q<Label>(ErrorLabel)
            .text = message;
    }
    #endregion


    #region Generate Data
    void BakeData ()
    {
        Debug.LogWarning("Baking Data...");

        var modules = ExtractModules();
        var prototypes = BakePrototypes(modules);

        Undo.RecordObject(_pLUT, $"Baking {prototypes.Length} Prototype{(prototypes.Length > 1 ? 's' : "")}");
        _pLUT.Populate(prototypes);
        EditorUtility.SetDirty(_pLUT);

        LoadPrototypesList();
        
        Debug.LogWarning("Baking completed!");
    }

    /// <summary>
    /// Retrieve in the current scene, all gameobjects with ModuleSockets attached to them and generate Module variants for all the valid ones.
    /// <br/>Duplicates and modules linked to no prefab are discarded.
    /// </summary>
    private Module[] ExtractModules ()
    {
        // 1. Retrieve in the scene, all gameobjets with a ModuleSockets that is an instance of a prefab
        // 2. Generates 4 Module variants, one for each possible rotation
        // 3. Get ride of the potential dupplicate (in case of symmetry for example)


        StringComparer moduleNameComparer = StringComparer.Ordinal;

        var modules =
            FindObjectsOfType<ModuleSockets>()
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
                        if (SocketUtility.IsFlippable(socket))
                            if (angle % 2 == 0)
                                return socket;
                            else
                                return SocketUtility.IsFlipped(socket) ? $"{id}" : $"{id}f";

                        if (SocketUtility.IsDirectional(socket)
                            && SocketUtility.ParseSocketLastDigit(socket, out int baseAngle))
                            return $"{id}_{(baseAngle + angle) % 4}";

                        return SocketUtility.WILDCARD;
                    } else
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
                    flag = modSocket.flag,
                    xPrev = rotateSideSocket(modSocket, ModuleSide.XPrev, angle),
                    xNext = rotateSideSocket(modSocket, ModuleSide.XNext, angle),
                    yPrev = rotateVerticalSocket(modSocket.yPrev, angle),
                    yNext = rotateVerticalSocket(modSocket.yNext, angle),
                    zPrev = rotateSideSocket(modSocket, ModuleSide.ZPrev, angle),
                    zNext = rotateSideSocket(modSocket, ModuleSide.ZNext, angle)
                };

                return mod;
            })
            .Distinct(new Module.ModuleComparer())
            .OrderBy(mod => mod.Prefab.name, new AlphaStringComparer())
            .ToArray();

        return modules;
    }

    /// <summary>
    /// Generate a list of Prototypes containing their module data as well as lists of other connectable Prototypes (as indices to this same list).
    /// </summary>
    private Prototype[] BakePrototypes (Module[] modules)
    {
        Prototype[] prototypes = new Prototype[modules.Length];
        for (int i = 0; i < modules.Length; i++) {
            var currentModule = modules[i];
            prototypes[i] = new Prototype {
                prefab = currentModule.Prefab,
                angle = currentModule.angle,
                flag = currentModule.flag,
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

    #endregion
}