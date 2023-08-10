using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance;

    public Texture2D defaultCursorImage;
    public Texture2D targetCursorImage;
    public Texture2D findCursorImage;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            if (Instance != this)
            {
                Destroy(this.gameObject);
            }
        }

        Cursor.SetCursor(defaultCursorImage, new Vector2(5, 5), CursorMode.Auto);
    }


}
