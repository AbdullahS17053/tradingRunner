using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    void Update()
    {
        scoreText.text = "Take Profit: " + GameManager.Instance.takeProfit.ToString();
    }
}
