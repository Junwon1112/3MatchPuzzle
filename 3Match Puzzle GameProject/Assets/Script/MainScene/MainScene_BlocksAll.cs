using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainScene_BlocksAll : MonoBehaviour
{
    MainScene_Blocks[] childBlocks;

    private void Awake()
    {
        childBlocks = GetComponentsInChildren<MainScene_Blocks>();
        for(int i = 0; i < childBlocks.Length; i++)
        {
            childBlocks[i].blocks_ID = i;
        }
    }

    
}
