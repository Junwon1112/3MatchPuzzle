using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GameBoard : MonoBehaviour
{
    //���� ���� : UI, Top:40, Bottom:80, object: 0, 0.5, scale 7, 8
    //���� ������Ʈ ��ġ : -3.5 , -2.5 ~ 3.5, 3.5 (8 X 7), vector2��:  (0~340) X (85 ~ 375)
    public Input input;
    public int currentStage;

    [SerializeField]
    GameObject[] objs; //=> Ÿ�� ����
    [SerializeField]
    GameObject[] wall; //=> Ÿ�� ����
    [SerializeField]
    int[] wallPosId;   //=> �� ��ġ (0 ~ 55 (column X low_visable) )
    public GameObject selectBlock;  // => �������� �� �̹���

    GameBoard_TopUI gameBoard_TopUI;
    GameBoard_BottomUI gameBoard_BottomUI;

    //����� ����� �ڵ����� static�̵Ǽ� �ν��Ͻ��� ȣ������ �ʴ´�.
    public const int COLUMN_NUM = 8;
    public const int LOW_NUM_VISIBLE = 7;      //������ ���̴� �κ�
    public const int LOW_NUM_TOTAL = 25;        //������ �Ⱥ��̴� �κ�
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

            if (hit.collider != null)   //onClick_Start���� ����ĳ��Ʈ�� �Ҵ�������
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

    //����Ÿ���� �Լ��� �Ķ���ͷ� �޴� �ڷ�ƾ
    IEnumerator CoWaitChange_Return(System.Func<int, int, FourDirection, bool> ChangeBlock_Match, int i, int j, FourDirection fourDirection)
    {
        while (!isChangeEnd)
        {
            yield return null;
        }
        //��ġ�ٲ� �� ��ġ Ȯ�� �� ����� �ǵ����� ����
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
                        if (!isDestroyAnimEnd[m, n])        //��� ��� �ı��ִϸ��̼��� ������ true�� �ȴ�
                        {
                            isBlockDestroyEnd = false;
                            break;
                        }
                        else if (isDestroyAnimEnd[LOW_NUM_VISIBLE - 1, COLUMN_NUM - 1])
                        {
                            isBlockDestroyEnd = true;
                        }
                    }
                    //2�� for�� Ż��
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
                    //2�� for�� Ż��
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

    //VoidŸ���� �Լ��� �Ķ���ͷ� �޴� �ڷ�ƾ

    //IEnumerator CoWaitMove_Void(System.Action<int, int , FourDirection> Destroy_Block, int i, int j, FourDirection fourDirection)
    //{
    //    while(!isMoveEnd)
    //    {
    //        yield return null;
    //    }

    //    Destroy_Block(i, j, fourDirection);
    //}

    //����� �־�� �� ��ġ ����
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

    //��ü �� ��ġ
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

        //������ ���� ���ϴ� ��ġ�� ����
    private void RandomBlockInstantiate(int i, int j)
    {
        int randNum = Random.Range(0, objs.Length * 4) % objs.Length;

        GameObject obj = Instantiate(objs[randNum], (Vector3)blockPos[i, j], transform.rotation, transform.GetChild(1));
        gameBoard_Blocks[i, j] = obj.transform.GetComponentInChildren<BlockObject>();
        gameBoard_Blocks[i, j].blockID = (i * COLUMN_NUM) + j;
    }

    //�������� �Ʒ��� ��ӵ� ��ĭ�� ��� ä���� �� ���� �� ����
    private void RandomBlockInstantiate_Top(int index_J)
    {
        while (gameBoard_Blocks[LOW_NUM_TOTAL-1, index_J] == null)
        {
            RandomBlockInstantiate(LOW_NUM_TOTAL - 1, index_J);
            MoveBlockDown_More(LOW_NUM_TOTAL - 1, index_J);

            //��� ����
            //RandomBlockInstantiate_Top(index_J);
        }
    }
    //��Ī ������ ����� �ִ��� Ȯ��
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

    //�� ����� �� �ٽ� ��Ī �� �ı��ϴ� �Ϸ��� ����
    IEnumerator CheckMatchAndDestroy_All()
    {
        //��Ī �� ���� ������ ���� ��Ī�� �ȵɶ����� �ݺ�
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
        //��Ī �� ���� ������ ���� ��Ī�� �ȵɶ����� �ݺ�
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
            //2�� for�� Ż��
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
            //2�� for�� Ż��
            if (!isAnimEnd)
            {
                break;
            }
        }

        return isAnimEnd;
    }
    //---------------------------------------------------�� ��Ī üũ �޼���

    //���̴� ���� ��Ī
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

        //i,j���� ����üũ
        while (currentX_Value_Minus >= 0 && gameBoard_Blocks[i, j].blockTypeID == gameBoard_Blocks[i, currentX_Value_Minus].blockTypeID && !gameBoard_Blocks[i, currentX_Value_Minus].isWall)
        {
            gameBoardDestroyList.Add(gameBoard_Blocks[i, currentX_Value_Minus]);
            checkSame_LowLine++;
            currentX_Value_Minus--;
        }
        //i,j���� ������üũ
        while (currentX_Value_Plus <= COLUMN_NUM - 1 && gameBoard_Blocks[i, j].blockTypeID == gameBoard_Blocks[i, currentX_Value_Plus].blockTypeID && !gameBoard_Blocks[i, currentX_Value_Plus].isWall)
        {
            gameBoardDestroyList.Add(gameBoard_Blocks[i, currentX_Value_Plus]);
            checkSame_LowLine++;
            currentX_Value_Plus++;
        }
        //üũ�� �ֵ��� 3�̻��̸� �ش� ����� ���� ����(blockObject.isDestroyIntended = true)
        if (checkSame_LowLine >= 3)
        {
            isMatchBlock_Exist = true;

            bool isDoubleCheck = false;
            //gameBoard_BottomUI.SetScoreNumText(matchingScore[checkSame_LowLine - 3]); 

            int doubleCheck_Num = 0;

            //����Ʈ���ִ� blockObject������ �������� ���� �� ��ġ���� üũ
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

        //i,j���� �Ʒ���üũ
        while (currentY_Value_Minus >= 0 && gameBoard_Blocks[i, j].blockTypeID == gameBoard_Blocks[currentY_Value_Minus, j].blockTypeID && !gameBoard_Blocks[ currentY_Value_Minus, j].isWall)
        {
            gameBoardDestroyList.Add(gameBoard_Blocks[currentY_Value_Minus, j]);
            checkSame_ColumnLine++;
            currentY_Value_Minus--;
        }
        //i,j���� ����üũ
        while (currentY_Value_Plus <= LOW_NUM_VISIBLE - 1 && gameBoard_Blocks[i, j].blockTypeID == gameBoard_Blocks[currentY_Value_Plus, j].blockTypeID && !gameBoard_Blocks[ currentY_Value_Plus, j].isWall)
        {
            gameBoardDestroyList.Add(gameBoard_Blocks[currentY_Value_Plus, j]);
            checkSame_ColumnLine++;
            currentY_Value_Plus++;
        }
        //üũ�� �ֵ��� 3�̻��̸� �ش� ����� ���� ����(blockObject.isDestroyIntended = true)
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
                    if (doubleCheck_Num > 1) //�ϳ� �̻� ��ġ�� �ߺ��̶�� ���̹Ƿ� �ٽ� ���Ѱ��� ����
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


    //-----------------------------------------------------�� ��ġ ��ȯ ���� �޼���

    private FourDirection CheckDirection(Vector2 pos_1, Vector2 pos_2)
    {
        Vector2 dir = pos_2 - pos_1;
        if(dir.x > 0 && Mathf.Abs(dir.x) > Mathf.Abs(dir.y))    //��
        {
            return FourDirection.East;
        }
        else if(dir.x < 0 && Mathf.Abs(dir.x) > Mathf.Abs(dir.y))   //��
        {
            return FourDirection.West;
        }
        else if(dir.y > 0 && Mathf.Abs(dir.x) < Mathf.Abs(dir.y))   //��
        {
            return FourDirection.North;
        }
        else if(dir.y < 0 && Mathf.Abs(dir.x) < Mathf.Abs(dir.y))   //��
        {
            return FourDirection.South;
        }
        else    //0,0�� ������ ��
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
                Debug.Log("�Ϸ�");
            }
        }
    }

    //--------------------------------------------------��� �ϰ� ���� �޼���
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

        //�μ��ι��� ������ ���� üũ
        for (int i = 0; i < LOW_NUM_TOTAL; i++)
        {
            if (gameBoard_Blocks[i, columnIndex] == null)
            {
                isNullExist = true;
                break;
            }      
        }
        //������ �����ϸ� �ش� ���ΰ� +-1 ���� �ٿ�
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
                    //CheckDestroyAndBlockDown_Line_Data�Լ��� ��ϸ��� ������� �ʵ��� üũ�ϸ� �������� �����Ǿ��ٸ� �ٽ� �翷�� �󽽷��̾����� �ٽ� üũ
                    //isLineMoveIntended[]�� ������ ���������� �Լ��� �����̵��� ��
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

        //�μ��ι��� ������ ���� üũ
        //for (int i = 0; i < LOW_NUM_TOTAL; i++)
        //{
        //    if (gameBoard_Blocks[i, columnIndex] == null)
        //    {
        //        isNullExist = true;
        //        break;
        //    }
        //}
        //������ �����ϸ� �ش� ���ΰ� +-1 ���� �ٿ�
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
                    //���� �����̱�� �����Ǿ��ٸ� �ٽ� �翷�� �󽽷��̾����� �ٽ� üũ
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

    
    
    //���� ���� �� �ִ� ���� �Ʒ��� ����
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

            //originIndex�� �ʱ� ���̶�� �� �Ҵ�
            if(gameBoard_Blocks[i, j].originIndex_I == -1)
            {
                gameBoard_Blocks[i, j].originIndex_I = firstIndex_I;
                gameBoard_Blocks[i, j].originIndex_J = firstIndex_J;
            }
            else//originIndex�� �̹� �Ҵ�Ǿ��ִٸ� firstIndex�� originIndex������ ����
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
                    if(j > 0 && j < COLUMN_NUM - 1)     //�� ���� �ƴ� ��� �¿� ���� üũ
                    {
                        //�밢�� ���� ������ ���̰� ���Ʒ��� ����ִٸ� �װ�����(������) �̵�
                        if (gameBoard_Blocks[i + 1, j - 1] != null && gameBoard_Blocks[i, j - 1] == null && gameBoard_Blocks[i, j].previousDirection != FourDirection.East)
                        {
                            if (gameBoard_Blocks[i + 1, j - 1].isWall)
                            {
                                MoveBlockLeft_OneSlot_Data(i, j);
                                isBlockMoved = true;
                                //isBlockSideMove = true;
                                //�� �Լ��ȿ� MoveBlockDown_More�Լ��� ��������Ƿ� ���������
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
                    else if (j == 0)    //���� ���� �� ������ ���ϸ� üũ
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
                    else if (j == COLUMN_NUM -1)    //������ ���� �� ���� ���ϸ� üũ
                    {
                        //�밢�� ���� ������ ���̰� ���Ʒ��� ����ִٸ� �װ�����(������) �̵�
                        if (gameBoard_Blocks[i + 1, j - 1] != null && gameBoard_Blocks[i, j - 1] == null && gameBoard_Blocks[i, j].previousDirection != FourDirection.East)
                        {
                            if (gameBoard_Blocks[i + 1, j - 1].isWall)
                            {
                                MoveBlockLeft_OneSlot_Data(i, j);
                                isBlockMoved = true;
                                //isBlockSideMove = true;
                                //�� �Լ��ȿ� MoveBlockDown_More�Լ��� ��������Ƿ� ���������
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
            //���������� ��ġ�� �����Ǿ��� �� �� �̵�
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

    //�� �����͸��� ��ĭ ������ Ʈ�������� �Ȱǵ帲 
    private void MoveBlockDown_OneSlot_Data(int i, int j)
    {
        if (i > 0)
        {
            GameObject moveObj_1;

            moveObj_1 = gameBoard_Blocks[i, j].gameObject;

            //���⼭�� �����͸� �ű�

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

    //���� ��ĭ ������ ���Ӻ��忡�� ����
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

    //���� ������� �� Ʈ�������� �̵�
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
    //            Debug.Log("�Ϸ�");
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

                //�߰��� �����̴� ��찡 ������ ������ �����̴� �Լ��� �۵����� �ʵ��� Ż���Ű�� �Լ�
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

                    Debug.Log("�Ϸ�");
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
        //            Debug.Log("�Ϸ�");
        //        }
        //        else
        //        {
        //            previousPos_Sqr = currentPos_Sqr;
        //        }

        //    }

        //}
        //isNewBlocksMoveEnd[index_I, index_J] = true;
    }

    //-----------------------------------------------------------------�� �ı� ���� �޼���

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
            if (n != i) //[n, j]�� [i, j]�� ���� ������ �̹� ��������Ƿ� ���� ���
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
