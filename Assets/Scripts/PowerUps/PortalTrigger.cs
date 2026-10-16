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
            if (PowerUpHUD.Instance != null)
            {
                PowerUpHUD.Instance.ShowPortalAnnouncement("WARPING TO NEXT WORLD...");
            }
            TriggerWorldTransition();
        }
    }

    public void TriggerWorldTransition()
    {
        if (PowerUpHUD.Instance != null)
        {
            PowerUpHUD.Instance.ShowPortalAnnouncement("WARPING TO NEXT WORLD...");
        }
        // 1. Preserve run stats across world teleportation
        if (GameManager.Instance != null)
        {
            GameManager.isLoopingRun = true;
            GameManager.carryOverTakeProfit = GameManager.Instance.takeProfit;
            GameManager.carryOverDistance = GameManager.Instance.distanceRun;
            GameManager.carryOverTrades = GameManager.Instance.correctTrades;
            GameManager.carryOverSpeedMultiplier = GameManager.Instance.currentSpeedMultiplier;
        }

        // 2. Play transition SFX
        if (SoundManager.Instance != null && SoundManager.Instance.correctStrategySound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.correctStrategySound);
        }

        // 3. Determine next scene in cyclic loop
        int nextScene = targetSceneIndex;
        if (nextScene < 0)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            int currentIndex = SceneManager.GetActiveScene().buildIndex;

            if (currentSceneName.Equals("Main", System.StringComparison.OrdinalIgnoreCase) || currentIndex == 0)
            {
                nextScene = 2; // space world
            }
            else if (currentSceneName.IndexOf("space", System.StringComparison.OrdinalIgnoreCase) >= 0 || currentIndex == 2)
            {
                nextScene = 3; // Labyrinth World
            }
            else if (currentSceneName.IndexOf("Labyrinth", System.StringComparison.OrdinalIgnoreCase) >= 0 || currentIndex == 3)
            {
                nextScene = 0; // Main Cyberpunk City
            }
            else
            {
                nextScene = 0;
            }
        }

        // 4. Smooth load through LevelLoader
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
}
