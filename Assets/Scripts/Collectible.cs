using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectible : MonoBehaviour
{

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Player"))
        {
            Debug.Log("Collided With Coin");
            GameManager.Instance.takeProfit += 1;
            this.gameObject.SetActive(false);
        }
    }
}
