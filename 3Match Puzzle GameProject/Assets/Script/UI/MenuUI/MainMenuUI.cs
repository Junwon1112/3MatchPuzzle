using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;

public class MainMenuUI : MonoBehaviour
{
    Button retryButton;
    Button backToStageButton;
    Button optionButton;
    Button okButton;

    CanvasGroup canvasGroup;
    GameBoard gameBoard;

    OptionMenuUI optionMenuUI;


    private void OnDisable()
    {
        gameBoard.input.Disable();
        gameBoard.input.MainMenuUI.MenuUI.performed -= OnOpenMenuUI;
    }

    private void Awake()
    {
        retryButton = transform.GetChild(0).GetComponent<Button>();
        backToStageButton = transform.GetChild(1).GetComponent<Button>();
        optionButton = transform.GetChild(2).GetComponent<Button>();
        okButton = transform.GetChild(3).GetComponent<Button>();
        
        optionMenuUI = FindObjectOfType<OptionMenuUI>();

        canvasGroup = GetComponent<CanvasGroup>();
        gameBoard= FindObjectOfType<GameBoard>();
        
    }


    void Start()
    {
        gameBoard.input.Enable();
        gameBoard.input.MainMenuUI.MenuUI.performed += OnOpenMenuUI;


        retryButton.onClick.AddListener(OnClickRetryButton);
        backToStageButton.onClick.AddListener(OnClickBackToStageButton);
        optionButton.onClick.AddListener(OnClickOptionButton);
        okButton.onClick.AddListener(OnClickOKButton);
    }

    private void OnOpenMenuUI(InputAction.CallbackContext obj)
    {
        CanvasGroupOnOff();
    }

    private void OnClickRetryButton()
    {
        CanvasGroupOnOff();
        SceneManager.LoadScene(GameManager.instance.currentSceneIndex);
        SoundPlayer.Instance.PlaySound(SoundType_Effect.Effect_UISound);
        SoundPlayer.Instance.UnpauseBGM();
    }
    private void OnClickBackToStageButton()
    {
        CanvasGroupOnOff();
        SceneManager.LoadScene("StageSelect");
        SoundPlayer.Instance.PlaySound(SoundType_Effect.Effect_UISound);
        SoundPlayer.Instance.UnpauseBGM();
    }
    private void OnClickOptionButton()
    {
        optionMenuUI.OpenMainMenu();
    }
    private void OnClickOKButton()
    {
        CanvasGroupOnOff();
    }

    private void CanvasGroupOnOff()
    {
        if (canvasGroup.interactable)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Time.timeScale = 1;
            gameBoard.input.Control.Enable();
            SoundPlayer.Instance.PlayBGM();
        }
        else
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            Time.timeScale = 0;

            gameBoard.input.Control.Disable();

            SoundPlayer.Instance.PauseBGM();
            SoundPlayer.Instance.PlaySound(SoundType_Effect.Effect_UISound);

        }
    }
}
