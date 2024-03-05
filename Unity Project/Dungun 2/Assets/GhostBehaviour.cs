using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostBehaviour : MonoBehaviour
{
    public float GhostLifeTime = 0.2f;
    private float startTime = 0;
    private SpriteRenderer sr;

    void Start()
    {
        Destroy(gameObject, 0.2f);
        sr = GetComponentInChildren<SpriteRenderer>();
        startTime = Time.time;
    }

    private void Update()
    {
        Color t = sr.color;
        t.a = 1 - ((Time.time - startTime) / GhostLifeTime);
        sr.color = t;
    }
}
