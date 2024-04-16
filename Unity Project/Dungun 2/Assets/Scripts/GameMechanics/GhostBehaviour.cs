using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostBehaviour : MonoBehaviour
{
    public float GhostLifeTime = 0.2f;
    private SpriteRenderer sr;
    private float timeLived = 0;

    void Start()
    {
        Destroy(gameObject, GhostLifeTime);
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        timeLived += Time.deltaTime;
        Color t = sr.color;
        t.a = 1f - (float)(timeLived / GhostLifeTime);
        sr.color = t;
    }

    public void UpdateSprite(Sprite newSprite)
    {
       transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = newSprite;
    }
}
