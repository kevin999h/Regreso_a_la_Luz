using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class Momento3Builder
{
    private const string ScenesPath = "Assets/Scenes/";
    private const string GameplayController = "Assets/Animations/Player/Player_Gameplay.controller";

    private static readonly Color Cyan = new Color(0.3f, 0.95f, 0.9f, 1f);
    private static readonly Color Gold = new Color(1f, 0.85f, 0.35f, 1f);
    private static readonly Color Dark = new Color(0.02f, 0.05f, 0.09f, 0.92f);
    private static readonly Color VoltColor = new Color(0.4f, 1f, 0.95f, 1f);
    private static readonly Color LumenColor = new Color(1f, 0.85f, 0.35f, 1f);

    private static Transform solidsRoot;

    private class DialogueUI
    {
        public GameObject panel;
        public Text nameText;
        public Text contentText;
    }

    private class HudUI
    {
        public Text counter;
        public Text objective;
        public Text toast;
        public Text boost;
        public Text levelName;
        public Text kills;
        public Text weapon;
        public Text missionText;
        public Text finalBody;
        public Image[] slots;
        public Image[] lifeSlots;
        public GameObject missionPanel;
        public GameObject finalPanel;
        public Button nextButton;
        public Button menuButton;
        public Button restartButton;
        public DialogueUI dialogue;
        public GameObject infoPanel;
        public Text infoText;
    }

    private class LevelSpec
    {
        public int index;
        public string sceneName;
        public string title;
        public string tilesPath;
        public string bgPath;
        public Color dark;
        public Color light;
        public Color dim;
        public int[][] blocks;
        public Vector2[] nuts;
        public Vector2[] batteries;
        public Vector2[] sparks;
        public float[] checkpoints;
        public float guideX;
        public float technicianX;
        public float generatorX;
        public float cameraMaxX;
        public float[] interactX;
        public string[] interactContent;
        public DialogueLine[] guideConv;
        public DialogueLine[] guideDone;
        public DialogueLine[] techConv;
        public DialogueLine[] techDone;
        public string finalTitle;
        public string finalBody;
        public string nextScene;
        public string collectiblePath;
        public string collectibleName;
        public string signAPath;
        public string signBPath;
        public string objectiveStart;
        public string objectiveReady;
        public string missionMessage;
        public float[] checkpointY;
        public Vector3[] enemies;
        public Vector3[] weapons;
        public bool hasBoss;
        public Vector3 boss;
    }

    private static DialogueLine L(string speaker, Color color, string text)
    {
        DialogueLine line = new DialogueLine();
        line.speakerName = speaker;
        line.nameColor = color;
        line.text = text;
        return line;
    }

    private static LevelSpec GetLevel(int n)
    {
        LevelSpec s = new LevelSpec();
        s.index = n;
        s.sceneName = "Nivel" + n;
        s.collectiblePath = "Assets/Sprites/Props/Nut.png";
        s.collectibleName = "Tuerca";
        s.signAPath = "Assets/Sprites/Props/Sign_Ohm.png";
        s.signBPath = "Assets/Sprites/Props/Terminal.png";
        s.guideDone = new[]
        {
            L("BIT", VoltColor, "¡Tienes las cinco tuercas! Corre al generador, al final del nivel, y habla con el Técnico Amp.")
        };

        if (n == 1)
        {
            s.title = "NIVEL 1 - Ciudad apagada";
            s.tilesPath = "Assets/Sprites/Environment/Circuit_Tiles.png";
            s.bgPath = "Assets/Sprites/Environment/GameBackground.png";
            s.dark = new Color(0.02f, 0.03f, 0.07f, 1f);
            s.light = new Color(0.55f, 0.82f, 0.98f, 1f);
            s.dim = new Color(0.4f, 0.45f, 0.6f, 1f);
            s.blocks = new int[][]
            {
                new int[] { -8, 21, -6, -3, 1 }, new int[] { 25, 40, -6, -3, 1 }, new int[] { 55, 90, -6, -3, 1 }, new int[] { 94, 118, -6, -3, 1 },
                new int[] { 42, 44, -2, -2, 1 }, new int[] { 47, 49, -1, -1, 1 }, new int[] { 51, 52, -2, -2, 1 },
                new int[] { 62, 63, -2, -1, 0 }, new int[] { 72, 84, -1, 0, 1 }, new int[] { 100, 102, -1, -1, 1 }
            };
            s.nuts = new Vector2[] { new Vector2(15.5f, -1.4f), new Vector2(23.5f, 0.4f), new Vector2(48.5f, 0.8f), new Vector2(78f, -1.4f), new Vector2(101.5f, 0.8f) };
            s.batteries = new Vector2[] { new Vector2(31f, -1.3f), new Vector2(67f, -1.3f) };
            s.sparks = new Vector2[0];
            s.checkpoints = new float[] { 56f };
            s.guideX = 5f;
            s.technicianX = 108f;
            s.generatorX = 113.5f;
            s.cameraMaxX = 110f;
            s.interactX = new float[] { 11f, 58f };
            s.interactContent = new string[]
            {
                "LEY DE OHM\n\nV = I x R\n\nEl voltaje (V, en voltios) es igual a la corriente (I, en amperios) por la resistencia (R, en ohmios).\n\nEjemplo: con 12 V y una resistencia de 4 ohmios, la corriente es I = V / R = 3 A.",
                "CIRCUITO CERRADO\n\nLa corriente solo circula si el circuito está CERRADO: fuente, conductores y carga conectados sin interrupciones.\n\nSi el circuito se abre, la corriente se detiene y la luz se apaga."
            };
            s.guideConv = new[]
            {
                L("BIT", VoltColor, "Bip... ¡Theo! Mi memoria está dañada, pero recuerdo esto: el generador de la ciudad se apagó y todo quedó a oscuras."),
                L("Theo (Tú)", LumenColor, "¿Qué necesitas que haga, BIT?"),
                L("BIT", VoltColor, "Cinco tuercas del generador salieron volando por el circuito. Recógelas todas."),
                L("BIT", VoltColor, "Salta con ESPACIO y deslízate con S para pasar los túneles bajos. Las pilas te dan turbo por unos segundos."),
                L("BIT", VoltColor, "Lee los carteles y terminales con E: te enseñarán cómo funciona un circuito. Cuando termines, busca al Técnico Amp junto al generador.")
            };
            s.techConv = new[]
            {
                L("Técnico Amp", VoltColor, "Aún faltan tuercas. Sin las cinco, el generador no arranca."),
                L("Técnico Amp", VoltColor, "Revisa el HUD: ahí ves cuántas llevas.")
            };
            s.techDone = new[]
            {
                L("Técnico Amp", VoltColor, "¡Excelente! Con estas cinco tuercas el generador queda bien ajustado."),
                L("Theo (Tú)", LumenColor, "¿Y ahora qué falta?"),
                L("Técnico Amp", VoltColor, "Un circuito necesita fuente, conductores y carga. Ya tenemos todo: ¡que vuelva la luz!")
            };
            s.finalTitle = "¡LUZ RESTAURADA!";
            s.finalBody = "Nivel 1 completado.\n\nAprendiste que la corriente fluye en un circuito cerrado y que V = I x R (Ley de Ohm).";
            s.nextScene = "Nivel2";
        }
        else if (n == 2)
        {
            s.title = "NIVEL 2 - Zona infectada";
            s.tilesPath = "Assets/Sprites/Environment/Infected_Tiles.png";
            s.bgPath = "Assets/Sprites/Environment/Infected_Background.png";
            s.collectiblePath = "Assets/Sprites/Props/Chip.png";
            s.collectibleName = "Chip";
            s.signAPath = "Assets/Sprites/Props/Sign_Warning.png";
            s.signBPath = "Assets/Sprites/Props/Sign_Fuse.png";
            s.dark = new Color(0.01f, 0.05f, 0.03f, 1f);
            s.light = new Color(0.6f, 0.95f, 0.7f, 1f);
            s.dim = new Color(0.35f, 0.5f, 0.4f, 1f);
            s.blocks = new int[][]
            {
                new int[] { -8, 22, -6, -3, 1 }, new int[] { 26, 52, -6, -3, 1 }, new int[] { 53, 58, -6, -2, 1 }, new int[] { 59, 66, -6, -1, 1 },
                new int[] { 67, 74, -6, -2, 1 }, new int[] { 75, 100, -6, -3, 1 }, new int[] { 105, 150, -6, -3, 1 },
                new int[] { 44, 46, -1, -1, 1 }, new int[] { 84, 86, -1, -1, 1 }, new int[] { 102, 103, -2, -2, 1 }
            };
            s.nuts = new Vector2[] { new Vector2(45f, 0.8f), new Vector2(62.5f, 0.8f), new Vector2(85f, 0.8f), new Vector2(102.8f, -0.2f), new Vector2(112f, -1.3f) };
            s.batteries = new Vector2[] { new Vector2(35f, -1.3f), new Vector2(95f, -1.3f) };
            s.sparks = new Vector2[0];
            s.checkpoints = new float[] { 27f, 62f, 106f, 124f };
            s.checkpointY = new float[] { -2f, 0f, -2f, -2f };
            s.enemies = new Vector3[] { new Vector3(28f, -2f, 3f), new Vector3(37f, -2f, 4f), new Vector3(49f, -2f, 3f), new Vector3(56f, -1f, 2f), new Vector3(71f, -1f, 2f), new Vector3(90f, -2f, 4f), new Vector3(112f, -2f, 4f) };
            s.weapons = new Vector3[] { new Vector3(14f, -1.4f, 1f), new Vector3(121f, -1.4f, 2f) };
            s.hasBoss = true;
            s.boss = new Vector3(136f, -2f, 8f);
            s.guideX = 3f;
            s.technicianX = 147f;
            s.generatorX = 150f;
            s.cameraMaxX = 143f;
            s.interactX = new float[] { 9f, 79f };
            s.interactContent = new string[]
            {
                "CORTOCIRCUITO\n\nOcurre cuando la corriente encuentra un camino de resistencia casi nula.\n\nComo I = V / R, si R es muy pequeña la corriente se vuelve enorme y puede quemar el circuito.",
                "EL FUSIBLE\n\nEs un componente de seguridad: si la corriente supera su límite, se funde y abre el circuito.\n\nAsí protege a los demás componentes de un cortocircuito."
            };
            s.guideConv = new[]
            {
                L("BIT", VoltColor, "¡Theo! Un virus invadió la Zona infectada y corrompió nuestros chips. Tienes 3 vidas para superar el sector."),
                L("Theo (Tú)", LumenColor, "¿Y cómo los enfrento?"),
                L("BIT", VoltColor, "Busca el hacha de mantenimiento más adelante y ataca con J o clic, o sáltales encima."),
                L("BIT", VoltColor, "Recoge 5 chips y derrota a los virus. Al final te espera el Gran Cortocircuito (resiste 9 golpes): necesitarás el Hacha de plasma.")
            };
            s.guideDone = new[]
            {
                L("BIT", VoltColor, "¡Lo lograste! Ve al generador, al final del nivel, y habla con el Técnico Amp.")
            };
            s.techConv = new[]
            {
                L("Técnico Amp", VoltColor, "Aún no puedo reparar: faltan chips o el Gran Cortocircuito sigue activo.")
            };
            s.techDone = new[]
            {
                L("Técnico Amp", VoltColor, "¡Excelente! Sistema limpio y chips instalados."),
                L("Theo (Tú)", LumenColor, "¿Ya recuperamos toda la ciudad?"),
                L("Técnico Amp", VoltColor, "Falta el último sector: el Núcleo Central de Alta Tensión. ¡Ve hacia allá!")
            };
            s.objectiveStart = "Misión: recoge 5 chips y derrota a los virus y al Gran Cortocircuito";
            s.objectiveReady = "Misión: habla con el Técnico Amp, junto al generador";
            s.missionMessage = "¡Misión cumplida! Reuniste los 5 chips y derrotaste al Gran Cortocircuito.\nHabla con el Técnico Amp, junto al generador.";
            s.finalTitle = "¡ZONA INFECTADA LIMPIA!";
            s.finalBody = "Nivel 2 completado.\n\nAprendiste qué es un cortocircuito y para qué sirve un fusible. ¡Prepárate para el Núcleo Central!";
            s.nextScene = "Nivel3";
        }
        else
        {
            s.title = "NIVEL 3 - Núcleo Central";
            s.tilesPath = "Assets/Sprites/Environment/Core_Tiles.png";
            s.bgPath = "Assets/Sprites/Environment/Level3_Background.png";
            s.collectiblePath = "Assets/Sprites/Props/Chip.png";
            s.collectibleName = "Núcleo";
            s.signAPath = "Assets/Sprites/Props/Sign_Ohm.png";
            s.signBPath = "Assets/Sprites/Props/Terminal.png";
            s.dark = new Color(0.04f, 0.01f, 0.09f, 1f);
            s.light = new Color(0.75f, 0.55f, 1f, 1f);
            s.dim = new Color(0.55f, 0.4f, 0.75f, 1f);
            s.blocks = new int[][]
            {
                new int[] { -8, 28, -6, -3, 1 },
                new int[] { 32, 62, -6, -3, 1 },
                new int[] { 44, 46, -1, 0, 0 }, // Plataforma elevada / paso limpio
                new int[] { 66, 98, -6, -3, 1 },
                new int[] { 75, 79, -1, -1, 1 },
                new int[] { 83, 87, 0, 0, 1 },
                new int[] { 102, 138, -6, -3, 1 },
                new int[] { 114, 116, -1, 0, 0 },
                new int[] { 124, 130, -1, -1, 1 },
                new int[] { 142, 178, -6, -3, 1 },
                new int[] { 152, 156, -1, -1, 1 },
                new int[] { 160, 165, 0, 0, 1 },
                new int[] { 182, 222, -6, -3, 1 },
                new int[] { 196, 198, -1, 0, 0 },
                new int[] { 206, 212, -1, -1, 1 },
                new int[] { 226, 285, -6, -3, 1 }
            };
            s.nuts = new Vector2[]
            {
                new Vector2(45.5f, 1.6f),
                new Vector2(85.5f, 1.8f),
                new Vector2(127.5f, 0.8f),
                new Vector2(163f, 1.8f),
                new Vector2(209.5f, 0.8f)
            };
            s.batteries = new Vector2[]
            {
                new Vector2(36f, -1.3f),
                new Vector2(106f, -1.3f),
                new Vector2(186f, -1.3f),
                new Vector2(232f, -1.3f)
            };
            s.sparks = new Vector2[]
            {
                new Vector2(54f, -2f),
                new Vector2(92f, -2f),
                new Vector2(134f, -2f),
                new Vector2(172f, -2f),
                new Vector2(216f, -2f)
            };
            s.checkpoints = new float[] { 34f, 68f, 104f, 144f, 184f, 228f };
            s.checkpointY = new float[] { -2f, -2f, -2f, -2f, -2f, -2f };
            s.enemies = new Vector3[]
            {
                new Vector3(22f, -2f, 3f),
                new Vector3(40f, -2f, 3f),
                new Vector3(58f, -2f, 3f),
                new Vector3(73f, -2f, 4f),
                new Vector3(88f, -2f, 4f),
                new Vector3(110f, -2f, 3f),
                new Vector3(122f, -2f, 4f),
                new Vector3(148f, -2f, 4f),
                new Vector3(166f, -2f, 4f),
                new Vector3(190f, -2f, 4f),
                new Vector3(204f, -2f, 4f),
                new Vector3(219f, -2f, 3f),
                new Vector3(236f, -2f, 4f),
                new Vector3(246f, -2f, 4f)
            };
            s.weapons = new Vector3[]
            {
                new Vector3(12f, -1.4f, 3f),
                new Vector3(50f, -1.4f, 4f),
                new Vector3(96f, -1.4f, 4f),
                new Vector3(136f, -1.4f, 4f),
                new Vector3(175f, -1.4f, 4f),
                new Vector3(214f, -1.4f, 4f),
                new Vector3(248f, -1.4f, 4f),
                new Vector3(268f, -1.4f, 4f)
            };
            s.hasBoss = true;
            s.boss = new Vector3(262f, -2f, 10f);
            s.guideX = 3f;
            s.technicianX = 274f;
            s.generatorX = 279f;
            s.cameraMaxX = 272f;
            s.interactX = new float[] { 18f, 133f };
            s.interactContent = new string[]
            {
                "POTENCIA ELÉCTRICA\n\nP = V x I\n\nLa potencia (P, en vatios W) es la energía que consume o entrega un circuito por segundo.\n\nSi el voltaje es 120 V y la corriente es 5 A, la potencia es P = 120 x 5 = 600 W.",
                "ESTABILIDAD DE LA RED\n\nPara que toda Ciudad Circuito funcione sin apagones, el Núcleo Central debe mantener voltaje constante, fusibles activos y cero fugas a tierra."
            };
            s.guideConv = new[]
            {
                L("BIT", VoltColor, "¡Bienvenido al Núcleo Central, Theo! Este es el recorrido más largo y peligroso de toda la red."),
                L("Theo (Tú)", LumenColor, "¡Hay demasiados virus allá adelante!"),
                L("BIT", VoltColor, "Recoge la Pistola de Pulsos Eléctricos aquí adelante. Dispara a distancia con J o clic, pero cuida tus balas: ¡son contadas!"),
                L("BIT", VoltColor, "Puedes recoger cargadores extra en el camino. Reúne los 5 núcleos, derrota a los 14 virus y vence al Megacortocircuito final.")
            };
            s.guideDone = new[]
            {
                L("BIT", VoltColor, "¡Lo conseguiste, Theo! Habla con el Técnico Amp junto al reactor maestro para encender toda la ciudad.")
            };
            s.techConv = new[]
            {
                L("Técnico Amp", VoltColor, "Aún quedan núcleos por recolectar o el jefe del Núcleo Central sigue bloqueando la red.")
            };
            s.techDone = new[]
            {
                L("Técnico Amp", VoltColor, "¡Increíble trabajo, Theo! El Núcleo Central está al 100% de potencia."),
                L("Theo (Tú)", LumenColor, "¡Toda Ciudad Circuito volvió a iluminarse!"),
                L("Técnico Amp", VoltColor, "Dominaste la Ley de Ohm, el circuito cerrado, los fusibles y la potencia eléctrica. ¡Felicitaciones, héroe de la energía!")
            };
            s.objectiveStart = "Misión Final: recoge los 5 núcleos y derrota al Megacortocircuito";
            s.objectiveReady = "Misión cumplida: habla con el Técnico Amp junto al reactor final";
            s.missionMessage = "¡Misión Final Cumplida! Reuniste los 5 núcleos y venciste al Megacortocircuito.\nHabla con el Técnico Amp junto al reactor para restaurar la luz.";
            s.finalTitle = "¡FELICITACIONES!";
            s.finalBody = "¡COMPLETASTE REGRESO A LA LUZ!\n\nRestauraste el 100% de la energía de Ciudad Circuito junto a Theo, BIT y el Técnico Amp.\n\nConceptos dominados:\n• Nivel 1: Ley de Ohm (V = I x R) y Circuito Cerrado\n• Nivel 2: Cortocircuitos y Protección con Fusibles\n• Nivel 3: Potencia Eléctrica (P = V x I) y Red Central\n\n¡Excelente trabajo, Ingeniero!";
            s.nextScene = "";
        }
        return s;
    }

    [MenuItem("Momento3/Construir todo")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureSortingLayer("Background", true);
        EnsureSortingLayer("Characters", false);

        AnimatorController controller = BuildGameplayController();
        BuildMainMenu();
        for (int n = 1; n <= 3; n++)
        {
            BuildLevel(GetLevel(n), controller);
        }

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenesPath + "MainMenu.unity", true),
            new EditorBuildSettingsScene(ScenesPath + "Nivel1.unity", true),
            new EditorBuildSettingsScene(ScenesPath + "Nivel2.unity", true),
            new EditorBuildSettingsScene(ScenesPath + "Nivel3.unity", true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(ScenesPath + "MainMenu.unity");
        EditorUtility.DisplayDialog("Momento 3", "¡Listo! MainMenu, Nivel1, Nivel2 y Nivel3 construidas y registradas en Build Settings.", "OK");
    }

    private static SerializedObject LoadTagManager()
    {
        return new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
    }

    private static void EnsureSortingLayer(string layerName, bool atFront)
    {
        SerializedObject tm = LoadTagManager();
        SerializedProperty layers = tm.FindProperty("m_SortingLayers");
        for (int i = 0; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == layerName)
            {
                return;
            }
        }

        int index = atFront ? 0 : layers.arraySize;
        layers.InsertArrayElementAtIndex(index);
        SerializedProperty e = layers.GetArrayElementAtIndex(index);
        e.FindPropertyRelative("name").stringValue = layerName;
        e.FindPropertyRelative("uniqueID").intValue = layerName.GetHashCode() & 0x7fffffff;
        e.FindPropertyRelative("locked").boolValue = false;
        tm.ApplyModifiedProperties();
    }

    private static Font GetFont()
    {
#if UNITY_2022_2_OR_NEWER
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
    }

    private static Sprite[] LoadSprites(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
    }

    private static Sprite LoadSingle(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void Place(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static Text Txt(Transform parent, string name, string content, int size, Color color, TextAnchor align, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 dim)
    {
        GameObject go = NewUI(name, parent);
        Text t = go.AddComponent<Text>();
        t.font = GetFont();
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        Place(go, anchor, pivot, pos, dim);
        return t;
    }

    private static GameObject Box(Transform parent, string name, Color color, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 dim)
    {
        GameObject go = NewUI(name, parent);
        Image img = go.AddComponent<Image>();
        img.color = color;
        Place(go, anchor, pivot, pos, dim);
        return go;
    }

    private static Button Btn(Transform parent, string name, string label, Vector2 pos, float width)
    {
        GameObject go = NewUI(name, parent);
        Place(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(width, 110f));

        Image img = go.AddComponent<Image>();
        img.sprite = LoadSingle("Assets/Sprites/UI/Button.png");
        img.type = Image.Type.Sliced;

        Button b = go.AddComponent<Button>();
        b.targetGraphic = img;
        ColorBlock colors = b.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.6f, 1f, 0.95f, 1f);
        colors.pressedColor = new Color(0.35f, 0.7f, 0.7f, 1f);
        colors.selectedColor = Color.white;
        b.colors = colors;

        Text t = Txt(go.transform, "Text", label, 40, new Color(0.75f, 1f, 0.98f, 1f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        t.fontStyle = FontStyle.Bold;
        Stretch(t.gameObject);
        return b;
    }

    private static void Fade(GameObject go)
    {
        go.AddComponent<PanelFade>();
    }

    private static GameObject NewCanvas()
    {
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform));
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        return canvasGo;
    }

    private static GameObject CreateAudio()
    {
        GameObject go = new GameObject("AudioManager");
        AudioSource music = go.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.loop = true;
        AudioSource sfx = go.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.loop = false;
        AudioManager manager = go.AddComponent<AudioManager>();

        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("musicSource").objectReferenceValue = music;
        so.FindProperty("sfxSource").objectReferenceValue = sfx;

        string[] sfxNames = { "click", "collect", "jump", "dialogue", "powerup", "complete", "hurt", "attack", "hit" };
        SerializedProperty clips = so.FindProperty("sfxClips");
        clips.arraySize = sfxNames.Length;
        for (int i = 0; i < sfxNames.Length; i++)
        {
            clips.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/" + sfxNames[i] + ".wav");
        }

        string[] scenes = { "MainMenu", "Nivel1", "Nivel2", "Nivel3" };
        string[] tracks = { "menu", "nivel1", "nivel2", "nivel3" };
        SerializedProperty sceneNames = so.FindProperty("sceneNames");
        SerializedProperty sceneMusic = so.FindProperty("sceneMusic");
        sceneNames.arraySize = scenes.Length;
        sceneMusic.arraySize = scenes.Length;
        for (int i = 0; i < scenes.Length; i++)
        {
            sceneNames.GetArrayElementAtIndex(i).stringValue = scenes[i];
            sceneMusic.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/" + tracks[i] + ".wav");
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    private static void BuildMainMenu()
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.03f, 0.07f, 1f);
        cam.orthographic = true;
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        GameObject canvasGo = NewCanvas();

        GameObject bgGo = NewUI("BackgroundImage", canvasGo.transform);
        Image bg = bgGo.AddComponent<Image>();
        bg.sprite = LoadSingle("Assets/Sprites/UI/MenuBackground.png");
        bg.preserveAspect = false;
        Stretch(bgGo);

        Text title = Txt(canvasGo.transform, "TitleText", "REGRESO A LA LUZ", 140, Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 400f), new Vector2(1800f, 200f));
        title.fontStyle = FontStyle.Bold;
        Outline outline = title.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.02f, 0.1f, 0.15f, 1f);
        outline.effectDistance = new Vector2(6f, -6f);

        Text subtitle = Txt(canvasGo.transform, "SubtitleText", "Un Serious Game sobre circuitos eléctricos", 50, Cyan, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 290f), new Vector2(1600f, 80f));
        Outline o2 = subtitle.gameObject.AddComponent<Outline>();
        o2.effectColor = new Color(0f, 0f, 0f, 1f);
        o2.effectDistance = new Vector2(3f, -3f);

        GameObject menuPanel = NewUI("MenuPanel", canvasGo.transform);
        Stretch(menuPanel);
        Button play = Btn(menuPanel.transform, "Btn_Play", "JUGAR", new Vector2(-690f, -400f), 420f);
        Button instructions = Btn(menuPanel.transform, "Btn_Instructions", "INSTRUCCIONES", new Vector2(-230f, -400f), 420f);
        Button credits = Btn(menuPanel.transform, "Btn_Credits", "CRÉDITOS", new Vector2(230f, -400f), 420f);
        Button quit = Btn(menuPanel.transform, "Btn_Quit", "SALIR", new Vector2(690f, -400f), 420f);
        Button cont = Btn(menuPanel.transform, "Btn_Continue", "CONTINUAR", new Vector2(0f, -270f), 420f);

        GameObject instructionsPanel = NewUI("InstructionsPanel", canvasGo.transform);
        Image insImg = instructionsPanel.AddComponent<Image>();
        insImg.color = new Color(0f, 0f, 0f, 0.95f);
        Stretch(instructionsPanel);
        Text insTitle = Txt(instructionsPanel.transform, "Titulo", "INSTRUCCIONES", 70, Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 440f), new Vector2(1500f, 110f));
        insTitle.fontStyle = FontStyle.Bold;
        string insBody =
            "HISTORIA\n" +
            "Ciudad Circuito se quedó a oscuras: su generador se apagó y cinco tuercas salieron disparadas por el circuito. Theo, un joven reparador, y BIT, un dron de memoria dañada, deben recuperarlas para devolver la luz. El nivel 1 enseña la Ley de Ohm y el circuito cerrado; el nivel 2 enseña qué es un cortocircuito y para qué sirve un fusible, mientras enfrentas virus que invadieron el sistema.\n\n" +
            "OBJETIVO\n" +
            "Nivel 1: recoge las 5 tuercas. Nivel 2: recoge 5 chips, derrota a los virus y al Gran Cortocircuito. Luego habla con el Técnico Amp, junto al generador.\n\n" +
            "CONTROLES\n" +
            "A / D o flechas: mover     |     ESPACIO: saltar     |     S: agacharse y deslizarse\n" +
            "E: hablar y leer     |     J o clic: atacar     |     R: reiniciar     |     M: silenciar\n\n" +
            "AYUDAS\n" +
            "Busca las armas del nivel para poder atacar. Las pilas te dan turbo por unos segundos. Las chispas te devuelven al último checkpoint. Si caes a un vacío, reapareces.";
        Txt(instructionsPanel.transform, "Contenido", insBody, 30, Color.white, TextAnchor.UpperLeft, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1700f, 740f));
        Button closeIns = Btn(instructionsPanel.transform, "Btn_CloseInstructions", "VOLVER", new Vector2(0f, -440f), 420f);

        GameObject creditsPanel = NewUI("CreditsPanel", canvasGo.transform);
        Image panelImg = creditsPanel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.95f);
        Stretch(creditsPanel);
        Text creditsTitle = Txt(creditsPanel.transform, "Titulo", "CRÉDITOS", 70, Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 440f), new Vector2(1500f, 110f));
        creditsTitle.fontStyle = FontStyle.Bold;
        string body =
            "REGRESO A LA LUZ\n" +
            "Versión Prototipo - Momento 3 - Semestre II / 2026\n\n" +
            "1. PROPÓSITO EDUCATIVO\n" +
            "- Área: Física - Electricidad y circuitos\n" +
            "- El jugador repara un generador y aprende la Ley de Ohm, el circuito cerrado, los cortocircuitos y los fusibles.\n\n" +
            "2. EQUIPO DE DESARROLLO\n" +
            "- Diseño, programación, arte e interfaces: Kevin Yoany Hernández Ramírez\n" +
            "- Docente: Ing. Luz Karime Castellanos Joya - Universitaria Tecnológica de Santander\n\n" +
            "3. HERRAMIENTAS Y RECURSOS\n" +
            "- Unity 2021.3 LTS, C#, GitHub y Claude (IA) como apoyo.\n" +
            "- Arte, música y efectos de sonido: elaborados para este prototipo.\n\n" +
            "AVISO LEGAL Y ACADÉMICO: prototipo desarrollado con fines formativos y académicos. No posee fines comerciales ni lucrativos.";
        Txt(creditsPanel.transform, "Credito", body, 30, Color.white, TextAnchor.UpperLeft, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1700f, 740f));
        Button closeCredits = Btn(creditsPanel.transform, "Btn_CloseCredits", "VOLVER", new Vector2(0f, -440f), 420f);

        GameObject managerGo = new GameObject("MenuManager");
        MainMenuManager manager = managerGo.AddComponent<MainMenuManager>();
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("creditsPanel").objectReferenceValue = creditsPanel;
        so.FindProperty("instructionsPanel").objectReferenceValue = instructionsPanel;
        so.FindProperty("continueButton").objectReferenceValue = cont.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(play.onClick, manager.PlayGame);
        UnityEventTools.AddPersistentListener(instructions.onClick, manager.OpenInstructions);
        UnityEventTools.AddPersistentListener(credits.onClick, manager.OpenCredits);
        UnityEventTools.AddPersistentListener(quit.onClick, manager.QuitGame);
        UnityEventTools.AddPersistentListener(cont.onClick, manager.ContinueGame);
        UnityEventTools.AddPersistentListener(closeIns.onClick, manager.CloseInstructions);
        UnityEventTools.AddPersistentListener(closeCredits.onClick, manager.CloseCredits);

        Fade(instructionsPanel);
        Fade(creditsPanel);
        instructionsPanel.SetActive(false);
        creditsPanel.SetActive(false);

        CreateAudio();
        EditorSceneManager.SaveScene(scene, ScenesPath + "MainMenu.unity");
    }

    private static AnimationClip Clip(string name)
    {
        return AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player/Player_" + name + ".anim");
    }

    private static AnimatorStateTransition Tr(AnimatorState from, AnimatorState to)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0f;
        return t;
    }

    private static AnimatorStateTransition TrAny(AnimatorStateMachine sm, AnimatorState to)
    {
        AnimatorStateTransition t = sm.AddAnyStateTransition(to);
        t.hasExitTime = false;
        t.duration = 0f;
        t.canTransitionToSelf = false;
        return t;
    }

    private static AnimatorController BuildGameplayController()
    {
        AssetDatabase.DeleteAsset(GameplayController);
        AnimatorController c = AnimatorController.CreateAnimatorControllerAtPath(GameplayController);
        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        c.AddParameter(new AnimatorControllerParameter { name = "IsGrounded", type = AnimatorControllerParameterType.Bool, defaultBool = true });
        c.AddParameter("IsSliding", AnimatorControllerParameterType.Bool);
        c.AddParameter("IsCrouching", AnimatorControllerParameterType.Bool);
        c.AddParameter("IsBoosted", AnimatorControllerParameterType.Bool);
        c.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = c.layers[0].stateMachine;
        AnimatorState idle = sm.AddState("Player_Idle");
        idle.motion = Clip("Idle");
        AnimatorState walk = sm.AddState("Player_Walk");
        walk.motion = Clip("Walk");
        AnimatorState run = sm.AddState("Player_Run");
        run.motion = Clip("Run");
        AnimatorState jump = sm.AddState("Player_Jump");
        jump.motion = Clip("Jump");
        AnimatorState slide = sm.AddState("Player_Slide");
        slide.motion = Clip("Slide");
        AnimatorState crawl = sm.AddState("Player_Crawl");
        crawl.motion = Clip("Crawl");
        AnimatorState hurt = sm.AddState("Player_Hurt");
        hurt.motion = Clip("Hurt");
        sm.defaultState = idle;

        AnimatorStateTransition anyHurt = sm.AddAnyStateTransition(hurt);
        anyHurt.hasExitTime = false;
        anyHurt.duration = 0f;
        anyHurt.canTransitionToSelf = true;
        anyHurt.AddCondition(AnimatorConditionMode.If, 0f, "Hurt");

        AnimatorStateTransition hurtToIdle = hurt.AddTransition(idle);
        hurtToIdle.hasExitTime = true;
        hurtToIdle.exitTime = 1f;
        hurtToIdle.duration = 0f;

        AnimatorState attack = sm.AddState("Player_Attack");
        attack.motion = Clip("Attack");
        AnimatorStateTransition anyAttack = sm.AddAnyStateTransition(attack);
        anyAttack.hasExitTime = false;
        anyAttack.duration = 0f;
        anyAttack.canTransitionToSelf = true;
        anyAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
        AnimatorStateTransition attackToIdle = attack.AddTransition(idle);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 1f;
        attackToIdle.duration = 0f;

        Tr(idle, walk).AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");
        Tr(walk, idle).AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");
        Tr(walk, run).AddCondition(AnimatorConditionMode.If, 0f, "IsBoosted");
        Tr(run, walk).AddCondition(AnimatorConditionMode.IfNot, 0f, "IsBoosted");
        Tr(run, idle).AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");

        TrAny(sm, slide).AddCondition(AnimatorConditionMode.If, 0f, "IsSliding");
        TrAny(sm, crawl).AddCondition(AnimatorConditionMode.If, 0f, "IsCrouching");
        TrAny(sm, jump).AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");

        Tr(slide, idle).AddCondition(AnimatorConditionMode.IfNot, 0f, "IsSliding");
        Tr(crawl, idle).AddCondition(AnimatorConditionMode.IfNot, 0f, "IsCrouching");
        Tr(jump, idle).AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");

        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
        return c;
    }

    private static Tile[] BuildTiles(string sheetPath)
    {
        Sprite[] sprites = LoadSprites(sheetPath);
        Tile[] tiles = new Tile[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            string path = "Assets/Tilemaps/" + sprites[i].name + ".asset";
            AssetDatabase.DeleteAsset(path);
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprites[i];
            tile.colliderType = Tile.ColliderType.Grid;
            AssetDatabase.CreateAsset(tile, path);
            tiles[i] = tile;
        }
        return tiles;
    }

    private static void Fill(Tilemap tm, Tile[] tiles, int x0, int x1, int yBottom, int yTop, bool capBottom)
    {
        for (int x = x0; x <= x1; x++)
        {
            int col = x0 == x1 ? 1 : (x == x0 ? 0 : (x == x1 ? 2 : 1));
            for (int y = yBottom; y <= yTop; y++)
            {
                int row = 1;
                if (y == yTop) row = 0;
                else if (y == yBottom && capBottom) row = 2;
                tm.SetTile(new Vector3Int(x, y, 0), tiles[row * 3 + col]);
            }
        }
    }

    private static void Block(Tilemap tm, Tile[] tiles, int[] b)
    {
        Fill(tm, tiles, b[0], b[1], b[2], b[3], b[4] == 1);

        float width = b[1] - b[0] + 1;
        float height = b[3] - b[2] + 1;
        GameObject solid = new GameObject("Solid_" + b[0] + "_" + b[2]);
        solid.transform.SetParent(solidsRoot, false);
        solid.transform.position = new Vector3(b[0] + width * 0.5f, b[2] + height * 0.5f, 0f);
        BoxCollider2D box = solid.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width, height);
    }

    private static GameObject Prop(string name, Sprite sprite, Vector3 pos, string sortingLayer, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        return go;
    }

    private static GameObject MakePrompt(Transform parent, Sprite sprite, Vector3 localPos)
    {
        GameObject go = new GameObject("InteractPrompt");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Characters";
        sr.sortingOrder = 20;
        go.SetActive(false);
        return go;
    }

    private static void SetSprites(SerializedObject so, string field, Sprite[] sprites)
    {
        SerializedProperty arr = so.FindProperty(field);
        arr.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
        {
            arr.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
    }

    private static void SetLines(SerializedObject so, string field, DialogueLine[] lines)
    {
        SerializedProperty arr = so.FindProperty(field);
        arr.arraySize = lines.Length;
        for (int i = 0; i < lines.Length; i++)
        {
            SerializedProperty e = arr.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("speakerName").stringValue = lines[i].speakerName;
            e.FindPropertyRelative("nameColor").colorValue = lines[i].nameColor;
            e.FindPropertyRelative("text").stringValue = lines[i].text;
        }
    }

    private static void AddLoop(GameObject go, Sprite[] frames, float fps)
    {
        SpriteLoop loop = go.AddComponent<SpriteLoop>();
        SerializedObject sl = new SerializedObject(loop);
        SetSprites(sl, "frames", frames);
        sl.FindProperty("framesPerSecond").floatValue = fps;
        sl.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateNpc(string name, Vector3 pos, Sprite[] frames, Sprite prompt, DialogueLine[] conv, DialogueLine[] done, bool finishes, DialogueUI ui)
    {
        GameObject npc = Prop(name, frames[0], pos, "Characters", 2);
        npc.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        AddLoop(npc, frames, 4f);

        BoxCollider2D trigger = npc.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.offset = new Vector2(0f, 0.8f);
        trigger.size = new Vector2(3.5f, 1.6f);

        GameObject promptGo = MakePrompt(npc.transform, prompt, new Vector3(0f, 1.5f, 0f));
        promptGo.transform.localScale = new Vector3(1f / 1.5f, 1f / 1.5f, 1f);

        NPCDialogue dialogue = npc.AddComponent<NPCDialogue>();
        SerializedObject so = new SerializedObject(dialogue);
        SetLines(so, "conversation", conv);
        SetLines(so, "completedConversation", done);
        so.FindProperty("finishesLevel").boolValue = finishes;
        so.FindProperty("dialoguePanel").objectReferenceValue = ui.panel;
        so.FindProperty("nameText").objectReferenceValue = ui.nameText;
        so.FindProperty("contentText").objectReferenceValue = ui.contentText;
        so.FindProperty("interactionPrompt").objectReferenceValue = promptGo;
        so.ApplyModifiedPropertiesWithoutUndo();
        return npc;
    }

    private static GameObject CreateInteractable(string name, Sprite sprite, Vector3 pos, Sprite prompt, string content, HudUI hud)
    {
        GameObject go = Prop(name, sprite, pos, "Characters", 1);
        go.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        BoxCollider2D trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.offset = new Vector2(0f, 0.5f);
        trigger.size = new Vector2(3f, 1.4f);

        GameObject promptGo = MakePrompt(go.transform, prompt, new Vector3(0f, 1.3f, 0f));
        promptGo.transform.localScale = new Vector3(1f / 1.5f, 1f / 1.5f, 1f);

        InteractableObject io = go.AddComponent<InteractableObject>();
        SerializedObject so = new SerializedObject(io);
        so.FindProperty("promptFloatingText").objectReferenceValue = promptGo;
        so.FindProperty("infoDisplayPanel").objectReferenceValue = hud.infoPanel;
        so.FindProperty("infoTextComponent").objectReferenceValue = hud.infoText;
        so.FindProperty("educationalContent").stringValue = content;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    private static void CreateNut(Vector3 pos, Sprite[] frames, string itemName)
    {
        GameObject go = Prop("Tuerca", frames[0], pos, "Characters", 3);
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;
        AddLoop(go, frames, 8f);
        CollectibleItem item = go.AddComponent<CollectibleItem>();
        SerializedObject cso = new SerializedObject(item);
        cso.FindProperty("itemName").stringValue = itemName;
        cso.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateEnemy(string name, Vector3 pos, Sprite[] frames, float range, int hp, float speed, float scale, int requiredLevel, bool boss, string display, Vector2 colSize, Vector2 colOffset)
    {
        GameObject go = Prop(name, frames[0], pos, "Characters", 4);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.offset = colOffset;
        col.size = colSize;
        AddLoop(go, frames, 6f);
        Enemy enemy = go.AddComponent<Enemy>();
        SerializedObject so = new SerializedObject(enemy);
        so.FindProperty("maxHealth").intValue = hp;
        so.FindProperty("moveSpeed").floatValue = speed;
        so.FindProperty("patrolRange").floatValue = range;
        so.FindProperty("requiredWeaponLevel").intValue = requiredLevel;
        so.FindProperty("isBoss").boolValue = boss;
        so.FindProperty("displayName").stringValue = display;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateWeapon(Vector3 pos, Sprite[] frames, int level, string weaponName)
    {
        GameObject go = Prop("Arma_" + level, frames[0], pos, "Characters", 3);
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;
        AddLoop(go, frames, 3f);
        WeaponPickup wp = go.AddComponent<WeaponPickup>();
        SerializedObject so = new SerializedObject(wp);
        so.FindProperty("weaponLevel").intValue = level;
        so.FindProperty("weaponName").stringValue = weaponName;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateBattery(Vector3 pos, Sprite[] frames)
    {
        GameObject go = Prop("PilaTurbo", frames[0], pos, "Characters", 3);
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;
        AddLoop(go, frames, 6f);
        go.AddComponent<SpeedBoostItem>();
    }

    private static void CreateSpark(Vector3 pos, Sprite[] frames)
    {
        GameObject go = Prop("Chispa", frames[0], pos, "Characters", 3);
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.offset = new Vector2(0f, 0.4f);
        col.size = new Vector2(0.7f, 0.8f);
        AddLoop(go, frames, 10f);
        go.AddComponent<Hazard>();
    }

    private static void CreateCheckpoint(Vector3 pos, Sprite[] frames)
    {
        GameObject go = Prop("Checkpoint", frames[0], pos, "Characters", 1);
        go.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.offset = new Vector2(0f, 0.8f);
        col.size = new Vector2(0.8f, 1.6f);
        Checkpoint cp = go.AddComponent<Checkpoint>();
        SerializedObject so = new SerializedObject(cp);
        so.FindProperty("activeSprite").objectReferenceValue = frames[1];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static HudUI BuildGameUI(Transform canvas, Sprite nutIcon, LevelSpec spec)
    {
        HudUI hud = new HudUI();
        Vector2 tl = new Vector2(0f, 1f);
        Vector2 tr = new Vector2(1f, 1f);
        Vector2 bl = new Vector2(0f, 0f);
        Vector2 br = new Vector2(1f, 0f);
        Vector2 tc = new Vector2(0.5f, 1f);
        Vector2 bc = new Vector2(0.5f, 0f);
        Vector2 c = new Vector2(0.5f, 0.5f);

        bool isCombat = spec.enemies != null;
        float panelHeight = isCombat ? 330f : 185f;
        GameObject hudPanel = Box(canvas, "HUDPanel", new Color(0f, 0f, 0f, 0.65f), tl, tl, new Vector2(20f, -20f), new Vector2(1120f, panelHeight));
        hud.objective = Txt(hudPanel.transform, "ObjectiveText", "", 32, Color.white, TextAnchor.MiddleLeft, tl, tl, new Vector2(20f, -10f), new Vector2(1080f, 52f));

        // Fila 1: Tuercas / Chips / Núcleos con su dibujo y 5 slots
        GameObject iconGo = NewUI("NutIcon", hudPanel.transform);
        Image icon = iconGo.AddComponent<Image>();
        icon.sprite = nutIcon;
        icon.preserveAspect = true;
        Place(iconGo, tl, tl, new Vector2(20f, -72f), new Vector2(74f, 74f));

        hud.counter = Txt(hudPanel.transform, "CounterText", "", 44, Gold, TextAnchor.MiddleLeft, tl, tl, new Vector2(108f, -74f), new Vector2(420f, 70f));
        hud.counter.fontStyle = FontStyle.Bold;

        hud.slots = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject slot = NewUI("Slot" + (i + 1), hudPanel.transform);
            Image img = slot.AddComponent<Image>();
            img.sprite = nutIcon;
            img.preserveAspect = true;
            img.color = new Color(1f, 1f, 1f, 0.2f);
            Place(slot, tl, tl, new Vector2(540f + i * 84f, -74f), new Vector2(68f, 68f));
            hud.slots[i] = img;
        }

        // Fila 2 (Niveles 2 y 3): Vidas con su dibujito (icono de energía/vida) + 3 slots visuales iguales a los chips
        Sprite lifeSprite = LoadSprites("Assets/Sprites/Props/Battery.png")[0];
        GameObject lifeIconGo = NewUI("LifeIcon", hudPanel.transform);
        Image lifeIcon = lifeIconGo.AddComponent<Image>();
        lifeIcon.sprite = lifeSprite;
        lifeIcon.preserveAspect = true;
        Place(lifeIconGo, tl, tl, new Vector2(20f, -162f), new Vector2(74f, 74f));
        lifeIconGo.SetActive(isCombat);

        hud.kills = Txt(hudPanel.transform, "KillsText", "", 44, new Color(0.4f, 1f, 0.95f, 1f), TextAnchor.MiddleLeft, tl, tl, new Vector2(108f, -164f), new Vector2(420f, 70f));
        hud.kills.fontStyle = FontStyle.Bold;

        hud.lifeSlots = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject lslot = NewUI("LifeSlot" + (i + 1), hudPanel.transform);
            Image limg = lslot.AddComponent<Image>();
            limg.sprite = lifeSprite;
            limg.preserveAspect = true;
            limg.color = Color.white;
            Place(lslot, tl, tl, new Vector2(540f + i * 84f, -164f), new Vector2(68f, 68f));
            lslot.SetActive(isCombat);
            hud.lifeSlots[i] = limg;
        }

        // Fila 3 (Niveles 2 y 3): Arma y balas bien separadas abajo
        hud.weapon = Txt(hudPanel.transform, "WeaponText", "", 36, Gold, TextAnchor.MiddleLeft, tl, tl, new Vector2(24f, -252f), new Vector2(1050f, 54f));
        hud.weapon.fontStyle = FontStyle.Bold;

        hud.levelName = Txt(canvas, "LevelNameText", spec.title, 40, Gold, TextAnchor.MiddleRight, tr, tr, new Vector2(-30f, -28f), new Vector2(720f, 60f));
        hud.levelName.fontStyle = FontStyle.Bold;
        Outline lnOutline = hud.levelName.gameObject.AddComponent<Outline>();
        lnOutline.effectColor = Color.black;
        lnOutline.effectDistance = new Vector2(2f, -2f);

        hud.boost = Txt(canvas, "BoostText", "", 48, Cyan, TextAnchor.MiddleRight, tr, tr, new Vector2(-30f, -96f), new Vector2(600f, 70f));
        hud.boost.fontStyle = FontStyle.Bold;

        hud.toast = Txt(canvas, "ToastText", "", 56, Gold, TextAnchor.MiddleCenter, tc, tc, new Vector2(0f, -260f), new Vector2(1400f, 90f));
        hud.toast.fontStyle = FontStyle.Bold;
        Outline toastOutline = hud.toast.gameObject.AddComponent<Outline>();
        toastOutline.effectColor = Color.black;
        toastOutline.effectDistance = new Vector2(3f, -3f);

        Txt(canvas, "ControlsHint", "A / D: mover   |   ESPACIO: saltar   |   S: agacharse / deslizarse   |   E: interactuar   |   J / clic: atacar   |   R: reiniciar   |   M: silenciar", 28, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleLeft, bl, bl, new Vector2(24f, 14f), new Vector2(1800f, 44f));

        DialogueUI dui = new DialogueUI();
        dui.panel = Box(canvas, "DialoguePanel", Dark, bc, bc, new Vector2(0f, 40f), new Vector2(1500f, 330f));
        Outline dOutline = dui.panel.AddComponent<Outline>();
        dOutline.effectColor = Cyan;
        dOutline.effectDistance = new Vector2(3f, -3f);
        dui.nameText = Txt(dui.panel.transform, "NPCNameText", "", 54, Gold, TextAnchor.MiddleLeft, tl, tl, new Vector2(40f, -18f), new Vector2(1000f, 70f));
        dui.nameText.fontStyle = FontStyle.Bold;
        dui.contentText = Txt(dui.panel.transform, "DialogueContentText", "", 42, Color.white, TextAnchor.UpperLeft, tl, tl, new Vector2(40f, -100f), new Vector2(1420f, 170f));
        Txt(dui.panel.transform, "ContinueHint", "Presiona E o Espacio para continuar...", 30, new Color(0.7f, 0.8f, 0.85f, 1f), TextAnchor.MiddleRight, br, br, new Vector2(-30f, 14f), new Vector2(800f, 44f));
        Fade(dui.panel);
        dui.panel.SetActive(false);
        hud.dialogue = dui;

        hud.infoPanel = Box(canvas, "InfoPanel", Dark, c, c, new Vector2(0f, 60f), new Vector2(1300f, 520f));
        Outline iOutline = hud.infoPanel.AddComponent<Outline>();
        iOutline.effectColor = Cyan;
        iOutline.effectDistance = new Vector2(3f, -3f);
        hud.infoText = Txt(hud.infoPanel.transform, "InfoText", "", 42, Color.white, TextAnchor.MiddleLeft, c, c, new Vector2(0f, 20f), new Vector2(1200f, 420f));
        Txt(hud.infoPanel.transform, "CloseHint", "Presiona E para cerrar", 30, new Color(0.7f, 0.8f, 0.85f, 1f), TextAnchor.MiddleRight, br, br, new Vector2(-30f, 14f), new Vector2(600f, 44f));
        Fade(hud.infoPanel);
        hud.infoPanel.SetActive(false);

        hud.missionPanel = Box(canvas, "MissionCompletePanel", new Color(0.02f, 0.2f, 0.12f, 0.93f), c, c, new Vector2(0f, 250f), new Vector2(1500f, 260f));
        Outline mOutline = hud.missionPanel.AddComponent<Outline>();
        mOutline.effectColor = new Color(0.3f, 1f, 0.6f, 1f);
        mOutline.effectDistance = new Vector2(3f, -3f);
        Text mText = Txt(hud.missionPanel.transform, "Text", "¡Felicidades! Reuniste las 5 tuercas.\nAhora lleva el avance al Técnico Amp, junto al generador, al final del nivel.", 46, Color.white, TextAnchor.MiddleCenter, c, c, Vector2.zero, Vector2.zero);
        mText.fontStyle = FontStyle.Bold;
        hud.missionText = mText;
        Stretch(mText.gameObject);
        Fade(hud.missionPanel);
        hud.missionPanel.SetActive(false);

        hud.finalPanel = Box(canvas, "FinalPanel", new Color(0.01f, 0.04f, 0.09f, 0.92f), c, c, Vector2.zero, Vector2.zero);
        Stretch(hud.finalPanel);
        GameObject finalCard = Box(hud.finalPanel.transform, "FinalCard", new Color(0.03f, 0.09f, 0.16f, 0.96f), c, c, new Vector2(0f, 40f), new Vector2(1620f, 680f));
        Outline fCardOut = finalCard.AddComponent<Outline>();
        fCardOut.effectColor = Gold;
        fCardOut.effectDistance = new Vector2(4f, -4f);
        Text fTitle = Txt(finalCard.transform, "FinalTitle", spec.finalTitle, 88, Gold, TextAnchor.MiddleCenter, c, c, new Vector2(0f, 230f), new Vector2(1520f, 120f));
        fTitle.fontStyle = FontStyle.Bold;
        hud.finalBody = Txt(finalCard.transform, "FinalBody", spec.finalBody, 36, Color.white, TextAnchor.MiddleCenter, c, c, new Vector2(0f, -30f), new Vector2(1460f, 440f));

        bool hasNext = !string.IsNullOrEmpty(spec.nextScene);
        float menuX = hasNext ? 0f : -260f;
        float restartX = hasNext ? 480f : 260f;
        if (hasNext)
        {
            hud.nextButton = Btn(hud.finalPanel.transform, "Btn_Next", "SIGUIENTE", new Vector2(-480f, -330f), 420f);
        }
        hud.menuButton = Btn(hud.finalPanel.transform, "Btn_Menu", "MENÚ", new Vector2(menuX, -330f), 420f);
        hud.restartButton = Btn(hud.finalPanel.transform, "Btn_Restart", "REINICIAR", new Vector2(restartX, -330f), 420f);
        Fade(hud.finalPanel);
        hud.finalPanel.SetActive(false);

        return hud;
    }

    private static void BuildLevel(LevelSpec spec, AnimatorController controller)
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Sprite[] nutFrames = LoadSprites(spec.collectiblePath);
        Sprite[] batteryFrames = LoadSprites("Assets/Sprites/Props/Battery.png");
        Sprite[] npcFrames = LoadSprites("Assets/Sprites/Props/Npc_Tecnico.png");
        Sprite[] generatorFrames = LoadSprites("Assets/Sprites/Props/Generator.png");
        Sprite[] sparkFrames = LoadSprites("Assets/Sprites/Props/Spark.png");
        Sprite[] checkpointFrames = LoadSprites("Assets/Sprites/Props/Checkpoint.png");
        Sprite signSprite = LoadSingle(spec.signAPath);
        Sprite terminalSprite = LoadSingle(spec.signBPath);
        Sprite promptSprite = LoadSingle("Assets/Sprites/UI/PromptE.png");
        Sprite bgSprite = LoadSingle(spec.bgPath);
        Sprite idleSprite = LoadSprites("Assets/Sprites/Soldier_Idle.png").First(s => s.name == "Soldier_Idle_0");

        GameObject bgRoot = new GameObject("Background");
        SpriteRenderer[] bgRenderers = new SpriteRenderer[18];
        for (int i = 0; i < 18; i++)
        {
            GameObject copy = Prop("BG_" + i, bgSprite, new Vector3(-10f + i * 18.75f, -0.5f, 0f), "Background", 0);
            copy.transform.SetParent(bgRoot.transform, true);
            copy.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
            bgRenderers[i] = copy.GetComponent<SpriteRenderer>();
        }

        GameObject grid = new GameObject("Grid");
        grid.AddComponent<Grid>();
        GameObject tmGo = new GameObject("Tilemap");
        tmGo.transform.SetParent(grid.transform, false);
        Tilemap tilemap = tmGo.AddComponent<Tilemap>();
        TilemapRenderer tmr = tmGo.AddComponent<TilemapRenderer>();
        tmr.sortingOrder = -1;
        Rigidbody2D tmBody = tmGo.AddComponent<Rigidbody2D>();
        tmBody.bodyType = RigidbodyType2D.Static;
        TilemapCollider2D tmCollider = tmGo.AddComponent<TilemapCollider2D>();
        tmCollider.usedByComposite = true;
        CompositeCollider2D composite = tmGo.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        tmCollider.usedByComposite = true;
        tmBody.bodyType = RigidbodyType2D.Static;

        Tile[] tiles = BuildTiles(spec.tilesPath);
        GameObject solids = new GameObject("GroundColliders");
        solidsRoot = solids.transform;
        for (int i = 0; i < spec.blocks.Length; i++)
        {
            Block(tilemap, tiles, spec.blocks[i]);
        }
        composite.GenerateGeometry();

        PhysicsMaterial2D noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/NoFriction.physicsMaterial2D");
        if (noFriction == null)
        {
            noFriction = new PhysicsMaterial2D("NoFriction");
            noFriction.friction = 0f;
            noFriction.bounciness = 0f;
            AssetDatabase.CreateAsset(noFriction, "Assets/Materials/NoFriction.physicsMaterial2D");
        }

        GameObject canvasGo = NewCanvas();
        HudUI hud = BuildGameUI(canvasGo.transform, nutFrames[0], spec);

        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, -1.4f, 0f);
        player.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        SpriteRenderer psr = player.AddComponent<SpriteRenderer>();
        psr.sprite = idleSprite;
        psr.sortingLayerName = "Characters";
        psr.sortingOrder = 5;
        Animator animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        CapsuleCollider2D capsule = player.AddComponent<CapsuleCollider2D>();
        capsule.offset = new Vector2(0f, 0.45f);
        capsule.size = new Vector2(0.45f, 0.8f);
        capsule.direction = CapsuleDirection2D.Vertical;
        capsule.sharedMaterial = noFriction;
        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform, false);
        groundCheck.transform.localPosition = Vector3.zero;
        PlayerController pc = player.AddComponent<PlayerController>();
        player.AddComponent<PlayerCombat>();
        SerializedObject pso = new SerializedObject(pc);
        pso.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;
        pso.FindProperty("groundLayer").intValue = -1;
        pso.ApplyModifiedPropertiesWithoutUndo();

        CreateNpc("NPC_Guia", new Vector3(spec.guideX, -2f, 0f), npcFrames, promptSprite, spec.guideConv, spec.guideDone, false, hud.dialogue);
        CreateNpc("NPC_Tecnico", new Vector3(spec.technicianX, -2f, 0f), npcFrames, promptSprite, spec.techConv, spec.techDone, true, hud.dialogue);

        CreateInteractable("Cartel_1", signSprite, new Vector3(spec.interactX[0], -2f, 0f), promptSprite, spec.interactContent[0], hud);
        CreateInteractable("Terminal_2", terminalSprite, new Vector3(spec.interactX[1], -2f, 0f), promptSprite, spec.interactContent[1], hud);

        for (int i = 0; i < spec.nuts.Length; i++) CreateNut(new Vector3(spec.nuts[i].x, spec.nuts[i].y, 0f), nutFrames, spec.collectibleName);
        for (int i = 0; i < spec.batteries.Length; i++) CreateBattery(new Vector3(spec.batteries[i].x, spec.batteries[i].y, 0f), batteryFrames);
        for (int i = 0; i < spec.sparks.Length; i++) CreateSpark(new Vector3(spec.sparks[i].x, spec.sparks[i].y, 0f), sparkFrames);
        for (int i = 0; i < spec.checkpoints.Length; i++)
        {
            float cy = spec.checkpointY != null ? spec.checkpointY[i] : -2f;
            CreateCheckpoint(new Vector3(spec.checkpoints[i], cy, 0f), checkpointFrames);
        }

        if (spec.enemies != null)
        {
            Sprite[] virusFrames = LoadSprites("Assets/Sprites/Props/Virus.png");
            Sprite[] bossFrames = LoadSprites("Assets/Sprites/Props/Boss.png");
            Sprite[] weapon1Frames = LoadSprites("Assets/Sprites/Props/Weapon1.png");
            Sprite[] weapon2Frames = LoadSprites("Assets/Sprites/Props/Weapon2.png");
            Sprite[] weapon3Frames = LoadSprites("Assets/Sprites/Props/Weapon3.png");
            if (weapon3Frames == null || weapon3Frames.Length == 0) weapon3Frames = weapon2Frames;

            int vHp = spec.index == 3 ? 3 : 2;
            float vSpeed = spec.index == 3 ? 3.2f : 1.8f;
            float vScale = spec.index == 3 ? 1.3f : 1.2f;

            for (int i = 0; i < spec.enemies.Length; i++)
            {
                Vector3 e = spec.enemies[i];
                CreateEnemy("Virus_" + (i + 1), new Vector3(e.x, e.y, 0f), virusFrames, e.z, vHp, vSpeed, vScale, 1, false, "un virus", new Vector2(0.7f, 0.7f), new Vector2(0f, 0.4f));
            }
            if (spec.hasBoss)
            {
                int bossHp = spec.index == 2 ? 14 : 22;
                int reqWeapon = spec.index == 2 ? 2 : 3;
                float bossSpeed = spec.index == 2 ? 2.5f : 3.1f;
                float bossScale = spec.index == 2 ? 1.75f : 2.1f;
                string bossTitle = spec.index == 2 ? "Gran Cortocircuito" : "Megacortocircuito del Núcleo";
                CreateEnemy("Boss_GranCortocircuito", new Vector3(spec.boss.x, spec.boss.y, 0f), bossFrames, spec.boss.z, bossHp, bossSpeed, bossScale, reqWeapon, true, bossTitle, new Vector2(1.5f, 1.6f), new Vector2(0f, 0.9f));
            }
            for (int w = 0; w < spec.weapons.Length; w++)
            {
                Vector3 wp = spec.weapons[w];
                int wLevel = Mathf.RoundToInt(wp.z);
                if (wLevel == 1)
                    CreateWeapon(new Vector3(wp.x, wp.y, 0f), weapon1Frames, 1, "Hacha de mantenimiento");
                else if (wLevel == 2)
                    CreateWeapon(new Vector3(wp.x, wp.y, 0f), weapon2Frames, 2, "Hacha de plasma");
                else if (wLevel == 3)
                    CreateWeapon(new Vector3(wp.x, wp.y, 0f), weapon3Frames, 3, "Pistola de Pulsos");
                else
                    CreateWeapon(new Vector3(wp.x, wp.y, 0f), batteryFrames, 4, "Cargador de Pulsos (+18 balas)");
            }
        }

        GameObject generator = Prop("Generador", generatorFrames[0], new Vector3(spec.generatorX, -2f, 0f), "Characters", 1);

        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = spec.dark;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0f, -0.5f, -10f);
        CameraFollow2D follow = camGo.AddComponent<CameraFollow2D>();
        SerializedObject cso = new SerializedObject(follow);
        cso.FindProperty("target").objectReferenceValue = player.transform;
        cso.FindProperty("minX").floatValue = 1f;
        cso.FindProperty("maxX").floatValue = spec.cameraMaxX;
        cso.ApplyModifiedPropertiesWithoutUndo();

        CreateAudio();

        GameObject hudGo = new GameObject("HUDManager");
        GameHUDManager manager = hudGo.AddComponent<GameHUDManager>();
        SerializedObject hso = new SerializedObject(manager);
        hso.FindProperty("counterText").objectReferenceValue = hud.counter;
        hso.FindProperty("objectiveText").objectReferenceValue = hud.objective;
        hso.FindProperty("toastText").objectReferenceValue = hud.toast;
        hso.FindProperty("boostText").objectReferenceValue = hud.boost;
        hso.FindProperty("levelNameText").objectReferenceValue = hud.levelName;
        hso.FindProperty("finalBodyText").objectReferenceValue = hud.finalBody;
        hso.FindProperty("missionCompletePanel").objectReferenceValue = hud.missionPanel;
        hso.FindProperty("finalPanel").objectReferenceValue = hud.finalPanel;
        hso.FindProperty("worldCamera").objectReferenceValue = cam;
        hso.FindProperty("generatorRenderer").objectReferenceValue = generator.GetComponent<SpriteRenderer>();
        hso.FindProperty("generatorOnSprite").objectReferenceValue = generatorFrames[1];
        hso.FindProperty("levelIndex").intValue = spec.index;
        hso.FindProperty("killsText").objectReferenceValue = hud.kills;
        hso.FindProperty("weaponText").objectReferenceValue = hud.weapon;
        hso.FindProperty("missionText").objectReferenceValue = hud.missionText;
        hso.FindProperty("collectibleLabel").stringValue = spec.collectibleName + "s";
        hso.FindProperty("combatLevel").boolValue = spec.enemies != null;
        hso.FindProperty("requireBoss").boolValue = spec.hasBoss;
        hso.FindProperty("totalEnemies").intValue = spec.enemies != null ? spec.enemies.Length : 0;
        if (!string.IsNullOrEmpty(spec.objectiveStart))
        {
            hso.FindProperty("objectiveStart").stringValue = spec.objectiveStart;
            hso.FindProperty("objectiveReady").stringValue = spec.objectiveReady;
        }
        if (!string.IsNullOrEmpty(spec.missionMessage))
        {
            hso.FindProperty("missionMessage").stringValue = spec.missionMessage;
        }
        hso.FindProperty("nextSceneName").stringValue = spec.nextScene;
        hso.FindProperty("darkColor").colorValue = spec.dark;
        hso.FindProperty("lightColor").colorValue = spec.light;
        hso.FindProperty("dimTint").colorValue = spec.dim;
        SerializedProperty slots = hso.FindProperty("nutSlots");
        slots.arraySize = hud.slots.Length;
        for (int i = 0; i < hud.slots.Length; i++)
        {
            slots.GetArrayElementAtIndex(i).objectReferenceValue = hud.slots[i];
        }
        SerializedProperty lslots = hso.FindProperty("lifeSlots");
        if (lslots != null && hud.lifeSlots != null)
        {
            lslots.arraySize = hud.lifeSlots.Length;
            for (int i = 0; i < hud.lifeSlots.Length; i++)
            {
                lslots.GetArrayElementAtIndex(i).objectReferenceValue = hud.lifeSlots[i];
            }
        }
        SerializedProperty bgs = hso.FindProperty("backgroundRenderers");
        bgs.arraySize = bgRenderers.Length;
        for (int i = 0; i < bgRenderers.Length; i++)
        {
            bgs.GetArrayElementAtIndex(i).objectReferenceValue = bgRenderers[i];
        }
        hso.ApplyModifiedPropertiesWithoutUndo();

        if (hud.nextButton != null)
        {
            UnityEventTools.AddPersistentListener(hud.nextButton.onClick, manager.NextLevel);
        }
        UnityEventTools.AddPersistentListener(hud.menuButton.onClick, manager.GoToMenu);
        UnityEventTools.AddPersistentListener(hud.restartButton.onClick, manager.RestartGame);

        EditorSceneManager.SaveScene(scene, ScenesPath + spec.sceneName + ".unity");
    }
}
