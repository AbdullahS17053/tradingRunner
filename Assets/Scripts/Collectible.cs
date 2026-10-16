using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectible : MonoBehaviour
{
    public ParticleSystem coinParticles;

    public MeshRenderer coinRenderer;
    public Collider coinCollider;

    [Header("Particle Offsets")]
    [SerializeField] private float spawnHeightOffset = 1.0f;

    private void Update()
    {
        if (coinCollider != null && !coinCollider.enabled) return;

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

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Player"))
        {
            if (coinCollider != null) coinCollider.enabled = false;

            if (coinRenderer != null) coinRenderer.enabled = false;

            Vector3 particlePos = coinParticles.transform.position;
            particlePos.y += spawnHeightOffset;
            coinParticles.transform.position = particlePos;

            coinParticles.Play();

            int tpGain = (PowerUpManager.Instance != null && PowerUpManager.Instance.IsDoubleTPActive) ? 2 : 1;
            GameManager.Instance.takeProfit += tpGain;
            SoundManager.Instance.PlaySFX(SoundManager.Instance.collectTPSound);

            StartCoroutine(DisableAfterEmissionComplete());
        }
    }

    private IEnumerator DisableAfterEmissionComplete()
    {
        float totalWaitTime = coinParticles.main.duration + coinParticles.main.startLifetime.constantMax;

        yield return new WaitForSeconds(totalWaitTime);

        if (coinRenderer != null) coinRenderer.enabled = true;
        if (coinCollider != null) coinCollider.enabled = true;

        this.gameObject.SetActive(false);
    }
}
