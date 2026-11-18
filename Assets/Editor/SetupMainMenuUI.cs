using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public class SetupMainMenuUI
{
    [MenuItem("Tools/Setup Main Menu UI & Scene")]
    public static void ExecuteSetup()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "MainMenu")
        {
            scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        }

        Debug.Log("[SetupMainMenuUI] Starting setup on scene: " + scene.path);

        // 1. Load Font and Sprites
        TMP_FontAsset mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Synty/InterfaceSciFiMenus/Fonts/WDXL_Lubrifont_SC/WDXLLubrifontSC-Regular SDF.asset");
        if (mainFont == null)
        {
            mainFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        Sprite btnFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceSciFiMenus/Sprites/SciFiMenus/SPR_SciFiMenus_Button_Frame_04.png");
        Sprite btnFrameGlow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceSciFiMenus/Sprites/SciFiMenus/SPR_SciFiMenus_Button_Frame_04_Glow.png");
        Sprite playIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceSciFiMenus/Sprites/Icons_SciFiMenus/SPR_SciFiMenus_Icon_Play_01.png");
        Sprite charactersIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceSciFiMenus/Sprites/Icons_Menu/ICON_SciFiMenus_Menu_Solo_01_Clean.png");
        Sprite storeIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceSciFiMenus/Sprites/Icons_Menu/ICON_SciFiMenus_Menu_Shop_01_Clean.png");
        Sprite rankingIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceSciFiMenus/Sprites/Icons_Menu/ICON_SciFiMenus_Menu_Trophy_01_Clean.png");
        Sprite settingsIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceSciFiMenus/Sprites/Icons_Menu/ICON_SciFiMenus_Menu_Settings_01_Clean.png");
        Sprite soundOnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceSciFiMenus/Sprites/Icons_Menu/ICON_SciFiMenus_Menu_Sound_On_01_Clean.png");
        Sprite soundOffSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceSciFiMenus/Sprites/Icons_Menu/ICON_SciFiMenus_Menu_Sound_Off_01_Clean.png");
        Sprite musicOnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceSciFiMenus/Sprites/Icons_Menu/ICON_SciFiMenus_Menu_Music_On_01_Clean.png");
        Sprite musicOffSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceSciFiMenus/Sprites/Icons_Menu/ICON_SciFiMenus_Menu_Music_Off_01_Clean.png");
        Sprite saveIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceSciFiMenus/Sprites/Icons_Menu/ICON_SciFiMenus_Menu_Save_01_Clean.png");
        Sprite tickIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceSciFiMenus/Sprites/Icons_Menu/ICON_SciFiMenus_Menu_Tick_01_Clean.png");
        Sprite coinIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceSciFiMenus/Sprites/Icons_Menu/ICON_SciFiMenus_Menu_Currency_01_Clean.png");
        Sprite gemIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceSciFiMenus/Sprites/Icons_Menu/ICON_SciFiMenus_Menu_Currency_02_Clean.png");
        Sprite crestSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceSciFiMenus/Sprites/Crests/SPR_SciFiMenus_Crest_Shield_02_Gold.png");

        // 2. Setup Cameras
        GameObject menuCamObj = GameObject.Find("MenuCamera");
        if (menuCamObj == null)
        {
            menuCamObj = new GameObject("MenuCamera");
        }
        Camera menuCam = menuCamObj.GetComponent<Camera>();
        if (menuCam == null) menuCam = menuCamObj.AddComponent<Camera>();
        menuCam.depth = 1;
        menuCam.clearFlags = CameraClearFlags.Skybox;
        menuCam.fieldOfView = 58f;
        // Position facing crouching character on left side
        menuCam.transform.position = new Vector3(-1.35f, 0.95f, 12.1f);
        menuCam.transform.rotation = Quaternion.Euler(4.8f, 154.2f, 0f);

        AudioListener menuListener = menuCamObj.GetComponent<AudioListener>();
        if (menuListener == null) menuCamObj.AddComponent<AudioListener>();

        // Disable Player's internal camera during main menu so MenuCamera is sole view
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            foreach (var cam in playerObj.GetComponentsInChildren<Camera>(true))
            {
                if (cam.gameObject != menuCamObj)
                {
                    cam.gameObject.SetActive(false);
                }
            }
        }

        // 3. Find or Create Main Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        GameObject canvasObj = null;
        if (canvas != null)
        {
            canvasObj = canvas.gameObject;
        }
        else
        {
            canvasObj = new GameObject("MainCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Configure CanvasScaler if present
        CanvasScaler existingScaler = canvasObj.GetComponent<CanvasScaler>();
        if (existingScaler != null)
        {
            existingScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            existingScaler.referenceResolution = new Vector2(1080, 1920);
            existingScaler.matchWidthOrHeight = 0.5f;
        }

        // 4. Create or replace ReferenceUI under Canvas
        Transform existingRefUI = canvasObj.transform.Find("ReferenceMenuUI");
        if (existingRefUI != null)
        {
            Object.DestroyImmediate(existingRefUI.gameObject);
        }

        // Hide legacy MainMenuOverlay / MenuPanel if present
        Transform oldOverlay = canvasObj.transform.Find("MainMenuOverlay");
        if (oldOverlay != null) oldOverlay.gameObject.SetActive(false);
        Transform oldMenuPanel = canvasObj.transform.Find("MenuPanel");
        if (oldMenuPanel != null) oldMenuPanel.gameObject.SetActive(false);

        GameObject menuRoot = new GameObject("ReferenceMenuUI");
        menuRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform rootRect = menuRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        CanvasGroup rootCg = menuRoot.AddComponent<CanvasGroup>();

        // --- TOP-LEFT: Logo & Candlesticks ---
        GameObject logoContainer = new GameObject("LogoContainer");
        logoContainer.transform.SetParent(menuRoot.transform, false);
        RectTransform logoRect = logoContainer.AddComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0f, 1f);
        logoRect.anchorMax = new Vector2(0f, 1f);
        logoRect.pivot = new Vector2(0f, 1f);
        logoRect.anchoredPosition = new Vector2(40f, -40f);
        logoRect.sizeDelta = new Vector2(480f, 160f);

        // Accent Row (Runner, Crown, Candlesticks)
        GameObject accentRow = new GameObject("AccentRow");
        accentRow.transform.SetParent(logoContainer.transform, false);
        RectTransform accentRect = accentRow.AddComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = new Vector2(0f, 0f);
        accentRect.sizeDelta = new Vector2(480f, 55f);
        HorizontalLayoutGroup accentHlg = accentRow.AddComponent<HorizontalLayoutGroup>();
        accentHlg.childAlignment = TextAnchor.MiddleCenter;
        accentHlg.spacing = 16f;
        accentHlg.childControlWidth = false;
        accentHlg.childControlHeight = false;
        accentHlg.childForceExpandWidth = false;
        accentHlg.childForceExpandHeight = false;

        // Candlesticks Mini Display (Left)
        CreateCandlestickBadge(accentRow.transform, 70f, 40f);

        // Crown Icon (Center)
        GameObject crownObj = new GameObject("CrownIcon");
        crownObj.transform.SetParent(accentRow.transform, false);
        RectTransform crownRect = crownObj.AddComponent<RectTransform>();
        crownRect.sizeDelta = new Vector2(50f, 50f);
        Image crownImg = crownObj.AddComponent<Image>();
        crownImg.sprite = crestSprite != null ? crestSprite : storeIconSprite;
        crownImg.color = new Color(1f, 0.85f, 0.1f, 1f);
        crownImg.preserveAspect = true;

        // Runner Silhouette / Candlesticks Mini Display (Right)
        CreateCandlestickBadge(accentRow.transform, 70f, 40f);

        // Game Title Text
        GameObject titleObj = new GameObject("GameTitle");
        titleObj.transform.SetParent(logoContainer.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(1f, 0f);
        titleRect.pivot = new Vector2(0.5f, 0f);
        titleRect.anchoredPosition = new Vector2(0f, 10f);
        titleRect.sizeDelta = new Vector2(480f, 75f);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        if (mainFont != null) titleText.font = mainFont;
        titleText.text = "<i>Runner</i><b>Forex</b><color=#00E5FF>3D</color>";
        titleText.fontSize = 54;
        titleText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.95f, 0.98f, 1f, 1f);

        // --- TOP-RIGHT: Currency Pills (Coins & Gems) ---
        GameObject curContainer = new GameObject("CurrenciesContainer");
        curContainer.transform.SetParent(menuRoot.transform, false);
        RectTransform curRect = curContainer.AddComponent<RectTransform>();
        curRect.anchorMin = new Vector2(1f, 1f);
        curRect.anchorMax = new Vector2(1f, 1f);
        curRect.pivot = new Vector2(1f, 1f);
        curRect.anchoredPosition = new Vector2(-40f, -40f);
        curRect.sizeDelta = new Vector2(460f, 110f);
        VerticalLayoutGroup curVlg = curContainer.AddComponent<VerticalLayoutGroup>();
        curVlg.childAlignment = TextAnchor.UpperRight;
        curVlg.spacing = 10f;
        curVlg.childControlWidth = false;
        curVlg.childControlHeight = false;

        // Gold Coins Pill
        CreateCurrencyPill(curContainer.transform, btnFrame, coinIconSprite, new Color(1f, 0.84f, 0f), "125,000", mainFont, 240f);

        // Blue Gems Pill
        CreateCurrencyPill(curContainer.transform, btnFrame, gemIconSprite, new Color(0f, 0.85f, 1f), "3,500", mainFont, 210f);

        // --- RIGHT-SIDE VERTICAL BUTTON STACK ---
        GameObject stackContainer = new GameObject("ButtonStack");
        stackContainer.transform.SetParent(menuRoot.transform, false);
        RectTransform stackRect = stackContainer.AddComponent<RectTransform>();
        stackRect.anchorMin = new Vector2(1f, 0.5f);
        stackRect.anchorMax = new Vector2(1f, 0.5f);
        stackRect.pivot = new Vector2(1f, 0.5f);
        stackRect.anchoredPosition = new Vector2(-40f, -25f);
        stackRect.sizeDelta = new Vector2(360f, 620f);
        VerticalLayoutGroup stackVlg = stackContainer.AddComponent<VerticalLayoutGroup>();
        stackVlg.childAlignment = TextAnchor.MiddleRight;
        stackVlg.spacing = 14f;
        stackVlg.childControlWidth = false;
        stackVlg.childControlHeight = false;

        // 1. PLAY BUTTON (Vibrant Neon Green Glowing Pill)
        Button playBtn = CreateMenuPillButton(
            stackContainer.transform,
            "PlayButton",
            btnFrameGlow != null ? btnFrameGlow : btnFrame,
            new Color(0.02f, 0.88f, 0.28f, 1f), // Glowing neon green
            new Color(0.3f, 1f, 0.5f, 1f),
            playIconSprite,
            Color.white,
            "PLAY",
            mainFont,
            360f, 84f, 38f, true
        );

        // 2. CHARACTERS BUTTON (Glowing Blue Pill)
        Button charactersBtn = CreateMenuPillButton(
            stackContainer.transform,
            "CharactersButton",
            btnFrame,
            new Color(0.04f, 0.12f, 0.28f, 0.95f),
            new Color(0f, 0.8f, 1f, 1f),
            charactersIconSprite,
            new Color(1f, 0.85f, 0.1f),
            "CHARACTERS",
            mainFont,
            330f, 62f, 22f, false
        );

        // 3. STORE BUTTON
        Button storeBtn = CreateMenuPillButton(
            stackContainer.transform,
            "StoreButton",
            btnFrame,
            new Color(0.04f, 0.12f, 0.28f, 0.95f),
            new Color(0f, 0.8f, 1f, 1f),
            storeIconSprite,
            new Color(1f, 0.85f, 0.1f),
            "STORE",
            mainFont,
            330f, 62f, 22f, false
        );

        // 4. RANKING BUTTON
        Button rankingBtn = CreateMenuPillButton(
            stackContainer.transform,
            "RankingButton",
            btnFrame,
            new Color(0.04f, 0.12f, 0.28f, 0.95f),
            new Color(0f, 0.8f, 1f, 1f),
            rankingIconSprite,
            new Color(1f, 0.85f, 0.1f),
            "RANKING",
            mainFont,
            330f, 62f, 22f, false
        );

        // 5. FOREX QUIZ BUTTON
        Button forexQuizBtn = CreateMenuPillButton(
            stackContainer.transform,
            "ForexQuizButton",
            btnFrame,
            new Color(0.04f, 0.12f, 0.28f, 0.95f),
            new Color(0f, 0.8f, 1f, 1f),
            null, // custom candlesticks icon
            Color.white,
            "FOREX QUIZ",
            mainFont,
            330f, 62f, 22f, false
        );
        // Add candlestick miniature as icon for Forex Quiz
        Transform forexIconSlot = forexQuizBtn.transform.Find("IconSlot");
        if (forexIconSlot != null)
        {
            CreateCandlestickBadge(forexIconSlot, 36f, 32f);
        }

        // 6. SETTINGS BUTTON
        Button settingsBtn = CreateMenuPillButton(
            stackContainer.transform,
            "SettingsButton",
            btnFrame,
            new Color(0.04f, 0.12f, 0.28f, 0.95f),
            new Color(0f, 0.8f, 1f, 1f),
            settingsIconSprite,
            new Color(0.85f, 0.92f, 1f),
            "SETTINGS",
            mainFont,
            330f, 62f, 22f, false
        );

        // --- BOTTOM-LEFT: Sound & Music Controls ---
        GameObject audioControls = new GameObject("AudioControls");
        audioControls.transform.SetParent(menuRoot.transform, false);
        RectTransform audioRect = audioControls.AddComponent<RectTransform>();
        audioRect.anchorMin = new Vector2(0f, 0f);
        audioRect.anchorMax = new Vector2(0f, 0f);
        audioRect.pivot = new Vector2(0f, 0f);
        audioRect.anchoredPosition = new Vector2(40f, 40f);
        audioRect.sizeDelta = new Vector2(170f, 75f);
        HorizontalLayoutGroup audioHlg = audioControls.AddComponent<HorizontalLayoutGroup>();
        audioHlg.spacing = 14f;
        audioHlg.childAlignment = TextAnchor.MiddleLeft;
        audioHlg.childControlWidth = false;
        audioHlg.childControlHeight = false;

        // Sound Button
        Button soundBtn = CreateSquareIconButton(audioControls.transform, "SoundButton", btnFrame, soundOnSprite, 70f);
        Image soundImg = soundBtn.transform.Find("Icon").GetComponent<Image>();

        // Music Button
        Button musicBtn = CreateSquareIconButton(audioControls.transform, "MusicButton", btnFrame, musicOnSprite, 70f);
        Image musicImg = musicBtn.transform.Find("Icon").GetComponent<Image>();

        // --- BOTTOM-RIGHT: Cloud Save (Firebase) ---
        GameObject cloudSaveObj = new GameObject("CloudSavePill");
        cloudSaveObj.transform.SetParent(menuRoot.transform, false);
        RectTransform cloudRect = cloudSaveObj.AddComponent<RectTransform>();
        cloudRect.anchorMin = new Vector2(1f, 0f);
        cloudRect.anchorMax = new Vector2(1f, 0f);
        cloudRect.pivot = new Vector2(1f, 0f);
        cloudRect.anchoredPosition = new Vector2(-40f, 40f);
        cloudRect.sizeDelta = new Vector2(300f, 68f);

        Image cloudBg = cloudSaveObj.AddComponent<Image>();
        if (btnFrame != null)
        {
            cloudBg.sprite = btnFrame;
            cloudBg.type = Image.Type.Sliced;
        }
        cloudBg.color = new Color(0.04f, 0.12f, 0.28f, 0.95f);
        Button cloudBtn = cloudSaveObj.AddComponent<Button>();

        HorizontalLayoutGroup cloudHlg = cloudSaveObj.AddComponent<HorizontalLayoutGroup>();
        cloudHlg.childAlignment = TextAnchor.MiddleCenter;
        cloudHlg.spacing = 12f;
        cloudHlg.padding = new RectOffset(14, 14, 4, 4);
        cloudHlg.childControlWidth = false;
        cloudHlg.childControlHeight = false;

        // Cloud Upload Icon (Green)
        GameObject cloudIconObj = new GameObject("CloudIcon");
        cloudIconObj.transform.SetParent(cloudSaveObj.transform, false);
        RectTransform cIconRect = cloudIconObj.AddComponent<RectTransform>();
        cIconRect.sizeDelta = new Vector2(40f, 40f);
        Image cIcon = cloudIconObj.AddComponent<Image>();
        cIcon.sprite = saveIconSprite;
        cIcon.color = new Color(0.05f, 0.95f, 0.35f, 1f);
        cIcon.preserveAspect = true;

        // Text: Cloud Save (Firebase)
        GameObject cloudTextObj = new GameObject("CloudText");
        cloudTextObj.transform.SetParent(cloudSaveObj.transform, false);
        RectTransform cTextRect = cloudTextObj.AddComponent<RectTransform>();
        cTextRect.sizeDelta = new Vector2(170f, 50f);
        TextMeshProUGUI cText = cloudTextObj.AddComponent<TextMeshProUGUI>();
        if (mainFont != null) cText.font = mainFont;
        cText.text = "<b>Cloud Save</b>\n<size=13><color=#70C0FF>(Firebase)</color></size>";
        cText.fontSize = 18;
        cText.alignment = TextAlignmentOptions.Center;
        cText.color = Color.white;

        // Green Checkmark Tick Icon
        GameObject tickObj = new GameObject("TickIcon");
        tickObj.transform.SetParent(cloudSaveObj.transform, false);
        RectTransform tickRect = tickObj.AddComponent<RectTransform>();
        tickRect.sizeDelta = new Vector2(30f, 30f);
        Image tickImg = tickObj.AddComponent<Image>();
        tickImg.sprite = tickIconSprite;
        tickImg.color = new Color(0.05f, 0.95f, 0.35f, 1f);
        tickImg.preserveAspect = true;

        // 5. Add / Configure MainMenuController
        MainMenuController menuController = Object.FindObjectOfType<MainMenuController>();
        if (menuController == null)
        {
            GameObject controllerObj = new GameObject("MainMenuController");
            menuController = controllerObj.AddComponent<MainMenuController>();
        }

        menuController.menuCamera = menuCam;
        menuController.player = playerObj;
        menuController.runnerController = playerObj != null ? playerObj.GetComponent<RunnerController>() : null;
        menuController.playerAnimator = playerObj != null ? playerObj.GetComponentInChildren<Animator>() : null;
        menuController.menuCanvasGroup = rootCg;

        menuController.playButton = playBtn;
        menuController.charactersButton = charactersBtn;
        menuController.storeButton = storeBtn;
        menuController.rankingButton = rankingBtn;
        menuController.forexQuizButton = forexQuizBtn;
        menuController.settingsButton = settingsBtn;

        menuController.soundButton = soundBtn;
        menuController.soundIcon = soundImg;
        menuController.musicButton = musicBtn;
        menuController.musicIcon = musicImg;
        menuController.cloudSaveButton = cloudBtn;

        menuController.soundOnSprite = soundOnSprite;
        menuController.soundOffSprite = soundOffSprite;
        menuController.musicOnSprite = musicOnSprite;
        menuController.musicOffSprite = musicOffSprite;

        // Mark Dirty & Save Scene
        EditorUtility.SetDirty(menuCamObj);
        EditorUtility.SetDirty(menuRoot);
        EditorUtility.SetDirty(menuController);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SetupMainMenuUI] Successfully configured MainMenu scene with exact reference UI, cameras, animations and controller!");
    }

    private static void CreateCurrencyPill(Transform parent, Sprite frame, Sprite icon, Color iconColor, string amount, TMP_FontAsset font, float width)
    {
        GameObject pill = new GameObject("CurrencyPill_" + amount);
        pill.transform.SetParent(parent, false);
        RectTransform rt = pill.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 46f);

        Image bg = pill.AddComponent<Image>();
        if (frame != null)
        {
            bg.sprite = frame;
            bg.type = Image.Type.Sliced;
        }
        bg.color = new Color(0.03f, 0.1f, 0.22f, 0.92f);

        HorizontalLayoutGroup hlg = pill.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(10, 8, 4, 4);
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(pill.transform, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(32f, 32f);
        Image img = iconObj.AddComponent<Image>();
        img.sprite = icon;
        img.color = iconColor;
        img.preserveAspect = true;

        // Amount Text
        GameObject textObj = new GameObject("Amount");
        textObj.transform.SetParent(pill.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.sizeDelta = new Vector2(width - 85f, 36f);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = amount;
        tmp.fontSize = 20;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        // Green Plus Badge
        GameObject plusObj = new GameObject("PlusBtn");
        plusObj.transform.SetParent(pill.transform, false);
        RectTransform plusRt = plusObj.AddComponent<RectTransform>();
        plusRt.sizeDelta = new Vector2(30f, 30f);
        Image plusBg = plusObj.AddComponent<Image>();
        if (frame != null) { plusBg.sprite = frame; plusBg.type = Image.Type.Sliced; }
        plusBg.color = new Color(0.05f, 0.75f, 0.25f, 1f);

        GameObject plusTextObj = new GameObject("Text");
        plusTextObj.transform.SetParent(plusObj.transform, false);
        RectTransform ptRt = plusTextObj.AddComponent<RectTransform>();
        ptRt.anchorMin = Vector2.zero; ptRt.anchorMax = Vector2.one;
        ptRt.offsetMin = Vector2.zero; ptRt.offsetMax = Vector2.zero;
        TextMeshProUGUI pt = plusTextObj.AddComponent<TextMeshProUGUI>();
        if (font != null) pt.font = font;
        pt.text = "+";
        pt.fontSize = 20;
        pt.fontStyle = FontStyles.Bold;
        pt.alignment = TextAlignmentOptions.Center;
        pt.color = Color.white;
    }

    private static Button CreateMenuPillButton(
        Transform parent,
        string name,
        Sprite frame,
        Color bgColor,
        Color glowColor,
        Sprite icon,
        Color iconColor,
        string label,
        TMP_FontAsset font,
        float width,
        float height,
        float fontSize,
        bool isPlayButton
    )
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        Image bg = btnObj.AddComponent<Image>();
        if (frame != null)
        {
            bg.sprite = frame;
            bg.type = Image.Type.Sliced;
        }
        bg.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;

        HorizontalLayoutGroup hlg = btnObj.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = isPlayButton ? 16f : 12f;
        hlg.padding = new RectOffset(20, 20, 4, 4);
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        // Icon Slot
        GameObject iconSlot = new GameObject("IconSlot");
        iconSlot.transform.SetParent(btnObj.transform, false);
        RectTransform isRt = iconSlot.AddComponent<RectTransform>();
        float iconSize = isPlayButton ? 44f : 34f;
        isRt.sizeDelta = new Vector2(iconSize, iconSize);

        if (icon != null)
        {
            Image img = iconSlot.AddComponent<Image>();
            img.sprite = icon;
            img.color = iconColor;
            img.preserveAspect = true;
        }

        // Label Text
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        RectTransform ltRt = labelObj.AddComponent<RectTransform>();
        ltRt.sizeDelta = new Vector2(width - iconSize - 60f, height);
        TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.fontStyle = isPlayButton ? (FontStyles.Bold | FontStyles.Italic) : FontStyles.Bold;
        tmp.characterSpacing = 2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btn;
    }

    private static Button CreateSquareIconButton(Transform parent, string name, Sprite frame, Sprite icon, float size)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);

        Image bg = btnObj.AddComponent<Image>();
        if (frame != null)
        {
            bg.sprite = frame;
            bg.type = Image.Type.Sliced;
        }
        bg.color = new Color(0.04f, 0.12f, 0.28f, 0.95f);

        Button btn = btnObj.AddComponent<Button>();

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = new Vector2(10, 10);
        iconRt.offsetMax = new Vector2(-10, -10);
        Image img = iconObj.AddComponent<Image>();
        img.sprite = icon;
        img.color = Color.white;
        img.preserveAspect = true;

        return btn;
    }

    private static void CreateCandlestickBadge(Transform parent, float width, float height)
    {
        GameObject candleContainer = new GameObject("CandlestickBadge");
        candleContainer.transform.SetParent(parent, false);
        RectTransform cRt = candleContainer.AddComponent<RectTransform>();
        cRt.sizeDelta = new Vector2(width, height);

        HorizontalLayoutGroup hlg = candleContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 4f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        Color green = new Color(0.05f, 0.95f, 0.35f, 1f);
        Color red = new Color(1f, 0.2f, 0.25f, 1f);

        CreateSingleBar(candleContainer.transform, red, 6f, 22f);
        CreateSingleBar(candleContainer.transform, green, 6f, 32f);
        CreateSingleBar(candleContainer.transform, red, 6f, 18f);
        CreateSingleBar(candleContainer.transform, green, 6f, 28f);
    }

    private static void CreateSingleBar(Transform parent, Color color, float width, float height)
    {
        GameObject bar = new GameObject("Bar");
        bar.transform.SetParent(parent, false);
        RectTransform rt = bar.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);
        Image img = bar.AddComponent<Image>();
        img.color = color;
    }
}
