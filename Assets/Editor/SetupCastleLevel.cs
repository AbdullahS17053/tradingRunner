using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using RunnerForex.Castle;

public class SetupCastleLevel : EditorWindow
{
    [MenuItem("Tools/Setup Castle Level Scene")]
    public static void MenuSetup()
    {
        ExecuteSetup();
    }

    [MenuItem("Tools/Setup Castle Sequence (Doors, Lottery Box, Portal)")]
    public static void EnhanceCastleSequenceMenu()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "Castle level" && !scene.path.Contains("Castle level"))
        {
            Debug.Log("[SetupCastleLevel] Active scene is not Castle level.");
            return;
        }

        GameObject rearBot = GameObject.Find("FomoBot_RearChaser");
        if (rearBot != null) rearBot.SetActive(false);

        GameObject seqObj = GameObject.Find("CastleSequenceController");
        if (seqObj == null) seqObj = new GameObject("CastleSequenceController");
        CastleSequenceController seq = seqObj.GetComponent<CastleSequenceController>();
        if (seq == null) seq = seqObj.AddComponent<CastleSequenceController>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SetupCastleLevel] Enhanced Castle Sequence setup saved to scene.");
    }

    public static void ExecuteSetup()
    {
        var scene = EditorSceneManager.GetActiveScene();
        Debug.Log($"[SetupCastleLevel] Detected active scene: name='{scene.name}', path='{scene.path}'");

        if (scene.name != "Castle level" && !scene.path.Contains("Castle level"))
        {
            Debug.Log("[SetupCastleLevel] Active scene is not Castle level. Skipping auto-setup.");
            return; // Only run when Castle level is open
        }

        Debug.Log("[SetupCastleLevel] Starting automated Castle Level setup...");

        // 1. Remove existing gameplay root if present for clean idempotent rebuild
        GameObject existingRoot = GameObject.Find("-- CASTLE GAMEPLAY --");
        if (existingRoot != null)
        {
            DestroyImmediate(existingRoot);
        }

        // Disable existing static camera in demo scene
        Camera[] allCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in allCams)
        {
            if (cam.gameObject.name == "Main Camera" && cam.transform.parent == null)
            {
                cam.gameObject.SetActive(false);
            }
        }

        GameObject gameplayRoot = new GameObject("-- CASTLE GAMEPLAY --");
        gameplayRoot.transform.position = Vector3.zero;

        // 2. Setup Track & Running Corridor (Z: -10 to 90)
        GameObject trackRoot = new GameObject("CastleTrack");
        trackRoot.transform.SetParent(gameplayRoot.transform, false);

        // Main Floor Collider
        GameObject floorObj = new GameObject("TrackFloor", typeof(BoxCollider));
        floorObj.transform.SetParent(trackRoot.transform, false);
        floorObj.transform.position = new Vector3(0f, -0.1f, 40f);
        BoxCollider floorCol = floorObj.GetComponent<BoxCollider>();
        floorCol.size = new Vector3(12f, 0.2f, 110f);

        // Track Visual Runway (Marble / Red Carpet strip)
        GameObject carpetObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        carpetObj.name = "RedCarpetRunway";
        carpetObj.transform.SetParent(trackRoot.transform, false);
        carpetObj.transform.position = new Vector3(0f, -0.05f, 40f);
        carpetObj.transform.localScale = new Vector3(8.5f, 0.1f, 105f);
        DestroyImmediate(carpetObj.GetComponent<Collider>());
        Renderer carpetRend = carpetObj.GetComponent<Renderer>();
        carpetRend.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        carpetRend.sharedMaterial.color = new Color(0.6f, 0.08f, 0.12f, 1f); // Royal Burgundy Red

        // Side Borders / Gold Trim
        GameObject borderL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        borderL.name = "BorderLeft";
        borderL.transform.SetParent(trackRoot.transform, false);
        borderL.transform.position = new Vector3(-4.4f, 0.15f, 40f);
        borderL.transform.localScale = new Vector3(0.4f, 0.3f, 105f);
        Renderer bordLRend = borderL.GetComponent<Renderer>();
        bordLRend.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        bordLRend.sharedMaterial.color = new Color(0.9f, 0.75f, 0.2f, 1f); // Gold

        GameObject borderR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        borderR.name = "BorderRight";
        borderR.transform.SetParent(trackRoot.transform, false);
        borderR.transform.position = new Vector3(4.4f, 0.15f, 40f);
        borderR.transform.localScale = new Vector3(0.4f, 0.3f, 105f);
        Renderer bordRRend = borderR.GetComponent<Renderer>();
        bordRRend.sharedMaterial = bordLRend.sharedMaterial;

        // 3. Instantiate Player
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        GameObject playerInstance = null;
        if (playerPrefab != null)
        {
            playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, gameplayRoot.transform);
            playerInstance.name = "Player";
            playerInstance.transform.position = new Vector3(0f, 0.15f, 0f);
            playerInstance.transform.rotation = Quaternion.identity;

            // Ensure internal camera is active
            Camera playerCam = playerInstance.GetComponentInChildren<Camera>(true);
            if (playerCam != null)
            {
                playerCam.gameObject.SetActive(true);
                playerCam.tag = "MainCamera";
            }
        }

        // 4. Managers (GameManager & GemReviveManager)
        GameObject managerObj = new GameObject("GameManager", typeof(GameManager), typeof(GemReviveManager));
        managerObj.transform.SetParent(gameplayRoot.transform, false);
        GameManager gm = managerObj.GetComponent<GameManager>();
        GemReviveManager grm = managerObj.GetComponent<GemReviveManager>();

        // Wire player to GameManager
        if (playerInstance != null)
        {
            gm.playerAnimator = playerInstance.GetComponentInChildren<Animator>();
        }

        // 5. Place Three Collectible Gems
        GameObject gemsRoot = new GameObject("CollectibleGems");
        gemsRoot.transform.SetParent(gameplayRoot.transform, false);

        CreateGemInstance("Assets/PolygonFantasyKingdom/Prefabs/Items/SM_Item_Gem_01.prefab", gemsRoot.transform, new Vector3(0f, 1.2f, 18f), GemPickup.GemColorType.CyanDiamond, new Color(0f, 0.9f, 1f));
        CreateGemInstance("Assets/PolygonFantasyKingdom/Prefabs/Items/SM_Item_Gem_02.prefab", gemsRoot.transform, new Vector3(-2.5f, 1.2f, 36f), GemPickup.GemColorType.PurpleAmethyst, new Color(0.8f, 0.2f, 1f));
        CreateGemInstance("Assets/PolygonFantasyKingdom/Prefabs/Items/SM_Item_Gem_03.prefab", gemsRoot.transform, new Vector3(2.5f, 1.2f, 54f), GemPickup.GemColorType.GoldTopaz, new Color(1f, 0.85f, 0.1f));

        // 6. Place Two FOMO Bot Enemies
        GameObject enemiesRoot = new GameObject("FomoBots");
        enemiesRoot.transform.SetParent(gameplayRoot.transform, false);

        GameObject fomoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FOMO.prefab");
        if (fomoPrefab != null)
        {
            // Bot 1: Front Patrol / Charge Bot at Z = 28
            GameObject bot1 = (GameObject)PrefabUtility.InstantiatePrefab(fomoPrefab, enemiesRoot.transform);
            PrefabUtility.UnpackPrefabInstance(bot1, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            bot1.name = "FomoBot_FrontPatrol";
            bot1.tag = "Obstacle";
            bot1.transform.position = new Vector3(0f, 0.5f, 28f);
            bot1.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            CapsuleCollider col1 = bot1.GetComponent<CapsuleCollider>();
            if (col1 == null) col1 = bot1.AddComponent<CapsuleCollider>();
            col1.isTrigger = true;
            Rigidbody rb1 = bot1.GetComponent<Rigidbody>();
            if (rb1 == null) rb1 = bot1.AddComponent<Rigidbody>();
            rb1.isKinematic = true;
            rb1.useGravity = false;
            FomoBot fb1 = bot1.AddComponent<FomoBot>();
            fb1.behaviorType = FomoBot.BotBehaviorType.FrontPatrolCharge;
            fb1.movementSpeed = 6f;
            fb1.botVariantName = "Castle Guard Bot";

            // Bot 2: Rear Chaser Bot at Z = -10 (catches up when runner reaches Z = 42)
            GameObject bot2 = (GameObject)PrefabUtility.InstantiatePrefab(fomoPrefab, enemiesRoot.transform);
            PrefabUtility.UnpackPrefabInstance(bot2, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            bot2.name = "FomoBot_RearChaser";
            bot2.tag = "Obstacle";
            bot2.transform.position = new Vector3(2.5f, 0.5f, -10f);
            bot2.transform.rotation = Quaternion.identity;
            CapsuleCollider col2 = bot2.GetComponent<CapsuleCollider>();
            if (col2 == null) col2 = bot2.AddComponent<CapsuleCollider>();
            col2.isTrigger = true;
            Rigidbody rb2 = bot2.GetComponent<Rigidbody>();
            if (rb2 == null) rb2 = bot2.AddComponent<Rigidbody>();
            rb2.isKinematic = true;
            rb2.useGravity = false;
            FomoBot fb2 = bot2.AddComponent<FomoBot>();
            fb2.behaviorType = FomoBot.BotBehaviorType.RearChaser;
            fb2.rearApproachSpeed = 22f;
            fb2.botVariantName = "Rear Flanker Bot";
        }

        // 7. Place Trading Floor Computer Desk & Chair at Z = 74
        GameObject deskRoot = new GameObject("ExecutiveTradingStation");
        deskRoot.transform.SetParent(gameplayRoot.transform, false);
        deskRoot.transform.position = new Vector3(0f, 0f, 74f);

        GameObject tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonFantasyKingdom/Prefabs/Props/Furniture/SM_Prop_Table_Fancy_01.prefab");
        if (tablePrefab != null)
        {
            GameObject table = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab, deskRoot.transform);
            table.transform.localPosition = new Vector3(0f, 0f, 2.2f);
            table.transform.localRotation = Quaternion.identity;
        }

        GameObject chairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonFantasyKingdom/Prefabs/Props/Furniture/SM_Prop_Chair_Fancy_01.prefab");
        GameObject chair = null;
        if (chairPrefab != null)
        {
            chair = (GameObject)PrefabUtility.InstantiatePrefab(chairPrefab, deskRoot.transform);
            chair.transform.localPosition = new Vector3(0f, 0f, 0.8f);
            chair.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }

        GameObject monitorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/PolygonSciFiCity/Prefabs/Props/SM_Prop_Monitor_01.prefab");
        if (monitorPrefab != null)
        {
            GameObject mon = (GameObject)PrefabUtility.InstantiatePrefab(monitorPrefab, deskRoot.transform);
            mon.transform.localPosition = new Vector3(0f, 0.88f, 2.0f);
            mon.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            mon.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
        }

        // Camera viewpoint when seated at computer screen
        GameObject camPosObj = new GameObject("ComputerCameraView");
        camPosObj.transform.SetParent(deskRoot.transform, false);
        camPosObj.transform.localPosition = new Vector3(0f, 1.45f, 1.0f);
        camPosObj.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);

        // Interaction Trigger
        GameObject triggerObj = new GameObject("ComputerTriggerZone", typeof(BoxCollider), typeof(CastleComputerTrigger));
        triggerObj.transform.SetParent(deskRoot.transform, false);
        triggerObj.transform.localPosition = new Vector3(0f, 1f, -0.5f);
        BoxCollider trigCol = triggerObj.GetComponent<BoxCollider>();
        trigCol.isTrigger = true;
        trigCol.size = new Vector3(8f, 3f, 2.5f);

        CastleComputerTrigger compTrig = triggerObj.GetComponent<CastleComputerTrigger>();
        compTrig.chairSeatTransform = chair != null ? chair.transform : deskRoot.transform;
        compTrig.computerCameraPosition = camPosObj.transform;

        // Exit Portal
        GameObject exitPortalObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        exitPortalObj.name = "CastleExitPortal";
        exitPortalObj.transform.SetParent(gameplayRoot.transform, false);
        exitPortalObj.transform.position = new Vector3(0f, 2f, 85f);
        exitPortalObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        exitPortalObj.transform.localScale = new Vector3(4f, 0.1f, 4f);
        Renderer portalRend = exitPortalObj.GetComponent<Renderer>();
        portalRend.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
        portalRend.sharedMaterial.color = new Color(0f, 0.95f, 1f, 0.9f);
        exitPortalObj.SetActive(false); // Unlocks on challenge completion

        // 8. Build Castle Challenge UI Canvas
        BuildCastleChallengeUI(gameplayRoot.transform, compTrig, exitPortalObj);

        // 9. Add EventSystem for UI interactions (BUY, SELL, WAIT, Modals)
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            esObj.transform.SetParent(gameplayRoot.transform, false);
        }

        // 10. Add Gameplay HUD
        BuildCastleHUD(gameplayRoot.transform);

        // Save scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SetupCastleLevel] Castle Level setup complete! Scene saved.");
    }

    private static void CreateGemInstance(string prefabPath, Transform parent, Vector3 localPos, GemPickup.GemColorType type, Color glow)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject gemObj;
        if (prefab != null)
        {
            gemObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            PrefabUtility.UnpackPrefabInstance(gemObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
        else
        {
            gemObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gemObj.transform.SetParent(parent, false);
        }

        gemObj.name = $"Gem_{type}";
        gemObj.transform.position = localPos;
        gemObj.transform.localScale = Vector3.one * 1.5f;

        SphereCollider sc = gemObj.GetComponent<SphereCollider>();
        if (sc == null) sc = gemObj.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.8f;

        Rigidbody rb = gemObj.GetComponent<Rigidbody>();
        if (rb == null) rb = gemObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        GemPickup gp = gemObj.AddComponent<GemPickup>();
        gp.gemType = type;
        gp.gemGlowColor = glow;
    }

    private static void BuildCastleHUD(Transform parent)
    {
        GameObject hudCanvasObj = new GameObject("CastleGameplayHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        hudCanvasObj.transform.SetParent(parent, false);

        Canvas hudCanvas = hudCanvasObj.GetComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 5;

        CanvasScaler scaler = hudCanvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        GameUI gameUI = hudCanvasObj.AddComponent<GameUI>();

        GameObject scoreObj = new GameObject("ScoreDisplay", typeof(RectTransform), typeof(TextMeshProUGUI));
        scoreObj.transform.SetParent(hudCanvasObj.transform, false);
        RectTransform rt = scoreObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -60);
        rt.sizeDelta = new Vector2(400, 60);

        TextMeshProUGUI scoreTxt = scoreObj.GetComponent<TextMeshProUGUI>();
        scoreTxt.text = "0";
        scoreTxt.fontSize = 48;
        scoreTxt.fontStyle = FontStyles.Bold;
        scoreTxt.alignment = TextAlignmentOptions.Center;
        scoreTxt.color = new Color(1f, 0.85f, 0.2f, 1f);
        gameUI.scoreText = scoreTxt;
    }

    private static void BuildCastleChallengeUI(Transform parent, CastleComputerTrigger compTrig, GameObject exitPortal)
    {
        GameObject canvasObj = new GameObject("CastleChallengeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(parent, false);

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        CastleChallengeController controller = canvasObj.AddComponent<CastleChallengeController>();
        controller.challengeCanvas = canvasObj;
        controller.computerTrigger = compTrig;
        controller.exitPortal = exitPortal;

        // Main Panel (Center 880 x 1400)
        GameObject mainPanel = new GameObject("MainChallengePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        mainPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform mainRt = mainPanel.GetComponent<RectTransform>();
        mainRt.sizeDelta = new Vector2(920, 1460);
        Image mainBg = mainPanel.GetComponent<Image>();
        mainBg.color = new Color(0.04f, 0.07f, 0.14f, 0.96f); // Deep Cyberpunk Navy

        // Header: Pair & Timeframe (e.g. XAUUSD · M15)
        GameObject pairObj = new GameObject("PairTimeframeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        pairObj.transform.SetParent(mainPanel.transform, false);
        RectTransform pairRt = pairObj.GetComponent<RectTransform>();
        pairRt.anchoredPosition = new Vector2(-180, 660);
        pairRt.sizeDelta = new Vector2(480, 50);
        TextMeshProUGUI pairTxt = pairObj.GetComponent<TextMeshProUGUI>();
        pairTxt.text = "XAUUSD · M15";
        pairTxt.fontSize = 32;
        pairTxt.fontStyle = FontStyles.Bold;
        pairTxt.color = new Color(0f, 0.95f, 1f, 1f);
        controller.pairTimeframeText = pairTxt;

        // Header: Timer Text
        GameObject timerObj = new GameObject("TimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
        timerObj.transform.SetParent(mainPanel.transform, false);
        RectTransform timerRt = timerObj.GetComponent<RectTransform>();
        timerRt.anchoredPosition = new Vector2(280, 660);
        timerRt.sizeDelta = new Vector2(260, 50);
        TextMeshProUGUI timerTxt = timerObj.GetComponent<TextMeshProUGUI>();
        timerTxt.text = "40:00";
        timerTxt.fontSize = 30;
        timerTxt.fontStyle = FontStyles.Bold;
        timerTxt.alignment = TextAlignmentOptions.Right;
        timerTxt.color = new Color(1f, 0.85f, 0.2f, 1f);
        controller.timerText = timerTxt;

        // Challenge Counter
        GameObject progObj = new GameObject("ProgressText", typeof(RectTransform), typeof(TextMeshProUGUI));
        progObj.transform.SetParent(mainPanel.transform, false);
        RectTransform progRt = progObj.GetComponent<RectTransform>();
        progRt.anchoredPosition = new Vector2(0, 605);
        progRt.sizeDelta = new Vector2(800, 35);
        TextMeshProUGUI progTxt = progObj.GetComponent<TextMeshProUGUI>();
        progTxt.text = "Challenge 1 of 5";
        progTxt.fontSize = 22;
        progTxt.alignment = TextAlignmentOptions.Center;
        progTxt.color = new Color(0.7f, 0.8f, 0.95f, 0.8f);
        controller.progressCounterText = progTxt;

        // Chart Container Box
        GameObject chartBox = new GameObject("ChartBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        chartBox.transform.SetParent(mainPanel.transform, false);
        RectTransform chartBoxRt = chartBox.GetComponent<RectTransform>();
        chartBoxRt.anchoredPosition = new Vector2(0, 220);
        chartBoxRt.sizeDelta = new Vector2(840, 680);
        Image chartBoxBg = chartBox.GetComponent<Image>();
        chartBoxBg.color = new Color(0.02f, 0.04f, 0.08f, 1f);

        // Chart Area (Candles)
        GameObject chartAreaObj = new GameObject("CandlesArea", typeof(RectTransform));
        chartAreaObj.transform.SetParent(chartBox.transform, false);
        RectTransform cAreaRt = chartAreaObj.GetComponent<RectTransform>();
        cAreaRt.anchoredPosition = new Vector2(0, 70);
        cAreaRt.sizeDelta = new Vector2(800, 500);

        // RSI Area
        GameObject rsiAreaObj = new GameObject("RsiArea", typeof(RectTransform));
        rsiAreaObj.transform.SetParent(chartBox.transform, false);
        RectTransform rsiRt = rsiAreaObj.GetComponent<RectTransform>();
        rsiRt.anchoredPosition = new Vector2(0, -260);
        rsiRt.sizeDelta = new Vector2(800, 110);

        CastleChartRenderer chartRend = chartBox.AddComponent<CastleChartRenderer>();
        chartRend.chartArea = cAreaRt;
        chartRend.rsiArea = rsiRt;
        controller.chartRenderer = chartRend;

        // Scenario Description Card
        GameObject scenObj = new GameObject("ScenarioCard", typeof(RectTransform), typeof(TextMeshProUGUI));
        scenObj.transform.SetParent(mainPanel.transform, false);
        RectTransform scenRt = scenObj.GetComponent<RectTransform>();
        scenRt.anchoredPosition = new Vector2(0, -200);
        scenRt.sizeDelta = new Vector2(840, 110);
        TextMeshProUGUI scenTxt = scenObj.GetComponent<TextMeshProUGUI>();
        scenTxt.fontSize = 20;
        scenTxt.lineSpacing = 6;
        scenTxt.alignment = TextAlignmentOptions.Center;
        scenTxt.color = Color.white;
        controller.scenarioDescriptionText = scenTxt;

        // Action Buttons: BUY, SELL, WAIT
        controller.buyButton = CreateActionButton(mainPanel.transform, "BUY", new Vector2(-280, -320), new Color(0.05f, 0.85f, 0.4f, 1f));
        controller.sellButton = CreateActionButton(mainPanel.transform, "SELL", new Vector2(0, -320), new Color(0.95f, 0.22f, 0.32f, 1f));
        controller.waitButton = CreateActionButton(mainPanel.transform, "WAIT", new Vector2(280, -320), new Color(1f, 0.75f, 0.1f, 1f));

        // Special Banners (News 24, Risk 27)
        controller.newsSpreadBanner = CreateBanner(mainPanel.transform, "HIGH-IMPACT NEWS SOON · SPREADS WIDENING", new Vector2(0, -410), new Color(1f, 0.45f, 0.1f, 1f));
        controller.riskWarningBanner = CreateBanner(mainPanel.transform, "STOP-LOSS RISK TOO HIGH FOR THIS ACCOUNT", new Vector2(0, -410), new Color(0.95f, 0.2f, 0.2f, 1f));

        // Feedback Modal
        controller.feedbackModal = CreateFeedbackModal(mainPanel.transform, controller);

        // Failure Modal
        controller.failureModal = CreateFailureModal(mainPanel.transform, controller);

        // Victory Modal
        controller.victoryModal = CreateVictoryModal(mainPanel.transform);

        canvasObj.SetActive(false); // Activated by trigger zone
    }

    private static Button CreateActionButton(Transform parent, string label, Vector2 pos, Color btnColor)
    {
        GameObject btnObj = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(240, 75);
        Image img = btnObj.GetComponent<Image>();
        img.color = btnColor;
        Button btn = btnObj.GetComponent<Button>();

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.sizeDelta = rt.sizeDelta;
        TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 28;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;

        return btn;
    }

    private static GameObject CreateBanner(Transform parent, string text, Vector2 pos, Color color)
    {
        GameObject bannerObj = new GameObject("Banner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bannerObj.transform.SetParent(parent, false);
        RectTransform rt = bannerObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(840, 48);
        Image img = bannerObj.GetComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, 0.25f);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(bannerObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.sizeDelta = rt.sizeDelta;
        TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.fontSize = 18;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = color;

        bannerObj.SetActive(false);
        return bannerObj;
    }

    private static GameObject CreateFeedbackModal(Transform parent, CastleChallengeController controller)
    {
        GameObject modal = new GameObject("FeedbackModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modal.transform.SetParent(parent, false);
        RectTransform rt = modal.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(800, 480);
        Image bg = modal.GetComponent<Image>();
        bg.color = new Color(0.06f, 0.1f, 0.2f, 0.98f);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(modal.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0, 160);
        titleRt.sizeDelta = new Vector2(720, 50);
        TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
        titleTxt.fontSize = 32;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Center;
        controller.feedbackTitleText = titleTxt;

        GameObject expObj = new GameObject("Explanation", typeof(RectTransform), typeof(TextMeshProUGUI));
        expObj.transform.SetParent(modal.transform, false);
        RectTransform expRt = expObj.GetComponent<RectTransform>();
        expRt.anchoredPosition = new Vector2(0, 30);
        expRt.sizeDelta = new Vector2(720, 180);
        TextMeshProUGUI expTxt = expObj.GetComponent<TextMeshProUGUI>();
        expTxt.fontSize = 22;
        expTxt.lineSpacing = 6;
        expTxt.alignment = TextAlignmentOptions.Center;
        expTxt.color = Color.white;
        controller.feedbackExplanationText = expTxt;

        controller.nextChallengeButton = CreateActionButton(modal.transform, "CONTINUE", new Vector2(0, -150), new Color(0f, 0.8f, 1f, 1f));
        modal.SetActive(false);
        return modal;
    }

    private static GameObject CreateFailureModal(Transform parent, CastleChallengeController controller)
    {
        GameObject modal = new GameObject("FailureModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modal.transform.SetParent(parent, false);
        RectTransform rt = modal.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(800, 520);
        Image bg = modal.GetComponent<Image>();
        bg.color = new Color(0.12f, 0.04f, 0.06f, 0.98f);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(modal.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0, 180);
        titleRt.sizeDelta = new Vector2(720, 50);
        TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
        titleTxt.text = "<color=#FF3D71>CHALLENGE FAILED</color>";
        titleTxt.fontSize = 32;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Center;

        GameObject reasonObj = new GameObject("ReasonText", typeof(RectTransform), typeof(TextMeshProUGUI));
        reasonObj.transform.SetParent(modal.transform, false);
        RectTransform reasonRt = reasonObj.GetComponent<RectTransform>();
        reasonRt.anchoredPosition = new Vector2(0, 40);
        reasonRt.sizeDelta = new Vector2(720, 180);
        TextMeshProUGUI reasonTxt = reasonObj.GetComponent<TextMeshProUGUI>();
        reasonTxt.fontSize = 20;
        reasonTxt.lineSpacing = 6;
        reasonTxt.alignment = TextAlignmentOptions.Center;
        reasonTxt.color = Color.white;
        controller.failureReasonText = reasonTxt;

        controller.retryButton = CreateActionButton(modal.transform, "RETRY (NEW 5)", new Vector2(0, -110), new Color(0.95f, 0.45f, 0.1f, 1f));
        controller.leaveWithoutRewardButton = CreateActionButton(modal.transform, "LEAVE WITHOUT REWARD", new Vector2(0, -195), new Color(0.4f, 0.45f, 0.55f, 1f));

        modal.SetActive(false);
        return modal;
    }

    private static GameObject CreateVictoryModal(Transform parent)
    {
        GameObject modal = new GameObject("VictoryModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modal.transform.SetParent(parent, false);
        RectTransform rt = modal.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(800, 420);
        Image bg = modal.GetComponent<Image>();
        bg.color = new Color(0.04f, 0.14f, 0.08f, 0.98f);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(modal.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0, 120);
        titleRt.sizeDelta = new Vector2(720, 60);
        TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
        titleTxt.text = "<color=#00E676>CHALLENGE PASSED!</color>";
        titleTxt.fontSize = 36;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Center;

        GameObject rewardObj = new GameObject("RewardText", typeof(RectTransform), typeof(TextMeshProUGUI));
        rewardObj.transform.SetParent(modal.transform, false);
        RectTransform rewRt = rewardObj.GetComponent<RectTransform>();
        rewRt.anchoredPosition = new Vector2(0, 20);
        rewRt.sizeDelta = new Vector2(720, 60);
        TextMeshProUGUI rewTxt = rewardObj.GetComponent<TextMeshProUGUI>();
        rewTxt.text = "+1,000 COINS EARNED!\n<size=20><color=#FFD700>Portal to Next World Unlocked</color></size>";
        rewTxt.fontSize = 28;
        rewTxt.fontStyle = FontStyles.Bold;
        rewTxt.alignment = TextAlignmentOptions.Center;
        rewTxt.color = Color.white;

        modal.SetActive(false);
        return modal;
    }
}
