using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainScene_Blocks : MonoBehaviour
{
    public int blocks_ID = 0;

    public float moveSpeed = 60.0f;
    public float randeomMoveSpeedRange_Min = 30.0f;
    public float randeomMoveSpeedRange_Max = 90.0f;

    public float speedChangeTime = 1.5f;


    RectTransform rectTransform;

    int dir_y;
    int targetPos_y;

    float timer = 0;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (blocks_ID % 2 == 0)
        {
            dir_y = -1;
            targetPos_y = -60;
        }
        else
        {
            dir_y = 1;
            targetPos_y = 60;
        }
    }


    void Update()
    {
        MoveBlocks();
        RandomSpeedChange();
    }

    private void MoveBlocks()
    {
        transform.localPosition += new Vector3(0,dir_y,0) * moveSpeed * Time.deltaTime;

        if(blocks_ID % 2 == 0)
        {
            if(transform.localPosition.y < targetPos_y )
            {
                transform.localPosition = new Vector3(transform.localPosition.x, 0, transform.localPosition.z);
                rectTransform.GetChild(transform.childCount-1).SetAsFirstSibling();
            }
        }
        else
        {
            if (transform.localPosition.y > targetPos_y)
            {
                transform.localPosition = new Vector3(transform.localPosition.x, 0, transform.localPosition.z);
                rectTransform.GetChild(0).SetAsLastSibling();
            }
        }
    }

    private void RandomSpeedChange()
    {
        timer += Time.deltaTime;

        if(timer > speedChangeTime)
        {
            timer = 0;
            moveSpeed = Random.Range(randeomMoveSpeedRange_Min, randeomMoveSpeedRange_Max);
        }
    }
}
