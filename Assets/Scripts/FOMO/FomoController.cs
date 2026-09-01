using UnityEngine;

public class FOMOController : MonoBehaviour
{
    [Header("Targeting")]
    public Transform player;
    
    public float currentDistanceBehind = 20f;
    public float penaltyDistance = 7f;
    public float catchUpSpeed = 5f;

    private void Update()
    {
        if (player == null) return;

        Vector3 targetPosition = player.position;
        targetPosition.z -= currentDistanceBehind;
        
        targetPosition.x = 0; 
        transform.position = Vector3.Lerp(transform.position, targetPosition, catchUpSpeed * Time.deltaTime);

        if (currentDistanceBehind <= 2f) 
        {
            // GameManager.Instance.TriggerGameOver();
            Debug.Log("Game Over");
        }
    }

    public void PunishPlayer()
    {
        currentDistanceBehind -= penaltyDistance;
        Debug.Log("FOMO got closer! Distance is now: " + currentDistanceBehind);
    }
}