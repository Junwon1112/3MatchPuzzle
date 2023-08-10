using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainScene_BackgroundUI : MonoBehaviour
{
    CanvasGroup canvasGroup;
    Image backgroundImage;
    float timer = 0.0f;
    public float sceneStartTime = 3.0f;

    Button startButton;
    Button optionButton;
    Button exitButton;

    OptionMenuUI optionMenuUI;


    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        backgroundImage = GetComponentInChildren<Image>();

        startButton = transform.GetChild(1).GetComponent<Button>();
        optionButton = transform.GetChild(2).GetComponent<Button>();
        exitButton = transform.GetChild(3).GetComponent<Button>();
        SoundPlayer.Instance.PlayBGM(SoundType_BGM.BGM_MainStage);

        optionMenuUI = FindObjectOfType<OptionMenuUI>();
    }

    private void Start()
    {
        StartCoroutine(CoMainSceneTimer());
        startButton.onClick.AddListener(GetStart);
        exitButton.onClick.AddListener(GetExit);
        optionButton.onClick.AddListener(optionMenuUI.OpenMainMenu);
    }

    private void OnDisable()
    {
        SoundPlayer.Instance.StopBGM();
    }
    

    IEnumerator CoMainSceneTimer()
    {
        while(timer < sceneStartTime)
        {
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        while (canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += 0.02f;
            //backgroundImage.color = color;
            yield return new WaitForFixedUpdate();
        }

        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void GetStart()
    {
        SoundPlayer.Instance.PlaySound(SoundType_Effect.Effect_UISound);
        SceneManager.LoadScene("StageSelect");
    }

    private void GetExit()
    {
        SoundPlayer.Instance.PlaySound(SoundType_Effect.Effect_UISound);
        Application.Quit();
    }
}
