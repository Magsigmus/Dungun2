using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Random rnd = new Random();
        float movespeed = .05f;
    }
}


// Update is called once per frame
void Update()
{
    int verHor = rnd.Next(0, 4);  // 0 - 3
    int dir = rnd.next(0, 2) * 2 - 1; //either -1 or 1
}

