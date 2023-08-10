using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WindowMenuUI : SideMenuUI
{
    Button okButton;
    bool isSideUIChangeComplete;

    protected override CanvasGroup SideCanvasGroup { get; set; }
    public override bool IsSideUIChangeComplete { get; set; }

    private void Awake()
    {
        SideCanvasGroup = GetComponent<CanvasGroup>();
        okButton = GetComponentInChildren<Button>();
    }

    private void Start()
    {
        IsSideUIChangeComplete = true;
        okButton.onClick.AddListener(SetWindow);
    }

    public override void SetWindow()
    {
        if (SideCanvasGroup.interactable == true)
        {
            SideCanvasGroup.alpha = 0;
            SideCanvasGroup.blocksRaycasts = false;
            SideCanvasGroup.interactable = false;

            IsSideUIChangeComplete = true;

            SoundPlayer.Instance.PlaySound(SoundType_Effect.Effect_UISound);

            //Time.timeScale = 1;
        }
        else
        {
            //Time.timeScale = 0;

            IsSideUIChangeComplete = false;

            SoundPlayer.Instance.PlaySound(SoundType_Effect.Effect_UISound);

            SideCanvasGroup.alpha = 1;
            SideCanvasGroup.blocksRaycasts = true;
            SideCanvasGroup.interactable = true;
        }
    }

}
