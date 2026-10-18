using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    public PowerUpType powerUpType;

    [Header("Animation")]
    public float rotationSpeed = 90f;
    public float bobSpeed = 3f;
    public float bobHeight = 0.25f;

    private Vector3 initialLocalPos;
    private bool isCollected = false;

    private void Start()
    {
        initialLocalPos = transform.position;
        ApplyTypeAesthetics();
    }

    private void Update()
    {
        if (isCollected) return;

        // Bobbing & Rotating
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        float newY = initialLocalPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsAnyPowerUpActive)
            {
                // A power-up is already active! Cannot pick up until that power ability is off and the bar goes out.
                return;
            }

            isCollected = true;

            if (PowerUpManager.Instance != null)
            {
                PowerUpManager.Instance.ActivatePowerUp(powerUpType);
            }

            SpawnPickupBurst();
            Destroy(gameObject);
        }
    }

    private void ApplyTypeAesthetics()
    {
        Color color = GetColorForType(powerUpType);

        // Adjust or add light
        Light lt = GetComponentInChildren<Light>();
        if (lt == null)
        {
            lt = gameObject.AddComponent<Light>();
        }
        lt.type = LightType.Point;
        lt.range = 6f;
        lt.intensity = 4f;
        lt.color = color;

        // Tint any renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            if (rend.material != null)
            {
                bool isIconAtlas = rend.material.name.Contains("PolygonIcons");
                if (!isIconAtlas)
                {
                    if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", color);
                    if (rend.material.HasProperty("_Color")) rend.material.SetColor("_Color", color);
                }
                else
                {
                    if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", Color.white);
                    if (rend.material.HasProperty("_Color")) rend.material.SetColor("_Color", Color.white);
                }

                if (rend.material.HasProperty("_EmissionColor"))
                {
                    rend.material.EnableKeyword("_EMISSION");
                    rend.material.SetColor("_EmissionColor", color * 2.0f);
                }
            }
        }

        // Add vibrant orbiting sparkles
        GameObject sparklePrefab = Resources.Load<GameObject>("PowerUps/FX_Sparkle_Orbit_01");
        if (sparklePrefab != null)
        {
            GameObject sp = Instantiate(sparklePrefab, transform);
            sp.transform.localPosition = Vector3.zero;
            sp.transform.localScale = Vector3.one * 0.75f;
        }

        // Add towering beacon beam visible down the entire road track
        GameObject beaconPrefab = Resources.Load<GameObject>("PowerUps/FX_Beacon");
        if (beaconPrefab != null)
        {
            GameObject bc = Instantiate(beaconPrefab, transform);
            bc.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            bc.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);
        }
    }

    private void SpawnPickupBurst()
    {
        GameObject burstPrefab = Resources.Load<GameObject>("PowerUps/FX_Sparkle_Orbit_01");
        if (burstPrefab != null)
        {
            GameObject burst = Instantiate(burstPrefab, transform.position, Quaternion.identity);
            burst.transform.localScale = Vector3.one * 1.5f;
            Destroy(burst, 0.8f);
        }

        GameObject burstLight = new GameObject("PowerUpPickupBurst");
        burstLight.transform.position = transform.position;
        Light lt = burstLight.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.range = 8f;
        lt.intensity = 6f;
        lt.color = GetColorForType(powerUpType);
        Destroy(burstLight, 0.35f);
    }

    public static Color GetColorForType(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Shield: return new Color(0.1f, 0.75f, 1f); // Cyan
            case PowerUpType.SpeedBoost: return new Color(1f, 0.45f, 0.05f); // Orange
            case PowerUpType.CoinMagnet: return new Color(0.1f, 0.95f, 0.5f); // Mint Green
            case PowerUpType.DoubleTP: return new Color(1f, 0.85f, 0.1f); // Gold
            case PowerUpType.WeaponUpgrade: return new Color(1f, 0.15f, 0.25f); // Crimson
            case PowerUpType.SlowMotion: return new Color(0.35f, 0.75f, 1f); // Ice Blue
            case PowerUpType.Invincibility: return new Color(0.95f, 0.25f, 1f); // Magenta
            default: return Color.white;
        }
    }

    public static string GetDisplayName(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Shield: return "SHIELD";
            case PowerUpType.SpeedBoost: return "BOOST 2X";
            case PowerUpType.CoinMagnet: return "MAGNET";
            case PowerUpType.DoubleTP: return "x2 PROFIT";
            case PowerUpType.WeaponUpgrade: return "LASER CANNON";
            case PowerUpType.SlowMotion: return "SLOW MOTION";
            case PowerUpType.Invincibility: return "INVINCIBLE";
            default: return type.ToString();
        }
    }

    public static PowerUpPickup CreatePowerUp(Transform parent, Vector3 localPos, PowerUpType type)
    {
        GameObject root = new GameObject("Pickup_" + type.ToString());
        try
        {
            root.tag = "PowerUp";
        }
        catch
        {
            // Tag might not be loaded yet
        }
        if (parent != null)
        {
            root.transform.SetParent(parent, false);
        }
        root.transform.localPosition = localPos;

        // Trigger Collider
        BoxCollider col = root.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(1.6f, 1.6f, 1.6f);

        Color colVal = GetColorForType(type);

        // Holographic Outer Diamond
        GameObject outer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        outer.name = "HoloDiamond";
        outer.transform.SetParent(root.transform, false);
        outer.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
        outer.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        Collider outerCol = outer.GetComponent<Collider>();
        if (outerCol != null) Destroy(outerCol);

        Renderer outerRend = outer.GetComponent<Renderer>();
        if (outerRend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(colVal.r, colVal.g, colVal.b, 0.85f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", colVal * 2.2f);
            outerRend.material = mat;
        }

        // Inner Glowing Core
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "EnergyCore";
        core.transform.SetParent(root.transform, false);
        core.transform.localScale = Vector3.one * 0.42f;
        Collider coreCol = core.GetComponent<Collider>();
        if (coreCol != null) Destroy(coreCol);

        Renderer coreRend = core.GetComponent<Renderer>();
        if (coreRend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = Color.white;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.white * 2.5f);
            coreRend.material = mat;
        }

        // Point Light
        Light lt = root.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.range = 5.5f;
        lt.intensity = 3.5f;
        lt.color = colVal;

        // Floating Title/Badge Text
        GameObject labelObj = new GameObject("LabelText");
        labelObj.transform.SetParent(root.transform, false);
        labelObj.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        TMPro.TextMeshPro tmp = labelObj.AddComponent<TMPro.TextMeshPro>();
        tmp.text = GetDisplayName(type);
        tmp.fontSize = 3.8f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = colVal;

        PowerUpPickup pickup = root.AddComponent<PowerUpPickup>();
        pickup.powerUpType = type;

        return pickup;
    }
}
