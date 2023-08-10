using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionUI_Buttons : MonoBehaviour
{
    Button volumeUIButton;
    Button windowUIButton;
    Button backButton;

    OptionMenuUI optionMenuUI;
    WindowMenuUI windowMenuUI;
    VolumeMenuUI volumeMenuUI;

    private void Awake()
    {
        optionMenuUI = transform.parent.GetComponent<OptionMenuUI>();
        volumeUIButton = transform.GetChild(1).GetComponent<Button>();
        windowUIButton = transform.GetChild(2).GetComponent<Button>();
        backButton = transform.GetChild(0).GetComponent<Button>();

        windowMenuUI = FindObjectOfType<WindowMenuUI>();
        volumeMenuUI = FindObjectOfType<VolumeMenuUI>();
    }

    void Start()
    {
        volumeUIButton.onClick.AddListener(SetVolumeUI);
        windowUIButton.onClick.AddListener(SetWindowUI);
        backButton.onClick.AddListener(UIOff);
    }

    private void SetVolumeUI()
    {
        volumeMenuUI.SetWindow();
    }
    private void SetWindowUI()
    {
        windowMenuUI.SetWindow();
    }
    private void UIOff()
    {
        optionMenuUI.CloseMainMenu();
    }

    
}
