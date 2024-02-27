using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.Controls.AxisControl;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class PlayerBehaviour : MonoBehaviour
{
    [Header("Movement Settings")]
    public float acceleration;
    public float deacceleration, maxSpeed, drag;

    [Header("Combat Settings")]
    public GameObject gunObject;
    public GameObject bulletPrefab;
    public float cooldownTime = 1f;

    private Vector2 vel, dir;
    private Rigidbody2D rb2D;
    private PlayerControls playerControls;
    private float cooldown = 0f;

    private void Awake()
    {
        playerControls = new PlayerControls();
        rb2D = GetComponent<Rigidbody2D>();
    }

    private void OnEnable() { playerControls.Enable(); }
    private void OnDisable() { playerControls.Disable(); }

    private void Update()
    {
        PointGun();
        cooldown += Time.deltaTime;

        if(playerControls.Default.Shoot.phase == InputActionPhase.Performed)
        {
            Shoot();
        }
    }

    void FixedUpdate()
    {
        Move();
    }

    void Shoot()
    {
        if (cooldown > cooldownTime)
        {
            cooldown = 0;
            GameObject newBullet = Instantiate(bulletPrefab);
            newBullet.transform.up = dir;
            newBullet.transform.position = dir + transform.position.ConvertTo<Vector2>();
        }
    }

    void PointGun()
    {
        Debug.Log(Input.GetJoystickNames()[0]);

        if (Input.GetJoystickNames()[0] != "")
        {
            Vector2 rightStick = playerControls.Default.Pointing.ReadValue<Vector2>();
            dir = rightStick.normalized;
        }
        else
        {
            dir = PointToMouse().normalized;
        }

        gunObject.transform.up = dir;
    }

    Vector2 PointToMouse()
    { //Sig: Makes the player point to the mouse
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition); // Gets the mouse position in world space
        Vector2 dir = new Vector2(mousePos.x - transform.position.x, mousePos.y - transform.position.y); // findes the vector to the mouse point
        return dir;
    }

    void Move()
    { //Sig: Makes the player move dependent on inputs

        Vector2 input = playerControls.Default.Move.ReadValue<Vector2>(); //Sig: Reads input

        vel.x = AccelerateVelocity(input.x, vel.x);
        vel.y = AccelerateVelocity(input.y, vel.y);

        rb2D.velocity = vel; //Sig: Sets the velocity of the rigidbody to the vector
    }

    float AccelerateVelocity(float change, float val)
    {
        //Sig: Takes account for the change in the velocity caused by the caused change
        float result = val;
        if(Math.Sign(change) == Math.Sign(val) || val == 0)
        {
            //Sig: If the change is in the same direction as the current movement, then use the acceleration value, and clamp to the max velocity.
            result = Math.Clamp(result + change * acceleration, 
                -maxSpeed * Math.Abs(change), 
                maxSpeed * Math.Abs(change));
        }
        else
        {
            // Sig: If the change is in the opposite direction as the current movement, then use the deacceleration value, and clamp to the max velocity.
            result += change * deacceleration;
        }

        //Sig: Apply drag.
        if (drag > Math.Abs(result)) { result = 0; }
        else { result += -Math.Sign(val) * drag; ; }

        return result;
    }
}
