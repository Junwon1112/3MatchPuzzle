using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class GameBoard_TopUI : MonoBehaviour
{
    Slider timer;

    [SerializeField]
    float limitTime = 60.0f;

    float remainTime;

    public float RemainTime
    {
        get { return remainTime; }
        set 
        {
            if(value <= limitTime && value >= 0)
            {
                remainTime = value;
            }
            else if(value > limitTime)
            {
                remainTime = limitTime;
            }
            else
            {
                remainTime = 0;
            }
             
        }
    }

    GameBoard gameBoard;
    GameBoard_BottomUI gameBoard_BottomUI;
    GameOver_UI gameOver_UI;
    NewRecordUI newRecordUI;

    bool isNewRecord = false;

    const int NO_RANK = 9999;

    private void Awake()
    {
        timer = GetComponentInChildren<Slider>();
        RemainTime = limitTime;
        gameBoard = FindObjectOfType<GameBoard>();
        gameBoard_BottomUI = FindObjectOfType<GameBoard_BottomUI>();
        gameOver_UI = FindObjectOfType<GameOver_UI>();
        newRecordUI = FindObjectOfType<NewRecordUI>();

        SoundPlayer.Instance.PlayBGM((SoundType_BGM)GameManager.instance.currentStage);
    }

    private void Start()
    {
        timer.value = RemainTime / limitTime;
    }

    private void Update()
    {
        if(RemainTime > 0)
        {
            Timer();
        }
    }

    private void Timer()
    {
        RemainTime -= Time.deltaTime;
        timer.value = RemainTime / limitTime;

        if(RemainTime <= 0)
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        gameBoard_BottomUI.UseSkillEnd();
        gameOver_UI.CanvasGroupOnOff();

        int scoreRank = NO_RANK;
        isNewRecord = GameManager.instance.IsTakeNewRecord(GameManager.instance.Score, out scoreRank);
        newRecordUI.endRank = scoreRank;
        newRecordUI.endScore = GameManager.instance.Score;

        if (isNewRecord)
        {
            newRecordUI.CanvasGroupOnOff();
            newRecordUI.rank_Text.text = scoreRank.ToString();
            newRecordUI.score_Text.text = GameManager.instance.Score.ToString();

            int currentStageIndex = GameManager.instance.currentSceneIndex - 3;
            if(GameManager.instance.Score > GameManager.instance.unLockStageRequire && currentStageIndex < 3)
            {
                GameManager.instance.isStageClear[currentStageIndex + 1] = true;
            }

        }
        else
        {
            newRecordUI.SetDataAsGameManager();
        }
    }
}
