#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using OrbWanderer.Core;
using OrbWanderer.Data;
using OrbWanderer.World;
using OrbWanderer.Inventory;
using OrbWanderer.Wildlife;
using OrbWanderer.Equipment;
using OrbWanderer.Player;
using OrbWanderer.UI;

public static class SceneSetupWizard
{
    [MenuItem("OrbWanderer/Setup Game Scene")]
    public static void SetupScene()
    {
        // Create a new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // --- Camera ---
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
            mainCam.backgroundColor = new Color(0.1f, 0.15f, 0.2f);
            mainCam.gameObject.AddComponent<CameraController>();
        }

        // --- Player ---
        var player = new GameObject("Player");
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Default");
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        var sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = AlienSpriteGenerator.CreateAlienPlayer();
        sr.sortingOrder = 5;
        var collider = player.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.8f, 0.8f);
        var pc = player.AddComponent<PlayerController>();
        SetSerializedField(pc, "spriteRenderer", sr);
        var detector = player.AddComponent<OrbDetector>();

        // --- Managers ---
        var managers = new GameObject("--- Managers ---");

        var gmObj = new GameObject("GameManager");
        gmObj.transform.SetParent(managers.transform);
        gmObj.AddComponent<GameManager>();

        var wmObj = new GameObject("WorldMapManager");
        wmObj.transform.SetParent(managers.transform);
        var wmm = wmObj.AddComponent<WorldMapManager>();
        // Load region data
        var regions = Resources.LoadAll<RegionData>("GameData/Regions");
        if (regions.Length > 0)
        {
            SetSerializedField(wmm, "allRegions", regions);
            // Find Grassland as starting region
            RegionData startRegion = null;
            foreach (var r in regions)
            {
                if (r.regionType == RegionType.Grassland)
                {
                    startRegion = r;
                    break;
                }
            }
            if (startRegion != null)
                SetSerializedField(wmm, "startingRegion", startRegion);
            else
                SetSerializedField(wmm, "startingRegion", regions[0]);
        }

        var smObj = new GameObject("SatchelManager");
        smObj.transform.SetParent(managers.transform);
        smObj.AddComponent<SatchelManager>();

        var cmObj = new GameObject("CompanionManager");
        cmObj.transform.SetParent(managers.transform);
        cmObj.AddComponent<CompanionManager>();

        var emObj = new GameObject("EquipmentManager");
        emObj.transform.SetParent(managers.transform);
        var em = emObj.AddComponent<EquipmentManager>();
        var equipment = Resources.LoadAll<EquipmentData>("GameData/Equipment");
        if (equipment.Length > 0)
        {
            SetSerializedField(em, "allEquipment", equipment);
        }

        // --- Alien World Generator ---
        var worldGenObj = new GameObject("AlienWorldGenerator");
        worldGenObj.transform.SetParent(managers.transform);
        var worldGen = worldGenObj.AddComponent<AlienWorldGenerator>();
        if (regions.Length > 0)
        {
            worldGen.SetRegions(regions);
        }

        // --- Canvas for all UI ---
        var canvasObj = new GameObject("MainCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // --- HUD ---
        var hudObj = CreateUIPanel(canvasObj.transform, "HUD", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        var hud = hudObj.AddComponent<HUDManager>();

        // Orb counter
        var orbCounterObj = CreateUIText(hudObj.transform, "OrbCountText", "0 / 50",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, -40), new Vector2(200, 40));
        SetSerializedField(hud, "totalOrbText", orbCounterObj.GetComponent<Text>());

        // Region name
        var regionGroup = new GameObject("RegionNameGroup");
        regionGroup.transform.SetParent(hudObj.transform, false);
        var regionGroupRect = regionGroup.AddComponent<RectTransform>();
        regionGroupRect.anchorMin = new Vector2(0.5f, 1);
        regionGroupRect.anchorMax = new Vector2(0.5f, 1);
        regionGroupRect.anchoredPosition = new Vector2(0, -50);
        regionGroupRect.sizeDelta = new Vector2(300, 40);
        var regionCG = regionGroup.AddComponent<CanvasGroup>();
        var regionText = CreateUIText(regionGroup.transform, "RegionText", "Verdant Meadows",
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        regionText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        regionText.GetComponent<Text>().fontSize = 24;
        SetSerializedField(hud, "currentRegionText", regionText.GetComponent<Text>());
        SetSerializedField(hud, "regionNameGroup", regionCG);

        // Companion panel
        var companionPanel = CreateUIPanel(hudObj.transform, "CompanionPanel",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-120, -40), new Vector2(200, 60));
        SetSerializedField(hud, "companionPanel", companionPanel);

        var companionIcon = new GameObject("CompanionIcon");
        companionIcon.transform.SetParent(companionPanel.transform, false);
        var ciRect = companionIcon.AddComponent<RectTransform>();
        ciRect.anchorMin = new Vector2(0, 0.5f);
        ciRect.anchorMax = new Vector2(0, 0.5f);
        ciRect.anchoredPosition = new Vector2(25, 0);
        ciRect.sizeDelta = new Vector2(40, 40);
        var ciImage = companionIcon.AddComponent<Image>();
        ciImage.color = Color.cyan;
        SetSerializedField(hud, "companionIcon", ciImage);

        var companionName = CreateUIText(companionPanel.transform, "CompanionName", "None",
            new Vector2(0.3f, 0.5f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        companionName.GetComponent<Text>().fontSize = 14;
        SetSerializedField(hud, "companionNameText", companionName.GetComponent<Text>());

        var friendshipSlider = CreateSlider(companionPanel.transform, "FriendshipBar",
            new Vector2(0.3f, 0), new Vector2(1, 0.4f));
        SetSerializedField(hud, "friendshipBar", friendshipSlider);

        // Mount indicator
        var mountInd = CreateUIPanel(hudObj.transform, "MountIndicator",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-100, 200), new Vector2(160, 30));
        SetSerializedField(hud, "mountIndicator", mountInd);
        var mountText = CreateUIText(mountInd.transform, "MountSpeedText", "Speed: 1.0x",
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        mountText.GetComponent<Text>().fontSize = 14;
        SetSerializedField(hud, "mountSpeedText", mountText.GetComponent<Text>());
        mountInd.SetActive(false);

        // Notification area
        var notifContainer = new GameObject("NotificationContainer");
        notifContainer.transform.SetParent(hudObj.transform, false);
        var notifRect = notifContainer.AddComponent<RectTransform>();
        notifRect.anchorMin = new Vector2(0.5f, 0.8f);
        notifRect.anchorMax = new Vector2(0.5f, 0.8f);
        notifRect.sizeDelta = new Vector2(400, 200);
        SetSerializedField(hud, "notificationContainer", notifRect);

        // Notification prefab (saved as asset)
        var notifPrefab = CreateNotificationPrefab();
        SetSerializedField(hud, "notificationPrefab", notifPrefab);

        // --- World Map UI ---
        var mapCanvasObj = new GameObject("MapCanvas");
        mapCanvasObj.transform.SetParent(canvasObj.transform, false);
        var mapCanvas = mapCanvasObj.AddComponent<Canvas>();
        mapCanvas.overrideSorting = true;
        mapCanvas.sortingOrder = 200;
        mapCanvasObj.AddComponent<GraphicRaycaster>();
        var mapBg = mapCanvasObj.AddComponent<Image>();
        mapBg.color = new Color(0, 0, 0, 0.85f);
        var mapBgRect = mapCanvasObj.GetComponent<RectTransform>();
        mapBgRect.anchorMin = Vector2.zero;
        mapBgRect.anchorMax = Vector2.one;
        mapBgRect.sizeDelta = Vector2.zero;

        var mapContainer = new GameObject("MapContainer");
        mapContainer.transform.SetParent(mapCanvasObj.transform, false);
        var mapContainerRect = mapContainer.AddComponent<RectTransform>();
        mapContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapContainerRect.sizeDelta = new Vector2(800, 800);

        var worldMapUI = mapCanvasObj.AddComponent<WorldMapUI>();
        SetSerializedField(worldMapUI, "mapCanvas", mapCanvas);
        SetSerializedField(worldMapUI, "mapContainer", mapContainerRect);

        // Region tile prefab
        var tilePrefab = CreateRegionTilePrefab();
        SetSerializedField(worldMapUI, "regionTilePrefab", tilePrefab);

        // Player marker prefab
        var markerPrefab = CreatePlayerMarkerPrefab();
        SetSerializedField(worldMapUI, "playerMarkerPrefab", markerPrefab);

        mapCanvasObj.SetActive(false);

        // --- Satchel UI ---
        var satchelCanvasObj = new GameObject("SatchelCanvas");
        satchelCanvasObj.transform.SetParent(canvasObj.transform, false);
        var satchelCanvas = satchelCanvasObj.AddComponent<Canvas>();
        satchelCanvas.overrideSorting = true;
        satchelCanvas.sortingOrder = 200;
        satchelCanvasObj.AddComponent<GraphicRaycaster>();
        var satchelBg = satchelCanvasObj.AddComponent<Image>();
        satchelBg.color = new Color(0, 0, 0, 0.85f);
        var satchelBgRect = satchelCanvasObj.GetComponent<RectTransform>();
        satchelBgRect.anchorMin = Vector2.zero;
        satchelBgRect.anchorMax = Vector2.one;
        satchelBgRect.sizeDelta = Vector2.zero;

        var satchelContent = new GameObject("SatchelContent");
        satchelContent.transform.SetParent(satchelCanvasObj.transform, false);
        var satchelContentRect = satchelContent.AddComponent<RectTransform>();
        satchelContentRect.anchorMin = new Vector2(0.1f, 0.1f);
        satchelContentRect.anchorMax = new Vector2(0.9f, 0.85f);
        satchelContentRect.sizeDelta = Vector2.zero;
        var gridLayout = satchelContent.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(90, 100);
        gridLayout.spacing = new Vector2(10, 10);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 5;

        var satchelUI = satchelCanvasObj.AddComponent<SatchelUI>();
        SetSerializedField(satchelUI, "satchelCanvas", satchelCanvas);
        SetSerializedField(satchelUI, "contentContainer", satchelContentRect);

        // Orb slot prefab
        var orbSlotPrefab = CreateOrbSlotPrefab();
        SetSerializedField(satchelUI, "orbSlotPrefab", orbSlotPrefab);

        // Capacity bar
        var capBarArea = new GameObject("CapacityBarArea");
        capBarArea.transform.SetParent(satchelCanvasObj.transform, false);
        var capBarRect = capBarArea.AddComponent<RectTransform>();
        capBarRect.anchorMin = new Vector2(0.1f, 0.9f);
        capBarRect.anchorMax = new Vector2(0.9f, 0.95f);
        capBarRect.sizeDelta = Vector2.zero;
        var capSlider = CreateSlider(capBarArea.transform, "CapacitySlider",
            new Vector2(0, 0), new Vector2(0.7f, 1));
        SetSerializedField(satchelUI, "capacityBar", capSlider);
        var capText = CreateUIText(capBarArea.transform, "CapacityText", "0 / 50",
            new Vector2(0.75f, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        capText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        SetSerializedField(satchelUI, "capacityText", capText.GetComponent<Text>());

        satchelCanvasObj.SetActive(false);

        // --- Mobile Input UI ---
        var inputObj = new GameObject("MobileInput");
        inputObj.transform.SetParent(canvasObj.transform, false);
        var inputRect = inputObj.AddComponent<RectTransform>();
        inputRect.anchorMin = Vector2.zero;
        inputRect.anchorMax = Vector2.one;
        inputRect.sizeDelta = Vector2.zero;
        var mobileInput = inputObj.AddComponent<MobileInputUI>();

        // Joystick
        var joystickBg = new GameObject("JoystickBackground");
        joystickBg.transform.SetParent(inputObj.transform, false);
        var jbRect = joystickBg.AddComponent<RectTransform>();
        jbRect.anchorMin = new Vector2(0, 0);
        jbRect.anchorMax = new Vector2(0, 0);
        jbRect.anchoredPosition = new Vector2(150, 150);
        jbRect.sizeDelta = new Vector2(150, 150);
        var jbImg = joystickBg.AddComponent<Image>();
        jbImg.color = new Color(1, 1, 1, 0.3f);
        jbImg.sprite = CreateCircleSprite();

        var joystickHandle = new GameObject("JoystickHandle");
        joystickHandle.transform.SetParent(joystickBg.transform, false);
        var jhRect = joystickHandle.AddComponent<RectTransform>();
        jhRect.sizeDelta = new Vector2(60, 60);
        var jhImg = joystickHandle.AddComponent<Image>();
        jhImg.color = new Color(1, 1, 1, 0.6f);
        jhImg.sprite = CreateCircleSprite();

        SetSerializedField(mobileInput, "joystickBackground", jbRect);
        SetSerializedField(mobileInput, "joystickHandle", jhRect);

        // Buttons
        var interactBtn = CreateUIButton(inputObj.transform, "InteractButton", "Act",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-100, 150), new Vector2(80, 80),
            new Color(0.2f, 0.8f, 0.3f, 0.7f));
        SetSerializedField(mobileInput, "interactButton", interactBtn);

        var mountBtn = CreateUIButton(inputObj.transform, "MountButton", "Mount",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-190, 100), new Vector2(70, 70),
            new Color(0.8f, 0.6f, 0.2f, 0.7f));
        SetSerializedField(mobileInput, "mountButton", mountBtn);

        var mapBtn = CreateUIButton(inputObj.transform, "MapButton", "Map",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -40), new Vector2(60, 60),
            new Color(0.3f, 0.5f, 0.9f, 0.7f));
        SetSerializedField(mobileInput, "mapButton", mapBtn);

        var satchelBtn = CreateUIButton(inputObj.transform, "SatchelButton", "Bag",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-130, -40), new Vector2(60, 60),
            new Color(0.7f, 0.4f, 0.2f, 0.7f));
        SetSerializedField(mobileInput, "satchelButton", satchelBtn);

        SetSerializedField(mobileInput, "worldMapUI", worldMapUI);
        SetSerializedField(mobileInput, "satchelUI", satchelUI);

        // --- EventSystem ---
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // --- Save Scene ---
        string scenesFolder = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(scenesFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
        EditorSceneManager.SaveScene(scene, scenesFolder + "/MainGame.unity");

        Debug.Log("Alien world scene created! Saved to Assets/Scenes/MainGame.unity");
        Debug.Log("Regions: " + regions.Length + " | Equipment: " + equipment.Length);
        Debug.Log("Islands, ocean, creatures, and orbs will generate at runtime.");
        Debug.Log("Hit Play to explore the alien archipelago!");
    }

    // --- Helper methods ---

    private static void SetSerializedField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetSerializedField(Object target, string fieldName, Object[] values)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null && prop.isArray)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static GameObject CreateUIPanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        return obj;
    }

    private static GameObject CreateUIText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        var t = obj.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 18;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        return obj;
    }

    private static Button CreateUIButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        var img = obj.AddComponent<Image>();
        img.color = color;
        img.sprite = CreateCircleSprite();
        var btn = obj.AddComponent<Button>();

        var textObj = CreateUIText(obj.transform, "Label", label,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        textObj.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        textObj.GetComponent<Text>().fontSize = 14;

        return btn;
    }

    private static Slider CreateSlider(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);
        var sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = anchorMin;
        sliderRect.anchorMax = anchorMax;
        sliderRect.sizeDelta = Vector2.zero;

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f);

        // Fill area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        var fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        var fillImg = fillObj.AddComponent<Image>();
        fillImg.color = Color.green;

        var slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.targetGraphic = bgImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 0.5f;

        // Remove handle since we just want a fill bar
        slider.handleRect = null;

        return slider;
    }

    private static Sprite CreatePlaceholderSprite(string name, Color color)
    {
        var tex = new Texture2D(4, 4);
        var pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.name = name;
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }

    private static Sprite CreateCircleSprite()
    {
        int size = 64;
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        float radius = center - 1;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        tex.name = "Circle";
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static GameObject CreateNotificationPrefab()
    {
        string prefabPath = "Assets/Resources/Prefabs";
        EnsureFolder(prefabPath);

        var obj = new GameObject("NotificationToast");
        var rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(350, 50);
        var bg = obj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        var cg = obj.AddComponent<CanvasGroup>();

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-20, 0);
        var text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 16;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath + "/NotificationToast.prefab");
        Object.DestroyImmediate(obj);
        return prefab;
    }

    private static GameObject CreateRegionTilePrefab()
    {
        string prefabPath = "Assets/Resources/Prefabs";
        EnsureFolder(prefabPath);

        var obj = new GameObject("RegionTile");
        var rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(50, 50);
        var img = obj.AddComponent<Image>();
        img.color = Color.gray;
        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;

        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(obj.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        var label = labelObj.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 10;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.text = "???";

        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath + "/RegionTile.prefab");
        Object.DestroyImmediate(obj);
        return prefab;
    }

    private static GameObject CreatePlayerMarkerPrefab()
    {
        string prefabPath = "Assets/Resources/Prefabs";
        EnsureFolder(prefabPath);

        var obj = new GameObject("PlayerMarker");
        var rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(20, 20);
        var img = obj.AddComponent<Image>();
        img.color = Color.yellow;

        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath + "/PlayerMarker.prefab");
        Object.DestroyImmediate(obj);
        return prefab;
    }

    private static GameObject CreateOrbSlotPrefab()
    {
        string prefabPath = "Assets/Resources/Prefabs";
        EnsureFolder(prefabPath);

        var obj = new GameObject("OrbSlot");
        var rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(90, 100);
        obj.AddComponent<CanvasGroup>();

        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(obj.transform, false);
        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0, 10);
        iconRect.sizeDelta = new Vector2(50, 50);
        var iconImg = iconObj.AddComponent<Image>();
        iconImg.color = Color.white;

        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(obj.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0);
        nameRect.anchorMax = new Vector2(1, 0.3f);
        nameRect.sizeDelta = Vector2.zero;
        var nameText = nameObj.AddComponent<Text>();
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 11;
        nameText.color = Color.white;
        nameText.alignment = TextAnchor.MiddleCenter;

        var countObj = new GameObject("Count");
        countObj.transform.SetParent(obj.transform, false);
        var countRect = countObj.AddComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0.5f, 0.5f);
        countRect.anchorMax = new Vector2(0.5f, 0.5f);
        countRect.anchoredPosition = new Vector2(0, 10);
        countRect.sizeDelta = new Vector2(50, 30);
        var countText = countObj.AddComponent<Text>();
        countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countText.fontSize = 18;
        countText.color = Color.white;
        countText.alignment = TextAnchor.MiddleCenter;
        countText.text = "0";

        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath + "/OrbSlot.prefab");
        Object.DestroyImmediate(obj);
        return prefab;
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i])) continue;
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
#endif
