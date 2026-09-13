using System.Collections;
using UnityEngine;

public class FOMOController : MonoBehaviour
{
    [Header("Targeting")]
    public Transform player;
    public Animator playerAnimator;

    [Header("FOMO Mechanics")]
    public float currentDistanceBehind = 3.5f;
    public float penaltyDistance = 1.0f;
    public float catchUpSpeed = 10f;

    [Header("Recovery Mechanics")]
    public float maxDistanceBehind = 3.5f;
    public float recoveryDelay = 4.0f;
    public float recoveryRate = 0.5f;

    [Header("Visual Effects (Laser & Death)")]
    public LineRenderer fomoLaser;
    public Transform fomoLaserOrigin;
    public Animator fomoAnimator;

    [Header("Cinematic Flight Settings")]
    public float ascendHeight = 15f;
    public float ascendDuration = 2.0f;
    public float dropHeight = 15f;
    public float descendDuration = 2.0f;
    public float hoverForwardDistance = 4.0f;
    public float hoverLevitationHeight = 2.5f;

    [Header("Attack Timing")]
    public float captureToLaserDelay = 1.5f;
    public float laserFadeDuration = 0.2f;

    private float timeSinceLastMistake = 0f;
    private bool isCatching = false;

    private void Update()
    {
        if (player == null || isCatching) return;

        timeSinceLastMistake += Time.deltaTime;

        if (timeSinceLastMistake >= recoveryDelay && currentDistanceBehind < maxDistanceBehind)
        {
            currentDistanceBehind += recoveryRate * Time.deltaTime;
            currentDistanceBehind = Mathf.Min(currentDistanceBehind, maxDistanceBehind);
        }

        Vector3 targetPosition = player.position;
        targetPosition.z -= currentDistanceBehind;

        transform.position = Vector3.Lerp(transform.position, targetPosition, catchUpSpeed * Time.deltaTime);

        if (currentDistanceBehind <= 0.8f)
        {
            StartCoroutine(CatchPlayerSequence());
        }
    }

    public void PunishPlayer()
    {
        if (isCatching) return;

        timeSinceLastMistake = 0f;
        currentDistanceBehind -= penaltyDistance;
        currentDistanceBehind = Mathf.Max(currentDistanceBehind, 0f);
    }

    private IEnumerator CatchPlayerSequence()
    {
        isCatching = true;

        if (RunnerController.Instance != null)
        {
            RunnerController.Instance.enabled = false;
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsCaptured", true);
            playerAnimator.Play("StandingIdle", 0, 0f);
        }

        Vector3 startPos = transform.position;
        Vector3 skyPos = startPos + (Vector3.up * ascendHeight);

        float elapsed = 0f;
        while (elapsed < ascendDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / ascendDuration);
            transform.position = Vector3.Lerp(startPos, skyPos, t);
            yield return null;
        }

        Vector3 targetPos = player.position + (Vector3.forward * hoverForwardDistance) + (Vector3.up * hoverLevitationHeight);
        Vector3 dropPos = targetPos + (Vector3.up * dropHeight);

        transform.position = dropPos;
        elapsed = 0f;

        while (elapsed < descendDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / descendDuration);
            transform.position = Vector3.Lerp(dropPos, targetPos, t);

            transform.LookAt(player.position + (Vector3.up * 1f));
            yield return null;
        }

        transform.position = targetPos;
        transform.LookAt(player.position + (Vector3.up * 1f));

        if (fomoAnimator != null)
        {
            fomoAnimator.SetTrigger("Attack");
            SoundManager.Instance.PlaySFX(SoundManager.Instance.fomoLaserShotSound);
        }

        yield return new WaitForSeconds(captureToLaserDelay);

        if (fomoLaser != null && fomoLaserOrigin != null)
        {
            fomoLaser.gameObject.SetActive(true);
            fomoLaser.useWorldSpace = true;
            fomoLaser.enabled = true;

            fomoLaser.startWidth = 0.1f;
            fomoLaser.endWidth = 0.1f;
            fomoLaser.SetPosition(0, fomoLaserOrigin.position);
            fomoLaser.SetPosition(1, player.position + (Vector3.up * 1f));
        }

        if (playerAnimator != null)
        {
            playerAnimator.Play("Die", 0, 0f);
        }

        float fadeElapsed = 0f;
        while (fadeElapsed < laserFadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            if (fomoLaser != null)
            {
                float currentWidth = Mathf.Lerp(0.1f, 0f, fadeElapsed / laserFadeDuration);
                fomoLaser.startWidth = currentWidth;
                fomoLaser.endWidth = currentWidth;
            }
            yield return null;
        }

        if (fomoLaser != null)
        {
            fomoLaser.enabled = false;
        }

        yield return new WaitForSeconds(1.5f - laserFadeDuration);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }
}
