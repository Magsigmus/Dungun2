using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBehaviour : MonoBehaviour
{
    public Transform player;


    // Update is called once per frame
    void Update()
    {
        Vector3 newPosition = player.position;
        newPosition.z = -10;
        gameObject.transform.position = newPosition;

    }
}
