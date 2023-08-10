using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BackButton : MonoBehaviour
{
    Button backButton;

    private void Awake()
    {
        backButton = GetComponent<Button>();
    }

    private void Start()
    {
        backButton.onClick.AddListener(ClickBackButton);
    }

    private void ClickBackButton()
    {
        SceneManager.LoadScene(GameManager.instance.currentSceneIndex - 1);
        
    }
}
