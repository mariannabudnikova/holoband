using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity.InputModule;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    [Tooltip("Music sample associated with the band")]
    public AudioSource music;

    /// <summary>
    /// PLay the song associated with the band
    /// </summary>
    public void PlaySong()
    {
        Debug.Log("Play song");
        music.Play();
    }
}
