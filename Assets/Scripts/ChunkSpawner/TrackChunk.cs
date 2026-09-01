using UnityEngine;

public class TrackChunk : MonoBehaviour
{
    public Transform connectionPoint;
    public Transform coinsParent;

    // Optional: Add references to obstacles, coins, or enemies here 
    // if you want the spawner to reset them when the chunk is recycled.

    private void OnEnable()
    {
        ResetCollectibles();
    }

    private void ResetCollectibles()
    {
        if (coinsParent == null) return;

        foreach (Transform coin in coinsParent)
        {
            coin.gameObject.SetActive(true);
        }
    }
}