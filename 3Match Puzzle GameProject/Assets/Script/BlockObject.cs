using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BlockObject : MonoBehaviour
{
    public int blockTypeID;
    public int blockID;
    public bool isWall = false;
    public bool isDestroyIntended_Low = false;
    public bool isDestroyIntended_Column = false;
    public bool isAnimationEnd = true;
    public SpriteRenderer spriteRenderer;
    public Animator anim;
    public GameBoard board;
    public List<Vector2> targetPos = new List<Vector2>();
    //public bool isDestroyAnimationPlaying;

    //Vector2 mousePos_Drag_Start;
    //Vector2 mousePos_Drag_End;

    //bool isDraging = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        board = FindObjectOfType<GameBoard>();
    }

    //유니티 엔진의 애니메이션에서 이벤트로 실행
    public void DestroyBlock()
    {
        board.isDestroyAnimEnd[blockID / GameBoard.COLUMN_NUM, blockID % GameBoard.COLUMN_NUM] = true;
        board.gameBoard_Blocks[blockID / GameBoard.COLUMN_NUM, blockID % GameBoard.COLUMN_NUM] = null;
        //board.CheckBlockDown_Line(blockID % GameBoard.COLUMN_NUM);
        
        //isAnimationEnd = true;
        Destroy(this.gameObject);
    }

    public void PlayDestroyAnim()
    {
        //isAnimationEnd = false;
        anim.SetTrigger("isDestroyIntended");
    }

    public bool IsAnimEnd()
    {
        if(anim.GetCurrentAnimatorStateInfo(0).IsName("DestroyAnim") && 
            anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0)
        {
            return true;
        }
        return false;
    }


}
