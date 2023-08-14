using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GameBoard : MonoBehaviour
{
    //게임 보드 : UI, Top:40, Bottom:80, object: 0, 0.5, scale 7, 8
    //게임 오브젝트 위치 : -3.5 , -2.5 ~ 3.5, 3.5 (8 X 7), vector2값:  (0~340) X (85 ~ 375)
    public Input input;
    public int currentStage;

    [SerializeField]
    GameObject[] objs; //=> 타일 종류
    [SerializeField]
    GameObject[] wall; //=> 타일 종류
    [SerializeField]
    int[] wallPosId;   //=> 벽 위치 (0 ~ 55 (column X low_visable) )
    public GameObject selectBlock;  // => 선택했을 때 이미지

    GameBoard_TopUI gameBoard_TopUI;
    GameBoard_BottomUI gameBoard_BottomUI;

    //참고로 상수는 자동으로 static이되서 인스턴스로 호출하지 않는다.
    public const int COLUMN_NUM = 8;
    public const int LOW_NUM_VISIBLE = 7;      //보드의 보이는 부분
    public const int LOW_NUM_TOTAL = 25;        //보드의 안보이는 부분
    public int total_Block;

    Vector2[,] blockPos;        //blockPos[Low, Column]
    public BlockObject[,] gameBoard_Blocks;    //gameBoard[Low, Column]
    public BlockObject[,] gameBoard_Blocks_CheckBatch;
    int [,] gameBoard_Id;    //gameBoard[Low, Column]

    List<BlockObject> gameBoardDestroyList = new List<BlockObject>();

    Vector2 mousePos_Drag_Start;
    Vector2 mousePos_Drag_End;

    Vector3 mousePos_World;

    bool isDraging = false;

    Camera cam;
    public const float MAXDISTANCE = 15.0f;
    RaycastHit2D hit;

    public bool isChangeEnd = true;
    bool[,] isNewBlocksMoveEnd = new bool[LOW_NUM_TOTAL, COLUMN_NUM];
    bool[] isLineMoveEnd = new bool[COLUMN_NUM];
    bool[] isLineMoveIntended = new bool[COLUMN_NUM];
    public bool[,] isDestroyAnimEnd = new bool[LOW_NUM_TOTAL, COLUMN_NUM];

    [SerializeField]
    float blockMoveSpeed = 0.1f;

    [SerializeField]
    int[] matchingScore = new int[8];
    float addTime = 2.0f;

    

    private void Awake()
    {
        input = new Input();
        currentStage = GameManager.instance.currentSceneIndex - 2;
        total_Block = COLUMN_NUM * LOW_NUM_TOTAL;
        cam = FindObjectOfType<Camera>();
        gameBoard_TopUI = FindObjectOfType<GameBoard_TopUI>();
        gameBoard_BottomUI = FindObjectOfType<GameBoard_BottomUI>();

        for (int i = 0; i < LOW_NUM_TOTAL; i++)
        {
            for(int j = 0; j < COLUMN_NUM; j++)
            {
                isNewBlocksMoveEnd[i, j] = true;
                isDestroyAnimEnd[i, j] = true;
                isLineMoveEnd[j] = true;
                isLineMoveIntended[j] = false;

            }
        }
    }

    private void OnEnable()
    {
        input.Enable();
        input.Control.Click.started += OnClick_Start;
        input.Control.Click.canceled += OnClick_End;
        //input.Control.Drag.started += OnDrag_Start;
        //input.Control.Drag.performed += OnDrag_End;
    }


    private void OnDisable()
    {
        //input.Control.Drag.performed -= OnDrag_End;
        //input.Control.Click.started -= OnDrag_Start;
        input.Control.Click.canceled += OnClick_End;
        input.Control.Click.performed -= OnClick_Start;
        input.Disable();
    }

    private void Start()
    {
        SetBlockPos();
        gameBoard_Blocks = new BlockObject[LOW_NUM_TOTAL, COLUMN_NUM];
        //BlockBatch_All();
        //StartCoroutine(CheckMatchAndDestroy_All());
        do
        {
            BlockBatch_All();
            StartCoroutine(CheckMatchAndDestroy_All());
        } while (!IsBlockCanMatch());
    }
    private void OnClick_Start(InputAction.CallbackContext obj)
    {
        if (isChangeEnd && IsBlockMovingEnd() && IsDestroyAnimEnd())
        {
            Vector2 mousePos_Start = Mouse.current.position.ReadValue();
            mousePos_Drag_Start = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            hit = Physics2D.Raycast(mousePos_Drag_Start, transform.forward, MAXDISTANCE);
        }
    }

    private void OnClick_End(InputAction.CallbackContext obj)
    {
        if (isChangeEnd && IsBlockMovingEnd() && IsDestroyAnimEnd())
        {
            Vector2 mousePos_End = Mouse.current.position.ReadValue();
            mousePos_Drag_End = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            if (hit.collider != null)   //onClick_Start에서 레이캐스트로 할당해줬음
            {
                BlockObject block = hit.transform.GetComponent<BlockObject>();
                int index_I = block.blockID / COLUMN_NUM;
                int index_J = block.blockID % COLUMN_NUM;

                FourDirection dir_ToMove = CheckDirection(mousePos_Drag_Start, mousePos_Drag_End);
                Debug.Log($"{dir_ToMove}");
                if( ChangeBlock(index_I, index_J, dir_ToMove))
                {
                    StartCoroutine(CoWaitChange_Return(IsChangeBlockMatch_CheckMatch, index_I, index_J, dir_ToMove));
                }

            }
        }

    }

    //리턴타입의 함수를 파라미터로 받는 코루틴
    IEnumerator CoWaitChange_Return(System.Func<int, int, FourDirection, bool> ChangeBlock_Match, int i, int j, FourDirection fourDirection)
    {
        while (!isChangeEnd)
        {
            yield return null;
        }
        //위치바꾼 후 매치 확인 뒤 블록을 되돌릴지 결정
        if(ChangeBlock_Match(i, j, fourDirection))
        {
            DestroyIntendedBlocks_AfterMatch(i, j, fourDirection);
            //isDestroyAnimEnd = new bool[LOW_NUM_TOTAL, COLUMN_NUM];

            bool isBlockDestroyEnd = false;

            while (!isBlockDestroyEnd)
            {
                for (int m = 0; m < LOW_NUM_VISIBLE; m++)
                {
                    for (int n = 0; n < COLUMN_NUM; n++)
                    {
                        if (!isDestroyAnimEnd[m, n])        //얘는 블록 파괴애니메이션이 끝나면 true가 된다
                        {
                            isBlockDestroyEnd = false;
                            break;
                        }
                        else if (isDestroyAnimEnd[LOW_NUM_VISIBLE - 1, COLUMN_NUM - 1])
                        {
                            isBlockDestroyEnd = true;
                        }
                    }
                    //2중 for문 탈출
                    if (!isBlockDestroyEnd)
                    {
                        break;
                    }
                }


                yield return null;
            }

            CheckDestroyAndBlockDown();

            bool isLineBlockMoveEnd = false;

            while (!isLineBlockMoveEnd)
            {
                for (int m = 0; m < LOW_NUM_TOTAL; m++)
                {
                    for (int n = 0; n < COLUMN_NUM; n++)
                    {
                        if (!isNewBlocksMoveEnd[m, n])
                        {
                            isLineBlockMoveEnd = false;
                            break;
                        }
                        else if (isNewBlocksMoveEnd[LOW_NUM_TOTAL - 1, COLUMN_NUM - 1])
                        {
                            isLineBlockMoveEnd = true;
                        }
                    }
                    //2중 for문 탈출
                    if (!isLineBlockMoveEnd)
                    {
                        break;
                    }
                }


                yield return null;
            }

            StartCoroutine(CheckMatchAndDestroy_All());
        }
        else
        {
            ChangeBlock(i, j, fourDirection);
        }
    }

    //Void타입의 함수를 파라미터로 받는 코루틴

    //IEnumerator CoWaitMove_Void(System.Action<int, int , FourDirection> Destroy_Block, int i, int j, FourDirection fourDirection)
    //{
    //    while(!isMoveEnd)
    //    {
    //        yield return null;
    //    }

    //    Destroy_Block(i, j, fourDirection);
    //}

    //블록이 있어야 할 위치 설정
    private void SetBlockPos()
    {
        blockPos = new Vector2[LOW_NUM_TOTAL, COLUMN_NUM];
        gameBoard_Id = new int[LOW_NUM_TOTAL, COLUMN_NUM];
        for ( int i = 0; i < LOW_NUM_TOTAL; i++ )
        {
            for( int j = 0; j < COLUMN_NUM; j++ )
            {
                const float COMPENSATE_NUM = 0.5f;
                Vector2 compensateVector = new Vector2(COMPENSATE_NUM, COMPENSATE_NUM);

                blockPos[i, j] = new Vector2Int(-4 + j, -3 + i) + compensateVector;
                gameBoard_Id[i, j] = j + (i * COLUMN_NUM); 
            }
        }
    }

    //전체 블럭 배치
    private void BlockBatch_All()
    {
        for (int i = 0; i < wallPosId.Length; i++)
        {
            WallBlockInstantiate(wallPosId[i] / COLUMN_NUM, wallPosId[i] % COLUMN_NUM);
        }

        for (int i = 0; i < total_Block; i++)
        {
            if (gameBoard_Blocks[i / COLUMN_NUM, i % COLUMN_NUM] == null)
            {
                RandomBlockInstantiate(i / COLUMN_NUM, i % COLUMN_NUM);
            }
            else if(!gameBoard_Blocks[i / COLUMN_NUM, i % COLUMN_NUM].isWall)
            {
                RandomBlockInstantiate(i / COLUMN_NUM, i % COLUMN_NUM);
            }
        }
    }

    private void WallBlockInstantiate(int i, int j)
    {
        GameObject obj = Instantiate(wall[0], (Vector3)blockPos[i, j], transform.rotation, transform.GetChild(1));
        gameBoard_Blocks[i, j] = obj.transform.GetComponentInChildren<BlockObject>();
        gameBoard_Blocks[i, j].blockID = (i * COLUMN_NUM) + j;
    }

        //랜덤한 블럭을 원하는 위치에 생성
    private void RandomBlockInstantiate(int i, int j)
    {
        int randNum = Random.Range(0, objs.Length * 4) % objs.Length;

        GameObject obj = Instantiate(objs[randNum], (Vector3)blockPos[i, j], transform.rotation, transform.GetChild(1));
        gameBoard_Blocks[i, j] = obj.transform.GetComponentInChildren<BlockObject>();
        gameBoard_Blocks[i, j].blockID = (i * COLUMN_NUM) + j;
    }

    //맨위에서 아래로 계속된 빈칸이 모두 채워질 때 까지 블럭 생성
    private void RandomBlockInstantiate_Top(int index_J)
    {
        while (gameBoard_Blocks[LOW_NUM_TOTAL-1, index_J] == null)
        {
            RandomBlockInstantiate(LOW_NUM_TOTAL - 1, index_J);
            MoveBlockDown_More(LOW_NUM_TOTAL - 1, index_J);

            //재귀 형태
            //RandomBlockInstantiate_Top(index_J);
        }
    }
    //매칭 가능한 블록이 있는지 확인
    private bool IsBlockCanMatch()
    {
        bool isBlockCanMatch = false;

        for(int i = 0; i < LOW_NUM_VISIBLE; i++)
        {
            for(int j = 0; j < COLUMN_NUM; j++)
            {
                if (j < COLUMN_NUM - 1 && !gameBoard_Blocks[i, j].isWall && !gameBoard_Blocks[i, j + 1].isWall)
                {
                    ChangeBlock_Data(i, j, i, j + 1);
                    isBlockCanMatch = IsChangeBlockMatch_CheckMatch(i, j, FourDirection.East);
                    if (isBlockCanMatch)
                    {
                        ChangeBlock_Data(i, j, i, j + 1);
                        return isBlockCanMatch;
                    }
                    else
                    {
                        ChangeBlock_Data(i, j, i, j + 1);
                    }

                }
            }
        }

        for (int i = 0; i < LOW_NUM_VISIBLE; i++)
        {
            for (int j = 0; j < COLUMN_NUM; j++)
            {
                if (i < LOW_NUM_VISIBLE - 1 && !gameBoard_Blocks[i, j].isWall && !gameBoard_Blocks[i + 1, j].isWall)
                {
                    ChangeBlock_Data(i, j, i + 1, j);
                    isBlockCanMatch = IsChangeBlockMatch_CheckMatch(i, j, FourDirection.North);
                    if (isBlockCanMatch)
                    {
                        ChangeBlock_Data(i, j, i + 1, j);
                        return isBlockCanMatch;
                    }
                    else
                    {
                        ChangeBlock_Data(i, j, i + 1, j);
                    }

                }
            }
        }

        return isBlockCanMatch;
    }

    //블럭 재생성 후 다시 매칭 및 파괴하는 일련의 과정
    IEnumerator CheckMatchAndDestroy_All()
    {
        //매칭 후 새로 생성된 블럭도 매칭이 안될때까지 반복
        while(CheckMatchBlock_All())
        {
            DestroyIntendedBlocks_All();
            while (!IsDestroyAnimEnd())
            {
                yield return null;
            }

            CheckDestroyAndBlockDown();
            while(!IsBlockMovingEnd())
            {
                yield return null;
            }
        }
        
    }

    public IEnumerator DestroyAnd_CheckMatch_AndDestroy_All()
    {
        //매칭 후 새로 생성된 블럭도 매칭이 안될때까지 반복
        do
        {
            DestroyIntendedBlocks_All();
            while (!IsDestroyAnimEnd())
            {
                yield return null;
            }

            CheckDestroyAndBlockDown();
            while (!IsBlockMovingEnd())
            {
                yield return null;
            }
        } while (CheckMatchBlock_All());

    }

    public bool IsBlockMovingEnd()
    {
        bool isLineBlockMoveEnd = true;
        for (int i = 0; i < LOW_NUM_TOTAL; i++)
        {
            for (int j = 0; j < COLUMN_NUM; j++)
            {
                if (!isNewBlocksMoveEnd[i, j])
                {
                    isLineBlockMoveEnd = false;
                    break;
                }
                else if (isNewBlocksMoveEnd[LOW_NUM_TOTAL - 1, COLUMN_NUM - 1])
                {
                    isLineBlockMoveEnd = true;
                }
            }
            //2중 for문 탈출
            if (!isLineBlockMoveEnd)
            {
                break;
            }
        }

        return isLineBlockMoveEnd;
    }

    public bool IsDestroyAnimEnd()
    {
        bool isAnimEnd = true;
        for (int i = 0; i < LOW_NUM_VISIBLE; i++)
        {
            for (int j = 0; j < COLUMN_NUM; j++)
            {
                if (!isDestroyAnimEnd[i, j])
                {
                    isAnimEnd = false;
                    break;
                }
                else if (isDestroyAnimEnd[LOW_NUM_TOTAL - 1, COLUMN_NUM - 1])
                {
                    isAnimEnd = true;
                }
            }
            //2중 for문 탈출
            if (!isAnimEnd)
            {
                break;
            }
        }

        return isAnimEnd;
    }
    //---------------------------------------------------블럭 매칭 체크 메서드

    //보이는 곳만 매칭
    private bool CheckMatchBlock_All()
    {
        bool isMatchBlock_Exist = false;

        for(int i = 0; i < LOW_NUM_VISIBLE; i++)
        {
            for(int j = 0; j < COLUMN_NUM; j++)
            {
                if(CheckMatch(i, j))
                {
                    isMatchBlock_Exist = true;
                }
            }
        }
        return isMatchBlock_Exist;
    }

    private bool IsChangeBlockMatch_CheckMatch(int i, int j, FourDirection fourDirection)
    {
        bool isMatchBlock_Exist_1 = false;
        bool isMatchBlock_Exist_2 = false;

        isMatchBlock_Exist_1 = CheckMatch(i, j);

        switch (fourDirection)
        {
            case FourDirection.East:
                isMatchBlock_Exist_2 = CheckMatch(i, j + 1);
                break;
            case FourDirection.West:
                isMatchBlock_Exist_2 = CheckMatch(i, j - 1);
                break;
            case FourDirection.South:
                isMatchBlock_Exist_2 = CheckMatch(i - 1, j);
                break;
            case FourDirection.North:
                isMatchBlock_Exist_2 = CheckMatch(i + 1, j);
                break;
            default:
                break;
        }

        if(isMatchBlock_Exist_1 || isMatchBlock_Exist_2)
        {
            return true;
        }
        return false;
    }


    private bool CheckMatch(int i, int j)
    {
        bool isMatchBlock_Exist = false;

        int checkSame_LowLine = 1;
        int currentX_Value_Minus = j - 1;
        int currentX_Value_Plus = j + 1;

        gameBoardDestroyList.Add(gameBoard_Blocks[i, j]);

        //i,j기준 왼쪽체크
        while (currentX_Value_Minus >= 0 && gameBoard_Blocks[i, j].blockTypeID == gameBoard_Blocks[i, currentX_Value_Minus].blockTypeID && !gameBoard_Blocks[i, currentX_Value_Minus].isWall)
        {
            gameBoardDestroyList.Add(gameBoard_Blocks[i, currentX_Value_Minus]);
            checkSame_LowLine++;
            currentX_Value_Minus--;
        }
        //i,j기준 오른쪽체크
        while (currentX_Value_Plus <= COLUMN_NUM - 1 && gameBoard_Blocks[i, j].blockTypeID == gameBoard_Blocks[i, currentX_Value_Plus].blockTypeID && !gameBoard_Blocks[i, currentX_Value_Plus].isWall)
        {
            gameBoardDestroyList.Add(gameBoard_Blocks[i, currentX_Value_Plus]);
            checkSame_LowLine++;
            currentX_Value_Plus++;
        }
        //체크된 애들이 3이상이면 해당 블록은 삭제 예정(blockObject.isDestroyIntended = true)
        if (checkSame_LowLine >= 3)
        {
            isMatchBlock_Exist = true;

            bool isDoubleCheck = false;
            //gameBoard_BottomUI.SetScoreNumText(matchingScore[checkSame_LowLine - 3]); 

            int doubleCheck_Num = 0;

            //리스트에있는 blockObject삭제할 예정으로 변경 및 겹치는지 체크
            foreach (BlockObject blockObject in gameBoardDestroyList)
            {
                if(!blockObject.isDestroyIntended_Low)
                {
                    blockObject.isDestroyIntended_Low = true;
                }
                else
                {
                    doubleCheck_Num++;
                    if(doubleCheck_Num > 1) 
                    {
                        isDoubleCheck = true;
                        //gameBoard_BottomUI.SetScoreNumText(-matchingScore[checkSame_LowLine - 3]);
                        break;
                    }
                }
            }

            if(!isDoubleCheck)
            {
                gameBoard_BottomUI.SetScoreNumText(matchingScore[checkSame_LowLine - 3]);
                gameBoard_TopUI.RemainTime += addTime;
            }
        }
        gameBoardDestroyList.Clear();

        int checkSame_ColumnLine = 1;
        int currentY_Value_Minus = i - 1;
        int currentY_Value_Plus = i + 1;

        gameBoardDestroyList.Add(gameBoard_Blocks[i, j]);

        //i,j기준 아래쪽체크
        while (currentY_Value_Minus >= 0 && gameBoard_Blocks[i, j].blockTypeID == gameBoard_Blocks[currentY_Value_Minus, j].blockTypeID && !gameBoard_Blocks[ currentY_Value_Minus, j].isWall)
        {
            gameBoardDestroyList.Add(gameBoard_Blocks[currentY_Value_Minus, j]);
            checkSame_ColumnLine++;
            currentY_Value_Minus--;
        }
        //i,j기준 위쪽체크
        while (currentY_Value_Plus <= LOW_NUM_VISIBLE - 1 && gameBoard_Blocks[i, j].blockTypeID == gameBoard_Blocks[currentY_Value_Plus, j].blockTypeID && !gameBoard_Blocks[ currentY_Value_Plus, j].isWall)
        {
            gameBoardDestroyList.Add(gameBoard_Blocks[currentY_Value_Plus, j]);
            checkSame_ColumnLine++;
            currentY_Value_Plus++;
        }
        //체크된 애들이 3이상이면 해당 블록은 삭제 예정(blockObject.isDestroyIntended = true)
        if (checkSame_ColumnLine >= 3)
        {
            isMatchBlock_Exist = true;
            bool isDoubleCheck = false;

            //gameBoard_BottomUI.SetScoreNumText(matchingScore[checkSame_ColumnLine - 3]);
            int doubleCheck_Num = 0;

            foreach (BlockObject blockObject in gameBoardDestroyList)
            {
                if (!blockObject.isDestroyIntended_Column)
                {
                    blockObject.isDestroyIntended_Column = true;
                }
                else
                {
                    doubleCheck_Num++;
                    if (doubleCheck_Num > 1) //하나 이상 겹치면 중복이라는 뜻이므로 다시 더한값을 빼줌
                    {
                        isDoubleCheck = true;
                        //gameBoard_BottomUI.SetScoreNumText(-matchingScore[checkSame_ColumnLine - 3]);
                        break;
                    }
                }
            }

            if(!isDoubleCheck)
            {
                gameBoard_BottomUI.SetScoreNumText(matchingScore[checkSame_ColumnLine - 3]);
                gameBoard_TopUI.RemainTime += addTime;
            }
        }
        gameBoardDestroyList.Clear();
        return isMatchBlock_Exist;
    }


    //-----------------------------------------------------블럭 위치 교환 관련 메서드

    private FourDirection CheckDirection(Vector2 pos_1, Vector2 pos_2)
    {
        Vector2 dir = pos_2 - pos_1;
        if(dir.x > 0 && Mathf.Abs(dir.x) > Mathf.Abs(dir.y))    //동
        {
            return FourDirection.East;
        }
        else if(dir.x < 0 && Mathf.Abs(dir.x) > Mathf.Abs(dir.y))   //서
        {
            return FourDirection.West;
        }
        else if(dir.y > 0 && Mathf.Abs(dir.x) < Mathf.Abs(dir.y))   //북
        {
            return FourDirection.North;
        }
        else if(dir.y < 0 && Mathf.Abs(dir.x) < Mathf.Abs(dir.y))   //남
        {
            return FourDirection.South;
        }
        else    //0,0이 찍혔을 때
        {
            return FourDirection.None;
        }

    }

    private FourDirection ReverseDirection(FourDirection fourDirection)
    {
        switch (fourDirection)
        {
            case FourDirection.East:
                return FourDirection.West;

            case FourDirection.West:
                return FourDirection.East;

            case FourDirection.South:
                return FourDirection.North;

            case FourDirection.North:
                return FourDirection.South;

            default:
                return FourDirection.None;

        }
    }

    private bool ChangeBlock(int i, int j, FourDirection fourDirection)
    {
        bool isChangeSuccess = false;
        switch (fourDirection)
        {
            case FourDirection.East:
                if(j < COLUMN_NUM-1 && !gameBoard_Blocks[i, j].isWall && !gameBoard_Blocks[i, j + 1].isWall)
                {
                    isChangeSuccess = true;

                    StartCoroutine(CoChangeBlock(gameBoard_Blocks[i, j].transform, gameBoard_Blocks[i, j+1].transform, blockPos[i, j + 1], blockPos[i, j]));
                    ChangeBlock_Data(i, j, i, j + 1);

                }
                return isChangeSuccess;
            case FourDirection.West:
                if (j > 0 && !gameBoard_Blocks[i, j].isWall && !gameBoard_Blocks[i, j - 1].isWall)
                {
                    isChangeSuccess = true;

                    StartCoroutine(CoChangeBlock(gameBoard_Blocks[i, j].transform, gameBoard_Blocks[i, j - 1].transform, blockPos[i, j - 1], blockPos[i, j]));
                    ChangeBlock_Data(i, j, i, j - 1);

                }
                return isChangeSuccess;
            case FourDirection.South:
                if (i > 0 && !gameBoard_Blocks[i, j].isWall && !gameBoard_Blocks[i -1, j].isWall)
                {
                    isChangeSuccess = true;

                    StartCoroutine(CoChangeBlock(gameBoard_Blocks[i, j].transform, gameBoard_Blocks[i -1, j].transform, blockPos[i -1, j], blockPos[i, j]));
                    ChangeBlock_Data(i, j, i -1, j);

                }
                return isChangeSuccess;
            case FourDirection.North:
                if (i < LOW_NUM_VISIBLE -1 && !gameBoard_Blocks[i, j].isWall && !gameBoard_Blocks[i +1, j].isWall)
                {
                    isChangeSuccess = true;

                    StartCoroutine(CoChangeBlock(gameBoard_Blocks[i, j].transform, gameBoard_Blocks[i + 1, j].transform, blockPos[i + 1, j], blockPos[i, j]));
                    ChangeBlock_Data(i, j, i + 1, j);

                }
                return isChangeSuccess;
            default:
                break;
        }
        return isChangeSuccess;
    }

    private void ChangeBlock_Data(int block1_i, int block1_j, int block2_i, int block2_j )
    {
        GameObject moveObj_1;
        GameObject moveObj_2;

        moveObj_1 = gameBoard_Blocks[block1_i, block1_j].gameObject;
        moveObj_2 = gameBoard_Blocks[block2_i, block2_j].gameObject;

        gameBoard_Blocks[block1_i, block1_j] = moveObj_2.GetComponent<BlockObject>();
        gameBoard_Blocks[block2_i, block2_j] = moveObj_1.GetComponent<BlockObject>();

        gameBoard_Blocks[block1_i, block1_j].blockID = gameBoard_Id[block1_i, block1_j];
        gameBoard_Blocks[block2_i, block2_j].blockID = gameBoard_Id[block2_i, block2_j];
    }

    IEnumerator CoChangeBlock(Transform moveTransform_1 , Transform moveTransform_2, Vector2 targetPos_1, Vector2 targetPos_2)
    {
        isChangeEnd = false;
        while(!isChangeEnd)
        {
            yield return null;

            moveTransform_1.position = Vector2.Lerp((Vector2)moveTransform_1.position, targetPos_1, blockMoveSpeed * 2);
            moveTransform_2.position = Vector2.Lerp((Vector2)moveTransform_2.position, targetPos_2, blockMoveSpeed * 2);


            float destinationRatio = 0.99f;
            Vector2 destinationPos_1 = targetPos_1 - targetPos_1 * destinationRatio;
            Vector2 currentPos_1 = targetPos_1 - (Vector2)moveTransform_1.position;

            Vector2 destinationPos_2 = targetPos_2 - targetPos_2 * destinationRatio;
            Vector2 currentPos_2 = targetPos_2 - (Vector2)moveTransform_2.position;


            if ((currentPos_1.sqrMagnitude <= destinationPos_1.sqrMagnitude) && (currentPos_2.sqrMagnitude <= destinationPos_2.sqrMagnitude))
            {
                moveTransform_1.position = targetPos_1;
                moveTransform_2.position = targetPos_2;
                isChangeEnd = true;
                Debug.Log("완료");
            }
        }
    }

    //--------------------------------------------------블록 하강 관련 메서드
    private void CheckDestroyAndBlockDown()
    {
        for(int j =0; j < COLUMN_NUM; j++)
        {
            CheckDestroyAndBlockDown_Line_Data(j);
        }

        for (int i = 0; i < LOW_NUM_TOTAL; i++)
        {
            for (int j = 0; j < COLUMN_NUM; j++)
            {
                if (gameBoard_Blocks[i, j].targetPos.Count != 0)
                {
                    StartCoroutine(CoMoveBlock_Multi(gameBoard_Blocks[i, j].transform, i, j));
                    //isNewBlocksMoveEnd[i, j] ||
                    //if (firstIndex_I == LOW_NUM_TOTAL - 1)
                    //{
                    //}
                }
            }
        }
    }

    private void CheckDestroyAndBlockDown_Line_Data(int columnIndex)
    {
        bool isNullExist = false;

        //인수로받은 라인의 빈블록 체크
        for (int i = 0; i < LOW_NUM_TOTAL; i++)
        {
            if (gameBoard_Blocks[i, columnIndex] == null)
            {
                isNullExist = true;
                break;
            }      
        }
        //빈블록이 존재하면 해당 라인과 +-1 라인 다운
        if(isNullExist)
        {
            CheckBlockDown_Line_Data(columnIndex);

            if (columnIndex > 0)
            {
                CheckBlockDown_Line_Data(columnIndex - 1);
            }

            if (columnIndex < COLUMN_NUM - 1)
            {
                CheckBlockDown_Line_Data(columnIndex + 1);
            }

            bool isLineMoveIntendedExist;

            do
            {
                //List<int> intendedColumnIndex = new List<int>();
                isLineMoveIntendedExist = false;
                for (int j = 0; j < COLUMN_NUM; j++)
                {
                    //CheckDestroyAndBlockDown_Line_Data함수가 블록마다 실행되지 않도록 체크하며 움직임이 예정되었다면 다시 양옆에 빈슬롯이없는지 다시 체크
                    //isLineMoveIntended[]는 빈블록이 없을때까지 함수를 움직이도록 함
                    if (isLineMoveIntended[j])
                    {
                        isLineMoveIntendedExist = true;
                        isLineMoveIntended[j] = false;
                        CheckDestroyAndBlockDown_Line_Data(j);
                        if (j > 0)
                        {
                            CheckDestroyAndBlockDown_Line_Data(j - 1);
                        }

                        if (j < COLUMN_NUM - 1)
                        {
                            CheckDestroyAndBlockDown_Line_Data(j + 1);
                        }
                        //intendedColumnIndex.Add(j);
                    }
                }

            } while (isLineMoveIntendedExist);
        }

    }

    private void CheckDestroyAndBlockDown_Line(int columnIndex)
    {
        bool isNullExist = false;

        //인수로받은 라인의 빈블록 체크
        //for (int i = 0; i < LOW_NUM_TOTAL; i++)
        //{
        //    if (gameBoard_Blocks[i, columnIndex] == null)
        //    {
        //        isNullExist = true;
        //        break;
        //    }
        //}
        //빈블록이 존재하면 해당 라인과 +-1 라인 다운
        //if (isNullExist)
        //{

            if (columnIndex > 0)
            {
                CheckBlockDown_Line_Data(columnIndex - 1);
            }
            CheckBlockDown_Line_Data(columnIndex);

            if (columnIndex < COLUMN_NUM - 1)
            {
                CheckBlockDown_Line_Data(columnIndex + 1);
            }

            bool isLineMoveIntendedExist = false;

            do
            {
                //List<int> intendedColumnIndex = new List<int>();
                isLineMoveIntendedExist = false;
                for (int j = 0; j < COLUMN_NUM; j++)
                {
                    //블럭이 움직이기로 예정되었다면 다시 양옆에 빈슬롯이없는지 다시 체크
                    if (isLineMoveIntended[j])
                    {
                        isLineMoveIntendedExist = true;
                        isLineMoveIntended[j] = false;
                        if (j > 0)
                        {
                            CheckDestroyAndBlockDown_Line_Data(j - 1);
                        }
                        CheckDestroyAndBlockDown_Line_Data(j);

                        if (j < COLUMN_NUM - 1)
                        {
                            CheckDestroyAndBlockDown_Line_Data(j + 1);
                        }
                        //intendedColumnIndex.Add(j);
                    }
                }

            } while (isLineMoveIntendedExist);
        //}

        for(int i = 0; i < LOW_NUM_TOTAL; i++)
        {
            for(int j = 0; j < COLUMN_NUM; j++)
            {
                if (gameBoard_Blocks[i, j].targetPos.Count != 0)
                {
                    StartCoroutine(CoMoveBlock_Multi(gameBoard_Blocks[i, j].transform, i, j));
                    //isNewBlocksMoveEnd[i, j] ||
                    //if (firstIndex_I == LOW_NUM_TOTAL - 1)
                    //{
                    //}
                }
            }
        }

    }

    public void CheckBlockDown_Line_Data(int ColumnIndex)
    {
        //if (isLineMoveEnd[ColumnIndex])
        //{
            for (int i = 0; i < LOW_NUM_TOTAL; i++)
            {
                int index_I = i;

                MoveBlockDown_More(index_I, ColumnIndex);

            }
        //}
        RandomBlockInstantiate_Top(ColumnIndex);
    }

    
    
    //블럭을 내릴 수 있는 가장 아래로 내림
    private void MoveBlockDown_More(int i, int j)
    {
        if(gameBoard_Blocks[i, j] != null)
        {
            int firstIndex_I = i;
            int firstIndex_J = j;
            bool isBlockMoveEnd = false;
            bool isBlockMoved = false;
            //bool isBlockSideMove = false;
            bool isWall = true;
            //FourDirection previousMove = FourDirection.None;

            //originIndex가 초기 값이라면 값 할당
            if(gameBoard_Blocks[i, j].originIndex_I == -1)
            {
                gameBoard_Blocks[i, j].originIndex_I = firstIndex_I;
                gameBoard_Blocks[i, j].originIndex_J = firstIndex_J;
            }
            else//originIndex가 이미 할당되어있다면 firstIndex를 originIndex값으로 변경
            {
                firstIndex_I = gameBoard_Blocks[i, j].originIndex_I;
                firstIndex_J = gameBoard_Blocks[i, j].originIndex_J;
            }


            if(!gameBoard_Blocks[i, j].isWall)
            {
                isWall = false;
            }
            while(!isBlockMoveEnd && !isWall)
            {
                if ( i > 0)
                {
                    bool is_I_Zero = false;
                    bool isMoved_Down = false;
                    while (gameBoard_Blocks[i - 1, j] == null)
                    {
                        MoveBlockDown_OneSlot_Data(i, j);
                        isMoved_Down = true;
                        if (i > 1)
                        {
                            i--;
                        }
                        else
                        {
                            is_I_Zero = true;
                        }
                    }

                    if(is_I_Zero)
                    {
                        i = 0;
                    }

                    if(isMoved_Down)
                    {
                        isBlockMoved = true;
                        gameBoard_Blocks[i, j].targetPos.Add(blockPos[i, j]);
                        gameBoard_Blocks[i, j].destination = blockPos[i, j];
                    }
                }

                if( i < LOW_NUM_VISIBLE - 1)
                {
                    if(j > 0 && j < COLUMN_NUM - 1)     //양 끝이 아닌 경우 좌우 빈블록 체크
                    {
                        //대각선 위쪽 방향이 벽이고 벽아래가 비어있다면 그곳으로(옆으로) 이동
                        if (gameBoard_Blocks[i + 1, j - 1] != null && gameBoard_Blocks[i, j - 1] == null && gameBoard_Blocks[i, j].previousDirection != FourDirection.East)
                        {
                            if (gameBoard_Blocks[i + 1, j - 1].isWall)
                            {
                                MoveBlockLeft_OneSlot_Data(i, j);
                                isBlockMoved = true;
                                //isBlockSideMove = true;
                                //이 함수안에 MoveBlockDown_More함수가 들어있으므로 재귀형태임
                                j--;
                                gameBoard_Blocks[i, j].previousDirection = FourDirection.West;
                                gameBoard_Blocks[i, j].targetPos.Add(blockPos[i, j]);
                                continue;
                            }
                        }

                        if (gameBoard_Blocks[i + 1, j + 1] != null && gameBoard_Blocks[i, j + 1] == null && gameBoard_Blocks[i, j].previousDirection != FourDirection.West)
                        {
                            if (gameBoard_Blocks[i + 1, j + 1].isWall)
                            {
                                MoveBlockRight_OneSlot_Data(i, j);
                                isBlockMoved = true;
                                //isBlockSideMove = true;
                                j++;
                                gameBoard_Blocks[i, j].previousDirection = FourDirection.East;
                                gameBoard_Blocks[i, j].targetPos.Add(blockPos[i, j]);
                                continue;
                            }
                        }
                    }
                    else if (j == 0)    //왼쪽 끝일 때 오른쪽 빈블록만 체크
                    {
                        if (gameBoard_Blocks[i + 1, j + 1] != null && gameBoard_Blocks[i, j + 1] == null && gameBoard_Blocks[i, j].previousDirection != FourDirection.West)
                        {
                            if (gameBoard_Blocks[i + 1, j + 1].isWall)
                            {
                                MoveBlockRight_OneSlot_Data(i, j);
                                isBlockMoved = true;
                                //isBlockSideMove = true;
                                j++;
                                gameBoard_Blocks[i, j].previousDirection = FourDirection.East;
                                gameBoard_Blocks[i, j].targetPos.Add(blockPos[i, j]);
                                continue;
                            }

                        }
                    }
                    else if (j == COLUMN_NUM -1)    //오른쪽 끝일 때 왼쪽 빈블록만 체크
                    {
                        //대각선 위쪽 방향이 벽이고 벽아래가 비어있다면 그곳으로(옆으로) 이동
                        if (gameBoard_Blocks[i + 1, j - 1] != null && gameBoard_Blocks[i, j - 1] == null && gameBoard_Blocks[i, j].previousDirection != FourDirection.East)
                        {
                            if (gameBoard_Blocks[i + 1, j - 1].isWall)
                            {
                                MoveBlockLeft_OneSlot_Data(i, j);
                                isBlockMoved = true;
                                //isBlockSideMove = true;
                                //이 함수안에 MoveBlockDown_More함수가 들어있으므로 재귀형태임
                                j--;
                                gameBoard_Blocks[i, j].previousDirection = FourDirection.West;
                                gameBoard_Blocks[i, j].targetPos.Add(blockPos[i, j]);
                                continue;
                            }
                        }
                    }
                }
                isBlockMoveEnd = true;
            }

            if (isBlockMoved)
            {
                isLineMoveIntended[j] = true;
                //isLineMoveEnd[firstIndex_J] = false;
            }
            else
            {
                //isLineMoveEnd[firstIndex_J] = true;
            }


            //
            //최종적으로 위치가 결정되었을 때 블럭 이동
            //if (gameBoard_Blocks[i, j].targetPos.Count != 0)
            //{
            //    //isNewBlocksMoveEnd[i, j] ||
            //    if ( firstIndex_I == LOW_NUM_TOTAL - 1)
            //    {
            //        StartCoroutine(CoMoveBlock_Multi(gameBoard_Blocks[i, j].transform, i, j));
            //    }
            //}

        }
    }

    //IEnumerator CoWaitMoveDown_And_CheckMoveDownMore(int index_I, int index_J)
    //{
    //    while (!isNewBlocksMoveEnd[index_I, index_J])
    //    {
    //        yield return null;
    //    }

    //    MoveBlockDown_More(index_I - 1, index_J);
    //}

    //블럭 데이터만을 한칸 내리며 트랜스폼은 안건드림 
    private void MoveBlockDown_OneSlot_Data(int i, int j)
    {
        if (i > 0)
        {
            GameObject moveObj_1;

            moveObj_1 = gameBoard_Blocks[i, j].gameObject;

            //여기서는 데이터만 옮김

            gameBoard_Blocks[i, j] = null;
            gameBoard_Blocks[i - 1, j] = moveObj_1.GetComponent<BlockObject>();

            gameBoard_Blocks[i - 1, j].blockID = gameBoard_Id[i - 1, j];
        }
    }

    private void MoveBlockLeft_OneSlot_Data(int i, int j)
    {
        if (j > 0)
        {
            GameObject moveObj_1;

            moveObj_1 = gameBoard_Blocks[i, j].gameObject;

            gameBoard_Blocks[i, j] = null;
            gameBoard_Blocks[i, j - 1] = moveObj_1.GetComponent<BlockObject>();

            gameBoard_Blocks[i, j -1].blockID = gameBoard_Id[i, j - 1];
        }
    }

    private void MoveBlockRight_OneSlot_Data(int i, int j)
    {
        if (j < COLUMN_NUM -1)
        {
            GameObject moveObj_1;

            moveObj_1 = gameBoard_Blocks[i, j].gameObject;

            gameBoard_Blocks[i, j] = null;
            gameBoard_Blocks[i, j + 1] = moveObj_1.GetComponent<BlockObject>();

            gameBoard_Blocks[i, j + 1].blockID = gameBoard_Id[i, j + 1];
        }
    }

    //블럭을 한칸 내리며 게임보드에도 세팅
    //private void MoveBlockDown_OneSlot(int i, int j)
    //{
    //    if (i > 0)
    //    {
    //        GameObject moveObj_1;

    //        moveObj_1 = gameBoard_Blocks[i, j].gameObject;

    //        StartCoroutine(CoMoveBlock(moveObj_1.transform, i - 1, j));

    //        gameBoard_Blocks[i, j] = null;
    //        gameBoard_Blocks[i - 1, j] = moveObj_1.GetComponent<BlockObject>();

    //        gameBoard_Blocks[i - 1, j].blockID = gameBoard_Id[i - 1, j];
    //    }
    //}

    //방향 상관없이 블럭 트랜스폼을 이동
    //IEnumerator CoMoveBlock(Transform moveTransform, int index_I, int index_J)
    //{
    //    isNewBlocksMoveEnd[index_I, index_J] = false;
    //    Vector2 targetPos = blockPos[index_I, index_J];
    //    Vector2 initiate_Position = (Vector2)moveTransform.position;

    //    float previousPos_Sqr = 9999999;

    //    while (!isNewBlocksMoveEnd[index_I, index_J])
    //    {
    //        yield return null;
    //        //moveTransform.position = Vector2.Lerp(initiate_Position, targetPos, moveSpeed);

    //        //currentRatio = currentRatio + blockMoveSpeed;
    //        //float destinationRatio = 0.95f;
    //        //Vector2 destinationPos_1 = targetPos - targetPos * destinationRatio;

    //        moveTransform.position += blockMoveSpeed * (Vector3)Vector2.down; 


    //        Vector2 currentPos_1 = targetPos - (Vector2)moveTransform.position;

    //        float currentPos_1_Sqr = currentPos_1.sqrMagnitude;

    //        if ( currentPos_1_Sqr > previousPos_Sqr)
    //        {
    //            moveTransform.position = targetPos;
    //            isNewBlocksMoveEnd[index_I, index_J] = true;
    //            Debug.Log("완료");
    //        }
    //        else
    //        {
    //            previousPos_Sqr = currentPos_1_Sqr;
    //        }

    //    }
    //}

    IEnumerator CoMoveBlock_Multi(Transform moveTransform,int index_I, int index_J )
    {
        isNewBlocksMoveEnd[index_I, index_J] = false;
        bool isCurrentMoveEnd;
        //bool isLoopOut = false;

        //int currentOriginIndex_I = gameBoard_Blocks[index_I, index_J].originIndex_I;
        //int currentOriginIndex_J = gameBoard_Blocks[index_I, index_J].originIndex_J;


        while (gameBoard_Blocks[index_I, index_J].targetPos.Count > 0)
        {
            isCurrentMoveEnd = false;
            Vector2 targetPos = gameBoard_Blocks[index_I, index_J].targetPos[0];
            Vector2 initiate_Position = (Vector2)moveTransform.position;
            Vector2 dir = (targetPos - initiate_Position).normalized;

            //gameBoard_Blocks[index_I, index_J].isMoving = true;
            //if (gameBoard_Blocks[index_I, index_J].isMoving == false)
            //{

            //}

            float previousPos_Sqr = 9999999;

            while (!isCurrentMoveEnd)
            {
                yield return null;
                //moveTransform.position = Vector2.Lerp(initiate_Position, targetPos, moveSpeed);

                //currentRatio = currentRatio + blockMoveSpeed;
                //float destinationRatio = 0.95f;
                //Vector2 destinationPos_1 = targetPos - targetPos * destinationRatio;

                //추가로 움직이는 경우가 생길경우 기존에 움직이는 함수는 작동하지 않도록 탈출시키는 함수
                //if(gameBoard_Blocks[index_I, index_J].isMoveMore.Count == 1)
                //{
                //    gameBoard_Blocks[index_I, index_J].isMoveMore.Clear();
                //    break;
                //}
                //else if(gameBoard_Blocks[index_I, index_J].isMoveMore.Count > 1)
                //{
                //    gameBoard_Blocks[index_I, index_J].isMoveMore.RemoveAt(0);
                //    break;
                //}


                //if(gameBoard_Blocks[index_I, index_J] == null)
                //{
                //    isLoopOut = true;
                //    isNewBlocksMoveEnd[index_I, index_J] = true;
                //    break;
                //}
                //else
                //{
                //    if (gameBoard_Blocks[index_I, index_J].originIndex_I != currentOriginIndex_I || gameBoard_Blocks[index_I, index_J].originIndex_J != currentOriginIndex_J)
                //    {
                //        isLoopOut = true;
                //        isNewBlocksMoveEnd[index_I, index_J] = true;
                //        break;
                //    }
                //}
                //isNewBlocksMoveEnd[index_I, index_J] = false;
                moveTransform.position += blockMoveSpeed * (Vector3)dir;


                Vector2 currentPos = targetPos - (Vector2)moveTransform.position;

                float currentPos_Sqr = currentPos.sqrMagnitude;

                if (currentPos_Sqr > previousPos_Sqr)
                {
                    moveTransform.position = targetPos;
                    isCurrentMoveEnd = true;
                    if (gameBoard_Blocks[index_I, index_J].targetPos.Count > 1)
                    {
                        gameBoard_Blocks[index_I, index_J].targetPos.RemoveAt(0);
                        //isLoopOut = true;
                    }
                    else
                    {
                        gameBoard_Blocks[index_I, index_J].targetPos.Clear();
                        isNewBlocksMoveEnd[index_I, index_J] = true;
                        //isLoopOut = true;
                    }

                    Debug.Log("완료");
                }
                else
                {
                    previousPos_Sqr = currentPos_Sqr;
                }

            }

            //if(isLoopOut)
            //{
            //    break;
            //}

        }

        //if(!isLoopOut)
        //{
        //    //gameBoard_Blocks[index_I, index_J].isMoving = false;
        //    isNewBlocksMoveEnd[index_I, index_J] = true;
        //}


        //for (int i = 0; i < targetPos_Multi.Count; i++)
        //{
        //    isCurrentMoveEnd = false;
        //    Vector2 targetPos = targetPos_Multi[i];
        //    Vector2 initiate_Position = (Vector2)moveTransform.position;
        //    Vector2 dir = (targetPos - initiate_Position).normalized;

        //    float previousPos_Sqr = 9999999;

        //    while (!isCurrentMoveEnd)
        //    {
        //        yield return null;
        //        //moveTransform.position = Vector2.Lerp(initiate_Position, targetPos, moveSpeed);

        //        //currentRatio = currentRatio + blockMoveSpeed;
        //        //float destinationRatio = 0.95f;
        //        //Vector2 destinationPos_1 = targetPos - targetPos * destinationRatio;

        //        moveTransform.position += blockMoveSpeed * (Vector3)dir;


        //        Vector2 currentPos = targetPos - (Vector2)moveTransform.position;

        //        float currentPos_Sqr = currentPos.sqrMagnitude;

        //        if (currentPos_Sqr > previousPos_Sqr)
        //        {
        //            moveTransform.position = targetPos;
        //            isCurrentMoveEnd = true;
        //            Debug.Log("완료");
        //        }
        //        else
        //        {
        //            previousPos_Sqr = currentPos_Sqr;
        //        }

        //    }

        //}
        //isNewBlocksMoveEnd[index_I, index_J] = true;
    }

    //-----------------------------------------------------------------블럭 파괴 관련 메서드

    private void DestroyIntendedBlocks_All()
    {
        for (int i = 0; i < LOW_NUM_VISIBLE; i++)
        {
            for (int j = 0; j < COLUMN_NUM; j++)
            {
                DestroyIntendedBlock(i, j);
            }
        }
    }

    public void DestroyIntendedBlock(int i, int j)
    {
        bool isSoundPlaying = false;
        if (gameBoard_Blocks[i, j].isDestroyIntended_Low || gameBoard_Blocks[i, j].isDestroyIntended_Column)
        {
            if(!gameBoard_Blocks[i, j].isWall)
            {
                //gameBoard[i, m].spriteRenderer.color = Color.blue;

                //Destroy(gameBoard[i, m].gameObject);

                if (!isSoundPlaying)
                {
                    isSoundPlaying = true;
                    SoundPlayer.Instance.PlaySound(SoundType_Effect.Effect_DieSound);
                }

                gameBoard_Blocks[i, j].PlayDestroyAnim();
                isDestroyAnimEnd[i, j] = false;
            }
        }
    }

    private void DestroyIntendedBlocks_AfterMatch(int i, int j, FourDirection fourDirection)
    {
        switch (fourDirection)
        {
            case FourDirection.East:
                if(j < COLUMN_NUM -1)
                {
                    DestroyIntendedBlocks_Cross(i, j);
                    DestroyIntendedBlocks_Cross(i, j + 1);
                }
                break;
            case FourDirection.West:
                if (j > 0)
                {
                    DestroyIntendedBlocks_Cross(i, j);
                    DestroyIntendedBlocks_Cross(i, j - 1);
                }
                break;
            case FourDirection.South:
                if (i > 0)
                {
                    DestroyIntendedBlocks_Cross(i, j);
                    DestroyIntendedBlocks_Cross(i - 1, j);
                }
                break;
            case FourDirection.North:
                if (i < LOW_NUM_VISIBLE - 1)
                {
                    DestroyIntendedBlocks_Cross(i, j);
                    DestroyIntendedBlocks_Cross(i + 1, j);
                }
                break;
            case FourDirection.None:
                break;
            default:
                break;
        }

    }

    public void DestroyIntendedBlocks_Cross(int i, int j)
    {
        for (int m = 0; m < COLUMN_NUM; m++)
        {
            if (gameBoard_Blocks[i,m] != null)
            {
                DestroyIntendedBlock(i, m);

            }
        }
        for(int n = 0; n < LOW_NUM_VISIBLE; n++)
        {
            if (n != i) //[n, j]가 [i, j]일 떄는 위에서 이미 계산했으므로 빼고 계산
            {
                DestroyIntendedBlock(n, j);

            }
            
        }
        
    }


}

public enum FourDirection
{
    East = 0,
    West,
    South,
    North,
    None
}
