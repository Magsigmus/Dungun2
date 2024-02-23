using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    public float acceleration, deacceleration, maxSpeed, drag;
    public Vector2 vel;
    private Rigidbody2D rb2D;
    private PlayerControls playerControls;

    private void Awake()
    {

        playerControls = new PlayerControls();
        rb2D = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Move();
    }

    void PointToMouse()
    { // Makes the player point to the mouse
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition); // Gets the mouse position in world space
        Vector2 dir = new Vector2(mousePos.x - transform.position.x, mousePos.y - transform.position.y); // findes the vector to the mouse point
        transform.up = dir; // Sets the place where the gameobject is pointing to that vector
    }

    void Move()
    { // Makes the player move dependent on inputs

        Vector2 input = playerControls.Default.Move.ReadValue<Vector2>();

        Debug.Log(input);

        vel.x = AccelerateVal(input.x, vel.x);
        vel.y = AccelerateVal(input.y, vel.y);

        rb2D.velocity = vel; // Sets the velocity of the rigidbody to the vector
    }

    float AccelerateVal(float change, float val)
    {
        float result = val;
        if(Math.Sign(change) == Math.Sign(val) || val == 0)
        {
            result = Math.Clamp(result + change * acceleration, -maxSpeed * Math.Abs(change), maxSpeed * Math.Abs(change));
            
        }
        else
        {
            result += change * deacceleration;
        }

        if (drag > Math.Abs(result)) { result = 0; }
        else { result += -Math.Sign(val) * drag; ; }

        return result;
    }
}
