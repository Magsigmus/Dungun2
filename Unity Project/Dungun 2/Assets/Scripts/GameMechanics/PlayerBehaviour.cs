using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;
using static UnityEngine.InputSystem.Controls.AxisControl;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class PlayerBehaviour : MonoBehaviour
{
    [Header("Basic Movement Settings")]
    public float acceleration;
    public float deacceleration, maxSpeed, drag;

    [Header("Dashing Movement Settings")]
    public float dashingDistance;
    public float dashingTime;
    public float dashingCooldown = 1;
    public float inputBufferingTime = 0.2f;
    public int numberOfGhosts = 5;
    public GameObject ghostPrefab;
    private bool dashing = false;
    private float dashingTimer = 0;

    [Header("Combat Settings")]
    public GameObject gunObject;
    public GameObject bulletPrefab;
    public float cooldownTime = 1f;
    public int healthPoints = 5;

    private Vector2 vel, dir;
    private Rigidbody2D rb2D;
    private PlayerControls playerControls;
    private float cooldown = 0f;
    private SpriteRenderer[] renderers;
    private Collider2D[] colliders;
    private Animator animator;

    private bool facingRight = true;

    private void Awake()
    {
        playerControls = new PlayerControls();
        rb2D = GetComponent<Rigidbody2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        colliders = GetComponents<Collider2D>();
        animator = GetComponentInChildren<Animator>();
        Debug.Log(animator.gameObject.name);
    }

    private void OnEnable() { playerControls.Enable(); }
    private void OnDisable() { playerControls.Disable(); }

    private void Update()
    {
        cooldown += Time.deltaTime;
        dashingTimer += Time.deltaTime;
        animator.SetFloat("Speed", vel.magnitude);  //rasj: set animation speed to speed of player
    }

    void FixedUpdate()
    {
        //Sig: Dash handling
        if (dashing) { return; }
        if (dashingTimer > dashingCooldown &&
            playerControls.Default.Dash.phase == InputActionPhase.Performed)
        {
            StartCoroutine("Dash"); return;
        }

        PointGun();
        Move();
        Flip(); //rasj: flip the sprite

        //Sig: Ensures that the player can shoot.
        if (playerControls.Default.Shoot.phase == InputActionPhase.Performed)
        {
            Shoot();
        }

        
    }

    void Flip()
    {
        float dirX = playerControls.Default.Move.ReadValue<Vector2>().x;
        if (dirX < 0 )  //rasj: left
        {
            gameObject.transform.rotation = Quaternion.Euler(0, 180f, 0);
        }
        else if (dirX > 0) //rasj: right
        {
            gameObject.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    //Sig: Makes the player shoot.
    void Shoot()
    {
        //Sig: Check if the player can shoot.
        if (cooldown > cooldownTime)
        {
            cooldown = 0;

            //Sig: Spawn bullet
            GameObject newBullet = Instantiate(bulletPrefab);
            newBullet.transform.up = dir;
            newBullet.transform.position = dir + transform.position.ConvertTo<Vector2>();
        }
    }

    //Sig: Find the direction the bullets should point in
    void PointGun()
    {
        bool joyStickConnected = ((Input.GetJoystickNames().Length > 0) ? (Input.GetJoystickNames()[0] != "") : false);
        Vector2 zeroV = Vector2.zero;

        if (joyStickConnected)
        {
            //Sig: If a controller is connected, then get input from that
            Vector2 rightStick = playerControls.Default.Pointing.ReadValue<Vector2>();
            if (rightStick != zeroV) { dir = rightStick.normalized; }  //rasj: fixes gun pointing up when not aiming
        }
        else
        {
            //Sig: If there isn't a controller connected, then get the relative postion of the mouse in world space.
            dir = PointToMouse().normalized;
        }

        gunObject.transform.up = dir;
    }

    //Sig: Gets the vector that points to the mouse.
    Vector2 PointToMouse()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition); //Sig: Gets the mouse position in world space
        Vector2 dir = new Vector2(mousePos.x - transform.position.x, mousePos.y - transform.position.y); //Sig: findes the vector to the mouse point
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
        if (Math.Sign(change) == Math.Sign(val) || val == 0)
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

    void TakeDamage(int damage)
    {
        healthPoints -= damage;

        if (healthPoints <= 0)
        {
            Debug.Log("PLAYER DEAD!");
        }
    }

    IEnumerator Dash()
    {
        dashing = true;

        //Sig: Gives the player some time to change the direction equal to inputBufferingTime
        Vector2 dashDir = vel.normalized;
        float t = Time.time;
        while (Time.time - t < inputBufferingTime)
        {
            dashDir = playerControls.Default.Move.ReadValue<Vector2>().normalized;
            yield return new WaitForEndOfFrame();
        }

        //Sig: Hides the player and stops it from moving.
        Hide();
        rb2D.velocity = new Vector2();

        //Sig: Casts a ray which finds out how long the player can dash
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, dashDir, dashingDistance, LayerMask.GetMask("Obstacles"));

        //Sig: Finds the max distance the player can dash
        float maxDist = dashingDistance;
        if (hit.collider != null)
        {
            maxDist = Vector2.Distance(transform.position, hit.point - dashDir);
        }

        //Sig: Makes the dash effect
        float distTravelled = 0;
        for (int i = 0; i < numberOfGhosts; i++)
        {
            distTravelled += dashingDistance / (float)numberOfGhosts;
            distTravelled = Math.Clamp(distTravelled, 0, maxDist);
            GameObject newGhost = Instantiate(ghostPrefab);
            newGhost.transform.position = transform.position + distTravelled * dashDir.ConvertTo<Vector3>();

            yield return new WaitForSecondsRealtime(dashingTime / (float)numberOfGhosts);
        }

        //Sig: Moves the player and sets the velocity
        transform.position += maxDist * dashDir.ConvertTo<Vector3>();
        rb2D.velocity = vel;

        //Sig: Resets certain values, and shows the player.
        Show();
        dashing = false;
        dashingTimer = 0;
    }

    void Hide()
    {
        foreach (SpriteRenderer rnd in renderers) { rnd.enabled = false; }
        foreach (Collider2D col in colliders) { col.enabled = false; }
    }

    void Show()
    {
        foreach (SpriteRenderer rnd in renderers) { rnd.enabled = true; }
        foreach (Collider2D col in colliders) { col.enabled = true; }
    }
}
