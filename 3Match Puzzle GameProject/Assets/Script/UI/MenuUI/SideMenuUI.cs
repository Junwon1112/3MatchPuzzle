using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SideMenuUI : MonoBehaviour
{
    protected abstract CanvasGroup SideCanvasGroup { get; set; }

    public abstract bool IsSideUIChangeComplete { get; set; }
    public abstract void SetWindow();
    

}
