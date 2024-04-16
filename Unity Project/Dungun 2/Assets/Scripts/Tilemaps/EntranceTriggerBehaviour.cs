using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntranceTriggerBehaviour : MonoBehaviour
{
    public int componentIndex, roomIndex;
    public (int, Vector2Int) thisEntrance;
    LevelManger manager;

    // Start is called before the first frame update
    void Start()
    {
        manager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<LevelManger>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag != "Player") { return; }
        Debug.Log($"Detected player in room {roomIndex}");
        StartCoroutine(manager.DiscoverRoom(roomIndex, thisEntrance));
    }
}
