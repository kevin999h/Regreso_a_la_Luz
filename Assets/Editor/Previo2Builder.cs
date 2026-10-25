using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class Previo2Builder
{
    private const string ScenesPath = "Assets/Scenes/";
    private const string GameplayController = "Assets/Animations/Player/Player_Gameplay.controller";

    private static readonly Color Cyan = new Color(0.3f, 0.95f, 0.9f, 1f);
    private static readonly Color Gold = new Color(1f, 0.85f, 0.35f, 1f);
    private static readonly Color Dark = new Color(0.02f, 0.05f, 0.09f, 0.92f);

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
        public Image[] slots;
        public GameObject missionPanel;
        public GameObject finalPanel;
        public Button menuButton;
        public Button restartButton;
        public DialogueUI dialogue;
        public GameObject infoPanel;
        public Text infoText;
    }

    [MenuItem("Previo2/Construir todo")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureSortingLayer("Background", true);
        EnsureSortingLayer("Characters", false);
        int groundLayer = EnsureLayer("Ground");

        AnimatorController controller = BuildGameplayController();
        BuildMainMenu();
        BuildGameScene(controller, groundLayer);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenesPath + "MainMenu.unity", true),
            new EditorBuildSettingsScene(ScenesPath + "GameScene.unity", true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(ScenesPath + "MainMenu.unity");
        EditorUtility.DisplayDialog("Previo 2", "Listo. MainMenu y GameScene construidas y registradas en Build Settings.", "OK");
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

    private static int EnsureLayer(string layerName)
    {
        SerializedObject tm = LoadTagManager();
        SerializedProperty layers = tm.FindProperty("layers");
        while (layers.arraySize < 32)
        {
            layers.InsertArrayElementAtIndex(layers.arraySize);
            layers.GetArrayElementAtIndex(layers.arraySize - 1).stringValue = "";
        }

        for (int i = 8; i < 32; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
            {
                return i;
            }
        }

        for (int i = 8; i < 32; i++)
        {
            if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
            {
                layers.GetArrayElementAtIndex(i).stringValue = layerName;
                tm.ApplyModifiedProperties();
                return i;
            }
        }
        return 8;
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

    private static Button Btn(Transform parent, string name, string label, Vector2 pos)
    {
        GameObject go = NewUI(name, parent);
        Place(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(460f, 110f));

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

        Text t = Txt(go.transform, "Text", label, 46, new Color(0.75f, 1f, 0.98f, 1f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        t.fontStyle = FontStyle.Bold;
        Stretch(t.gameObject);
        return b;
    }

    private static GameObject NewCanvas(out GameObject eventSystem)
    {
        eventSystem = new GameObject("EventSystem");
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

        GameObject es;
        GameObject canvasGo = NewCanvas(out es);

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
        Button play = Btn(menuPanel.transform, "Btn_Play", "JUGAR", new Vector2(-520f, -400f));
        Button credits = Btn(menuPanel.transform, "Btn_Credits", "CRÉDITOS", new Vector2(0f, -400f));
        Button quit = Btn(menuPanel.transform, "Btn_Quit", "SALIR", new Vector2(520f, -400f));

        GameObject creditsPanel = NewUI("CreditsPanel", canvasGo.transform);
        Image panelImg = creditsPanel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.95f);
        Stretch(creditsPanel);

        Text creditsTitle = Txt(creditsPanel.transform, "Titulo", "INFORMACIÓN Y CRÉDITOS", 70, Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 440f), new Vector2(1500f, 110f));
        creditsTitle.fontStyle = FontStyle.Bold;

        string body =
            "REGRESO A LA LUZ\n" +
            "Versión Prototipo - Previo 2 - Semestre II / 2026\n\n" +
            "1. PROPÓSITO EDUCATIVO Y COMPETENCIAS\n" +
            "- Asignatura / Área: Física - Electricidad y circuitos\n" +
            "- Competencia formativa: el jugador repara un generador y aprende que la corriente fluye en un circuito cerrado, la Ley de Ohm (V = I x R) y cómo se combinan resistencias en serie y en paralelo.\n\n" +
            "2. EQUIPO DE DESARROLLO\n" +
            "- Diseño, programación, arte e interfaces: Kevin Yoany Hernández Ramírez\n\n" +
            "3. FUENTES Y REFERENCIAS ACADÉMICAS\n" +
            "- Guías de los Talleres Prácticos 2 y 3 - UTS, Desarrollo de Contenidos Digitales y Multimediales.\n" +
            "- Documentación oficial de Unity (Tilemaps, Animación, Scripting API).\n\n" +
            "4. ATRIBUCIÓN DE RECURSOS DE TERCEROS\n" +
            "- Arte y sprites: elaborados para este prototipo.\n\n" +
            "AVISO LEGAL Y ACADÉMICO: prototipo interactivo desarrollado con fines formativos y académicos. No posee fines comerciales ni lucrativos.";
        Txt(creditsPanel.transform, "Credito", body, 28, Color.white, TextAnchor.UpperLeft, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1600f, 740f));

        Button close = Btn(creditsPanel.transform, "Btn_CloseCredits", "VOLVER", new Vector2(0f, -440f));

        GameObject managerGo = new GameObject("MenuManager");
        MainMenuManager manager = managerGo.AddComponent<MainMenuManager>();
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("creditsPanel").objectReferenceValue = creditsPanel;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(play.onClick, manager.PlayGame);
        UnityEventTools.AddPersistentListener(credits.onClick, manager.OpenCredits);
        UnityEventTools.AddPersistentListener(quit.onClick, manager.QuitGame);
        UnityEventTools.AddPersistentListener(close.onClick, manager.CloseCredits);

        creditsPanel.SetActive(false);
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
        sm.defaultState = idle;

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

    private static Tile[] BuildTiles()
    {
        Sprite[] sprites = LoadSprites("Assets/Sprites/Environment/Circuit_Tiles.png");
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

    private static Transform solidsRoot;

    private static void Block(Tilemap tm, Tile[] tiles, int x0, int x1, int yBottom, int yTop, bool capBottom)
    {
        Fill(tm, tiles, x0, x1, yBottom, yTop, capBottom);

        float width = x1 - x0 + 1;
        float height = yTop - yBottom + 1;
        GameObject solid = new GameObject("Solid_" + x0 + "_" + yBottom);
        solid.transform.SetParent(solidsRoot, false);
        solid.transform.position = new Vector3(x0 + width * 0.5f, yBottom + height * 0.5f, 0f);
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

    private static DialogueLine L(string speaker, Color color, string text)
    {
        DialogueLine line = new DialogueLine();
        line.speakerName = speaker;
        line.nameColor = color;
        line.text = text;
        return line;
    }

    private static GameObject CreateNpc(string name, Vector3 pos, Sprite[] frames, Sprite prompt, DialogueLine[] conv, DialogueLine[] done, bool finishes, DialogueUI ui)
    {
        GameObject npc = Prop(name, frames[0], pos, "Characters", 2);
        npc.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        SpriteLoop loop = npc.AddComponent<SpriteLoop>();
        SerializedObject sl = new SerializedObject(loop);
        SetSprites(sl, "frames", frames);
        sl.FindProperty("framesPerSecond").floatValue = 4f;
        sl.ApplyModifiedPropertiesWithoutUndo();

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

    private static GameObject CreateNut(Vector3 pos, Sprite[] frames)
    {
        GameObject go = Prop("Tuerca", frames[0], pos, "Characters", 3);
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;
        SpriteLoop loop = go.AddComponent<SpriteLoop>();
        SerializedObject sl = new SerializedObject(loop);
        SetSprites(sl, "frames", frames);
        sl.FindProperty("framesPerSecond").floatValue = 8f;
        sl.ApplyModifiedPropertiesWithoutUndo();
        go.AddComponent<CollectibleItem>();
        return go;
    }

    private static GameObject CreateBattery(Vector3 pos, Sprite[] frames)
    {
        GameObject go = Prop("PilaTurbo", frames[0], pos, "Characters", 3);
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;
        SpriteLoop loop = go.AddComponent<SpriteLoop>();
        SerializedObject sl = new SerializedObject(loop);
        SetSprites(sl, "frames", frames);
        sl.FindProperty("framesPerSecond").floatValue = 6f;
        sl.ApplyModifiedPropertiesWithoutUndo();
        go.AddComponent<SpeedBoostItem>();
        return go;
    }

    private static HudUI BuildGameUI(Transform canvas, Sprite nutIcon)
    {
        HudUI hud = new HudUI();
        Vector2 tl = new Vector2(0f, 1f);
        Vector2 tr = new Vector2(1f, 1f);
        Vector2 bl = new Vector2(0f, 0f);
        Vector2 br = new Vector2(1f, 0f);
        Vector2 tc = new Vector2(0.5f, 1f);
        Vector2 bc = new Vector2(0.5f, 0f);
        Vector2 c = new Vector2(0.5f, 0.5f);

        GameObject hudPanel = Box(canvas, "HUDPanel", new Color(0f, 0f, 0f, 0.6f), tl, tl, new Vector2(20f, -20f), new Vector2(1250f, 190f));
        hud.objective = Txt(hudPanel.transform, "ObjectiveText", "", 36, Color.white, TextAnchor.MiddleLeft, tl, tl, new Vector2(20f, -12f), new Vector2(1210f, 56f));

        GameObject iconGo = NewUI("NutIcon", hudPanel.transform);
        Image icon = iconGo.AddComponent<Image>();
        icon.sprite = nutIcon;
        icon.preserveAspect = true;
        Place(iconGo, tl, tl, new Vector2(20f, -84f), new Vector2(84f, 84f));

        hud.counter = Txt(hudPanel.transform, "CounterText", "", 50, Gold, TextAnchor.MiddleLeft, tl, tl, new Vector2(116f, -88f), new Vector2(440f, 76f));
        hud.counter.fontStyle = FontStyle.Bold;

        hud.slots = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject slot = NewUI("Slot" + (i + 1), hudPanel.transform);
            Image img = slot.AddComponent<Image>();
            img.sprite = nutIcon;
            img.preserveAspect = true;
            img.color = new Color(1f, 1f, 1f, 0.2f);
            Place(slot, tl, tl, new Vector2(580f + i * 90f, -88f), new Vector2(76f, 76f));
            hud.slots[i] = img;
        }

        hud.boost = Txt(canvas, "BoostText", "", 52, Cyan, TextAnchor.MiddleRight, tr, tr, new Vector2(-30f, -30f), new Vector2(600f, 70f));
        hud.boost.fontStyle = FontStyle.Bold;

        hud.toast = Txt(canvas, "ToastText", "", 56, Gold, TextAnchor.MiddleCenter, tc, tc, new Vector2(0f, -260f), new Vector2(1400f, 90f));
        hud.toast.fontStyle = FontStyle.Bold;
        Outline toastOutline = hud.toast.gameObject.AddComponent<Outline>();
        toastOutline.effectColor = Color.black;
        toastOutline.effectDistance = new Vector2(3f, -3f);

        Txt(canvas, "ControlsHint", "A / D: mover   |   ESPACIO: saltar   |   S: agacharse / deslizarse   |   E: interactuar   |   R: reiniciar", 28, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleLeft, bl, bl, new Vector2(24f, 14f), new Vector2(1700f, 44f));

        DialogueUI dui = new DialogueUI();
        dui.panel = Box(canvas, "DialoguePanel", Dark, bc, bc, new Vector2(0f, 40f), new Vector2(1500f, 330f));
        Outline dOutline = dui.panel.AddComponent<Outline>();
        dOutline.effectColor = Cyan;
        dOutline.effectDistance = new Vector2(3f, -3f);
        dui.nameText = Txt(dui.panel.transform, "NPCNameText", "", 54, Gold, TextAnchor.MiddleLeft, tl, tl, new Vector2(40f, -18f), new Vector2(1000f, 70f));
        dui.nameText.fontStyle = FontStyle.Bold;
        dui.contentText = Txt(dui.panel.transform, "DialogueContentText", "", 42, Color.white, TextAnchor.UpperLeft, tl, tl, new Vector2(40f, -100f), new Vector2(1420f, 170f));
        Txt(dui.panel.transform, "ContinueHint", "Presiona E o Espacio para continuar...", 30, new Color(0.7f, 0.8f, 0.85f, 1f), TextAnchor.MiddleRight, br, br, new Vector2(-30f, 14f), new Vector2(800f, 44f));
        dui.panel.SetActive(false);
        hud.dialogue = dui;

        hud.infoPanel = Box(canvas, "InfoPanel", Dark, c, c, new Vector2(0f, 60f), new Vector2(1300f, 520f));
        Outline iOutline = hud.infoPanel.AddComponent<Outline>();
        iOutline.effectColor = Cyan;
        iOutline.effectDistance = new Vector2(3f, -3f);
        hud.infoText = Txt(hud.infoPanel.transform, "InfoText", "", 42, Color.white, TextAnchor.MiddleLeft, c, c, new Vector2(0f, 20f), new Vector2(1200f, 420f));
        Txt(hud.infoPanel.transform, "CloseHint", "Presiona E para cerrar", 30, new Color(0.7f, 0.8f, 0.85f, 1f), TextAnchor.MiddleRight, br, br, new Vector2(-30f, 14f), new Vector2(600f, 44f));
        hud.infoPanel.SetActive(false);

        hud.missionPanel = Box(canvas, "MissionCompletePanel", new Color(0.02f, 0.2f, 0.12f, 0.93f), c, c, new Vector2(0f, 250f), new Vector2(1500f, 260f));
        Outline mOutline = hud.missionPanel.AddComponent<Outline>();
        mOutline.effectColor = new Color(0.3f, 1f, 0.6f, 1f);
        mOutline.effectDistance = new Vector2(3f, -3f);
        Text mText = Txt(hud.missionPanel.transform, "Text", "¡Felicidades! Reuniste las 5 tuercas.\nAhora lleva el avance al Técnico Amp, junto al generador, al final del nivel.", 46, Color.white, TextAnchor.MiddleCenter, c, c, Vector2.zero, Vector2.zero);
        mText.fontStyle = FontStyle.Bold;
        Stretch(mText.gameObject);
        hud.missionPanel.SetActive(false);

        hud.finalPanel = Box(canvas, "FinalPanel", new Color(0f, 0f, 0f, 0.72f), c, c, Vector2.zero, Vector2.zero);
        Stretch(hud.finalPanel);
        Text fTitle = Txt(hud.finalPanel.transform, "FinalTitle", "¡LUZ RESTAURADA!", 120, Gold, TextAnchor.MiddleCenter, c, c, new Vector2(0f, 290f), new Vector2(1700f, 170f));
        fTitle.fontStyle = FontStyle.Bold;
        Txt(hud.finalPanel.transform, "FinalBody", "Nivel 1 completado.\n\nAprendiste que la corriente fluye en un circuito cerrado, que V = I x R (Ley de Ohm) y cómo se suman las resistencias en serie y en paralelo.", 44, Color.white, TextAnchor.MiddleCenter, c, c, new Vector2(0f, 40f), new Vector2(1500f, 380f));
        hud.menuButton = Btn(hud.finalPanel.transform, "Btn_Menu", "MENÚ", new Vector2(-260f, -330f));
        hud.restartButton = Btn(hud.finalPanel.transform, "Btn_Restart", "REINICIAR", new Vector2(260f, -330f));
        hud.finalPanel.SetActive(false);

        return hud;
    }

    private static void BuildGameScene(AnimatorController controller, int groundLayer)
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Sprite[] nutFrames = LoadSprites("Assets/Sprites/Props/Nut.png");
        Sprite[] batteryFrames = LoadSprites("Assets/Sprites/Props/Battery.png");
        Sprite[] npcFrames = LoadSprites("Assets/Sprites/Props/Npc_Tecnico.png");
        Sprite[] generatorFrames = LoadSprites("Assets/Sprites/Props/Generator.png");
        Sprite signSprite = LoadSingle("Assets/Sprites/Props/Sign_Ohm.png");
        Sprite terminalSprite = LoadSingle("Assets/Sprites/Props/Terminal.png");
        Sprite promptSprite = LoadSingle("Assets/Sprites/UI/PromptE.png");
        Sprite bgSprite = LoadSingle("Assets/Sprites/Environment/GameBackground.png");
        Sprite idleSprite = LoadSprites("Assets/Sprites/Soldier_Idle.png").First(s => s.name == "Soldier_Idle_0");

        Color white = Color.white;
        Color gold = Gold;

        GameObject bgRoot = new GameObject("Background");
        SpriteRenderer[] bgRenderers = new SpriteRenderer[8];
        for (int i = 0; i < 8; i++)
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
        tmBody.simulated = true;

        Tile[] tiles = BuildTiles();
        GameObject solids = new GameObject("GroundColliders");
        solidsRoot = solids.transform;
        Block(tilemap, tiles, -8, 21, -6, -3, true);
        Block(tilemap, tiles, 25, 40, -6, -3, true);
        Block(tilemap, tiles, 55, 90, -6, -3, true);
        Block(tilemap, tiles, 94, 118, -6, -3, true);
        Block(tilemap, tiles, 42, 44, -2, -2, true);
        Block(tilemap, tiles, 47, 49, -1, -1, true);
        Block(tilemap, tiles, 51, 52, -2, -2, true);
        Block(tilemap, tiles, 62, 63, -2, -1, false);
        Block(tilemap, tiles, 72, 84, -1, 0, true);
        Block(tilemap, tiles, 100, 102, -1, -1, true);

        composite.GenerateGeometry();

        PhysicsMaterial2D noFriction = new PhysicsMaterial2D("NoFriction");
        noFriction.friction = 0f;
        noFriction.bounciness = 0f;
        AssetDatabase.DeleteAsset("Assets/Materials/NoFriction.physicsMaterial2D");
        AssetDatabase.CreateAsset(noFriction, "Assets/Materials/NoFriction.physicsMaterial2D");

        GameObject es;
        GameObject canvasGo = NewCanvas(out es);
        HudUI hud = BuildGameUI(canvasGo.transform, nutFrames[0]);

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
        SerializedObject pso = new SerializedObject(pc);
        pso.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;
        pso.FindProperty("groundLayer").intValue = -1;
        pso.ApplyModifiedPropertiesWithoutUndo();

        Color lumenColor = gold;
        Color voltColor = new Color(0.4f, 1f, 0.95f, 1f);

        DialogueLine[] guiaConv = new[]
        {
            L("BIT", voltColor, "Bip... ¡Theo! Mi memoria está dañada, pero recuerdo esto: el generador de la ciudad se apagó y todo quedó a oscuras."),
            L("Theo (Tú)", lumenColor, "¿Qué necesitas que haga, BIT?"),
            L("BIT", voltColor, "Cinco tuercas del generador salieron volando por el circuito. Recógelas todas."),
            L("BIT", voltColor, "Salta con ESPACIO y deslízate con S para pasar los túneles bajos. Las pilas te dan turbo por unos segundos."),
            L("BIT", voltColor, "Lee los carteles y terminales con E: te enseñarán cómo funciona un circuito. Cuando termines, busca al Técnico Amp junto al generador.")
        };
        DialogueLine[] guiaDone = new[]
        {
            L("BIT", voltColor, "¡Tienes las cinco tuercas! Corre al generador, al final del nivel, y habla con el Técnico Amp.")
        };
        DialogueLine[] tecnicoConv = new[]
        {
            L("Técnico Amp", voltColor, "Aún faltan tuercas. Sin las cinco, el generador no arranca."),
            L("Técnico Amp", voltColor, "Revisa el HUD: ahí ves cuántas llevas.")
        };
        DialogueLine[] tecnicoDone = new[]
        {
            L("Técnico Amp", voltColor, "¡Excelente! Con estas cinco tuercas el generador queda bien ajustado."),
            L("Theo (Tú)", lumenColor, "¿Y ahora qué falta?"),
            L("Técnico Amp", voltColor, "Un circuito necesita fuente, conductores y carga. Ya tenemos todo: ¡que vuelva la luz!")
        };

        CreateNpc("NPC_Guia", new Vector3(5f, -2f, 0f), npcFrames, promptSprite, guiaConv, guiaDone, false, hud.dialogue);
        CreateNpc("NPC_Tecnico", new Vector3(108f, -2f, 0f), npcFrames, promptSprite, tecnicoConv, tecnicoDone, true, hud.dialogue);

        CreateInteractable("Cartel_LeyDeOhm", signSprite, new Vector3(11f, -2f, 0f), promptSprite,
            "LEY DE OHM\n\nV = I x R\n\nEl voltaje (V, en voltios) es igual a la corriente (I, en amperios) por la resistencia (R, en ohmios).\n\nEjemplo: con 12 V y una resistencia de 4 ohmios, la corriente es I = V / R = 3 A.", hud);
        CreateInteractable("Terminal_Resistencias", terminalSprite, new Vector3(58f, -2f, 0f), promptSprite,
            "RESISTENCIAS EN UN CIRCUITO\n\nEn SERIE se suman: R total = R1 + R2\n(10 ohmios + 20 ohmios = 30 ohmios).\n\nEn PARALELO el total es menor que la menor de ellas: 1 / R total = 1 / R1 + 1 / R2\n(dos de 20 ohmios en paralelo = 10 ohmios).", hud);

        CreateNut(new Vector3(15.5f, -1.4f, 0f), nutFrames);
        CreateNut(new Vector3(23.5f, 0.4f, 0f), nutFrames);
        CreateNut(new Vector3(48.5f, 0.8f, 0f), nutFrames);
        CreateNut(new Vector3(78f, -1.4f, 0f), nutFrames);
        CreateNut(new Vector3(101.5f, 0.8f, 0f), nutFrames);
        CreateBattery(new Vector3(31f, -1.3f, 0f), batteryFrames);
        CreateBattery(new Vector3(67f, -1.3f, 0f), batteryFrames);

        GameObject generator = Prop("Generador", generatorFrames[0], new Vector3(113.5f, -2f, 0f), "Characters", 1);

        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.03f, 0.07f, 1f);
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0f, -0.5f, -10f);
        CameraFollow2D follow = camGo.AddComponent<CameraFollow2D>();
        SerializedObject cso = new SerializedObject(follow);
        cso.FindProperty("target").objectReferenceValue = player.transform;
        cso.FindProperty("minX").floatValue = 1f;
        cso.FindProperty("maxX").floatValue = 110f;
        cso.ApplyModifiedPropertiesWithoutUndo();

        GameObject hudGo = new GameObject("HUDManager");
        GameHUDManager manager = hudGo.AddComponent<GameHUDManager>();
        SerializedObject hso = new SerializedObject(manager);
        hso.FindProperty("counterText").objectReferenceValue = hud.counter;
        hso.FindProperty("objectiveText").objectReferenceValue = hud.objective;
        hso.FindProperty("toastText").objectReferenceValue = hud.toast;
        hso.FindProperty("boostText").objectReferenceValue = hud.boost;
        hso.FindProperty("missionCompletePanel").objectReferenceValue = hud.missionPanel;
        hso.FindProperty("finalPanel").objectReferenceValue = hud.finalPanel;
        hso.FindProperty("worldCamera").objectReferenceValue = cam;
        hso.FindProperty("generatorRenderer").objectReferenceValue = generator.GetComponent<SpriteRenderer>();
        hso.FindProperty("generatorOnSprite").objectReferenceValue = generatorFrames[1];
        SerializedProperty slots = hso.FindProperty("nutSlots");
        slots.arraySize = hud.slots.Length;
        for (int i = 0; i < hud.slots.Length; i++)
        {
            slots.GetArrayElementAtIndex(i).objectReferenceValue = hud.slots[i];
        }
        SerializedProperty bgs = hso.FindProperty("backgroundRenderers");
        bgs.arraySize = bgRenderers.Length;
        for (int i = 0; i < bgRenderers.Length; i++)
        {
            bgs.GetArrayElementAtIndex(i).objectReferenceValue = bgRenderers[i];
        }
        hso.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(hud.menuButton.onClick, manager.GoToMenu);
        UnityEventTools.AddPersistentListener(hud.restartButton.onClick, manager.RestartGame);

        EditorSceneManager.SaveScene(scene, ScenesPath + "GameScene.unity");
    }
}
