using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBehaviour : MonoBehaviour
{
    public Transform player;

    [Header("Screen Shake Settings")]
    public float magnitude = 1f;
    public float duration = 1f;
    private float shakingTimer;

    private void Start()
    {
        shakingTimer = duration;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 newPosition = player.position;
        newPosition.z = -10;
        gameObject.transform.position = newPosition;

        if(shakingTimer < duration)
        {
            gameObject.transform.position += (Vector3)Random.insideUnitCircle * magnitude * ((duration - shakingTimer) / duration);
        }

        shakingTimer += Time.deltaTime;
    }

    public void StartScreenShake()
    {
        shakingTimer = 0;
    }
}
