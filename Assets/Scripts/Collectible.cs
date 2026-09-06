using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectible : MonoBehaviour
{

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Player"))
        {
            GameManager.Instance.takeProfit += 1;
            SoundManager.Instance.PlaySFX(SoundManager.Instance.collectTPSound);
            this.gameObject.SetActive(false);
        }
    }
}
