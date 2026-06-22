using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Runtime.CompilerServices;
using UnityEngine.UI;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public int playerLives = 10;
    public int playerMoney = 0;
    public GameObject defeatPanel;
    public TMP_Text lives;
    public TMP_Text money;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        
    }

    private void Start()
    {
        UpdateLivesUI();
        UpdateMoneyUI();
    }

    public void LoseLife(int dmg)
    {
        playerLives -= dmg;
        UpdateLivesUI();
        Debug.Log("剩餘生命:" + playerLives);
        if (playerLives <= 0)
        {
            Defeat();
        }
    }

    void Defeat()
    {
        defeatPanel.SetActive(true);
        Time.timeScale = 0;
    }

    public void Restart()
    {
        Debug.Log("Restart被按下了!");

        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void UpdateLivesUI()
    {
        lives.text = "Lives:" + playerLives;
    }

    public void UpdateMoneyUI()
    {
        money.text = "Money:" + playerMoney;
    }


}

