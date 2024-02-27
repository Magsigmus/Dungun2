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
    public int numberOfGhosts = 5;
    public GameObject ghostPrefab;
    public bool dashing = false;
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

    private void Awake()
    {
        playerControls = new PlayerControls();
        rb2D = GetComponent<Rigidbody2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        colliders = GetComponents<Collider2D>();
    }

    private void OnEnable() { playerControls.Enable(); }
    private void OnDisable() { playerControls.Disable(); }

    private void Update()
    {
        cooldown += Time.deltaTime;
        dashingTimer += Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (dashing) { return; }
        if (dashingTimer > dashingCooldown && playerControls.Default.Dash.phase == InputActionPhase.Performed) { StartCoroutine("Dash"); return; }

        PointGun();
        Move();

        if (playerControls.Default.Shoot.phase == InputActionPhase.Performed)
        {
            Shoot();
        }
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

        Debug.Log("MOVING");

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

    void TakeDamage(int damage)
    {
        healthPoints -= damage;

        if(healthPoints <= 0)
        {
            Debug.Log("PLAYER DEAD!");
        }
    }

    IEnumerator Dash()
    {
        Vector2 dashDir = vel.normalized;
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, dashDir, dashingDistance, LayerMask.GetMask("Obstacles"));
        dashing = true;

        rb2D.velocity = new Vector2();

        float maxDist = dashingDistance;
        float distTravelled = 0;
        if (hit.collider != null)
        {
            maxDist = Vector2.Distance(transform.position, hit.point - dashDir);
        }

        Debug.Log($"MaxDist: {maxDist}");

        Hide();
        for(int i = 0; i < numberOfGhosts; i++)
        {
            distTravelled += dashingDistance / (float)numberOfGhosts;
            Debug.Log(distTravelled);
            distTravelled = Math.Clamp(distTravelled, 0, maxDist);
            GameObject newGhost = Instantiate(ghostPrefab);
            newGhost.transform.position = transform.position + distTravelled * dashDir.ConvertTo<Vector3>();
            
            yield return new WaitForSecondsRealtime(dashingTime / (float)numberOfGhosts);
        }

        Debug.Log($"DashTravelled: {distTravelled}");
        Debug.Log(dashDir);
        Debug.Log(dashDir.ConvertTo<Vector3>());

        transform.position += maxDist * dashDir.ConvertTo<Vector3>();
        Show();

        rb2D.velocity = vel;
        
        dashing = false;
        dashingTimer = 0;
    }

    void Hide()
    {
        foreach(SpriteRenderer rnd in renderers) { rnd.enabled = false; }
        foreach(Collider2D col in colliders) { col.enabled = false; }
    }

    void Show()
    {
        foreach (SpriteRenderer rnd in renderers) { rnd.enabled = true; }
        foreach (Collider2D col in colliders) { col.enabled = true; }
    }
}
