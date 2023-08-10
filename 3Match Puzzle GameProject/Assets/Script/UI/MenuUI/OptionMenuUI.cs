using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ù��° ȭ�鿡�� �����ϴ� �޴�, �ΰ��ӳ��� ���� ������ ������ MainMenuUI�� �����Ѵ�.
/// </summary>
public class OptionMenuUI : MonoBehaviour
{
    CanvasGroup canvasGroup;
    bool isOpen;

    //SideMenuUI[] sideMenuUIs;

    protected CanvasGroup CanvasGroup { get; set; }
    protected bool IsOpen { get; set; }
    //protected SideMenuUI[] SideMenuUIs { get; set; }

    private void Awake()
    {
        CanvasGroup = GetComponent<CanvasGroup>();
        //SideMenuUIs = FindObjectsOfType<SideMenuUI>();
        IsOpen = false;
    }


    public void OnOffMainMenu()
    {
        if (!IsOpen)
        {
            OpenMainMenu();
        }
        else
        {
            CloseMainMenu();
        }
    }

    public void OpenMainMenu()
    {
        if (!IsChildMenuOpen())
        {
            //Time.timeScale = 0;

            IsOpen = true;

            CanvasGroup.alpha = 1;
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = true;

            SoundPlayer.Instance.PlaySound(SoundType_Effect.Effect_UISound);
        }
    }

    public void CloseMainMenu()
    {
        CanvasGroup.alpha = 0;
        CanvasGroup.blocksRaycasts = false;
        CanvasGroup.interactable = false;

        IsOpen = false;

        SoundPlayer.Instance.PlaySound(SoundType_Effect.Effect_UISound);

        //Time.timeScale = 1;
    }

    private bool IsChildMenuOpen()
    {
        bool isChildOpen = false;

        //for (int i = 0; i < SideMenuUIs.Length; i++)
        //{
        //    if (!SideMenuUIs[i].IsSideUIChangeComplete) //���� UI�� �߰��� ������ �߰�
        //    {
        //        isChildOpen = true;
        //        return isChildOpen;
        //    }
        //}
        return isChildOpen;
    }
}
