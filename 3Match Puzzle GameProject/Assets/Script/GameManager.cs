using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/*
�ʿ��� ��ҵ�
1. ���� üũ - ���� �Ŵ���
 1.1 Ÿ�Ϻ��� ������ �ٸ����ϰų� Ư�� Ÿ���� ���� �ٸ� ������ �ο��ϸ� �������
2. �ð� üũ - ���� �Ŵ���
 2.1 �ð��� Ÿ���� �Ƹ� ������ �þ���� �ϸ� ���� �� ����.

3. �������� Ÿ�� ��ġ - ���� ����
 3.1 ó�� ������ �����ϸ� ��ü Ÿ�Ͽ��� ��Ī�Ǵ°� �ִ��� Ȯ�� �� ����
4. �Ű��� �� �ϳ� �̻� ��Ī�� Ÿ���� �����ϴ��� üũ - ���� ����
 4.1. 3��ġ �̿ܿ��� 4���̻� ��ġ�� Ư���� ����� ��ġ�� üũ�� �� ������ ���� �� ����
+ �巡���� ������ ��Ī�� �ȴٸ� Ÿ�� ���� + ���� + �ణ�� �ð�

-�巡�� �Ǵ� �� Ÿ���� �̵� - ���
5. ��ġ��ũ������ Ŭ���ؼ� �����¿�� �巡�� ���� �Ǵ��ϰ� �巡��, 
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
        //������ �ƴ� ��Ÿ���� �ޱ����� �迭���� �ϳ��� ����
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

