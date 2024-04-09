using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntranceTriggerBehaviour : MonoBehaviour
{
    LevelManger manager;

    // Start is called before the first frame update
    void Start()
    {
        manager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<LevelManger>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        
    }

}
