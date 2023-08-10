using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class LoadingUI : MonoBehaviour
{
    Input input;

    TextMeshProUGUI loadingText;

    const int MAXNUM = 10000;
    string loadingText_Basic = "Loading";

    Slider loadingBar;

    AsyncOperation async;

    float loadingProgress = 0.0f;

    bool loadingCompleted = false;

    /// <summary>
    /// PrintLoading 코루틴을 외부에서 멈추게 하기 위한 전역 변수
    /// </summary>
    IEnumerator PrintLoadingText;

    private void Awake()
    {
        loadingText = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        loadingBar = GetComponentInChildren<Slider>();
        input = new Input();   
    }


    private void Start()
    {
        input.LoadingUI.Enable();
        input.LoadingUI.GoToNextScene.performed += OnGoToNextStage;

        PrintLoadingText = PrintLoading();
        StartCoroutine(PrintLoadingText);

        StartCoroutine(LoadScene());
    }

    private void OnDisable()
    {
        input.LoadingUI.GoToNextScene.performed -= OnGoToNextStage;
        input.LoadingUI.Disable();
    }


    /// <summary>
    /// 클릭하면 다음 스테이지로 넘어가지는 함수
    /// </summary>
    /// <param name="obj"></param>
    private void OnGoToNextStage(InputAction.CallbackContext obj)
    {
        if (loadingCompleted)
        {
            async.allowSceneActivation = true;
        }
    }

    /// <summary>
    /// 로딩 텍스트를 바꾸는 코루틴
    /// </summary>
    /// <returns></returns>
    IEnumerator PrintLoading()
    {
        for(int i = 0; i < MAXNUM; i++)
        {
            loadingText.text = loadingText_Basic;
            loadingText_Basic += ".0";
            i %= 4;

            if(i == 0)
            {
                loadingText_Basic = "Loading";
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator LoadScene()
    {
        async = SceneManager.LoadSceneAsync(GameManager.instance.currentSceneIndex + 1);
        async.allowSceneActivation = false;

        float tempNum;

        while(loadingProgress != 1.0)
        {
            tempNum = loadingProgress;
            loadingProgress = Mathf.Lerp(tempNum, async.progress + 0.1f, 0.1f);

            if(loadingProgress > 0.9f)
            {
                loadingProgress = 1.0f;
            }
            loadingBar.value = loadingProgress;

            yield return null;
        }

        loadingCompleted = true;
        StopCoroutine(PrintLoadingText);

        loadingText.text = "Please Click The Button";

    }
}
