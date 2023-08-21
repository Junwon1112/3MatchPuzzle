using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOver_UI : MonoBehaviour
{
    GameBoard gameBoard;

    Button retryButton;
    Button backButton;
    CanvasGroup canvasGroup;
    GameBoard_TopUI gameBoard_TopUI;

    public TextMeshProUGUI[] name_Text = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] score_Text = new TextMeshProUGUI[3];

    public const int NOT_STAGE = 99999;

    private void Awake()
    {
        retryButton = transform.GetChild(1).GetComponent<Button>();
        backButton = transform.GetChild(2).GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
        gameBoard = FindObjectOfType<GameBoard>();
        gameBoard_TopUI = FindObjectOfType<GameBoard_TopUI>();

        for(int i = 0; i < 3; i++)
        {
            name_Text[i] = transform.GetChild(3).GetChild(i).GetChild(1).GetComponent<TextMeshProUGUI>();
            score_Text[i] = transform.GetChild(3).GetChild(i).GetChild(2).GetComponent<TextMeshProUGUI>();
        }
    }

    void Start()
    {
        retryButton.onClick.AddListener(RetryButton);
        backButton.onClick.AddListener(BackButton);
    }


    public void CanvasGroupOnOff()
    {
        if(canvasGroup.interactable)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameBoard.input.Enable();
        }
        else
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            gameBoard.input.Disable();

            SoundPlayer.Instance.StopBGM();
            SoundPlayer.Instance.PlaySound(SoundType_Effect.Effect_GameOver);
            
        }
    }

    public void RetryButton()
    {
        CanvasGroupOnOff();
        gameBoard_TopUI.isGameOver = false;
        SceneManager.LoadScene(GameManager.instance.currentSceneIndex);
        SoundPlayer.Instance.PlaySound(SoundType_Effect.Effect_UISound);
        SoundPlayer.Instance.UnpauseBGM();
    }


    public void BackButton()
    {
        CanvasGroupOnOff();
        gameBoard_TopUI.isGameOver = false;
        GameManager.instance.currentStage = NOT_STAGE;
        SceneManager.LoadScene("StageSelect");
        SoundPlayer.Instance.PlaySound(SoundType_Effect.Effect_UISound);
    }

}
