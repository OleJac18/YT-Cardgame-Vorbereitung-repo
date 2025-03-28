using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Special Action Sounds")]
    public AudioClip peakSound;
    public AudioClip spySound;
    public AudioClip swapSound;

    [Header("Game Sounds")]
    public AudioClip mismatchSound;
    public AudioClip nextTurnSound;
    public AudioClip caboSound;


    [Header("Audio Source")]
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();  // Holt die AudioSource Component
    }

    public void PlaySound(AudioClip sound)
    {
        if (sound != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(sound);
        }
    }

    // Spielt den Sound nur einmal
    public void PlayMismatchSound()
    {
        PlaySound(mismatchSound);
    }

    public void PlayPeakSound()
    {
        Debug.Log("Peak sound played");
        PlaySound(peakSound);
    }

    public void PlaySpySound()
    {
        Debug.Log("Spy sound played");
        PlaySound(spySound);
    }

    public void PlaySwapSound()
    {
        Debug.Log("Swap sound played")
;        PlaySound(swapSound);
    }
    public void PlayCaboSound()
    {
        Debug.Log("Cabo sound played");
        PlaySound(caboSound);
    }

    public void PlayNextTurnSound()
    {
        Debug.Log("Next turn sound played");
        PlaySound(nextTurnSound);
    }
}
