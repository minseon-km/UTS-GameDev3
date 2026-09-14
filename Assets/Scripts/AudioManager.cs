using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public AudioClip[] musics;
    AudioSource audioSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        StartCoroutine("PlayIntroStart");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator PlayIntroStart()
    {
        audioSource.clip = musics[0];
        audioSource.Play();

        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            if (!audioSource.isPlaying) {
                audioSource.clip = musics[1];
                audioSource.Play();
                audioSource.loop = true;
            }
        }
    }
}
