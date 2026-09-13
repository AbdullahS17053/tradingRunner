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

    [Header("Cinematic Flight Settings")]
    public float ascendHeight = 15f;
    [Tooltip("Time it takes for FOMO to slowly fly up out of frame")]
    public float ascendDuration = 2.0f;
    public float dropHeight = 15f;
    [Tooltip("Time it takes for FOMO to slowly descend in front of the player")]
    public float descendDuration = 2.0f;
    public float hoverForwardDistance = 4.0f;
    public float hoverLevitationHeight = 2.5f;
    [Tooltip("Delay between the player getting scared and the laser firing")]
    public float scaredToLaserDelay = 1.5f;

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

        Debug.Log("FOMO got closer! Distance is now: " + currentDistanceBehind);
    }

    private IEnumerator CatchPlayerSequence()
    {
        isCatching = true;
        Time.timeScale = 0f; // Freeze game world

        // 1. FOMO slowly ascends out of the camera view
        Vector3 startPos = transform.position;
        Vector3 skyPos = startPos + (Vector3.up * ascendHeight);

        float elapsed = 0f;
        while (elapsed < ascendDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            // Using SmoothStep creates a nice ease-in/ease-out effect instead of stiff linear movement
            float t = Mathf.SmoothStep(0f, 1f, elapsed / ascendDuration);
            transform.position = Vector3.Lerp(startPos, skyPos, t);
            yield return null;
        }

        // 2. FOMO teleports high above the front of the player and slowly descends
        Vector3 targetPos = player.position + (Vector3.forward * hoverForwardDistance) + (Vector3.up * hoverLevitationHeight);
        Vector3 dropPos = targetPos + (Vector3.up * dropHeight);

        transform.position = dropPos;
        elapsed = 0f;

        while (elapsed < descendDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / descendDuration);
            transform.position = Vector3.Lerp(dropPos, targetPos, t);

            // Constantly look at the player's chest while dropping in
            transform.LookAt(player.position + (Vector3.up * 1f));
            yield return null;
        }

        // Snap to exact position to finalize movement
        transform.position = targetPos;
        transform.LookAt(player.position + (Vector3.up * 1f));

        // 3. Player stops and plays Scared Animation
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Scared");
        }

        // Let the Scared animation play out before firing the laser
        yield return new WaitForSecondsRealtime(scaredToLaserDelay);

        // 4. FOMO shoots the laser
        if (fomoLaser != null && fomoLaserOrigin != null)
        {
            fomoLaser.enabled = true;
            fomoLaser.SetPosition(0, fomoLaserOrigin.position);
            fomoLaser.SetPosition(1, player.position + (Vector3.up * 1f));
        }

        // 5. Player plays Death Animation
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Die");
        }

        // Wait for the player to fall over
        yield return new WaitForSecondsRealtime(1.5f);

        // Clean up laser
        if (fomoLaser != null) fomoLaser.enabled = false;

        // 6. Trigger Game Over UI
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }
}
