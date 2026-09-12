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

            GameManager.Instance.takeProfit += 1;
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
