using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RunnerForex.Castle
{
    public class CastleSequenceController : MonoBehaviour
    {
        public static CastleSequenceController Instance { get; private set; }

        [Header("Single FOMO Bot Enforcement")]
        [Tooltip("The front guard bot that telegraphs the straight laser beam")]
        public GameObject frontGuardBot;
        [Tooltip("The rear chaser bot which should be disabled in Castle")]
        public GameObject rearChaserBot;

        [Header("User Walk & Door Sequence Trigger")]
        [Tooltip("Reference to the user-created trigger collider")]
        public Collider userWalkSequenceCollider;

        [Header("Golden Doors & Walk Transition")]
        public Transform goldenDoorsTransform;
        public Transform leftDoorWing;
        public Transform rightDoorWing;
        public float doorZPosition = 63.5f;
        public float walkTriggerZPosition = 61.5f; // Fallback distance if trigger collider is not reached
        public float walkSpeed = 2.6f;

        [Header("Computer Room Lane")]
        public Transform computerLaneStartPoint;
        public Transform chairSeatTransform;
        public Transform computerCameraPosition;
        public AudioSource computerAudioSource;
        public AudioClip typingSound;

        [Header("Lottery Box & Portal")]
        public Transform lotteryBoxTransform;
        public Transform lotteryBoxLid;
        public Transform lotteryBoxCameraView;
        public GameObject lotteryBoxPortal;
        public ParticleSystem lotteryBoxParticles;
        public AudioClip chestOpenSound;

        [Header("Screen Fade Overlay")]
        public CanvasGroup fadeCanvasGroup;
        public float fadeDuration = 0.8f;

        [Header("Runtime State")]
        public bool hasTransitionedToWalk = false;
        public bool isSequenceRunning = false;
        private bool computerTriggerReached = false;

        private Quaternion leftDoorInitialRot = Quaternion.identity;
        private Quaternion rightDoorInitialRot = Quaternion.identity;
        private Quaternion leftDoorOpenRot = Quaternion.Euler(0f, -85f, 0f);
        private Quaternion rightDoorOpenRot = Quaternion.Euler(0f, 85f, 0f);

        private RunnerController runner;
        private Camera activeCamera;
        private Transform gameplayRoot;

        public static CastleSequenceController EnsureInstanceExists()
        {
            if (Instance != null) return Instance;

            CastleSequenceController existing = Object.FindFirstObjectByType<CastleSequenceController>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            GameObject go = new GameObject("CastleSequenceController");
            Instance = go.AddComponent<CastleSequenceController>();
            Debug.Log("[CastleSequenceController] Instance created and registered.");
            return Instance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadHandler()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedStatic;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedStatic;
        }

        private static void OnSceneLoadedStatic(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (scene.name.IndexOf("Castle", System.StringComparison.OrdinalIgnoreCase) >= 0 || scene.buildIndex == 4)
            {
                EnsureInstanceExists();
            }
        }

        private void Awake()
        {
            if (Instance == null || Instance == this)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            hasTransitionedToWalk = false;
            isSequenceRunning = false;
            computerTriggerReached = false;

            FindGameplayRoot();
            EnsureSingleFomoBot();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            FindGameplayRoot();
            EnsureSingleFomoBot();
            EnsureSceneElements();

            runner = RunnerController.Instance;
            if (runner == null)
            {
                runner = FindFirstObjectByType<RunnerController>();
            }

            activeCamera = Camera.main;
        }

        private void FindGameplayRoot()
        {
            if (gameplayRoot == null)
            {
                GameObject rootObj = GameObject.Find("-- CASTLE GAMEPLAY --");
                if (rootObj != null)
                {
                    gameplayRoot = rootObj.transform;
                }
            }
        }

        private void Update()
        {
            if (hasTransitionedToWalk || isSequenceRunning) return;

            if (runner == null)
            {
                runner = RunnerController.Instance ?? FindFirstObjectByType<RunnerController>();
                if (runner == null) return;
            }

            // 1. Check user-created trigger collider intersection
            if (userWalkSequenceCollider != null)
            {
                CharacterController cc = runner.GetComponent<CharacterController>();
                if (cc != null && userWalkSequenceCollider.bounds.Intersects(cc.bounds))
                {
                    Debug.Log("[CastleSequenceController] Detected intersection with user walk sequence collider!");
                    StartWalkAndDoorSequenceFromTrigger();
                    return;
                }
            }

            // 2. Fallback distance check if runner reaches walk trigger point
            float currentZ = runner.transform.parent != null ? runner.transform.localPosition.z : runner.transform.position.z;
            if (currentZ >= walkTriggerZPosition)
            {
                StartWalkAndDoorSequenceFromTrigger();
            }
        }

        /// <summary>
        /// Public entry point called by CastleWalkSequenceTrigger via OnTriggerStay or OnTriggerEnter.
        /// </summary>
        public void OnPlayerInWalkTriggerStay()
        {
            StartWalkAndDoorSequence();
        }

        public void OnPlayerInWalkTriggerBox()
        {
            StartWalkAndDoorSequence();
        }

        public void StartWalkAndDoorSequenceFromTrigger()
        {
            StartWalkAndDoorSequence();
        }

        public void NotifyComputerTriggerReached()
        {
            computerTriggerReached = true;
        }

        public void StartWalkAndDoorSequence()
        {
            if (hasTransitionedToWalk || isSequenceRunning) return;
            hasTransitionedToWalk = true;
            isSequenceRunning = true;
            Debug.Log("[CastleSequenceController] Walk sequence triggered! Walking towards golden doors.");

            if (runner == null)
            {
                runner = RunnerController.Instance ?? FindFirstObjectByType<RunnerController>();
            }

            if (runner != null)
            {
                runner.enabled = false;
                if (runner.animator != null)
                {
                    runner.animator.Play("Walk", 0, 0f);
                    runner.animator.CrossFadeInFixedTime("Walk", 0.15f);
                }
            }

            StartCoroutine(WalkToDoorAndTransitionRoutine());
        }

        /// <summary>
        /// Strictly enforces only ONE FOMO bot (Castle Guard Bot at Z=54.64) in Castle level.
        /// </summary>
        public void EnsureSingleFomoBot()
        {
            if (rearChaserBot != null)
            {
                rearChaserBot.SetActive(false);
            }

            GameObject rearObj = GameObject.Find("FomoBot_RearChaser");
            if (rearObj != null)
            {
                rearObj.SetActive(false);
            }

            FomoBot[] allBots = Object.FindObjectsByType<FomoBot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var bot in allBots)
            {
                if (bot == null) continue;
                string bName = bot.gameObject.name.ToLower();
                string vName = bot.botVariantName != null ? bot.botVariantName.ToLower() : "";

                if (bName.Contains("rear") || vName.Contains("rear") || bot.behaviorType == FomoBot.BotBehaviorType.RearChaser)
                {
                    bot.gameObject.SetActive(false);
                }
                else
                {
                    bot.gameObject.SetActive(true);
                    bot.telegraphDistance = 22.0f;
                    bot.beamReach = 45.0f;
                    bot.AlignToPlayerLane();
                    frontGuardBot = bot.gameObject;

                    // Clean up any duplicate base FOMOController components on the front guard bot
                    FOMOController[] fcs = bot.GetComponents<FOMOController>();
                    foreach (var fc in fcs)
                    {
                        if (fc.GetType() == typeof(FOMOController))
                        {
                            Destroy(fc);
                        }
                    }
                }
            }
        }

        private void DisableDoorColliders()
        {
            if (goldenDoorsTransform != null)
            {
                foreach (Collider c in goldenDoorsTransform.GetComponentsInChildren<Collider>(true))
                {
                    if (c == userWalkSequenceCollider) continue;
                    string n = c.gameObject.name.ToLower();
                    if (n.Contains("door") || n.Contains("portcullis") || n.Contains("gate") || n.Contains("wing"))
                    {
                        c.enabled = false;
                        Debug.Log("[CastleSequenceController] Disabled door obstacle collider: " + c.gameObject.name);
                    }
                }
            }
        }

        private IEnumerator AnimateDoorsOpenRoutine(float duration)
        {
            DisableDoorColliders();

            if (SoundManager.Instance != null && SoundManager.Instance.collectTPSound != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.collectTPSound);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                if (leftDoorWing != null) leftDoorWing.localRotation = Quaternion.Slerp(leftDoorInitialRot, leftDoorOpenRot, t);
                if (rightDoorWing != null) rightDoorWing.localRotation = Quaternion.Slerp(rightDoorInitialRot, rightDoorOpenRot, t);
                yield return null;
            }
            if (leftDoorWing != null) leftDoorWing.localRotation = leftDoorOpenRot;
            if (rightDoorWing != null) rightDoorWing.localRotation = rightDoorOpenRot;
        }

        /// <summary>
        /// Automatically finds and wires the user trigger collider, golden doors, lottery box, and UI elements.
        /// </summary>
        private void EnsureSceneElements()
        {
            FindGameplayRoot();

            // 0. Discover & Wire User Walk & Door Trigger Collider
            if (userWalkSequenceCollider == null)
            {
                GameObject userObj = GameObject.Find("Trigger ");
                if (userObj == null) userObj = GameObject.Find("Trigger");
                if (userObj == null) userObj = GameObject.Find("Trigger fow walk and get near so door openes and sequence continues for castel lavel ");
                if (userObj == null)
                {
                    foreach (var col in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        if (col.gameObject.name.StartsWith("Trigger") || col.gameObject.name.Contains("Trigger fow walk") || col.gameObject.name.Contains("door openes") || col.gameObject.name.Contains("sequence continues"))
                        {
                            userObj = col.gameObject;
                            userWalkSequenceCollider = col;
                            break;
                        }
                    }
                }
                else
                {
                    userWalkSequenceCollider = userObj.GetComponent<Collider>();
                }

                if (userObj != null)
                {
                    CastleWalkSequenceTrigger trig = userObj.GetComponent<CastleWalkSequenceTrigger>();
                    if (trig == null) trig = userObj.AddComponent<CastleWalkSequenceTrigger>();

                    BoxCollider bc = userObj.GetComponent<BoxCollider>();
                    if (bc != null) bc.isTrigger = true;

                    Rigidbody rb = userObj.GetComponent<Rigidbody>();
                    if (rb == null) rb = userObj.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity = false;

                    Debug.Log("[CastleSequenceController] Successfully wired user trigger: " + userObj.name);
                }
            }

            // 1. Screen Fade Overlay (kept static in hierarchy, disabled when clear)
            if (fadeCanvasGroup == null)
            {
                GameObject existingFade = FindObjectInScene("CastleScreenFadeCanvas") ?? FindObjectInScene("CastleScreenFadeOverlay");
                if (existingFade == null)
                {
                    GameObject hud = FindObjectInScene("CastleGameplayHUD");
                    if (hud != null)
                    {
                        Transform found = hud.transform.Find("CastleScreenFadeOverlay");
                        if (found != null) existingFade = found.gameObject;
                    }
                }

                if (existingFade != null)
                {
                    fadeCanvasGroup = existingFade.GetComponentInChildren<CanvasGroup>(true);
                    if (fadeCanvasGroup != null)
                    {
                        fadeCanvasGroup.alpha = 0f;
                        fadeCanvasGroup.blocksRaycasts = false;
                        fadeCanvasGroup.gameObject.SetActive(false);
                    }
                }
                else
                {
                    GameObject fadeCanvasObj = new GameObject("CastleScreenFadeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
                    Canvas c = fadeCanvasObj.GetComponent<Canvas>();
                    c.renderMode = RenderMode.ScreenSpaceOverlay;
                    c.sortingOrder = 999;

                    CanvasScaler cs = fadeCanvasObj.GetComponent<CanvasScaler>();
                    cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    cs.referenceResolution = new Vector2(1080, 1920);

                    fadeCanvasGroup = fadeCanvasObj.GetComponent<CanvasGroup>();
                    fadeCanvasGroup.alpha = 0f;
                    fadeCanvasGroup.blocksRaycasts = false;

                    GameObject blackImgObj = new GameObject("BlackOverlay", typeof(RectTransform), typeof(Image));
                    blackImgObj.transform.SetParent(fadeCanvasObj.transform, false);
                    RectTransform rt = blackImgObj.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                    Image img = blackImgObj.GetComponent<Image>();
                    img.color = Color.black;

                    fadeCanvasObj.SetActive(false);
                }
            }

            // 2. Real Castle Gate & Golden Doors
            if (goldenDoorsTransform == null)
            {
                GameObject gateObj = FindObjectInScene("GoldenDoors") ?? FindObjectInScene("SM_Bld_Castle_Wall_Gate_01");
                if (gateObj != null)
                {
                    goldenDoorsTransform = gateObj.transform;
                }
                else
                {
                    goldenDoorsTransform = CreateGoldenDoors(new Vector3(0f, 0f, doorZPosition));
                }
            }

            if (goldenDoorsTransform != null)
            {
                foreach (Transform child in goldenDoorsTransform.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name.Contains("Door_L") || child.name.Contains("DoorLeft") || child.name.Contains("Door_01"))
                    {
                        leftDoorWing = child;
                        leftDoorInitialRot = leftDoorWing.localRotation;
                        leftDoorOpenRot = leftDoorInitialRot * Quaternion.Euler(0f, -90f, 0f);
                    }
                    else if (child.name.Contains("Door_R") || child.name.Contains("DoorRight") || child.name.Contains("Door_02"))
                    {
                        rightDoorWing = child;
                        rightDoorInitialRot = rightDoorWing.localRotation;
                        rightDoorOpenRot = rightDoorInitialRot * Quaternion.Euler(0f, 90f, 0f);
                    }
                }
            }

            // 3. Computer Room Lane & Chair
            if (chairSeatTransform == null)
            {
                GameObject chairObj = FindObjectInScene("SM_Prop_Chair_Fancy_01");
                if (chairObj != null) chairSeatTransform = chairObj.transform;
            }

            if (computerCameraPosition == null)
            {
                GameObject camPosObj = FindObjectInScene("ComputerCameraView");
                if (camPosObj != null) computerCameraPosition = camPosObj.transform;
            }

            if (computerLaneStartPoint == null)
            {
                GameObject laneStartObj = FindObjectInScene("ComputerLaneStartPoint");
                if (laneStartObj == null)
                {
                    laneStartObj = new GameObject("ComputerLaneStartPoint");
                    if (gameplayRoot != null) laneStartObj.transform.SetParent(gameplayRoot, false);
                    laneStartObj.transform.localPosition = new Vector3(0f, 0.15f, 66f);
                }
                computerLaneStartPoint = laneStartObj.transform;
            }

            // 4. Lottery Box & Portal (at local X = 1.6, Y = 0, Z = 74.5)
            if (lotteryBoxTransform == null)
            {
                GameObject boxObj = FindObjectInScene("LotteryBox");
                if (boxObj != null)
                {
                    lotteryBoxTransform = boxObj.transform;
                }
                else
                {
                    lotteryBoxTransform = CreateLotteryBox(new Vector3(1.6f, 0f, 74.5f));
                }
            }

            if (lotteryBoxTransform != null && lotteryBoxLid == null)
            {
                foreach (Transform child in lotteryBoxTransform.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name.Contains("Lid") || child.name.Contains("Top"))
                    {
                        lotteryBoxLid = child;
                        break;
                    }
                }
            }

            if (lotteryBoxCameraView == null)
            {
                GameObject viewObj = FindObjectInScene("LotteryBoxCameraView");
                if (viewObj == null)
                {
                    viewObj = new GameObject("LotteryBoxCameraView");
                    if (gameplayRoot != null) viewObj.transform.SetParent(gameplayRoot, false);
                    viewObj.transform.localPosition = new Vector3(0.4f, 1.25f, 72.8f);
                    viewObj.transform.localRotation = Quaternion.Euler(12f, 25f, 0f);
                }
                lotteryBoxCameraView = viewObj.transform;
            }

            if (lotteryBoxPortal == null)
            {
                GameObject portalObj = FindObjectInScene("LotteryBoxPortal");
                if (portalObj != null)
                {
                    lotteryBoxPortal = portalObj;
                }
                else
                {
                    lotteryBoxPortal = CreateLotteryBoxPortal(lotteryBoxTransform);
                }
            }

            if (lotteryBoxPortal != null)
            {
                lotteryBoxPortal.SetActive(false);
            }
        }

        private GameObject FindObjectInScene(string objName)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.isLoaded)
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    var trs = root.GetComponentsInChildren<Transform>(true);
                    foreach (var t in trs)
                    {
                        if (t.name == objName) return t.gameObject;
                    }
                }
            }
            return GameObject.Find(objName);
        }

        private Transform CreateGoldenDoors(Vector3 localPos)
        {
            GameObject doorRoot = new GameObject("GoldenDoors");
            if (gameplayRoot != null) doorRoot.transform.SetParent(gameplayRoot, false);
            doorRoot.transform.localPosition = localPos;
            doorRoot.transform.localRotation = Quaternion.identity;

            Material goldMat = Resources.Load<Material>("Gold Walls");
#if UNITY_EDITOR
            if (goldMat == null)
            {
                goldMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/PolygonFantasyKingdom/Materials/Gold Walls.mat");
            }
#endif

            // Arch Frame Left
            GameObject archL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            archL.name = "ArchPillarLeft";
            archL.transform.SetParent(doorRoot.transform, false);
            archL.transform.localPosition = new Vector3(-2.2f, 2.5f, 0f);
            archL.transform.localScale = new Vector3(0.6f, 5.0f, 0.6f);
            Destroy(archL.GetComponent<Collider>());
            if (goldMat != null) archL.GetComponent<Renderer>().sharedMaterial = goldMat;

            // Arch Frame Right
            GameObject archR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            archR.name = "ArchPillarRight";
            archR.transform.SetParent(doorRoot.transform, false);
            archR.transform.localPosition = new Vector3(2.2f, 2.5f, 0f);
            archR.transform.localScale = new Vector3(0.6f, 5.0f, 0.6f);
            Destroy(archR.GetComponent<Collider>());
            if (goldMat != null) archR.GetComponent<Renderer>().sharedMaterial = goldMat;

            // Arch Top Header
            GameObject archTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            archTop.name = "ArchHeader";
            archTop.transform.SetParent(doorRoot.transform, false);
            archTop.transform.localPosition = new Vector3(0f, 5.0f, 0f);
            archTop.transform.localScale = new Vector3(5.0f, 0.6f, 0.6f);
            Destroy(archTop.GetComponent<Collider>());
            if (goldMat != null) archTop.GetComponent<Renderer>().sharedMaterial = goldMat;

            // Door Left Wing
            GameObject doorLeftPivot = new GameObject("Door_Left_Pivot");
            doorLeftPivot.transform.SetParent(doorRoot.transform, false);
            doorLeftPivot.transform.localPosition = new Vector3(-1.9f, 0f, 0f);

            GameObject doorLMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorLMesh.name = "Door_Left";
            doorLMesh.transform.SetParent(doorLeftPivot.transform, false);
            doorLMesh.transform.localPosition = new Vector3(0.95f, 2.2f, 0f);
            doorLMesh.transform.localScale = new Vector3(1.9f, 4.4f, 0.15f);
            Destroy(doorLMesh.GetComponent<Collider>());
            if (goldMat != null) doorLMesh.GetComponent<Renderer>().sharedMaterial = goldMat;
            leftDoorWing = doorLeftPivot.transform;

            // Door Right Wing
            GameObject doorRightPivot = new GameObject("Door_Right_Pivot");
            doorRightPivot.transform.SetParent(doorRoot.transform, false);
            doorRightPivot.transform.localPosition = new Vector3(1.9f, 0f, 0f);

            GameObject doorRMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorRMesh.name = "Door_Right";
            doorRMesh.transform.SetParent(doorRightPivot.transform, false);
            doorRMesh.transform.localPosition = new Vector3(-0.95f, 2.2f, 0f);
            doorRMesh.transform.localScale = new Vector3(1.9f, 4.4f, 0.15f);
            Destroy(doorRMesh.GetComponent<Collider>());
            if (goldMat != null) doorRMesh.GetComponent<Renderer>().sharedMaterial = goldMat;
            rightDoorWing = doorRightPivot.transform;

            return doorRoot.transform;
        }

        private Transform CreateLotteryBox(Vector3 localPos)
        {
            GameObject boxRoot = new GameObject("LotteryBox");
            if (gameplayRoot != null) boxRoot.transform.SetParent(gameplayRoot, false);
            boxRoot.transform.localPosition = localPos;
            boxRoot.transform.localRotation = Quaternion.identity;

            Material goldMat = Resources.Load<Material>("Gold Walls");
#if UNITY_EDITOR
            if (goldMat == null)
            {
                goldMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/PolygonFantasyKingdom/Materials/Gold Walls.mat");
            }
#endif

            // Chest Base
            GameObject chestBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chestBase.name = "LotteryBox_Base";
            chestBase.transform.SetParent(boxRoot.transform, false);
            chestBase.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            chestBase.transform.localScale = new Vector3(1.1f, 0.7f, 0.8f);
            Destroy(chestBase.GetComponent<Collider>());
            if (goldMat != null) chestBase.GetComponent<Renderer>().sharedMaterial = goldMat;

            // Chest Lid (Rotates from rear edge)
            GameObject lidPivot = new GameObject("LotteryBox_Lid");
            lidPivot.transform.SetParent(boxRoot.transform, false);
            lidPivot.transform.localPosition = new Vector3(0f, 0.7f, -0.4f);

            GameObject lidMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lidMesh.name = "LidMesh";
            lidMesh.transform.SetParent(lidPivot.transform, false);
            lidMesh.transform.localPosition = new Vector3(0f, 0.12f, 0.4f);
            lidMesh.transform.localScale = new Vector3(1.15f, 0.25f, 0.82f);
            Destroy(lidMesh.GetComponent<Collider>());
            if (goldMat != null) lidMesh.GetComponent<Renderer>().sharedMaterial = goldMat;

            lotteryBoxLid = lidPivot.transform;

            return boxRoot.transform;
        }

        private GameObject CreateLotteryBoxPortal(Transform boxParent)
        {
            GameObject portalObj = null;

#if UNITY_EDITOR
            GameObject portalPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonParticleFX/Prefabs/FX_Portal_Round_01.prefab");
            if (portalPrefab != null)
            {
                portalObj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(portalPrefab);
            }
#endif
            if (portalObj == null)
            {
                portalObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                portalObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                portalObj.transform.localScale = new Vector3(2.5f, 0.05f, 2.5f);
                Renderer r = portalObj.GetComponent<Renderer>();
                r.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
                r.sharedMaterial.color = new Color(0f, 0.95f, 1f, 0.9f);
            }

            portalObj.name = "LotteryBoxPortal";
            if (boxParent != null)
            {
                portalObj.transform.SetParent(boxParent, false);
                portalObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            }
            else if (gameplayRoot != null)
            {
                portalObj.transform.SetParent(gameplayRoot, false);
                portalObj.transform.localPosition = new Vector3(1.6f, 1.2f, 74.5f);
            }
            portalObj.transform.localRotation = Quaternion.identity;

            BoxCollider col = portalObj.GetComponent<BoxCollider>();
            if (col == null) col = portalObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(3f, 3f, 3f);

            PortalTrigger pt = portalObj.GetComponent<PortalTrigger>();
            if (pt == null) pt = portalObj.AddComponent<PortalTrigger>();
            pt.targetSceneIndex = -1; // Random world transition

            return portalObj;
        }

        /// <summary>
        /// Phase 1 & 2: Triggered by user's trigger collider (OnTriggerStay / OnTriggerEnter).
        /// Character switches to walk animation, moves towards the door while casting a ray forward from chest.
        /// When within 2m, door colliders disable and doors smoothly open. Screen fades to black,
        /// and player transitions to the computer room lane.
        /// </summary>
        private IEnumerator WalkToDoorAndTransitionRoutine()
        {
            isSequenceRunning = true;

            if (runner == null)
            {
                runner = RunnerController.Instance ?? FindFirstObjectByType<RunnerController>();
            }

            CharacterController cc = (runner != null) ? runner.GetComponent<CharacterController>() : null;

            // 1. Switch to walk state and disable runner swipe/jump input
            if (runner != null)
            {
                runner.enabled = false;
                if (runner.animator != null)
                {
                    runner.animator.Play("Walk", 0, 0f);
                    runner.animator.CrossFadeInFixedTime("Walk", 0.15f);
                }
            }

            Vector3 walkDir = (gameplayRoot != null ? gameplayRoot.forward : (runner != null ? runner.transform.forward : Vector3.forward)).normalized;

            // 2. Walk forward while casting ray from chest forward towards the door
            bool doorOpened = false;
            float maxWalkTimeBeforeDoor = 8.0f;
            float elapsedWalk = 0f;

            while (!doorOpened && elapsedWalk < maxWalkTimeBeforeDoor)
            {
                elapsedWalk += Time.deltaTime;

                if (runner != null && cc != null && cc.enabled)
                {
                    Vector3 moveStep = (walkDir * walkSpeed + Vector3.down * 9.8f) * Time.deltaTime;
                    cc.Move(moveStep);
                    runner.transform.rotation = Quaternion.LookRotation(walkDir);
                }

                // Raycast forward from chest (+1.3m up)
                Vector3 chestOrigin = (runner != null) ? runner.transform.position + Vector3.up * 1.3f : Vector3.zero;
                float detectedDist = float.MaxValue;

                if (Physics.Raycast(chestOrigin, walkDir, out RaycastHit hit, 10f))
                {
                    detectedDist = hit.distance;
                }

                // Geometric fallback distance to gate doors
                if (goldenDoorsTransform != null && runner != null)
                {
                    Vector3 toDoor = goldenDoorsTransform.position - runner.transform.position;
                    float planarDist = Vector3.Dot(toDoor, walkDir);
                    if (planarDist > 0f && planarDist < detectedDist)
                    {
                        detectedDist = planarDist;
                    }
                }

                // When distance is 2m, trigger door opening!
                if (detectedDist <= 2.0f)
                {
                    doorOpened = true;
                    Debug.Log($"[CastleSequenceController] Chest raycast detected door at {detectedDist:F2}m! Opening doors.");
                }

                yield return null;
            }

            // 3. Doors swing open smoothly (+/- 85 deg) and door colliders disable
            StartCoroutine(AnimateDoorsOpenRoutine(0.7f));

            // Continue walking through the open doorway as screen fades in to black
            float walkThroughTime = 0.9f;
            float walkThroughElapsed = 0f;

            // Fade screen in to black (0 -> 1 over fadeDuration)
            StartCoroutine(FadeScreenRoutine(0f, 1f, fadeDuration));

            while (walkThroughElapsed < walkThroughTime)
            {
                walkThroughElapsed += Time.deltaTime;
                if (runner != null && cc != null && cc.enabled)
                {
                    Vector3 moveStep = (walkDir * walkSpeed + Vector3.down * 9.8f) * Time.deltaTime;
                    cc.Move(moveStep);
                }
                yield return null;
            }

            yield return new WaitForSeconds(0.15f);

            // 4. Position player at entrance of computer lane
            if (runner != null)
            {
                if (cc != null) cc.enabled = false;

                if (gameplayRoot != null)
                {
                    runner.transform.SetParent(gameplayRoot, false);
                    runner.transform.localPosition = new Vector3(0f, 0.15f, 66f);
                    runner.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    Vector3 laneStart = computerLaneStartPoint != null ? computerLaneStartPoint.position : new Vector3(0f, 0.15f, 66f);
                    runner.transform.position = laneStart;
                    runner.transform.rotation = Quaternion.identity;
                }

                if (cc != null) cc.enabled = true;

                if (runner.animator != null)
                {
                    runner.animator.Play("Walk", 0, 0f);
                    runner.animator.CrossFadeInFixedTime("Walk", 0.15f);
                }
            }

            // 5. Fade screen back out (1 -> 0 over fadeDuration)
            yield return StartCoroutine(FadeScreenRoutine(1f, 0f, fadeDuration));

            // 6. Player sees himself in the computer lane and continues walking towards the desk
            yield return StartCoroutine(WalkToComputerDeskRoutine());
        }

        /// <summary>
        /// Phase 3: Walking down computer lane to desk. Chest raycast or trigger detects distance to desk/chair.
        /// When near (<= 1.25m or trigger touched), player stops and transitions to Idle right where he is,
        /// without any sudden position jump.
        /// Camera shifts smoothly to computer monitor, typing audio plays, and 5-question challenge UI opens.
        /// </summary>
        private IEnumerator WalkToComputerDeskRoutine()
        {
            computerTriggerReached = false;

            if (runner == null)
            {
                runner = RunnerController.Instance ?? FindFirstObjectByType<RunnerController>();
            }

            CharacterController cc = (runner != null) ? runner.GetComponent<CharacterController>() : null;
            Vector3 deskWalkDir = (gameplayRoot != null ? gameplayRoot.forward : (runner != null ? runner.transform.forward : Vector3.forward)).normalized;

            if (runner != null && runner.animator != null)
            {
                runner.animator.Play("Walk", 0, 0f);
                runner.animator.CrossFadeInFixedTime("Walk", 0.15f);
            }

            bool deskReached = false;
            float maxDeskWalkTime = 10.0f;
            float elapsedDeskWalk = 0f;

            while (!deskReached && elapsedDeskWalk < maxDeskWalkTime)
            {
                elapsedDeskWalk += Time.deltaTime;

                if (runner != null && runner.animator != null)
                {
                    var stateInfo = runner.animator.GetCurrentAnimatorStateInfo(0);
                    if (!stateInfo.IsName("Walk") && !stateInfo.IsName("Walk_BlendTree") && !deskReached)
                    {
                        runner.animator.Play("Walk", 0, 0f);
                    }
                }

                if (runner != null && cc != null && cc.enabled)
                {
                    Vector3 moveStep = (deskWalkDir * walkSpeed + Vector3.down * 9.8f) * Time.deltaTime;
                    cc.Move(moveStep);
                    runner.transform.rotation = Quaternion.LookRotation(deskWalkDir);
                }

                // Chest raycast forward towards desk / chair (ignore triggers so only physical geometry is detected)
                Vector3 chestOrigin = (runner != null) ? runner.transform.position + Vector3.up * 1.3f : Vector3.zero;
                float detectedDist = float.MaxValue;

                if (Physics.Raycast(chestOrigin, deskWalkDir, out RaycastHit hit, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    detectedDist = hit.distance;
                }

                // Geometric fallback distance to chair
                if (chairSeatTransform != null && runner != null)
                {
                    Vector3 toChair = chairSeatTransform.position - runner.transform.position;
                    float planarDist = Vector3.Dot(toChair, deskWalkDir);
                    if (planarDist > 0f && planarDist < detectedDist)
                    {
                        detectedDist = planarDist;
                    }
                }

                // When near table/chair or computer trigger reached, stop smoothly!
                if (detectedDist <= 1.25f || computerTriggerReached)
                {
                    deskReached = true;
                    Debug.Log($"[CastleSequenceController] Desk reached (dist: {detectedDist:F2}m, trigger: {computerTriggerReached})! Stopping at current position in Idle without changing position.");
                }

                yield return null;
            }

            // Keep player right where he walked to in Idle animation (DO NOT change position or snap to chair)
            if (runner != null && runner.animator != null)
            {
                runner.animator.SetBool("GameStarted", false);
                runner.animator.Play("Idle", 0, 0f);
                runner.animator.CrossFadeInFixedTime("Idle", 0.15f);
            }

            // Swoop camera to computer screen
            if (activeCamera != null && computerCameraPosition != null)
            {
                Vector3 camStart = activeCamera.transform.position;
                Quaternion rotStart = activeCamera.transform.rotation;
                float camElapsed = 0f;
                float swoopDuration = 1.2f;

                while (camElapsed < swoopDuration)
                {
                    camElapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, camElapsed / swoopDuration);
                    activeCamera.transform.position = Vector3.Lerp(camStart, computerCameraPosition.position, t);
                    activeCamera.transform.rotation = Quaternion.Slerp(rotStart, computerCameraPosition.rotation, t);
                    yield return null;
                }

                activeCamera.transform.position = computerCameraPosition.position;
                activeCamera.transform.rotation = computerCameraPosition.rotation;
            }

            // Duck background music slightly so typing and quiz audio are clear
            if (SoundManager.Instance != null && SoundManager.Instance.musicSource != null)
            {
                SoundManager.Instance.musicSource.volume = SoundManager.Instance.GetMusicVolume() * 0.3f;
            }

            // Play typing audio loop
            if (computerAudioSource != null)
            {
                if (typingSound != null) computerAudioSource.clip = typingSound;
                else if (SoundManager.Instance != null && SoundManager.Instance.collectTPSound != null) computerAudioSource.clip = SoundManager.Instance.collectTPSound;

                if (computerAudioSource.clip != null)
                {
                    computerAudioSource.loop = true;
                    computerAudioSource.Play();
                }
            }

            // Launch Forex 5-Question Challenge UI
            if (CastleChallengeController.Instance != null)
            {
                CastleChallengeController.Instance.StartCastleChallengeSession();
            }
            else
            {
                Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
                foreach (Transform t in allTransforms)
                {
                    if (t != null && t.gameObject.name == "MainChallengePanel" && t.gameObject.scene.isLoaded)
                    {
                        t.gameObject.SetActive(true);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Phase 4 & 5: When all 5 questions are complete, plays cinematic preview of lottery box opening,
        /// spawning dimensional portal, standing player up, and transitioning to next level.
        /// </summary>
        public void PlayLotteryBoxPortalCinematicSequence()
        {
            StartCoroutine(LotteryBoxPortalCinematicRoutine());
        }

        private IEnumerator LotteryBoxPortalCinematicRoutine()
        {
            // Restore background music
            if (SoundManager.Instance != null && SoundManager.Instance.musicSource != null)
            {
                SoundManager.Instance.musicSource.volume = SoundManager.Instance.GetMusicVolume();
            }

            // 1. Stop computer typing audio
            if (computerAudioSource != null && computerAudioSource.isPlaying)
            {
                computerAudioSource.Stop();
            }

            // 2. Hide quiz UI completely
            if (CastleChallengeController.Instance != null)
            {
                if (CastleChallengeController.Instance.challengeCanvas != null)
                {
                    CastleChallengeController.Instance.challengeCanvas.SetActive(false);
                }
                GameObject panel = CastleChallengeController.Instance.GetMainChallengePanel();
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }
            else
            {
                Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
                foreach (Transform t in allTransforms)
                {
                    if (t != null && t.gameObject.name == "MainChallengePanel" && t.gameObject.scene.isLoaded)
                    {
                        t.gameObject.SetActive(false);
                        break;
                    }
                }
            }

            yield return new WaitForSeconds(0.3f);

            // 3. Camera Swoops to Lottery Box Cinematic Preview Angle
            if (activeCamera != null && lotteryBoxCameraView != null)
            {
                Vector3 camStart = activeCamera.transform.position;
                Quaternion rotStart = activeCamera.transform.rotation;
                float swoopElapsed = 0f;
                float swoopTime = 1.2f;

                while (swoopElapsed < swoopTime)
                {
                    swoopElapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, swoopElapsed / swoopTime);
                    activeCamera.transform.position = Vector3.Lerp(camStart, lotteryBoxCameraView.position, t);
                    activeCamera.transform.rotation = Quaternion.Slerp(rotStart, lotteryBoxCameraView.rotation, t);
                    yield return null;
                }
                activeCamera.transform.position = lotteryBoxCameraView.position;
                activeCamera.transform.rotation = lotteryBoxCameraView.rotation;
            }

            yield return new WaitForSeconds(0.4f);

            // 4. Lottery Box opens smoothly (Lid rotates open)
            if (SoundManager.Instance != null && SoundManager.Instance.collectTPSound != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.collectTPSound);
            }

            if (lotteryBoxLid != null)
            {
                Quaternion lidStart = lotteryBoxLid.localRotation;
                Quaternion lidOpen = Quaternion.Euler(-100f, 0f, 0f);
                float lidElapsed = 0f;
                float openDuration = 1.0f;

                while (lidElapsed < openDuration)
                {
                    lidElapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, lidElapsed / openDuration);
                    lotteryBoxLid.localRotation = Quaternion.Slerp(lidStart, lidOpen, t);
                    yield return null;
                }
                lotteryBoxLid.localRotation = lidOpen;
            }

            // Sparkle particles burst
            if (lotteryBoxParticles != null)
            {
                lotteryBoxParticles.Play();
            }

            // 5. Dimensional Portal rises up from inside the box and expands
            if (lotteryBoxPortal != null)
            {
                lotteryBoxPortal.SetActive(true);
                Vector3 portalBasePos = lotteryBoxPortal.transform.position;
                Vector3 portalTargetPos = portalBasePos + (gameplayRoot != null ? gameplayRoot.up * 0.9f : Vector3.up * 0.9f);

                lotteryBoxPortal.transform.localScale = Vector3.one * 0.05f;

                if (SoundManager.Instance != null && SoundManager.Instance.correctStrategySound != null)
                {
                    SoundManager.Instance.PlaySFX(SoundManager.Instance.correctStrategySound);
                }

                float portalElapsed = 0f;
                float portalRiseTime = 1.0f;
                while (portalElapsed < portalRiseTime)
                {
                    portalElapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, portalElapsed / portalRiseTime);
                    lotteryBoxPortal.transform.position = Vector3.Lerp(portalBasePos, portalTargetPos, t);
                    lotteryBoxPortal.transform.localScale = Vector3.Lerp(Vector3.one * 0.05f, Vector3.one * 0.85f, t);
                    yield return null;
                }
                lotteryBoxPortal.transform.position = portalTargetPos;
                lotteryBoxPortal.transform.localScale = Vector3.one * 0.85f;
            }

            yield return new WaitForSeconds(0.6f);

            // 6. Player in Idle state facing lottery box
            if (runner != null && runner.animator != null)
            {
                runner.animator.Play("Idle", 0, 0f);
            }

            // Swoop camera to over-the-shoulder view framing player and portal
            if (activeCamera != null && runner != null)
            {
                Vector3 curCamPos = activeCamera.transform.position;
                Quaternion curCamRot = activeCamera.transform.rotation;
                Vector3 otsCamPos = runner.transform.position + (gameplayRoot != null ? -gameplayRoot.forward * 2.8f + gameplayRoot.up * 2.2f - gameplayRoot.right * 1.2f : new Vector3(-1.2f, 2.2f, -2.8f));
                Quaternion otsCamRot = runner.transform.rotation * Quaternion.Euler(15f, 25f, 0f);

                float otsElapsed = 0f;
                while (otsElapsed < 0.9f)
                {
                    otsElapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, otsElapsed / 0.9f);
                    activeCamera.transform.position = Vector3.Lerp(curCamPos, otsCamPos, t);
                    activeCamera.transform.rotation = Quaternion.Slerp(curCamRot, otsCamRot, t);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(0.4f);

            // 7. Player walks into the portal right on the lottery box
            if (runner != null)
            {
                if (runner.animator != null)
                {
                    runner.animator.CrossFadeInFixedTime("Walk_BlendTree", 0.2f);
                }

                bool hasParent = runner.transform.parent != null;
                Vector3 startLocal = hasParent ? runner.transform.localPosition : runner.transform.position;
                Vector3 portalTargetLocal = new Vector3(1.6f, startLocal.y, 74.5f);

                float stepElapsed = 0f;
                float stepDuration = 1.0f;
                while (stepElapsed < stepDuration)
                {
                    stepElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(stepElapsed / stepDuration);
                    if (hasParent)
                    {
                        runner.transform.localPosition = Vector3.Lerp(startLocal, portalTargetLocal, t);
                        runner.transform.localRotation = Quaternion.Slerp(runner.transform.localRotation, Quaternion.Euler(0f, 45f, 0f), t);
                    }
                    else
                    {
                        runner.transform.position = Vector3.Lerp(startLocal, portalTargetLocal, t);
                    }
                    yield return null;
                }
            }

            // 8. Trigger portal transition to the next world!
            if (lotteryBoxPortal != null)
            {
                PortalTrigger pt = lotteryBoxPortal.GetComponent<PortalTrigger>();
                if (pt != null)
                {
                    pt.TriggerWorldTransition();
                }
                else
                {
                    FindFirstObjectByType<PortalTrigger>()?.TriggerWorldTransition();
                }
            }
        }

        private IEnumerator FadeScreenRoutine(float fromAlpha, float toAlpha, float duration)
        {
            if (fadeCanvasGroup == null) yield break;

            if (!fadeCanvasGroup.gameObject.activeSelf)
            {
                fadeCanvasGroup.gameObject.SetActive(true);
            }

            fadeCanvasGroup.blocksRaycasts = (toAlpha > 0.5f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
                yield return null;
            }
            fadeCanvasGroup.alpha = toAlpha;

            if (toAlpha <= 0.05f)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
                fadeCanvasGroup.gameObject.SetActive(false);
            }
        }
    }
}
