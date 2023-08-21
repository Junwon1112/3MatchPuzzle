using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System;

public class GameBoard_BottomUI : MonoBehaviour
{
    TextMeshProUGUI score_Text;
    TextMeshProUGUI score_NumText;

    Button bombButton;
    Button crossButton;

    TextMeshProUGUI bomb_Num;
    TextMeshProUGUI cross_Num;

    GameBoard gameBoard;
    GameBoard_TopUI gameBoard_TopUI;

    //int score;

    int currentScore = 0;
    int initScore = 0;

    int bombCount;
    int crossCount;

    bool isSelecting;

    Vector2 mousePosition_World = Vector2.zero;
    Vector2 mousePosition_blockCenter_World = Vector2.zero;

    [SerializeField]
    float scoreChangeSpeed = 0.03f;

    new Camera camera;

    [SerializeField]
    int bombRange = 1;
    [SerializeField]
    int bombScore_EachBlock = 10;
    [SerializeField]
    int crossScore_EachBlock = 10;
    [SerializeField]
    float bombAddTime = 4.0f;
    [SerializeField]
    float crossAddTime = 4.0f;

    public const int BOMB = 1000;
    public const int CROSS = 2000;
    public const int NONE = 0;

    int skillType = NONE;

    int skillScore_Bomb;
    int skillScore_Cross;
    //public int Score
    //{
    //    get { return score; }
    //    set 
    //    {
    //        score = value; 
    //    }
    //}

    private void Awake()
    {
        score_Text = transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
        score_NumText = transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>();

        bombButton = transform.GetChild(1).GetChild(0).GetComponentInChildren<Button>();
        crossButton = transform.GetChild(1).GetChild(1).GetComponentInChildren<Button>();

        bomb_Num = transform.GetChild(1).GetChild(0).GetComponentInChildren<TextMeshProUGUI>();
        cross_Num = transform.GetChild(1).GetChild(1).GetComponentInChildren<TextMeshProUGUI>();

        gameBoard = FindObjectOfType<GameBoard>();
        gameBoard_TopUI = FindObjectOfType<GameBoard_TopUI>();

        SetScoreNumText(initScore);
        camera = FindObjectOfType<Camera>();
    }

    private void OnLevelWasLoaded(int level)
    {
        skillScore_Bomb = 0;
        skillScore_Cross = 0;

        bombCount = 1;
        crossCount = 1;

        SetSkillNumText(bombCount, crossCount);
    }

    private void Start()
    {
        bombButton.onClick.AddListener(ClickBombButton);
        crossButton.onClick.AddListener(ClickCrossButton);
    }

    public void SetScoreNumText(int addScore)
    {
        GameManager.instance.Score += addScore;
        skillScore_Bomb += addScore;
        skillScore_Cross += addScore;
        CheckGetSkill_Score();
        StartCoroutine(CoScoreChange());
    }

    public void SetSkillNumText(int bombCount, int crossCount)
    {
        bomb_Num.text = bombCount.ToString();
        cross_Num.text = crossCount.ToString();
    }

    IEnumerator CoScoreChange()
    {
        if(GameManager.instance.Score > currentScore)
        {
            while(GameManager.instance.Score > currentScore)
            {
                currentScore++;
                score_NumText.text = currentScore.ToString();
                yield return new WaitForSeconds(scoreChangeSpeed);
            }
        }
        else    //점수를 빼는 건 다시 계산하는 과정이고 점수가 같은 건 계산할 필요가 없으므로 바로 변환
        {
            currentScore = GameManager.instance.Score;
            score_NumText.text = currentScore.ToString();
        }
    }

    private void ClickBombButton()
    {
        if(gameBoard.isChangeEnd && gameBoard.IsBlockMovingEnd() && gameBoard.IsDestroyAnimEnd() && bombCount >0)
        {
            gameBoard.input.Control.Disable();
            gameBoard.input.UsingSkill.Enable();
            gameBoard.input.UsingSkill.Click.canceled += OnClick_When_SkillUsing;

            skillType = BOMB;
            Cursor.SetCursor(CursorManager.Instance.findCursorImage, new Vector2(5, 5), CursorMode.Auto);
            StartCoroutine(CoSelectingBlock_ForSkill());
            //Mouse.current.position

        }
    }


    private void ClickCrossButton()
    {
        if (gameBoard.isChangeEnd && gameBoard.IsBlockMovingEnd() && gameBoard.IsDestroyAnimEnd() && crossCount > 0)
        {
            gameBoard.input.Control.Disable();
            gameBoard.input.UsingSkill.Enable();
            gameBoard.input.UsingSkill.Click.canceled += OnClick_When_SkillUsing;

            skillType = CROSS;
            Cursor.SetCursor(CursorManager.Instance.findCursorImage, new Vector2(5, 5), CursorMode.Auto);
            StartCoroutine(CoSelectingBlock_ForSkill());
        }
    }

    private void OnClick_When_SkillUsing(InputAction.CallbackContext obj)
    {
        SetMousePos();
        RaycastHit2D hit;
        hit = Physics2D.Raycast(mousePosition_blockCenter_World, transform.forward, GameBoard.MAXDISTANCE);
        if(hit.collider != null)
        {
            if(skillType == BOMB)
            {
                bombCount -= 1;
                SetSkillNumText(bombCount, crossCount);

                BlockObject targetBlock = hit.transform.GetComponent<BlockObject>();
                uint arrayIndex_Low = (uint)targetBlock.blockID / GameBoard.COLUMN_NUM;
                uint arrayIndex_Column = (uint)targetBlock.blockID % GameBoard.COLUMN_NUM;

                gameBoard.UnCheckDestroyIntended();
                for(int i = 0; i < bombRange * 2 + 1; i++)
                {
                    for (int j = 0; j < bombRange * 2 + 1; j++)
                    {
                        if(arrayIndex_Low - bombRange + i > -1 && arrayIndex_Low - bombRange + i < GameBoard.LOW_NUM_VISIBLE
                            && arrayIndex_Column - bombRange + j > -1 && arrayIndex_Column - bombRange + j < GameBoard.COLUMN_NUM)
                        {
                            gameBoard.gameBoard_Blocks[arrayIndex_Low - bombRange + i, arrayIndex_Column - bombRange + j].isDestroyIntended_Low = true;
                            SetScoreNumText(bombScore_EachBlock);
                            

                        } 
                    }
                }
                StartCoroutine(gameBoard.DestroyAnd_CheckMatch_AndDestroy_All());
                gameBoard_TopUI.RemainTime += bombAddTime;
            }
            else if(skillType == CROSS)
            {
                crossCount -= 1;
                SetSkillNumText(bombCount, crossCount);

                BlockObject targetBlock = hit.transform.GetComponent<BlockObject>();
                uint arrayIndex_Low = (uint)targetBlock.blockID / GameBoard.COLUMN_NUM;
                uint arrayIndex_Column = (uint)targetBlock.blockID % GameBoard.COLUMN_NUM;

                gameBoard.UnCheckDestroyIntended();

                for (int m = 0; m < GameBoard.COLUMN_NUM; m++)
                {
                    if (gameBoard.gameBoard_Blocks[arrayIndex_Low, m] != null && !gameBoard.gameBoard_Blocks[arrayIndex_Low, m].isWall)
                    {
                        gameBoard.gameBoard_Blocks[arrayIndex_Low, m].isDestroyIntended_Low = true;

                    }
                }
                for (int n = 0; n < GameBoard.LOW_NUM_VISIBLE; n++)
                {
                    if (n != arrayIndex_Low && !gameBoard.gameBoard_Blocks[n, arrayIndex_Column].isWall) //[n, j]가 [i, j]일 떄는 위에서 이미 계산했으므로 빼고 계산
                    {
                        gameBoard.gameBoard_Blocks[n, arrayIndex_Column].isDestroyIntended_Low = true;

                    }

                }
                gameBoard_TopUI.RemainTime += crossAddTime;
                SetScoreNumText(crossScore_EachBlock);
                StartCoroutine(gameBoard.DestroyAnd_CheckMatch_AndDestroy_All());

            }
        }


        UseSkillEnd();
    }

    private void SetMousePos()
    {
        const float COMPENSATE_NUM = 0.5f;

        mousePosition_World = camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());


        mousePosition_blockCenter_World.x = Mathf.Floor(mousePosition_World.x) + COMPENSATE_NUM;
        mousePosition_blockCenter_World.y = Mathf.Floor(mousePosition_World.y) + COMPENSATE_NUM;
    }

    IEnumerator CoSelectingBlock_ForSkill()
    {
        isSelecting = true;

        SetMousePos();
        GameObject selelctBlock = Instantiate(gameBoard.selectBlock, (Vector3)mousePosition_blockCenter_World, transform.rotation, gameBoard.transform);

        while (isSelecting)
        {
            yield return null;

            SetMousePos();
            selelctBlock.transform.position = mousePosition_blockCenter_World;
        }
        Destroy(selelctBlock);
    }

    public void UseSkillEnd()
    {
        Cursor.SetCursor(CursorManager.Instance.defaultCursorImage, new Vector2(5, 5), CursorMode.Auto);
        gameBoard.input.Control.Enable();
        gameBoard.input.UsingSkill.Disable();
        gameBoard.input.UsingSkill.Click.performed -= OnClick_When_SkillUsing;
        isSelecting = false;
    }

    private void CheckGetSkill_Score()
    {
        if(skillScore_Bomb >= 1000)
        {
            skillScore_Bomb -= 1000;
            bombCount += 1;
        }

        if(skillScore_Cross >= 1500)
        {
            skillScore_Cross -= 1500;
            crossCount += 1;
        }
        SetSkillNumText(bombCount, crossCount);
    }

}
