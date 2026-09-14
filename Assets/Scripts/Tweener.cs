using UnityEngine;
using System.Collections.Generic;


public class Tweener : MonoBehaviour
{
    // private Tween activeTween;
    private List<Tween> activeTweens = new List<Tween>();
    private List<Tween> toRemove = new List<Tween>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        toRemove.Clear();
        float currentTime = Time.time;
        
        for (int i = 0; i < activeTweens.Count; i++)
        {
            Tween activeTween = activeTweens[i];
            if (activeTween == null || activeTween.Target == null)
            {
                toRemove.Add(activeTween);
                continue;
            }

            float t = Mathf.Clamp01((currentTime - activeTween.StartTime) / activeTween.Duration);
            activeTween.Target.position = Vector3.Lerp(activeTween.StartPos, activeTween.EndPos, t);

            if (t >= 1.0f)
            {
                activeTween.Target.position = activeTween.EndPos;
                toRemove.Add(activeTween);
            }
        }

        // 일괄 제거
        for (int i = 0; i < toRemove.Count; i++)
        {
            activeTweens.Remove(toRemove[i]);
        }
    }

    public bool AddTween (Transform targetObject,
        Vector3 startPos, Vector3 endPos, float duration)
    {
        //false if Tween already exists for the object
        if (! TweensExists(targetObject)) 
        {
            activeTweens.Add(new Tween(targetObject, startPos, endPos, Time.time, duration));
            return true;
        }
        else
            return false;
    }

    public bool TweensExists(Transform target)
    {
        foreach (Tween tween in activeTweens)
        {
            if (tween.Target == target) return true;
        }
        return false;
    }
}
