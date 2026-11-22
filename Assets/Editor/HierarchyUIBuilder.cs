using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using RunnerForex.Castle;

namespace RunnerForex.EditorTools
{
    public static class HierarchyUIBuilder
    {
        private const string PREFAB_REVIVE_PATH = "Assets/Prefabs/UI/GemReviveDialog.prefab";
        private const string PREFAB_FOREX_PATH = "Assets/Prefabs/UI/ForexPatternsPanel.prefab";
        private const string PREFAB_GAMEOVER_PATH = "Assets/Prefabs/UI/GameOverUI.prefab";

        [MenuItem("Tools/Bake All UI Into Hierarchy")]
        public static void BakeAll()
        {
            Debug.Log("<color=cyan>[HierarchyUIBuilder] Starting full UI baking pipeline...</color>");

            EnsurePrefabsFolder();
            GameObject revivePrefab = BuildOrGetRevivePrefab();
            GameObject forexPrefab = BuildOrGetForexPatternsPrefab();

            BakeCastleLevel(revivePrefab, forexPrefab);
            BakeMainMenu(forexPrefab);
            BakeCityScene(revivePrefab);
            BakeSpaceScene(revivePrefab);
            BakeLabyrinthScene(revivePrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[HierarchyUIBuilder] Successfully baked all UI panels into scene hierarchies!</color>");
        }

        [InitializeOnLoadMethod]
        private static void OnEditorLoaded()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool("HierarchyUI_Baked_Done", false))
                {
                    EditorPrefs.SetBool("HierarchyUI_Baked_Done", true);
                    BakeAll();
                }
            };
        }

        private static void EnsurePrefabsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }
        }

        #region Prefab Builders

        public static GameObject BuildOrGetRevivePrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_REVIVE_PATH);
            if (existing != null) return existing;

            GameObject root = new GameObject("GemReviveDialog", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            Canvas c = root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 1000;

            CanvasScaler cs = root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);

            // Dark Dimmer
            GameObject dimmer = new GameObject("Dimmer", typeof(RectTransform), typeof(Image));
            dimmer.transform.SetParent(root.transform, false);
            RectTransform drt = dimmer.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero;
            drt.anchorMax = Vector2.one;
            drt.sizeDelta = Vector2.zero;
            dimmer.GetComponent<Image>().color = new Color(0.01f, 0.02f, 0.05f, 0.88f);

            // Content Panel
            GameObject panel = new GameObject("ContentBox", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            RectTransform prt = panel.GetComponent<RectTransform>();
            prt.sizeDelta = new Vector2(620, 560);
            Image pImg = panel.GetComponent<Image>();
            pImg.color = new Color(0.06f, 0.09f, 0.18f, 0.98f);

            // Border Glow
            GameObject border = new GameObject("BorderGlow", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(panel.transform, false);
            RectTransform brt = border.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.sizeDelta = new Vector2(6, 6);
            border.GetComponent<Image>().color = new Color(0.7f, 0.2f, 1f, 0.6f);

            // Title
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(panel.transform, false);
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0, 205);
            trt.sizeDelta = new Vector2(560, 60);
            TextMeshProUGUI tt = titleObj.GetComponent<TextMeshProUGUI>();
            tt.text = "CONTINUE RUN?";
            tt.fontSize = 34;
            tt.fontStyle = FontStyles.Bold;
            tt.alignment = TextAlignmentOptions.Center;
            tt.color = new Color(1f, 0.85f, 0.2f);

            // Countdown
            GameObject cdObj = new GameObject("CountdownText", typeof(RectTransform), typeof(TextMeshProUGUI));
            cdObj.transform.SetParent(panel.transform, false);
            RectTransform cdrt = cdObj.GetComponent<RectTransform>();
            cdrt.anchoredPosition = new Vector2(0, 115);
            cdrt.sizeDelta = new Vector2(300, 90);
            TextMeshProUGUI cdt = cdObj.GetComponent<TextMeshProUGUI>();
            cdt.text = "9";
            cdt.fontSize = 68;
            cdt.fontStyle = FontStyles.Bold;
            cdt.alignment = TextAlignmentOptions.Center;
            cdt.color = new Color(0.1f, 0.95f, 1f);

            // Gem Cost
            GameObject costObj = new GameObject("GemCostText", typeof(RectTransform), typeof(TextMeshProUGUI));
            costObj.transform.SetParent(panel.transform, false);
            RectTransform costrt = costObj.GetComponent<RectTransform>();
            costrt.anchoredPosition = new Vector2(0, 25);
            costrt.sizeDelta = new Vector2(500, 45);
            TextMeshProUGUI costt = costObj.GetComponent<TextMeshProUGUI>();
            costt.text = "💎 5 Gems to Revive";
            costt.fontSize = 24;
            costt.fontStyle = FontStyles.Bold;
            costt.alignment = TextAlignmentOptions.Center;
            costt.color = new Color(0.85f, 0.45f, 1f);

            // Wallet
            GameObject walObj = new GameObject("PlayerWalletText", typeof(RectTransform), typeof(TextMeshProUGUI));
            walObj.transform.SetParent(panel.transform, false);
            RectTransform walrt = walObj.GetComponent<RectTransform>();
            walrt.anchoredPosition = new Vector2(0, -25);
            walrt.sizeDelta = new Vector2(500, 35);
            TextMeshProUGUI walt = walObj.GetComponent<TextMeshProUGUI>();
            walt.text = "You have: 25 Gems";
            walt.fontSize = 19;
            walt.alignment = TextAlignmentOptions.Center;
            walt.color = new Color(0.7f, 0.85f, 1f);

            // Revive Button
            GameObject btnRev = new GameObject("ReviveButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnRev.transform.SetParent(panel.transform, false);
            RectTransform rbrt = btnRev.GetComponent<RectTransform>();
            rbrt.anchoredPosition = new Vector2(0, -110);
            rbrt.sizeDelta = new Vector2(340, 65);
            btnRev.GetComponent<Image>().color = new Color(0.1f, 0.85f, 0.35f);

            GameObject rbtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            rbtObj.transform.SetParent(btnRev.transform, false);
            RectTransform rbtrt = rbtObj.GetComponent<RectTransform>();
            rbtrt.anchorMin = Vector2.zero;
            rbtrt.anchorMax = Vector2.one;
            rbtrt.sizeDelta = Vector2.zero;
            TextMeshProUGUI rbtt = rbtObj.GetComponent<TextMeshProUGUI>();
            rbtt.text = "REVIVE NOW";
            rbtt.fontSize = 22;
            rbtt.fontStyle = FontStyles.Bold;
            rbtt.alignment = TextAlignmentOptions.Center;
            rbtt.color = Color.white;

            // Give Up Button
            GameObject btnGive = new GameObject("GiveUpButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGive.transform.SetParent(panel.transform, false);
            RectTransform gbrt = btnGive.GetComponent<RectTransform>();
            gbrt.anchoredPosition = new Vector2(0, -195);
            gbrt.sizeDelta = new Vector2(240, 48);
            btnGive.GetComponent<Image>().color = new Color(0.45f, 0.15f, 0.2f, 0.9f);

            GameObject gbtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            gbtObj.transform.SetParent(btnGive.transform, false);
            RectTransform gbtrt = gbtObj.GetComponent<RectTransform>();
            gbtrt.anchorMin = Vector2.zero;
            gbtrt.anchorMax = Vector2.one;
            gbtrt.sizeDelta = Vector2.zero;
            TextMeshProUGUI gbtt = gbtObj.GetComponent<TextMeshProUGUI>();
            gbtt.text = "GIVE UP";
            gbtt.fontSize = 18;
            gbtt.fontStyle = FontStyles.Bold;
            gbtt.alignment = TextAlignmentOptions.Center;
            gbtt.color = new Color(0.9f, 0.9f, 0.9f);

            root.SetActive(false);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_REVIVE_PATH);
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static GameObject BuildOrGetForexPatternsPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_FOREX_PATH);
            if (existing != null) return existing;

            GameObject root = new GameObject("ForexPatternsPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(ForexPatternsPanel));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            ForexPatternsPanel panelComp = root.GetComponent<ForexPatternsPanel>();
            panelComp.panelRoot = root;

            // Fullscreen Dimmer Background
            GameObject bgObj = new GameObject("DimmerOverlay", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(root.transform, false);
            RectTransform bgrt = bgObj.GetComponent<RectTransform>();
            bgrt.anchorMin = Vector2.zero;
            bgrt.anchorMax = Vector2.one;
            bgrt.sizeDelta = Vector2.zero;
            bgObj.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.07f, 0.98f);

            // Header Banner
            GameObject bannerObj = new GameObject("HeaderBanner", typeof(RectTransform), typeof(Image));
            bannerObj.transform.SetParent(root.transform, false);
            RectTransform bnrt = bannerObj.GetComponent<RectTransform>();
            bnrt.anchorMin = new Vector2(0.04f, 0.90f);
            bnrt.anchorMax = new Vector2(0.96f, 0.98f);
            bnrt.offsetMin = Vector2.zero;
            bnrt.offsetMax = Vector2.zero;
            Image bannerImg = bannerObj.GetComponent<Image>();
            bannerImg.color = new Color(0.08f, 0.12f, 0.22f, 1f);
            panelComp.headerBannerImage = bannerImg;

            // Header Title
            GameObject titleObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(bannerObj.transform, false);
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            TextMeshProUGUI tt = titleObj.GetComponent<TextMeshProUGUI>();
            tt.text = "30 CANDLESTICK PATTERNS (WITH MOVEMENT ANIMATIONS)";
            tt.fontSize = 24;
            tt.fontStyle = FontStyles.Bold;
            tt.alignment = TextAlignmentOptions.Center;
            tt.color = new Color(0f, 0.95f, 1f);

            // Close Button
            GameObject cbObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            cbObj.transform.SetParent(bannerObj.transform, false);
            RectTransform cbrt = cbObj.GetComponent<RectTransform>();
            cbrt.anchorMin = new Vector2(0.92f, 0.15f);
            cbrt.anchorMax = new Vector2(0.99f, 0.85f);
            cbrt.sizeDelta = Vector2.zero;
            cbObj.GetComponent<Image>().color = new Color(1f, 0.25f, 0.35f);
            panelComp.closeButton = cbObj.GetComponent<Button>();

            GameObject cbTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            cbTxt.transform.SetParent(cbObj.transform, false);
            TextMeshProUGUI cbt = cbTxt.GetComponent<TextMeshProUGUI>();
            cbt.text = "✕";
            cbt.fontSize = 22;
            cbt.fontStyle = FontStyles.Bold;
            cbt.alignment = TextAlignmentOptions.Center;
            cbt.color = Color.white;

            // Scroll View Container
            GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollObj.transform.SetParent(root.transform, false);
            RectTransform srt = scrollObj.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.04f, 0.04f);
            srt.anchorMax = new Vector2(0.96f, 0.88f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;
            scrollObj.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.1f, 0.85f);
            ScrollRect sr = scrollObj.GetComponent<ScrollRect>();

            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewportObj.transform.SetParent(scrollObj.transform, false);
            RectTransform vprt = viewportObj.GetComponent<RectTransform>();
            vprt.anchorMin = Vector2.zero;
            vprt.anchorMax = Vector2.one;
            vprt.sizeDelta = Vector2.zero;
            sr.viewport = vprt;

            GameObject contentObj = new GameObject("GridContent", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform crt = contentObj.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.sizeDelta = new Vector2(0, 1400);

            GridLayoutGroup grid = contentObj.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(175, 220);
            grid.spacing = new Vector2(18, 18);
            grid.padding = new RectOffset(16, 16, 16, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = crt;
            panelComp.cardsGridContainer = contentObj.transform;

            // Bake all 30 Cards into Hierarchy
            var allPatterns = ForexPatternDatabase.GetAllPatterns();
            foreach (var info in allPatterns)
            {
                CreateCardInHierarchy(contentObj.transform, info);
            }

            // Create Detail Modal in Hierarchy
            CreateDetailModalInHierarchy(root.transform, panelComp);

            root.SetActive(false);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_FOREX_PATH);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateCardInHierarchy(Transform parent, ForexPatternInfo info)
        {
            GameObject cardObj = new GameObject($"Card_{info.id:02d}_{info.patternName}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObj.transform.SetParent(parent, false);

            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(175, 220);

            Image bg = cardObj.GetComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.09f, 0.95f);

            // Glowing border
            GameObject borderObj = new GameObject("BorderGlow", typeof(RectTransform), typeof(Image));
            borderObj.transform.SetParent(cardObj.transform, false);
            RectTransform brt = borderObj.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.sizeDelta = new Vector2(4, 4);
            Image borderImg = borderObj.GetComponent<Image>();
            borderImg.color = new Color(info.BiasColor.r, info.BiasColor.g, info.BiasColor.b, 0.4f);

            // Sprite Container
            GameObject sprObj = new GameObject("CardSprite", typeof(RectTransform), typeof(Image));
            sprObj.transform.SetParent(cardObj.transform, false);
            RectTransform srt = sprObj.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(4, 28);
            srt.offsetMax = new Vector2(-4, -4);
            Image sprImg = sprObj.GetComponent<Image>();
            sprImg.sprite = info.GetSprite();
            if (sprImg.sprite == null)
            {
                sprImg.color = new Color(1f, 1f, 1f, 0f);
            }

            // Procedural CandlestickBadge inside CardSprite
            GameObject badgeObj = new GameObject("CandlestickBadge", typeof(RectTransform), typeof(CandlestickBadge));
            badgeObj.transform.SetParent(sprObj.transform, false);
            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0f, 0.45f);
            badgeRt.anchorMax = new Vector2(1f, 1f);
            badgeRt.anchoredPosition = Vector2.zero;
            badgeRt.sizeDelta = Vector2.zero;
            badgeRt.pivot = new Vector2(0.5f, 0.5f);
            CandlestickBadge badge = badgeObj.GetComponent<CandlestickBadge>();
            badge.patternId = info.id;
            badge.Build(info.id);

            // Move Name Label at bottom
            GameObject moveLblObj = new GameObject("MoveLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            moveLblObj.transform.SetParent(cardObj.transform, false);
            RectTransform mlrt = moveLblObj.GetComponent<RectTransform>();
            mlrt.anchorMin = new Vector2(0f, 0f);
            mlrt.anchorMax = new Vector2(1f, 0.16f);
            mlrt.offsetMin = Vector2.zero;
            mlrt.offsetMax = Vector2.zero;
            TextMeshProUGUI mlt = moveLblObj.GetComponent<TextMeshProUGUI>();
            mlt.text = info.moveName;
            mlt.fontSize = 12;
            mlt.fontStyle = FontStyles.Bold;
            mlt.alignment = TextAlignmentOptions.Center;
            mlt.color = info.BiasColor;

            ForexPatternCardUI cardUI = cardObj.AddComponent<ForexPatternCardUI>();
            cardUI.patternInfo = info;
            cardUI.cardImage = sprImg;
            cardUI.borderGlow = borderImg;
            cardUI.moveNameText = mlt;
            cardUI.candlestickBadge = badge;

            ForexPatternMovementAnimator anim = cardObj.AddComponent<ForexPatternMovementAnimator>();
            anim.animationType = info.animationType;
            anim.cardTransform = rt;
            anim.cardImage = sprImg;
            cardUI.animator = anim;
        }

        private static void CreateDetailModalInHierarchy(Transform parent, ForexPatternsPanel panelComp)
        {
            GameObject modal = new GameObject("PatternDetailModal", typeof(RectTransform), typeof(Image));
            modal.transform.SetParent(parent, false);
            RectTransform dmRt = modal.GetComponent<RectTransform>();
            dmRt.anchorMin = new Vector2(0.12f, 0.12f);
            dmRt.anchorMax = new Vector2(0.88f, 0.88f);
            dmRt.sizeDelta = Vector2.zero;
            modal.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.1f, 0.98f);
            panelComp.detailModalRoot = modal;

            // Modal Header
            GameObject titleObj = new GameObject("DetailTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(modal.transform, false);
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0, 260);
            trt.sizeDelta = new Vector2(640, 50);
            TextMeshProUGUI tt = titleObj.GetComponent<TextMeshProUGUI>();
            tt.fontSize = 28;
            tt.fontStyle = FontStyles.Bold;
            tt.alignment = TextAlignmentOptions.Center;
            tt.color = new Color(0f, 0.95f, 1f);
            panelComp.detailTitleText = tt;

            // Move Name
            GameObject moveObj = new GameObject("DetailMoveName", typeof(RectTransform), typeof(TextMeshProUGUI));
            moveObj.transform.SetParent(modal.transform, false);
            RectTransform mrt = moveObj.GetComponent<RectTransform>();
            mrt.anchoredPosition = new Vector2(0, 210);
            mrt.sizeDelta = new Vector2(640, 40);
            TextMeshProUGUI mt = moveObj.GetComponent<TextMeshProUGUI>();
            mt.fontSize = 22;
            mt.fontStyle = FontStyles.Bold;
            mt.alignment = TextAlignmentOptions.Center;
            panelComp.detailMoveNameText = mt;

            // Big Card Image with movement animation
            GameObject cardImgObj = new GameObject("DetailCardImage", typeof(RectTransform), typeof(Image));
            cardImgObj.transform.SetParent(modal.transform, false);
            RectTransform crt = cardImgObj.GetComponent<RectTransform>();
            crt.anchoredPosition = new Vector2(0, 70);
            crt.sizeDelta = new Vector2(180, 220);
            Image cImg = cardImgObj.GetComponent<Image>();
            cImg.color = new Color(1f, 1f, 1f, 0f);
            panelComp.detailCardImage = cImg;
            panelComp.detailAnimator = cardImgObj.AddComponent<ForexPatternMovementAnimator>();

            // Modal CandlestickBadge
            GameObject modalBadgeObj = new GameObject("CandlestickBadge", typeof(RectTransform), typeof(CandlestickBadge));
            modalBadgeObj.transform.SetParent(cardImgObj.transform, false);
            RectTransform mbrt = modalBadgeObj.GetComponent<RectTransform>();
            mbrt.anchorMin = new Vector2(0f, 0.45f);
            mbrt.anchorMax = new Vector2(1f, 1f);
            mbrt.anchoredPosition = Vector2.zero;
            mbrt.sizeDelta = Vector2.zero;
            mbrt.pivot = new Vector2(0.5f, 0.5f);
            CandlestickBadge modalBadge = modalBadgeObj.GetComponent<CandlestickBadge>();
            modalBadge.patternId = 1;
            modalBadge.Build(1);

            // Signal Badge
            GameObject sigObj = new GameObject("DetailSignalBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
            sigObj.transform.SetParent(modal.transform, false);
            RectTransform srt = sigObj.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(0, -60);
            srt.sizeDelta = new Vector2(400, 35);
            TextMeshProUGUI st = sigObj.GetComponent<TextMeshProUGUI>();
            st.fontSize = 20;
            st.fontStyle = FontStyles.Bold;
            st.alignment = TextAlignmentOptions.Center;
            panelComp.detailSignalBadge = st;

            // Move Description
            GameObject mDescObj = new GameObject("DetailMoveDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
            mDescObj.transform.SetParent(modal.transform, false);
            RectTransform mdrt = mDescObj.GetComponent<RectTransform>();
            mdrt.anchoredPosition = new Vector2(0, -110);
            mdrt.sizeDelta = new Vector2(620, 50);
            TextMeshProUGUI mdt = mDescObj.GetComponent<TextMeshProUGUI>();
            mdt.fontSize = 17;
            mdt.alignment = TextAlignmentOptions.Center;
            mdt.color = new Color(0.7f, 0.85f, 1f, 0.9f);
            panelComp.detailMoveDescText = mdt;

            // Trading Rule Text
            GameObject ruleObj = new GameObject("DetailTradingRule", typeof(RectTransform), typeof(TextMeshProUGUI));
            ruleObj.transform.SetParent(modal.transform, false);
            RectTransform rrt = ruleObj.GetComponent<RectTransform>();
            rrt.anchoredPosition = new Vector2(0, -185);
            rrt.sizeDelta = new Vector2(620, 90);
            TextMeshProUGUI rt = ruleObj.GetComponent<TextMeshProUGUI>();
            rt.fontSize = 16;
            rt.alignment = TextAlignmentOptions.Center;
            rt.color = Color.white;
            panelComp.detailTradingRuleText = rt;

            // Close Modal Button
            GameObject dcbObj = new GameObject("DetailCloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            dcbObj.transform.SetParent(modal.transform, false);
            RectTransform dcbrt = dcbObj.GetComponent<RectTransform>();
            dcbrt.anchoredPosition = new Vector2(0, -270);
            dcbrt.sizeDelta = new Vector2(170, 48);
            dcbObj.GetComponent<Image>().color = new Color(0.2f, 0.4f, 0.7f);
            panelComp.detailCloseButton = dcbObj.GetComponent<Button>();

            GameObject dcbTxtObj = new GameObject("CloseTxt", typeof(RectTransform), typeof(TextMeshProUGUI));
            dcbTxtObj.transform.SetParent(dcbObj.transform, false);
            TextMeshProUGUI dcbTxt = dcbTxtObj.GetComponent<TextMeshProUGUI>();
            dcbTxt.text = "BACK";
            dcbTxt.fontSize = 18;
            dcbTxt.fontStyle = FontStyles.Bold;
            dcbTxt.alignment = TextAlignmentOptions.Center;
            dcbTxt.color = Color.white;

            modal.SetActive(false);
        }

        #endregion

        #region Scene Baking

        public static void BakeCastleLevel(GameObject revivePrefab, GameObject forexPrefab)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Castle level.unity", OpenSceneMode.Single);
            Debug.Log("[HierarchyUIBuilder] Baking Castle level.unity...");

            GameObject gameplayRoot = GameObject.Find("-- CASTLE GAMEPLAY --");
            if (gameplayRoot == null)
            {
                gameplayRoot = new GameObject("-- CASTLE GAMEPLAY --");
            }

            // 1. CastleScreenFadeCanvas
            GameObject fadeCanvas = GameObject.Find("CastleScreenFadeCanvas");
            if (fadeCanvas == null)
            {
                fadeCanvas = new GameObject("CastleScreenFadeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
                fadeCanvas.transform.SetParent(gameplayRoot.transform, false);
                Canvas c = fadeCanvas.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 999;
                CanvasScaler cs = fadeCanvas.GetComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1080, 1920);

                GameObject black = new GameObject("BlackOverlay", typeof(RectTransform), typeof(Image));
                black.transform.SetParent(fadeCanvas.transform, false);
                RectTransform brt = black.GetComponent<RectTransform>();
                brt.anchorMin = Vector2.zero;
                brt.anchorMax = Vector2.one;
                brt.sizeDelta = Vector2.zero;
                black.GetComponent<Image>().color = Color.black;
            }
            CanvasGroup fcg = fadeCanvas.GetComponent<CanvasGroup>();
            fcg.alpha = 0f;
            fcg.blocksRaycasts = false;
            fadeCanvas.SetActive(false);

            // 2. GoldenDoors
            GameObject doors = GameObject.Find("GoldenDoors");
            if (doors == null)
            {
                doors = new GameObject("GoldenDoors");
                doors.transform.SetParent(gameplayRoot.transform, false);
                doors.transform.position = new Vector3(0f, 0f, 63.5f);

                GameObject dl = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dl.name = "Door_L";
                dl.transform.SetParent(doors.transform, false);
                dl.transform.localPosition = new Vector3(-1.8f, 2.5f, 0f);
                dl.transform.localScale = new Vector3(3.4f, 5f, 0.4f);

                GameObject dr = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dr.name = "Door_R";
                dr.transform.SetParent(doors.transform, false);
                dr.transform.localPosition = new Vector3(1.8f, 2.5f, 0f);
                dr.transform.localScale = new Vector3(3.4f, 5f, 0.4f);
            }

            // 3. LotteryBox
            GameObject lbox = GameObject.Find("LotteryBox");
            if (lbox == null)
            {
                lbox = new GameObject("LotteryBox");
                lbox.transform.SetParent(gameplayRoot.transform, false);
                lbox.transform.position = new Vector3(0f, 0.6f, 85.5f);

                GameObject chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chest.name = "ChestBase";
                chest.transform.SetParent(lbox.transform, false);
                chest.transform.localScale = new Vector3(2.2f, 1.2f, 1.4f);

                GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lid.name = "ChestLid";
                lid.transform.SetParent(lbox.transform, false);
                lid.transform.localPosition = new Vector3(0f, 0.7f, 0f);
                lid.transform.localScale = new Vector3(2.3f, 0.35f, 1.45f);

                GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                portal.name = "LotteryPortal";
                portal.transform.SetParent(lbox.transform, false);
                portal.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                portal.transform.localScale = new Vector3(1.8f, 0.05f, 1.8f);
                portal.SetActive(false);
            }

            // 4. LotteryBoxCameraView & ComputerLaneStartPoint
            GameObject lcam = GameObject.Find("LotteryBoxCameraView");
            if (lcam == null)
            {
                lcam = new GameObject("LotteryBoxCameraView");
                lcam.transform.SetParent(gameplayRoot.transform, false);
                lcam.transform.position = new Vector3(0f, 3.2f, 80f);
                lcam.transform.rotation = Quaternion.Euler(22f, 0f, 0f);
            }

            GameObject startPt = GameObject.Find("ComputerLaneStartPoint");
            if (startPt == null)
            {
                startPt = new GameObject("ComputerLaneStartPoint");
                startPt.transform.SetParent(gameplayRoot.transform, false);
                startPt.transform.position = new Vector3(0f, 0f, 72f);
            }

            // 5. In CastleGameplayHUD / MainChallengePanel -> OpenCodexButton & ActivePatternCardContainer
            GameObject hud = GameObject.Find("CastleGameplayHUD");
            if (hud != null)
            {
                Transform mainPanel = hud.transform.Find("MainChallengePanel");
                if (mainPanel != null)
                {
                    Transform codexBtn = mainPanel.Find("OpenCodexButton");
                    if (codexBtn == null)
                    {
                        GameObject btnObj = new GameObject("OpenCodexButton", typeof(RectTransform), typeof(Image), typeof(Button));
                        btnObj.transform.SetParent(mainPanel, false);
                        RectTransform brt = btnObj.GetComponent<RectTransform>();
                        brt.anchoredPosition = new Vector2(280, 605);
                        brt.sizeDelta = new Vector2(220, 36);
                        btnObj.GetComponent<Image>().color = new Color(0.1f, 0.45f, 0.9f, 0.9f);

                        GameObject btnTxt = new GameObject("CodexText", typeof(RectTransform), typeof(TextMeshProUGUI));
                        btnTxt.transform.SetParent(btnObj.transform, false);
                        RectTransform btrt = btnTxt.GetComponent<RectTransform>();
                        btrt.anchorMin = Vector2.zero;
                        btrt.anchorMax = Vector2.one;
                        btrt.sizeDelta = Vector2.zero;
                        TextMeshProUGUI bt = btnTxt.GetComponent<TextMeshProUGUI>();
                        bt.text = "📖 30 PATTERNS";
                        bt.fontSize = 17;
                        bt.fontStyle = FontStyles.Bold;
                        bt.alignment = TextAlignmentOptions.Center;
                        bt.color = Color.white;
                    }

                    Transform cardCont = mainPanel.Find("ActivePatternCardContainer");
                    if (cardCont == null)
                    {
                        GameObject contObj = new GameObject("ActivePatternCardContainer", typeof(RectTransform), typeof(Image));
                        contObj.transform.SetParent(mainPanel, false);
                        RectTransform crt = contObj.GetComponent<RectTransform>();
                        crt.anchoredPosition = new Vector2(0, 485);
                        crt.sizeDelta = new Vector2(140, 165);
                        contObj.GetComponent<Image>().color = new Color(0.04f, 0.07f, 0.16f, 0.95f);

                        GameObject ptObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
                        ptObj.transform.SetParent(contObj.transform, false);
                        RectTransform ptrt = ptObj.GetComponent<RectTransform>();
                        ptrt.anchoredPosition = new Vector2(0, 68);
                        ptrt.sizeDelta = new Vector2(130, 24);
                        TextMeshProUGUI ptt = ptObj.GetComponent<TextMeshProUGUI>();
                        ptt.fontSize = 12;
                        ptt.fontStyle = FontStyles.Bold;
                        ptt.alignment = TextAlignmentOptions.Center;
                        ptt.color = new Color(0f, 0.95f, 1f);

                        GameObject cImgObj = new GameObject("CardImage", typeof(RectTransform), typeof(Image));
                        cImgObj.transform.SetParent(contObj.transform, false);
                        RectTransform cirt = cImgObj.GetComponent<RectTransform>();
                        cirt.anchoredPosition = new Vector2(0, 5);
                        cirt.sizeDelta = new Vector2(110, 110);
                        cImgObj.AddComponent<ForexPatternMovementAnimator>();

                        GameObject mvObj = new GameObject("MoveText", typeof(RectTransform), typeof(TextMeshProUGUI));
                        mvObj.transform.SetParent(contObj.transform, false);
                        RectTransform mvrt = mvObj.GetComponent<RectTransform>();
                        mvrt.anchoredPosition = new Vector2(0, -66);
                        mvrt.sizeDelta = new Vector2(130, 20);
                        TextMeshProUGUI mvt = mvObj.GetComponent<TextMeshProUGUI>();
                        mvt.fontSize = 11;
                        mvt.fontStyle = FontStyles.Bold;
                        mvt.alignment = TextAlignmentOptions.Center;
                    }
                }
            }

            // 6. CastleChallengeCanvas -> ForexPatternsPanel, GameOverUI, GemReviveDialog
            GameObject challengeCanvas = GameObject.Find("CastleChallengeCanvas");
            if (challengeCanvas != null)
            {
                // Instantiate ForexPatternsPanel if missing
                if (challengeCanvas.transform.Find("ForexPatternsPanel") == null && forexPrefab != null)
                {
                    GameObject fxObj = (GameObject)PrefabUtility.InstantiatePrefab(forexPrefab, challengeCanvas.transform);
                    fxObj.name = "ForexPatternsPanel";
                    fxObj.SetActive(false);
                }

                // Instantiate GemReviveDialog if missing
                if (challengeCanvas.transform.Find("GemReviveDialog") == null && revivePrefab != null)
                {
                    GameObject revObj = (GameObject)PrefabUtility.InstantiatePrefab(revivePrefab, challengeCanvas.transform);
                    revObj.name = "GemReviveDialog";
                    revObj.SetActive(false);
                }

                // Instantiate GameOverUI if missing
                GameObject gameOverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_GAMEOVER_PATH);
                if (challengeCanvas.transform.Find("GameOverUI") == null && gameOverPrefab != null)
                {
                    GameObject goObj = (GameObject)PrefabUtility.InstantiatePrefab(gameOverPrefab, challengeCanvas.transform);
                    goObj.name = "GameOverUI";
                    goObj.SetActive(false);
                }
            }

            // Wire Controller References
            CastleSequenceController seq = Object.FindFirstObjectByType<CastleSequenceController>();
            if (seq != null)
            {
                seq.fadeCanvasGroup = fcg;
                seq.goldenDoorsTransform = doors.transform;
                seq.leftDoorWing = doors.transform.Find("Door_L");
                seq.rightDoorWing = doors.transform.Find("Door_R");
                seq.lotteryBoxTransform = lbox.transform;
                seq.lotteryBoxLid = lbox.transform.Find("ChestLid");
                seq.lotteryBoxCameraView = lcam.transform;
                seq.lotteryBoxPortal = lbox.transform.Find("LotteryPortal")?.gameObject;
                seq.computerLaneStartPoint = startPt.transform;
                EditorUtility.SetDirty(seq);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[HierarchyUIBuilder] Castle level.unity successfully updated and saved.");
        }

        public static void BakeMainMenu(GameObject forexPrefab)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            Debug.Log("[HierarchyUIBuilder] Baking MainMenu.unity...");

            GameObject canvas = GameObject.Find("MenuCanvas") ?? GameObject.Find("MainMenuOverlay");
            if (canvas != null && forexPrefab != null)
            {
                Transform existing = canvas.transform.Find("ForexPatternsPanel");
                if (existing == null)
                {
                    GameObject fxObj = (GameObject)PrefabUtility.InstantiatePrefab(forexPrefab, canvas.transform);
                    fxObj.name = "ForexPatternsPanel";
                    fxObj.SetActive(false);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[HierarchyUIBuilder] MainMenu.unity successfully updated and saved.");
        }

        public static void BakeCityScene(GameObject revivePrefab)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/City.unity", OpenSceneMode.Single);
            Debug.Log("[HierarchyUIBuilder] Baking City.unity...");

            GameObject canvas = GameObject.Find("PauseMenuCanvas") ?? GameObject.Find("GameHUDOverlay");
            if (canvas != null && revivePrefab != null)
            {
                Transform existing = canvas.transform.Find("GemReviveDialog");
                if (existing == null)
                {
                    GameObject revObj = (GameObject)PrefabUtility.InstantiatePrefab(revivePrefab, canvas.transform);
                    revObj.name = "GemReviveDialog";
                    revObj.SetActive(false);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[HierarchyUIBuilder] City.unity successfully updated and saved.");
        }

        public static void BakeSpaceScene(GameObject revivePrefab)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Space.unity", OpenSceneMode.Single);
            Debug.Log("[HierarchyUIBuilder] Baking Space.unity...");

            GameObject canvas = GameObject.Find("PauseMenuCanvas") ?? GameObject.Find("GameHUDOverlay");
            if (canvas != null)
            {
                if (revivePrefab != null && canvas.transform.Find("GemReviveDialog") == null)
                {
                    GameObject revObj = (GameObject)PrefabUtility.InstantiatePrefab(revivePrefab, canvas.transform);
                    revObj.name = "GemReviveDialog";
                    revObj.SetActive(false);
                }

                GameObject gameOverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_GAMEOVER_PATH);
                if (gameOverPrefab != null && canvas.transform.Find("GameOverUI") == null)
                {
                    GameObject goObj = (GameObject)PrefabUtility.InstantiatePrefab(gameOverPrefab, canvas.transform);
                    goObj.name = "GameOverUI";
                    goObj.SetActive(false);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[HierarchyUIBuilder] Space.unity successfully updated and saved.");
        }

        public static void BakeLabyrinthScene(GameObject revivePrefab)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Labyrinth.unity", OpenSceneMode.Single);
            Debug.Log("[HierarchyUIBuilder] Baking Labyrinth.unity...");

            GameObject canvas = GameObject.Find("PauseMenuCanvas") ?? GameObject.Find("GameHUDOverlay");
            if (canvas != null && revivePrefab != null)
            {
                Transform existing = canvas.transform.Find("GemReviveDialog");
                if (existing == null)
                {
                    GameObject revObj = (GameObject)PrefabUtility.InstantiatePrefab(revivePrefab, canvas.transform);
                    revObj.name = "GemReviveDialog";
                    revObj.SetActive(false);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[HierarchyUIBuilder] Labyrinth.unity successfully updated and saved.");
        }

        #endregion
    }
}
