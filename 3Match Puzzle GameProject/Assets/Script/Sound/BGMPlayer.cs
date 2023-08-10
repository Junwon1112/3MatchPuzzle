using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMPlayer : MonoBehaviour
{
    void Start()
    {
        PlayBGM_Setting(0);
        //SoundPlayer.Instance?.PlayBGM();
    }

    private void OnLevelWasLoaded(int level)
    {
        PlayBGM_Setting(level);
    }

    private void PlayBGM_Setting(int stageIndex)
    {
        switch (stageIndex)
        {
            case 0:
                SoundPlayer.Instance?.PlayBGM(null);
                break;

            case 1:
                //SoundPlayer.Instance?.PlayBGM(SoundType_BGM.BGM_Main);
                break;
            case 2:
                //SoundPlayer.Instance?.PlayBGM(SoundType_BGM.BGM_SelectCharacter);
                break;
            case 3:
                SoundPlayer.Instance?.PlayBGM(null);
                break;
            case 4:
                //SoundPlayer.Instance?.PlayBGM(SoundType_BGM.BGM_Stage1);
                break;
            case 5:
                //SoundPlayer.Instance?.PlayBGM(SoundType_BGM.BGM_Stage1);
                break;


            default:
                break;
        }
    }


}
