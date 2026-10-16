using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    void Update()
    {
        if (scoreText != null && GameManager.Instance != null)
        {
            scoreText.text = GameManager.Instance.takeProfit.ToString();
        }
    }
}
