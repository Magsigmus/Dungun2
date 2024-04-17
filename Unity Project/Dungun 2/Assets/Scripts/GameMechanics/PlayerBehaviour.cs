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
using UnityEngine.SceneManagement;
using System.Linq.Expressions;

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
    public HeathBehaviour healthManager;

    [Header("Animation and VFX Settings")]
    public GameObject spriteMaskPrefab;
    private GameObject instantiatedSpriteMask;
    public Animator gunAnimator;
    public GameObject deathVFX;
    public float deathVFXtime = 2f;

    [Header("Audio Settings")]
    public AudioSource source;
    public AudioClip shootSound;
    public AudioClip hurtSound;
    public AudioClip deathSound;

    [HideInInspector]
    public LevelManger manager;

    private GameObject spriteGameobject;
    private Vector2 vel, dir;
    private Rigidbody2D rb2D;
    private PlayerControls playerControls;
    private float cooldown = 0f;
    private SpriteRenderer[] renderers;
    private Collider2D[] colliders;
    private Animator animator;
    private GameObject gunSpriteGameObject;
    private bool dead = false;

    private void Awake()
    {
        playerControls = new PlayerControls();
        rb2D = GetComponent<Rigidbody2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        colliders = GetComponents<Collider2D>();
        animator = GetComponentInChildren<Animator>();
        spriteGameobject = gameObject.transform.Find("Sprite").gameObject;
        healthManager.MaxHitPoints = healthPoints;
        gunSpriteGameObject = gunObject.transform.GetChild(0).gameObject;
        //Debug.Log(animator.gameObject.name);
    }

    private void OnEnable() { playerControls.Enable(); }
    private void OnDisable() { playerControls.Disable(); }

    private void Update()
    {
        if (dead) { return; }
        cooldown += Time.deltaTime;
        dashingTimer += Time.deltaTime;
        animator.SetFloat("Speed", vel.magnitude);  //rasj: set animation speed to speed of player
    }

    void FixedUpdate()
    {
        if (dead) { return; }

        //Sig: Dash handling
        if (dashing) { return; }
        if (dashingTimer > dashingCooldown &&
            playerControls.Default.Dash.phase == InputActionPhase.Performed &&
            rb2D.velocity.magnitude > 0) //rasj: also if actually moving, so no dashing in place
        {
            StartCoroutine("Dash"); return;
        }

        PointGun();
        Move();
        Flip(); //rasj: flip the sprite

        //Sig: Ensures that the player can shoot.
        if (playerControls.Default.Shoot.phase == InputActionPhase.Performed)
        {
            StartCoroutine(Shoot());
        }
    }

    void Flip()
    {
        float dirX = playerControls.Default.Move.ReadValue<Vector2>().x;
        if (dirX < 0 )  //rasj: left
        {
            spriteGameobject.transform.rotation = Quaternion.Euler(0, 180f, 0);
        }
        else if (dirX > 0) //rasj: right
        {
            spriteGameobject.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    //Sig: Makes the player shoot.
    IEnumerator Shoot()
    {
        Transform gunObject = transform.Find("Gun");

        //Sig: Check if the player can shoot.
        if (cooldown > cooldownTime)
        {
            cooldown = 0;

            source.PlayOneShot(shootSound);

            gunAnimator.SetBool("Shoot", true);

            //Sig: Spawn bullet
            GameObject newBullet = Instantiate(bulletPrefab);
            newBullet.transform.up = dir;
            newBullet.transform.position = dir + transform.position.ConvertTo<Vector2>();
        }

        yield return new WaitForEndOfFrame();

        gunAnimator.SetBool("Shoot", false);
    }

    //Sig: Find the direction the bullets should point in
    void PointGun()
    {
        bool joyStickConnected = ((Input.GetJoystickNames().Length > 0) ? (Input.GetJoystickNames()[0] != "") : false);
        //TODO: potentially add dualshock 4 compatability

        Vector2 zeroV = Vector2.zero;

        if (joyStickConnected)
        {
            //Sig: If a controller is connected, then get input from that
            Vector2 rightStick = playerControls.Default.Pointing.ReadValue<Vector2>();
            if (rightStick != zeroV) { dir = rightStick.normalized; }  //rasj: makes gun point towards last pointed direction instead of up
        }
        else
        {
            //Sig: If there isn't a controller connected, then get the relative postion of the mouse in world space.
            dir = PointToMouse().normalized;
        }

        gunObject.transform.up = dir;

        FlipGun();
    }

    private void FlipGun()
    {
        int side = Math.Sign(gunObject.transform.eulerAngles.z - 180);
        side = ((side == 0) ? 1 : side);
        Vector3 newScale = gunSpriteGameObject.transform.localScale;
        newScale.y = Math.Abs(newScale.y) * side;
        gunSpriteGameObject.transform.localScale = newScale;
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

    public void TakeDamage(int damage)
    {
        healthPoints -= damage;

        healthManager.CurrentHitPoints = healthPoints;

        if (healthPoints <= 0)
        {
            if (dead) { return; }
            source.PlayOneShot(deathSound);
            StartCoroutine(Death());
            return;
        }

        source.PlayOneShot(hurtSound);
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
        Sprite ghostSprite = spriteGameobject.GetComponent<SpriteRenderer>().sprite;

        float distTravelled = 0;
        for (int i = 0; i < numberOfGhosts; i++)
        {
            distTravelled += dashingDistance / (float)numberOfGhosts;
            distTravelled = Math.Clamp(distTravelled, 0, maxDist);
            GameObject newGhost = Instantiate(ghostPrefab);
            newGhost.transform.position = transform.position + distTravelled * dashDir.ConvertTo<Vector3>();
            newGhost.GetComponent<GhostBehaviour>().UpdateSprite(ghostSprite);
            newGhost.transform.rotation = spriteGameobject.transform.rotation;
            newGhost.transform.localScale = spriteGameobject.transform.lossyScale;

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

    public void StartExpandAnimation()
    {
        if(instantiatedSpriteMask != null) { Destroy(instantiatedSpriteMask); }
        instantiatedSpriteMask = Instantiate(spriteMaskPrefab);
        instantiatedSpriteMask.transform.parent = transform;
        instantiatedSpriteMask.transform.localPosition = new Vector3();
    }

    private IEnumerator Death()
    {
        dead = true;

        gunSpriteGameObject.GetComponent<SpriteRenderer>().enabled = false;
        Destroy(instantiatedSpriteMask);
        GetComponentInChildren<SpriteMask>().enabled = false;
        rb2D.velocity = new Vector2();
        rb2D.isKinematic = true;
        animator.SetBool("Dead", true);
        manager.RevealNextRoomPermentantly();

        Instantiate(deathVFX, gameObject.transform.position, Quaternion.identity);

        yield return new WaitForSeconds(deathVFXtime);

        SceneManager.LoadScene(1);  //rasj: 1 is the Game Over scene in build settings
    }
}
