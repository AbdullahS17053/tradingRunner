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

        [Header("Golden Doors & Walk Transition")]
        public Transform goldenDoorsTransform;
        public Transform leftDoorWing;
        public Transform rightDoorWing;
        public float doorZPosition = 63.5f;
        public float walkTriggerZPosition = 61.5f; // 1-2m before door
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

        private RunnerController runner;
        private Camera activeCamera;
        private Vector3 runnerCamOriginalLocalPos;
        private Quaternion runnerCamOriginalLocalRot;
        private Transform cameraFollowTarget;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitInCastleScene()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name.IndexOf("Castle", System.StringComparison.OrdinalIgnoreCase) >= 0 || activeScene.buildIndex == 4)
            {
                if (Instance == null)
                {
                    GameObject go = new GameObject("CastleSequenceController", typeof(CastleSequenceController));
                    Debug.Log("[CastleSequenceController] Auto-initialized in Castle level scene.");
                }
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureSingleFomoBot();
        }

        private void Start()
        {
            EnsureSingleFomoBot();
            EnsureSceneElements();

            runner = RunnerController.Instance;
            if (runner == null)
            {
                runner = FindFirstObjectByType<RunnerController>();
            }

            activeCamera = Camera.main;
            if (activeCamera != null)
            {
                runnerCamOriginalLocalPos = activeCamera.transform.localPosition;
                runnerCamOriginalLocalRot = activeCamera.transform.localRotation;
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

            // Check if player has reached 1-2 meters before the golden doors
            if (runner.transform.position.z >= walkTriggerZPosition)
            {
                hasTransitionedToWalk = true;
                StartCoroutine(WalkToDoorAndTransitionRoutine());
            }
        }

        /// <summary>
        /// Strictly enforces only ONE FOMO bot (Castle Guard Bot at Z=54.64) in Castle level.
        /// </summary>
        public void EnsureSingleFomoBot()
        {
            // Disable rear chaser bot if present
            if (rearChaserBot != null)
            {
                rearChaserBot.SetActive(false);
            }

            GameObject rearObj = GameObject.Find("FomoBot_RearChaser");
            if (rearObj != null)
            {
                rearObj.SetActive(false);
            }

            // Search all bots to ensure only the front patrol bot is active
            FomoBot[] allBots = Object.FindObjectsByType<FomoBot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var bot in allBots)
            {
                if (bot == null) continue;
                string bName = bot.gameObject.name.ToLower();
                string vName = bot.botVariantName != null ? bot.botVariantName.ToLower() : "";

                if (bName.Contains("rear") || vName.Contains("rear") || bot.behaviorType == FomoBot.BotBehaviorType.RearChaser)
                {
                    bot.gameObject.SetActive(false);
                    Debug.Log("[CastleSequenceController] Deactivated rear FOMO bot: " + bot.gameObject.name);
                }
                else if (bName.Contains("front") || vName.Contains("castle guard") || bot.behaviorType == FomoBot.BotBehaviorType.FrontPatrolCharge)
                {
                    bot.gameObject.SetActive(true);
                    frontGuardBot = bot.gameObject;
                }
            }
        }

        /// <summary>
        /// Automatically finds or dynamically builds any missing scene elements for the Castle flow.
        /// </summary>
        private void EnsureSceneElements()
        {
            // 1. Screen Fade Overlay
            if (fadeCanvasGroup == null)
            {
                GameObject existingFade = GameObject.Find("CastleScreenFadeCanvas");
                if (existingFade != null)
                {
                    fadeCanvasGroup = existingFade.GetComponentInChildren<CanvasGroup>();
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
                }
            }

            // 2. Golden Doors
            if (goldenDoorsTransform == null)
            {
                GameObject gdObj = GameObject.Find("GoldenDoors");
                if (gdObj != null)
                {
                    goldenDoorsTransform = gdObj.transform;
                }
                else
                {
                    goldenDoorsTransform = CreateGoldenDoors(new Vector3(0f, 0f, doorZPosition));
                }
            }

            // Cache door wings
            if (goldenDoorsTransform != null && (leftDoorWing == null || rightDoorWing == null))
            {
                foreach (Transform child in goldenDoorsTransform.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name.Contains("Door_01") || child.name.Contains("Door_Left") || child.name.Contains("DoorLeft"))
                    {
                        leftDoorWing = child;
                    }
                    else if (child.name.Contains("Door_02") || child.name.Contains("Door_Right") || child.name.Contains("DoorRight"))
                    {
                        rightDoorWing = child;
                    }
                }
            }

            // 3. Computer Lane & Chair
            if (chairSeatTransform == null)
            {
                GameObject chairObj = GameObject.Find("SM_Prop_Chair_Fancy_01");
                if (chairObj != null) chairSeatTransform = chairObj.transform;
            }

            if (computerCameraPosition == null)
            {
                GameObject camPosObj = GameObject.Find("ComputerCameraView");
                if (camPosObj != null) computerCameraPosition = camPosObj.transform;
            }

            if (computerLaneStartPoint == null)
            {
                GameObject laneStartObj = GameObject.Find("ComputerLaneStartPoint");
                if (laneStartObj == null)
                {
                    laneStartObj = new GameObject("ComputerLaneStartPoint");
                    laneStartObj.transform.position = new Vector3(0f, 0.15f, 66f);
                }
                computerLaneStartPoint = laneStartObj.transform;
            }

            // 4. Lottery Box & Portal
            if (lotteryBoxTransform == null)
            {
                GameObject boxObj = GameObject.Find("LotteryBox");
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
                GameObject viewObj = GameObject.Find("LotteryBoxCameraView");
                if (viewObj == null)
                {
                    viewObj = new GameObject("LotteryBoxCameraView");
                    viewObj.transform.position = new Vector3(0.4f, 1.25f, 72.8f);
                    viewObj.transform.rotation = Quaternion.Euler(12f, 25f, 0f);
                }
                lotteryBoxCameraView = viewObj.transform;
            }

            if (lotteryBoxPortal == null)
            {
                GameObject portalObj = GameObject.Find("LotteryBoxPortal");
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

        private Transform CreateGoldenDoors(Vector3 worldPos)
        {
            GameObject doorRoot = new GameObject("GoldenDoors");
            doorRoot.transform.position = worldPos;

            // Load Gold Walls material
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

            // Door Left Wing (Pivots at -1.9f)
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

            // Door Right Wing (Pivots at +1.9f)
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

        private Transform CreateLotteryBox(Vector3 worldPos)
        {
            GameObject boxRoot = new GameObject("LotteryBox");
            boxRoot.transform.position = worldPos;

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

            // Chest Lid (Rotates from rear edge: Z = -0.4f, Y = 0.7f)
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
                portalObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                portalObj.transform.localScale = new Vector3(2.5f, 0.05f, 2.5f);
                Renderer r = portalObj.GetComponent<Renderer>();
                r.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
                r.sharedMaterial.color = new Color(0f, 0.95f, 1f, 0.9f);
            }

            portalObj.name = "LotteryBoxPortal";
            Vector3 spawnPos = boxParent != null ? boxParent.position + new Vector3(0f, 1.2f, 0f) : new Vector3(1.6f, 1.2f, 74.5f);
            portalObj.transform.position = spawnPos;
            portalObj.transform.rotation = Quaternion.identity;

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
        /// Phase 1 & 2: Runner switches to walk 1-2m before golden doors, walks to doors, and fades out.
        /// </summary>
        private IEnumerator WalkToDoorAndTransitionRoutine()
        {
            isSequenceRunning = true;

            // 1. Switch to walk state
            if (runner != null)
            {
                runner.enabled = false; // Disable swipe & jump controls

                if (runner.animator != null)
                {
                    runner.animator.CrossFadeInFixedTime("Walk_BlendTree", 0.25f);
                }
            }

            // 2. Smoothly walk forward to the Golden Doors (doorZPosition = 63.5f)
            float startZ = runner != null ? runner.transform.position.z : walkTriggerZPosition;
            float targetZ = doorZPosition;
            float distance = Mathf.Max(0.1f, targetZ - startZ);
            float duration = distance / Mathf.Max(1f, walkSpeed);
            float elapsed = 0f;

            Vector3 walkStartPos = runner != null ? runner.transform.position : new Vector3(0f, 0.15f, startZ);
            Vector3 walkEndPos = new Vector3(0f, walkStartPos.y, targetZ);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (runner != null)
                {
                    runner.transform.position = Vector3.Lerp(walkStartPos, walkEndPos, t);
                    runner.transform.rotation = Quaternion.identity;
                }
                yield return null;
            }

            // 3. Reached Golden Doors -> Fade screen in (to black)
            yield return StartCoroutine(FadeScreenRoutine(0f, 1f, fadeDuration));

            // 4. In black screen: Swing open golden doors
            if (leftDoorWing != null) leftDoorWing.localRotation = Quaternion.Euler(0f, -85f, 0f);
            if (rightDoorWing != null) rightDoorWing.localRotation = Quaternion.Euler(0f, 85f, 0f);

            // Position player at entrance of computer lane
            Vector3 laneStart = computerLaneStartPoint != null ? computerLaneStartPoint.position : new Vector3(0f, 0.15f, 66f);
            if (runner != null)
            {
                runner.transform.position = laneStart;
                runner.transform.rotation = Quaternion.identity;
                if (runner.animator != null)
                {
                    runner.animator.Play("Walk_BlendTree", 0, 0f);
                }
            }

            // Reset camera to behind player
            if (activeCamera != null && runner != null)
            {
                activeCamera.transform.position = runner.transform.position + new Vector3(0f, 2.5f, -4f);
                activeCamera.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
            }

            yield return new WaitForSeconds(0.2f);

            // 5. Fade screen back out (revealing the computer lane)
            yield return StartCoroutine(FadeScreenRoutine(1f, 0f, fadeDuration));

            // 6. Player sees himself in the computer lane and continues walking towards the desk
            yield return StartCoroutine(WalkToComputerDeskRoutine());
        }

        /// <summary>
        /// Phase 3: Walking down computer lane to chair, sits down, swoops camera to monitor, launches quiz.
        /// </summary>
        private IEnumerator WalkToComputerDeskRoutine()
        {
            Vector3 deskWalkStart = runner != null ? runner.transform.position : new Vector3(0f, 0.15f, 66f);
            Vector3 deskChairPos = chairSeatTransform != null ? chairSeatTransform.position : new Vector3(0f, 0.15f, 74.8f);

            // Stop right in front of the chair
            Vector3 walkTarget = new Vector3(0f, deskWalkStart.y, deskChairPos.z - 0.7f);
            float dist = Mathf.Max(0.1f, walkTarget.z - deskWalkStart.z);
            float walkTime = dist / Mathf.Max(1f, walkSpeed);
            float elapsed = 0f;

            if (runner != null && runner.animator != null)
            {
                runner.animator.CrossFadeInFixedTime("Walk_BlendTree", 0.15f);
            }

            // Walk smoothly down the lane
            while (elapsed < walkTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / walkTime);
                if (runner != null)
                {
                    runner.transform.position = Vector3.Lerp(deskWalkStart, walkTarget, t);
                    runner.transform.rotation = Quaternion.identity;
                }
                yield return null;
            }

            // Align into chair seat
            float alignElapsed = 0f;
            Vector3 alignStart = runner != null ? runner.transform.position : walkTarget;
            Quaternion alignStartRot = runner != null ? runner.transform.rotation : Quaternion.identity;
            Quaternion targetChairRot = chairSeatTransform != null ? chairSeatTransform.rotation : Quaternion.identity;

            while (alignElapsed < 0.45f)
            {
                alignElapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, alignElapsed / 0.45f);
                if (runner != null)
                {
                    runner.transform.position = Vector3.Lerp(alignStart, deskChairPos, t);
                    runner.transform.rotation = Quaternion.Slerp(alignStartRot, targetChairRot, t);
                }
                yield return null;
            }

            // Play Sitting Pose
            if (runner != null && runner.animator != null)
            {
                runner.animator.Play("SittingPose", 0, 0f);
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
            // 1. Stop computer typing audio
            if (computerAudioSource != null && computerAudioSource.isPlaying)
            {
                computerAudioSource.Stop();
            }

            // 2. Hide quiz UI completely
            if (CastleChallengeController.Instance != null && CastleChallengeController.Instance.challengeCanvas != null)
            {
                CastleChallengeController.Instance.challengeCanvas.SetActive(false);
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
                Vector3 portalBasePos = lotteryBoxTransform != null ? lotteryBoxTransform.position + new Vector3(0f, 0.4f, 0f) : new Vector3(1.6f, 0.4f, 74.5f);
                Vector3 portalTargetPos = portalBasePos + new Vector3(0f, 0.9f, 0f);

                lotteryBoxPortal.transform.position = portalBasePos;
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

            // 6. Player stands up from the chair
            if (runner != null && runner.animator != null)
            {
                runner.animator.ResetTrigger("SitToStand");
                runner.animator.SetTrigger("SitToStand");
            }

            // Swoop camera to over-the-shoulder view framing player and portal
            if (activeCamera != null && runner != null)
            {
                Vector3 otsCamPos = runner.transform.position + new Vector3(-1.2f, 2.2f, -2.8f);
                Quaternion otsCamRot = Quaternion.Euler(15f, 25f, 0f);

                Vector3 curCamPos = activeCamera.transform.position;
                Quaternion curCamRot = activeCamera.transform.rotation;
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

                Vector3 startPos = runner.transform.position;
                Vector3 portalPos = lotteryBoxPortal != null ? lotteryBoxPortal.transform.position : (lotteryBoxTransform != null ? lotteryBoxTransform.position : startPos);
                Vector3 stepTarget = new Vector3(portalPos.x, startPos.y, portalPos.z);

                float stepElapsed = 0f;
                float stepDuration = 1.0f;
                while (stepElapsed < stepDuration)
                {
                    stepElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(stepElapsed / stepDuration);
                    runner.transform.position = Vector3.Lerp(startPos, stepTarget, t);
                    runner.transform.rotation = Quaternion.Slerp(runner.transform.rotation, Quaternion.LookRotation(portalPos - startPos), t);
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

            fadeCanvasGroup.blocksRaycasts = (toAlpha > 0.5f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
                yield return null;
            }
            fadeCanvasGroup.alpha = toAlpha;
        }
    }
}
