using UnityEngine;

public class FOMOController : MonoBehaviour
{
    [Header("Targeting")]
    public Transform player;
    
    [Header("FOMO Mechanics")]
    public float currentDistanceBehind = 3.5f;
    public float penaltyDistance = 1.0f;
    public float catchUpSpeed = 10f; 

    [Header("Recovery Mechanics")]
    public float maxDistanceBehind = 3.5f;
    public float recoveryDelay = 4.0f;
    public float recoveryRate = 0.5f;

    private float timeSinceLastMistake = 0f;

    private void Update()
    {
        if (player == null) return;

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
            GameManager.Instance.TriggerGameOver();
        }
    }

    public void PunishPlayer()
    {
        timeSinceLastMistake = 0f;

        currentDistanceBehind -= penaltyDistance;
        currentDistanceBehind = Mathf.Max(currentDistanceBehind, 0f);
        
        Debug.Log("FOMO got closer! Distance is now: " + currentDistanceBehind);
    }
}
