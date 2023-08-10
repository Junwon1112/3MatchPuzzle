using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

//플레이하지 않고 에디터에서만 작동하도록 만든 클래스, 게임 플레이하면 자동으로 삭제
[ExecuteInEditMode]
public class PosID_Edit : MonoBehaviour
{
    TextMeshProUGUI[] textMeshProUGUI;
    RectTransform rect;
    [SerializeField]
    GameObject id_TMP;

    Vector2[,] blockPos_UI;
    int[,] gameBoard_Id_UI;

    int temp = 0;

    private void Awake()
    {
        

    }

    void Start()
    {
        if(!Application.isPlaying)
        {
            textMeshProUGUI = GetComponentsInChildren<TextMeshProUGUI>();

            blockPos_UI = new Vector2[GameBoard.LOW_NUM_VISIBLE, GameBoard.COLUMN_NUM];
            gameBoard_Id_UI = new int[GameBoard.LOW_NUM_VISIBLE, GameBoard.COLUMN_NUM];
            for (int i = 0; i < GameBoard.LOW_NUM_VISIBLE; i++)
            {
                for (int j = 0; j < GameBoard.COLUMN_NUM; j++)
                {
                    blockPos_UI[i, j] = new Vector2Int(-140 + j * 40, -100 + i * 40);
                    gameBoard_Id_UI[i, j] = j + (i * GameBoard.COLUMN_NUM);
                }
            }

            for (int i = 0; i < GameBoard.LOW_NUM_VISIBLE; i++)
            {
                for (int j = 0; j < GameBoard.COLUMN_NUM; j++)
                {
                    rect = transform.GetChild(gameBoard_Id_UI[i, j]).GetComponent<RectTransform>();
                    rect.localPosition = blockPos_UI[i, j];
                }
            }

            foreach (TextMeshProUGUI id_Text in textMeshProUGUI)
            {
                id_Text.text = temp.ToString();
                temp++;

            }
            temp = 0;
        }
        else
        {
            Destroy(this.gameObject);
        }


        
    }

    
}
