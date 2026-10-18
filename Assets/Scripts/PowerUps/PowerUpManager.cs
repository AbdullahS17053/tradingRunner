using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PowerUpType
{
    Shield,          // Absorbs 1 obstacle collision
    SpeedBoost,      // 2x forward speed + invincibility
    CoinMagnet,      // Pulls nearby TP coins to player
    DoubleTP,        // Doubles collected TP (+2 TP per coin)
    WeaponUpgrade,   // Auto-fires rapid laser blaster bolts destroying obstacles
    SlowMotion,      // 0.5x bullet-time speed for precision dodging
    Invincibility    // Star-power rainbow aura, smash through obstacles
}

public class PowerUpManager : MonoBehaviour
{
    private static bool isShuttingDown = false;
    private static PowerUpManager _instance;

    public static bool HasInstance => _instance != null && !isShuttingDown;

    public static PowerUpManager Instance
    {
        get
        {
            if (isShuttingDown) return null;
            if (_instance == null)
            {
                _instance = FindObjectOfType<PowerUpManager>();
            }
            return _instance;
        }
    }

    public static PowerUpManager GetOrCreate()
    {
        if (isShuttingDown) return null;
        if (_instance == null)
        {
            _instance = FindObjectOfType<PowerUpManager>();
            if (_instance == null)
            {
                GameObject go = new GameObject("PowerUpManager");
                _instance = go.AddComponent<PowerUpManager>();
            }
        }
        _ = PowerUpHUD.Instance;
        return _instance;
    }

    [Header("Settings")]
    public float powerUpDuration = 5.0f;
    public float magnetRadius = 16f;
    public float magnetPullSpeed = 22f;

    // Cross-scene memory of last triggered power-up so new worlds start with a different one
    public static PowerUpType lastActivatedType = PowerUpType.SpeedBoost;

    // Cooldown timer after a power-up expires (4 seconds)
    public float postPowerUpCooldownTimer = 0f;

    [Header("Active States")]
    private Dictionary<PowerUpType, float> activeTimers = new Dictionary<PowerUpType, float>();
    private bool hasShield = false;
    private float invulnerabilityGraceTimer = 0f;

    [Header("Spawned Player/Ship FX References")]
    private Dictionary<PowerUpType, GameObject> attachedVFX = new Dictionary<PowerUpType, GameObject>();

    // Weapon shooting timer
    private float weaponFireTimer = 0f;
    private const float WEAPON_FIRE_INTERVAL = 0.32f;

    // Public Getters
    public bool HasShield => hasShield;
    public bool IsSpeedBoostActive => IsActive(PowerUpType.SpeedBoost);
    public bool IsMagnetActive => IsActive(PowerUpType.CoinMagnet);
    public bool IsDoubleTPActive => IsActive(PowerUpType.DoubleTP);
    public bool IsWeaponActive => IsActive(PowerUpType.WeaponUpgrade);
    public bool IsSlowMotionActive => IsActive(PowerUpType.SlowMotion);
    public bool IsInvincibleActive => IsActive(PowerUpType.Invincibility);
    public bool IsInvincible => IsInvincibleActive || IsSpeedBoostActive || invulnerabilityGraceTimer > 0f;
    public bool IsAnyPowerUpActive => activeTimers.Count > 0 || hasShield;
    public bool CanSpawnPowerUp => !IsAnyPowerUpActive && postPowerUpCooldownTimer <= 0f;

    public System.Action<PowerUpType, float, float> OnPowerUpUpdated; // (type, remainingTime, maxDuration)
    public System.Action<PowerUpType> OnPowerUpDeactivated;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Start()
    {
        if (PowerUpHUD.Instance == null)
        {
            GameObject hudGo = new GameObject("PowerUpHUD");
            hudGo.AddComponent<PowerUpHUD>();
        }
    }

    private void OnApplicationQuit()
    {
        isShuttingDown = true;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
        if (IsSlowMotionActive)
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && (GameManager.Instance.isGameOver || GameManager.Instance.isTradingChallengeActive))
        {
            return;
        }

        if (postPowerUpCooldownTimer > 0f)
        {
            postPowerUpCooldownTimer -= Time.deltaTime;
        }

        if (invulnerabilityGraceTimer > 0f)
        {
            invulnerabilityGraceTimer -= Time.deltaTime;
        }

        List<PowerUpType> activeKeys = new List<PowerUpType>(activeTimers.Keys);
        foreach (PowerUpType type in activeKeys)
        {
            activeTimers[type] -= Time.unscaledDeltaTime;
            float remaining = activeTimers[type];

            OnPowerUpUpdated?.Invoke(type, Mathf.Max(0f, remaining), powerUpDuration);

            if (remaining <= 0f)
            {
                DeactivatePowerUp(type);
            }
        }

        // Weapon auto-fire handling
        if (IsWeaponActive)
        {
            weaponFireTimer -= Time.deltaTime;
            if (weaponFireTimer <= 0f)
            {
                weaponFireTimer = WEAPON_FIRE_INTERVAL;
                FireLaserBolt();
            }
        }
    }

    public bool IsActive(PowerUpType type)
    {
        if (type == PowerUpType.Shield) return hasShield;
        return activeTimers.ContainsKey(type) && activeTimers[type] > 0f;
    }

    public float GetRemainingTime(PowerUpType type)
    {
        if (type == PowerUpType.Shield) return hasShield ? 999f : 0f;
        return activeTimers.TryGetValue(type, out float time) ? Mathf.Max(0f, time) : 0f;
    }

    public void ActivatePowerUp(PowerUpType type)
    {
        lastActivatedType = type;

        if (PowerUpHUD.Instance != null)
        {
            PowerUpHUD.Instance.ShowPowerUpAnnouncement(type);
        }

        if (type == PowerUpType.Shield)
        {
            hasShield = true;
            AttachVFX(PowerUpType.Shield);
            PlayActivationSound();
            OnPowerUpUpdated?.Invoke(PowerUpType.Shield, 1f, 1f);
            return;
        }

        activeTimers[type] = powerUpDuration;

        // Apply instant mechanics
        if (type == PowerUpType.SlowMotion)
        {
            Time.timeScale = 0.5f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }

        AttachVFX(type);
        PlayActivationSound();
        OnPowerUpUpdated?.Invoke(type, powerUpDuration, powerUpDuration);
    }

    public void DeactivatePowerUp(PowerUpType type)
    {
        postPowerUpCooldownTimer = 5.0f; // 4-second interval before next power-up can spawn

        if (type == PowerUpType.Shield)
        {
            hasShield = false;
        }

        if (activeTimers.ContainsKey(type))
        {
            activeTimers.Remove(type);
        }

        if (type == PowerUpType.SlowMotion)
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
        }

        RemoveVFX(type);
        OnPowerUpDeactivated?.Invoke(type);
    }

    public void ClearAllPowerUps()
    {
        var types = new System.Collections.Generic.List<PowerUpType>(activeTimers.Keys);
        foreach (var t in types)
        {
            DeactivatePowerUp(t);
        }
        hasShield = false;
        postPowerUpCooldownTimer = 0f;
    }

    public bool ConsumeShield()
    {
        if (hasShield)
        {
            hasShield = false;
            postPowerUpCooldownTimer = 5.0f;
            if (activeTimers.ContainsKey(PowerUpType.Shield))
            {
                activeTimers.Remove(PowerUpType.Shield);
            }
            invulnerabilityGraceTimer = 0.6f;
            RemoveVFX(PowerUpType.Shield);
            SpawnShieldShatterFX();
            OnPowerUpDeactivated?.Invoke(PowerUpType.Shield);

            if (SoundManager.Instance != null && SoundManager.Instance.crashSound != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.crashSound);
            }
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.ShakeCamera(0.2f, 0.15f);
            }
            return true;
        }
        return false;
    }

    private void PlayActivationSound()
    {
        if (SoundManager.Instance != null)
        {
            if (SoundManager.Instance.correctStrategySound != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.correctStrategySound);
            }
            else if (SoundManager.Instance.collectTPSound != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.collectTPSound);
            }
        }
    }

    private Transform GetActiveTargetTransform()
    {
        SpaceshipController ship = FindObjectOfType<SpaceshipController>();
        if (ship != null && ship.shipVisual != null)
        {
            return ship.shipVisual;
        }
        if (RunnerController.Instance != null)
        {
            return RunnerController.Instance.transform;
        }
        return null;
    }

    private void AttachVFX(PowerUpType type)
    {
        RemoveVFX(type);

        Transform target = GetActiveTargetTransform();
        if (target == null) return;

        bool isSpaceship = FindObjectOfType<SpaceshipController>() != null;

        if (type == PowerUpType.Shield)
        {
            GameObject shieldRoot = new GameObject("PlayerShieldAura");
            shieldRoot.transform.SetParent(target, false);
            shieldRoot.transform.localPosition = GetVFXOffset(PowerUpType.Shield, isSpaceship);
            PlayerShieldAura aura = shieldRoot.AddComponent<PlayerShieldAura>();
            aura.Setup(isSpaceship);
            attachedVFX[type] = shieldRoot;
            return;
        }

        if (!isSpaceship)
        {
            if (type == PowerUpType.SpeedBoost)
            {
                GameObject windPrefab = Resources.Load<GameObject>("PowerUps/FX_Wind_Streaks_01");
                if (windPrefab == null) windPrefab = LoadVFXPrefab(PowerUpType.SpeedBoost);
                if (windPrefab != null)
                {
                    GameObject windInst = Instantiate(windPrefab, target);
                    windInst.transform.localPosition = new Vector3(0f, 0.35f, -0.6f);
                    windInst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    windInst.transform.localScale = Vector3.one * 1.2f;
                    attachedVFX[type] = windInst;
                }
            }
            return;
        }

        GameObject vfxPrefab = LoadVFXPrefab(type);
        if (vfxPrefab != null)
        {
            GameObject instance = Instantiate(vfxPrefab, target);
            instance.transform.localPosition = GetVFXOffset(type, isSpaceship);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = GetVFXScale(type, isSpaceship);
            attachedVFX[type] = instance;
        }
        else
        {
            // Fallback runtime procedural aura if prefab is not assigned
            GameObject aura = CreateProceduralAura(type, target, isSpaceship);
            if (aura != null) attachedVFX[type] = aura;
        }
    }

    private void RemoveVFX(PowerUpType type)
    {
        if (attachedVFX.TryGetValue(type, out GameObject vfxObj) && vfxObj != null)
        {
            Destroy(vfxObj);
        }
        attachedVFX.Remove(type);
    }

    private GameObject LoadVFXPrefab(PowerUpType type)
    {
        string path = "";
        switch (type)
        {
            case PowerUpType.Shield:
                path = "FX_Hexagon_01";
                break;
            case PowerUpType.SpeedBoost:
                path = "FX_Flame_Booster_01";
                break;
            case PowerUpType.CoinMagnet:
                path = "FX_Sparkle_Orbit_01";
                break;
            case PowerUpType.DoubleTP:
                path = "FX_Money_Coins_01";
                break;
            case PowerUpType.WeaponUpgrade:
                path = "FX_Laser_Shot";
                break;
            case PowerUpType.SlowMotion:
                path = "FX_Swirl_03";
                break;
            case PowerUpType.Invincibility:
                path = "FX_RainbowGlow_01";
                break;
        }

        GameObject prefab = Resources.Load<GameObject>("PowerUps/" + path);
        if (prefab == null)
        {
            // Also attempt loading from direct PolygonParticleFX prefab path if needed
            prefab = Resources.Load<GameObject>(path);
        }
        return prefab;
    }

    private Vector3 GetVFXOffset(PowerUpType type, bool isSpaceship)
    {
        if (isSpaceship)
        {
            switch (type)
            {
                case PowerUpType.Shield: return new Vector3(0f, 0.2f, 0f);
                case PowerUpType.SpeedBoost: return new Vector3(0f, 0.1f, -1.8f);
                case PowerUpType.CoinMagnet: return new Vector3(0f, 0.4f, 0f);
                case PowerUpType.DoubleTP: return new Vector3(0f, 0.6f, 0f);
                case PowerUpType.WeaponUpgrade: return new Vector3(0f, 0.2f, 1.2f);
                case PowerUpType.SlowMotion: return new Vector3(0f, 0.3f, 0f);
                case PowerUpType.Invincibility: return new Vector3(0f, 0.2f, 0f);
                default: return Vector3.zero;
            }
        }
        else
        {
            switch (type)
            {
                case PowerUpType.Shield: return new Vector3(0f, 1.0f, 0f);
                case PowerUpType.SpeedBoost: return new Vector3(0f, 0.4f, -1.2f);
                case PowerUpType.CoinMagnet: return new Vector3(0f, 0.8f, 0f);
                case PowerUpType.DoubleTP: return new Vector3(0f, 1.2f, 0f);
                case PowerUpType.WeaponUpgrade: return new Vector3(0f, 0.6f, 0.8f);
                case PowerUpType.SlowMotion: return new Vector3(0f, 0.8f, 0f);
                case PowerUpType.Invincibility: return new Vector3(0f, 1.0f, 0f);
                default: return Vector3.zero;
            }
        }
    }

    private Vector3 GetVFXScale(PowerUpType type, bool isSpaceship)
    {
        if (isSpaceship)
        {
            switch (type)
            {
                case PowerUpType.Shield: return new Vector3(2.4f, 1.5f, 3.2f);
                case PowerUpType.SpeedBoost: return new Vector3(1.6f, 1.6f, 2.2f);
                case PowerUpType.CoinMagnet: return Vector3.one * 1.8f;
                case PowerUpType.DoubleTP: return Vector3.one * 1.6f;
                case PowerUpType.WeaponUpgrade: return Vector3.one * 1.5f;
                case PowerUpType.SlowMotion: return Vector3.one * 2.0f;
                case PowerUpType.Invincibility: return Vector3.one * 2.2f;
                default: return Vector3.one;
            }
        }
        else
        {
            switch (type)
            {
                case PowerUpType.Shield: return Vector3.one * 1.5f;
                case PowerUpType.SpeedBoost: return Vector3.one * 1.2f;
                case PowerUpType.Invincibility: return Vector3.one * 1.4f;
                default: return Vector3.one;
            }
        }
    }

    private GameObject CreateProceduralAura(PowerUpType type, Transform parent, bool isSpaceship)
    {
        GameObject aura = new GameObject("Aura_" + type.ToString());
        aura.transform.SetParent(parent, false);
        aura.transform.localPosition = GetVFXOffset(type, isSpaceship);

        Light light = aura.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 5f;
        light.intensity = 3.5f;

        switch (type)
        {
            case PowerUpType.Shield: light.color = new Color(0.2f, 0.7f, 1f); break;
            case PowerUpType.SpeedBoost: light.color = new Color(1f, 0.4f, 0.1f); break;
            case PowerUpType.CoinMagnet: light.color = new Color(0.2f, 1f, 0.8f); break;
            case PowerUpType.DoubleTP: light.color = new Color(1f, 0.85f, 0.1f); break;
            case PowerUpType.WeaponUpgrade: light.color = new Color(1f, 0.1f, 0.2f); break;
            case PowerUpType.SlowMotion: light.color = new Color(0.4f, 0.8f, 1f); break;
            case PowerUpType.Invincibility: light.color = new Color(1f, 0.2f, 1f); break;
        }
        return aura;
    }

    private void SpawnShieldShatterFX()
    {
        Transform target = GetActiveTargetTransform();
        Vector3 pos = target != null ? target.position + Vector3.up * 0.8f : transform.position;

        GameObject shatter = new GameObject("ShieldShatter");
        shatter.transform.position = pos;
        Light flash = shatter.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.range = 9f;
        flash.intensity = 7f;
        flash.color = new Color(0.3f, 0.85f, 1f);

        GameObject burstPrefab = Resources.Load<GameObject>("PowerUps/FX_Sparkle_Orbit_01");
        if (burstPrefab != null)
        {
            GameObject burst = Instantiate(burstPrefab, pos, Quaternion.identity);
            burst.transform.localScale = Vector3.one * 2.2f;
            Destroy(burst, 0.85f);
        }

        Destroy(shatter, 0.35f);
    }

    private void FireLaserBolt()
    {
        Transform target = GetActiveTargetTransform();
        if (target == null) return;

        Vector3 spawnPos = target.position + Vector3.up * 0.8f;
        Vector3 shootDir = RunnerController.Instance != null ? RunnerController.Instance.PathForward : target.forward;

        // Instantiate laser bolt
        GameObject bolt = new GameObject("LaserBolt");
        bolt.transform.position = spawnPos;
        bolt.transform.rotation = Quaternion.LookRotation(shootDir);

        LaserProjectile proj = bolt.AddComponent<LaserProjectile>();
        proj.Initialize(shootDir, 65f);

        // Sound
        if (SoundManager.Instance != null)
        {
            if (SoundManager.Instance.fomoLaserShotSound != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.fomoLaserShotSound);
            }
            else if (SoundManager.Instance.uiClickSound != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.uiClickSound);
            }
        }
    }

}
