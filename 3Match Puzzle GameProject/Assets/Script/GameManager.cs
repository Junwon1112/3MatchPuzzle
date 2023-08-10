using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/*
필요한 요소들
1. 점수 체크 - 게임 매니저
 1.1 타일별로 점수를 다르게하거나 특수 타일을 만들어서 다른 점수를 부여하면 재밌을듯
2. 시간 체크 - 게임 매니저
 2.1 시간은 타일을 꺠면 일정량 늘어나도록 하면 좋을 것 같다.

3. 랜덤으로 타일 배치 - 게임 보드
 3.1 처음 게임을 시작하면 전체 타일에서 매칭되는게 있는지 확인 후 생성
4. 옮겼을 때 하나 이상 매칭될 타일이 존재하는지 체크 - 게임 보드
 4.1. 3매치 이외에도 4개이상 매치나 특이한 모양의 매치도 체크할 수 있으면 좋을 것 같음
+ 드래그한 곳에서 매칭이 된다면 타일 삭제 + 점수 + 약간의 시간

-드래그 판단 및 타일의 이동 - 블록
5. 터치스크린으로 클릭해서 상하좌우로 드래그 한지 판단하고 드래그, 
*/
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    int score = 0;
    public int currentSceneIndex;

    public string[] rankerName = new string[3];
    public int[] scoreRecord = new int[3];

    public int[] stageBuildIndex;
    public bool[] isStageClear;

    public int unLockStageRequire = 500;

    public RankTable[] rankTable;

    public int currentStage;

    public int Score
    {
        get { return score; }
        set { score = value; }
    }




    FullScreenMode screenMode;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        GetCurrentScene();
        Initiate();


        screenMode = FullScreenMode.FullScreenWindow;
        Screen.SetResolution(325, 400, screenMode, 60);
    }

    private void OnLevelWasLoaded(int level)
    {
        GetCurrentScene();
        Score = 0;
    }

    private void Initiate()
    {
        for (int i = 0; i < rankerName.Length; i++)
        {
            rankerName[i] = "Not Recorded";
            scoreRecord[i] = 0;
        }

        rankTable = new RankTable[stageBuildIndex.Length];

        isStageClear = new bool[stageBuildIndex.Length];
        for (int i = 0; i < stageBuildIndex.Length; i++)
        {
            string[] tempName = new string[3] { "No Record", "No Record", "No Record" };
            int[] tempRecord = new int[3] { 0, 0, 0};

            rankTable[i].rankName = tempName;
            rankTable[i].rankScore = tempRecord;

            if (i != 0)
            {
                isStageClear[i] = false;
            }
            else
            {
                isStageClear[i] = true;
            }
        }

        

    }

    private void GetCurrentScene()
    {
        currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
    }

    public bool IsTakeNewRecord(int score, out int rank)
    {
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            if (score > scoreRecord[index])
            {
                rank = i + 1;

                int tempScore1 = score;
                string tempName1 = " ";
                while (index < 3)
                {
                    int tempScore2 = scoreRecord[index];
                    string tempName2 = rankerName[index];


                    scoreRecord[index] = tempScore1;
                    rankerName[index] = tempName1;
                    tempScore1 = tempScore2;
                    tempName1 = tempName2;

                    index++;
                }
                SetStageRecordToTempRecord(currentStage - 1);
                return true;
            }
        }
        rank = 9999;
        return false;
    }

    public void SetTempRecordToStageRecord(int stageIndex)
    {
        rankerName = rankTable[stageIndex].rankName;
        scoreRecord = rankTable[stageIndex].rankScore;
    }

    public void SetStageRecordToTempRecord(int stageIndex)
    {
        //참조가 아닌 값타입을 받기위해 배열값을 하나씩 지정
        for(int i = 0; i< 3; i++)
        {
            rankTable[stageIndex].rankName[i] = rankerName[i];
            rankTable[stageIndex].rankScore[i] = scoreRecord[i];
        }
    }


}

public struct RankTable
{
    public int[] rankScore;
    public string[] rankName;

    public RankTable( string[] rankName, int[] rankScore)
    {
        this.rankName = rankName;
        this.rankScore = rankScore;
    }
}

