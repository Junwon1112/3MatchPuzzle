using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * @brief 사운드가 재생되는 오브젝트에 붙는 스크립트
 * @details 
 * 이 스크립트가 붙는 오브젝트에는 AudioSource 컴포넌트가 필수
 * 사운드 재생 관련 기능 구현
 * 코루틴을 사용해 일정시간 딜레이 후 재생하도록 구현
 * 재생이 끝난 사운드 오브젝트는 Destroy함
 */

[RequireComponent(typeof(AudioSource))]     //해당 컴포넌트가 없을 경우 자동 추가
public class SoundObject : MonoBehaviour
{
    [SerializeField]
    private AudioSource audioSource = null;

    private AudioClip audioClip = null;

    private System.Action action_FinishListner = null;

    private IEnumerator obj_currentRoutine = null;

    private bool isStoppable = true;

    private bool isPlaying = false;


    [SerializeField]
    private bool isDestroyWhenPlayEnd = true;

    public AudioSource AudioSource
    {
        get 
        {
            return audioSource;
        }
        set
        {
            audioSource = value;
        }
    }

    public AudioClip AudioClip
    {
        get
        {
            return audioClip;
        }
        set
        {
            audioClip = value;
        }
    }

    public bool IsPlaying
    {
        get
        {
            return isPlaying;
        }
        private set 
        {
            isPlaying = value; 
        }
    }


    private void Awake()
    {
        if(GetComponent<AudioSource>())
        {
            audioSource = GetComponent<AudioSource>();
        }
        
    }

    public void Play(float _volume, float _delaySeconds, bool _isLoop, bool _isStoppable, System.Action _finishListner = null)
    {
        if(audioSource == null)
        {
            Debug.Log("No AudioSource");
            return;
        }
        isStoppable = _isStoppable;

        audioSource.clip = AudioClip;
        audioSource.volume = _volume;
        audioSource.loop = _isLoop;
        action_FinishListner = _finishListner;
        obj_currentRoutine = DelayPlaySound(_delaySeconds);
        StartCoroutine(obj_currentRoutine);
    }

    IEnumerator DelayPlaySound(float delayTime)
    {
        IsPlaying = true;
        yield return new WaitForSeconds(delayTime);
        audioSource.Play();
        yield return new WaitForSeconds(AudioClip.length);
        IsPlaying = false;
        if(action_FinishListner != null)
        {
            action_FinishListner();
        }
        if(!audioSource.loop)
        {
            DestroySound();
        }
    }

    public void Pause()
    {
        audioSource.Pause();
    }

    public void UnPause()
    {
        audioSource.UnPause();
    }

    public void Stop()
    {
        if(isStoppable)
        {
            audioSource.Stop();
            if(obj_currentRoutine != null)
            {
                StopCoroutine(obj_currentRoutine);
            }
            IsPlaying = false;
            DestroySound();
        }
    }

    private void DestroySound()
    {
        try 
        {
            if (isDestroyWhenPlayEnd)
                Destroy(gameObject);
        }
        catch(Exception e)
        {
            Debug.Log(e.ToString());
        }
    }
}
