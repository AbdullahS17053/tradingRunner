using System.Collections;
using UnityEngine;

public class GemPickup : MonoBehaviour
{
    public enum GemColorType { CyanDiamond, PurpleAmethyst, GoldTopaz }

    [Header("Gem Configuration")]
    public GemColorType gemType = GemColorType.CyanDiamond;
    public int gemValue = 1;

    [Header("Visual & Animation")]
    public float rotationSpeed = 90f;
    public float bobFrequency = 2f;
    public float bobAmplitude = 0.15f;
    public MeshRenderer gemRenderer;
    public Collider gemCollider;
    public ParticleSystem collectParticles;
    public ParticleSystem idleGlowParticles;

    [Header("Colors")]
    public Color gemGlowColor = new Color(0f, 0.9f, 1f, 1f);

    private Vector3 initialPosition;
    private bool isCollected = false;

    private void Awake()
    {
        if (gemCollider == null) gemCollider = GetComponent<Collider>();
        if (gemCollider != null) gemCollider.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void Start()
    {
        initialPosition = transform.position;
        if (gemCollider == null) gemCollider = GetComponent<Collider>();
        if (gemCollider != null) gemCollider.isTrigger = true;
        if (gemRenderer == null) gemRenderer = GetComponentInChildren<MeshRenderer>();
    }

    private void Update()
    {
        if (isCollected) return;

        // Gentle rotation and hover bobbing
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        float newY = initialPosition.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // PowerUp Magnet Attraction
        if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsMagnetActive)
        {
            Transform playerTarget = RunnerController.Instance != null ? RunnerController.Instance.transform : null;
            if (playerTarget != null)
            {
                float dist = Vector3.Distance(transform.position, playerTarget.position);
                if (dist <= PowerUpManager.Instance.magnetRadius)
                {
                    Vector3 targetPos = playerTarget.position + Vector3.up * 0.8f;
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, PowerUpManager.Instance.magnetPullSpeed * Time.deltaTime);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }

    public void Collect()
    {
        isCollected = true;

        if (gemCollider != null) gemCollider.enabled = false;
        if (gemRenderer != null) gemRenderer.enabled = false;
        if (idleGlowParticles != null) idleGlowParticles.Stop();

        if (collectParticles != null)
        {
            collectParticles.transform.position = transform.position + Vector3.up * 0.5f;
            collectParticles.Play();
        }

        // Add gems to persistent manager
        if (GemReviveManager.Instance != null)
        {
            GemReviveManager.Instance.AddGems(gemValue);
        }

        if (SoundManager.Instance != null)
        {
            if (SoundManager.Instance.collectTPSound != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.collectTPSound);
            }
        }

        StartCoroutine(DisableAfterParticles());
    }

    private IEnumerator DisableAfterParticles()
    {
        float wait = 1.0f;
        if (collectParticles != null)
        {
            wait = collectParticles.main.duration + collectParticles.main.startLifetime.constantMax;
        }

        yield return new WaitForSeconds(wait);

        if (gemCollider != null) gemCollider.enabled = true;
        if (gemRenderer != null) gemRenderer.enabled = true;
        isCollected = false;
        gameObject.SetActive(false);
    }
}
