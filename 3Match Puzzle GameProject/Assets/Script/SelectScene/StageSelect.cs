using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class StageSelect : MonoBehaviour
{
    Button[] stageSelectButtons;
    Image[] lockImages;

    public float setLockImage_Time = 2.0f;

    private void Awake()
    {
        stageSelectButtons = GetComponentsInChildren<Button>();

        lockImages = new Image[stageSelectButtons.Length];
    }
    private void OnLevelWasLoaded(int level)
    {
        for (int i = 0; i < stageSelectButtons.Length; i++)
        {
            int index = i;

            lockImages[i] = transform.GetChild(i).GetChild(4).GetComponent<Image>();

            if (GameManager.instance.isStageClear[i])
            {
                lockImages[i].color = Color.clear;
            }

        }
        SoundPlayer.Instance.StopBGM();
    }

    void Start()
    {
        for (int i = 0; i < stageSelectButtons.Length; i++)
        {
            int index = i;
            stageSelectButtons[index].onClick.AddListener(() => 
            {
                ClickStageSelect(index ,GameManager.instance.stageBuildIndex[index]); 
            });

            //lockImages[i] = transform.GetChild(i).GetChild(4).GetComponent<Image>();

            //if (GameManager.instance.isStageClear[i])
            //{
            //    lockImages[i].color = Color.clear;
            //}
            
        }
    }


    private void ClickStageSelect(int stageIndex ,int buildIndex)
    {
        SoundPlayer.Instance.PlaySound(SoundType_Effect.Effect_UISound);
        if (GameManager.instance.isStageClear[stageIndex])
        {
            GameManager.instance.currentStage = stageIndex + 1;
            GameManager.instance.SetTempRecordToStageRecord(stageIndex);
            SceneManager.LoadScene(buildIndex);
        }
        else
        {
            StartCoroutine(CoSetLockImage(stageIndex));
        }
    }

    IEnumerator CoSetLockImage(int stageIndex)
    {
        float timer = 0;
        Color color = new Color(lockImages[stageIndex].color.r, lockImages[stageIndex].color.g, lockImages[stageIndex].color.b, lockImages[stageIndex].color.a);
        float blinkSpeed = 2.0f; 
        while (timer < setLockImage_Time)
        {
            //timer += Time.deltaTime;
            timer += Time.fixedDeltaTime;
            if (Mathf.FloorToInt(timer * blinkSpeed) % 2 == 0)
            {
                //color.a -= Time.deltaTime;
                color.a -= Time.fixedDeltaTime * blinkSpeed;
            }
            else
            {
                //color.a += Time.deltaTime;
                color.a += Time.fixedDeltaTime * blinkSpeed;
            }
            lockImages[stageIndex].color = color;

            yield return new WaitForFixedUpdate();
        }
        color.a = 1.0f;
        lockImages[stageIndex].color = color;
    }
}
