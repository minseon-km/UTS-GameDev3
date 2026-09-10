using UnityEngine;
using System.Collections;

public class PlayerAudioManager : MonoBehaviour
{
    public AudioClip[] effects;
    AudioSource audioEffect;

    private Coroutine playRoutine;

    void Start()
    {
        audioEffect = GetComponent<AudioSource>();
        audioEffect.clip = effects[0];

        playRoutine = StartCoroutine(PlayRepeatedly(0.25f));
    }

    private IEnumerator PlayRepeatedly(float interval)
    {
        WaitForSeconds wait = new WaitForSeconds(interval);

        while (true)
        {
            audioEffect.Play();
            // below allows audio overlapping
            // audioEffect.PlayOneShot(audioEffect.clip);

            yield return wait;
        }
    }
}
