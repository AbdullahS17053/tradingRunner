using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalTrigger : MonoBehaviour
{
    [Header("Next Destination")]
    [Tooltip("If negative, automatically uses cyclic order: Main (0) -> Space (2) -> Labyrinth (3) -> Main (0)")]
    public int targetSceneIndex = -1;

    [Header("FX Settings")]
    public GameObject portalFX;

    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        if (other.CompareTag("Player"))
        {
            isTriggered = true;
            TriggerWorldTransition();
        }
    }

    public void TriggerWorldTransition()
    {
        // 1. Calculate portal aperture center and animate the player pulling into it and shrinking
        Vector3 portalCenter = transform.position;
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            portalCenter = col.bounds.center;
        }

        float warpAnimDuration = 0.9f;
        if (LevelLoader.Instance != null && LevelLoader.Instance.transitionTime > 0.1f)
        {
            warpAnimDuration = LevelLoader.Instance.transitionTime * 0.9f;
        }

        if (RunnerController.Instance != null)
        {
            RunnerController.Instance.AnimateWarpIntoPortal(portalCenter, warpAnimDuration);
        }

        // Freeze horizontal world bend changes during the warp animation
        WorldBendController bendCtrl = FindFirstObjectByType<WorldBendController>();
        if (bendCtrl != null)
        {
            bendCtrl.randomizeHorizontal = false;
        }

        if (PowerUpHUD.Instance != null)
        {
            PowerUpHUD.Instance.ShowPortalAnnouncement("WARPING TO NEXT WORLD...");
        }

        // 2. Preserve run stats across world teleportation (Score, Distance, Trades)
        // Reset speed progression to 1.0f base speed so entering a new level never moves at abnormal hyper-speed!
        if (GameManager.Instance != null)
        {
            GameManager.isLoopingRun = true;
            GameManager.carryOverTakeProfit = GameManager.Instance.takeProfit;
            GameManager.carryOverDistance = GameManager.Instance.distanceRun;
            GameManager.carryOverTrades = GameManager.Instance.correctTrades;
            GameManager.carryOverSpeedMultiplier = 1.0f;
            GameManager.Instance.currentSpeedMultiplier = 1.0f;
        }

        // Clean up any active power-up effects (speed boost, slow-mo) before transitioning
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.ResetAllPowerUps();
        }
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        // 3. Play transition SFX
        if (SoundManager.Instance != null && SoundManager.Instance.correctStrategySound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.correctStrategySound);
        }

        // 4. Randomly select next world from non-repeating world bag
        int nextScene = targetSceneIndex;
        if (nextScene < 0)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            nextScene = GetNextDestinationScene(currentIndex, currentSceneName);
        }

        // 5. Smooth load through LevelLoader (fades to black then loads scene)
        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.LoadScene(nextScene);
        }
        else
        {
            SceneManager.LoadScene(nextScene);
        }
    }

    public static GameObject CreatePortalGate(Transform parent, Vector3 localPos)
    {
        GameObject gate = new GameObject("DimensionalPortalGate");
        if (parent != null) gate.transform.SetParent(parent, false);
        gate.transform.localPosition = localPos;

        // Trigger Collider spanning all lanes
        BoxCollider col = gate.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = new Vector3(0f, 2.2f, 0f);
        col.size = new Vector3(8f, 4.4f, 2.5f);

        // Neon Gate Frame Material
        Material archMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        archMat.color = new Color(0.1f, 0.7f, 1f);
        archMat.EnableKeyword("_EMISSION");
        archMat.SetColor("_EmissionColor", new Color(0.2f, 1.2f, 2.5f));

        // Left Pillar - slim and sleek
        GameObject leftPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftPillar.name = "Pillar_Left";
        leftPillar.transform.SetParent(gate.transform, false);
        leftPillar.transform.localPosition = new Vector3(-3.8f, 2.2f, 0f);
        leftPillar.transform.localScale = new Vector3(0.18f, 4.4f, 0.18f);
        Collider lpCol = leftPillar.GetComponent<Collider>();
        if (lpCol != null) Destroy(lpCol);
        Renderer lpRend = leftPillar.GetComponent<Renderer>();
        if (lpRend != null) lpRend.material = archMat;

        // Right Pillar - slim and sleek
        GameObject rightPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightPillar.name = "Pillar_Right";
        rightPillar.transform.SetParent(gate.transform, false);
        rightPillar.transform.localPosition = new Vector3(3.8f, 2.2f, 0f);
        rightPillar.transform.localScale = new Vector3(0.18f, 4.4f, 0.18f);
        Collider rpCol = rightPillar.GetComponent<Collider>();
        if (rpCol != null) Destroy(rpCol);
        Renderer rpRend = rightPillar.GetComponent<Renderer>();
        if (rpRend != null) rpRend.material = archMat;

        // Top Header - slim matching sidebar thickness
        GameObject topBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topBar.name = "Arch_Top";
        topBar.transform.SetParent(gate.transform, false);
        topBar.transform.localPosition = new Vector3(0f, 4.4f, 0f);
        topBar.transform.localScale = new Vector3(7.78f, 0.18f, 0.18f);
        Collider tbCol = topBar.GetComponent<Collider>();
        if (tbCol != null) Destroy(tbCol);
        Renderer tbRend = topBar.GetComponent<Renderer>();
        if (tbRend != null) tbRend.material = archMat;

        // Glowing Portal Particle Core directly grounded on road
        GameObject portalFXPrefab = Resources.Load<GameObject>("PowerUps/FX_Portal_Round_01");
        if (portalFXPrefab == null)
        {
            portalFXPrefab = Resources.Load<GameObject>("FX_Portal_Round_01");
        }
        if (portalFXPrefab != null)
        {
            GameObject fxObj = Instantiate(portalFXPrefab, gate.transform);
            fxObj.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            fxObj.transform.localRotation = Quaternion.identity;
            fxObj.transform.localScale = Vector3.one * 1.0f;
        }

        // Add soft portal point light
        Light lt = gate.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.range = 10f;
        lt.intensity = 3.5f;
        lt.color = new Color(0.2f, 0.85f, 1f);

        // Holographic Warp Banner
        GameObject banner = new GameObject("WarpBanner");
        banner.transform.SetParent(gate.transform, false);
        banner.transform.localPosition = new Vector3(0f, 4.9f, 0f);
        TMPro.TextMeshPro bannerTmp = banner.AddComponent<TMPro.TextMeshPro>();
        bannerTmp.text = "NEXT DIMENSION >>";
        bannerTmp.fontSize = 3.8f;
        bannerTmp.fontStyle = TMPro.FontStyles.Bold;
        bannerTmp.alignment = TMPro.TextAlignmentOptions.Center;
        bannerTmp.color = new Color(0.2f, 1f, 1f);

        gate.AddComponent<PortalTrigger>();

        return gate;
    }

    public static void TriggerPortalDirectly()
    {
        var pt = FindFirstObjectByType<PortalTrigger>();
        if (pt != null)
        {
            pt.TriggerWorldTransition();
        }
        else
        {
            GameObject temp = new GameObject("TempPortal");
            var comp = temp.AddComponent<PortalTrigger>();
            comp.TriggerWorldTransition();
        }
    }

    // Non-repeating world playlist / bag across all 4 gameplay levels:
    // 0 = City, 2 = Space, 3 = Labyrinth, 4 = Castle
    public static readonly int[] AllGameplayScenes = new int[] { 0, 2, 3, 4 };
    private static System.Collections.Generic.List<int> unvisitedScenes = new System.Collections.Generic.List<int>();
    private static int lastVisitedSceneIndex = -1;

    public static void ResetLevelBag()
    {
        unvisitedScenes.Clear();
        lastVisitedSceneIndex = -1;
    }

    public static int GetNextDestinationScene(int currentSceneIndex, string currentSceneName)
    {
        int resolvedCurrentIndex = currentSceneIndex;
        if (resolvedCurrentIndex < 0 || resolvedCurrentIndex == 1) // 1 is MainMenu
        {
            if (currentSceneName.Equals("City", System.StringComparison.OrdinalIgnoreCase) || currentSceneName.Equals("Main", System.StringComparison.OrdinalIgnoreCase)) resolvedCurrentIndex = 0;
            else if (currentSceneName.IndexOf("space", System.StringComparison.OrdinalIgnoreCase) >= 0) resolvedCurrentIndex = 2;
            else if (currentSceneName.IndexOf("labyrinth", System.StringComparison.OrdinalIgnoreCase) >= 0) resolvedCurrentIndex = 3;
            else if (currentSceneName.IndexOf("castle", System.StringComparison.OrdinalIgnoreCase) >= 0) resolvedCurrentIndex = 4;
            else resolvedCurrentIndex = 0;
        }

        // Remove the current scene from unvisited pool so we never repeat the current world
        unvisitedScenes.RemoveAll(idx => idx == resolvedCurrentIndex);

        // If pool is empty or only had current scene, refill with all gameplay scenes excluding current scene
        if (unvisitedScenes.Count == 0)
        {
            System.Collections.Generic.List<int> refill = new System.Collections.Generic.List<int>(AllGameplayScenes);
            refill.RemoveAll(idx => idx == resolvedCurrentIndex);

            // If we have more than 1 option and lastVisitedSceneIndex is in the refill list,
            // place it at the very back of the list to prevent 2-level ping-pong (e.g. Castle <-> Labyrinth)
            if (lastVisitedSceneIndex >= 0 && refill.Contains(lastVisitedSceneIndex) && refill.Count > 1)
            {
                refill.Remove(lastVisitedSceneIndex);
                ShuffleList(refill);
                refill.Add(lastVisitedSceneIndex); // Append at the end so all other worlds are visited first!
                unvisitedScenes = refill;
            }
            else
            {
                ShuffleList(refill);
                unvisitedScenes = refill;
            }
        }

        int next = unvisitedScenes[0];
        unvisitedScenes.RemoveAt(0);
        lastVisitedSceneIndex = resolvedCurrentIndex;
        Debug.Log($"[PortalTrigger] World transition chosen: {resolvedCurrentIndex} -> {next}. Remaining in bag: {string.Join(",", unvisitedScenes)}");
        return next;
    }

    private static void ShuffleList(System.Collections.Generic.List<int> list)
    {
        var rng = new System.Random((int)(System.DateTime.UtcNow.Ticks & 0x7FFFFFFF));
        for (int i = list.Count - 1; i > 0; i--)
        {
            int r = rng.Next(i + 1);
            int tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
    }
}
