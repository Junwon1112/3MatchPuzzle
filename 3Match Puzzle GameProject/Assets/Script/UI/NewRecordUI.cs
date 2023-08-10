using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class NewRecordUI : MonoBehaviour
{
    CanvasGroup canvasGroup;
    GameOver_UI gameOverUI;

    public TextMeshProUGUI rank_Text;
    public TextMeshProUGUI score_Text;
    public TMP_InputField inputField_Name;

    public Button okButton;

    public int endRank;
    public int endScore;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        gameOverUI =  FindObjectOfType<GameOver_UI>();

        rank_Text = transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>();
        inputField_Name = transform.GetChild(1).GetChild(1).GetComponent<TMP_InputField>();
        score_Text = transform.GetChild(1).GetChild(2).GetComponent<TextMeshProUGUI>();
        okButton = transform.GetComponentInChildren<Button>();
    }

    private void Start()
    {
        okButton.onClick.AddListener(ClickOkButton);
    }

    public void CanvasGroupOnOff()
    {
        if (canvasGroup.interactable)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            SoundPlayer.Instance.PauseBGM();
            SoundPlayer.Instance.PlaySound(SoundType_Effect.Effect_GameOver);

        }
    }

    private void ClickOkButton()
    {
        GameManager.instance.rankerName[endRank - 1] = inputField_Name.text;
        SetDataAsGameManager();

        CanvasGroupOnOff();
    }

    public void SetDataAsGameManager()
    {
        for (int i = 0; i < 3; i++)
        {
            gameOverUI.name_Text[i].text = GameManager.instance.rankerName[i];
            gameOverUI.score_Text[i].text = GameManager.instance.scoreRecord[i].ToString();
        }
    }
}
