using Assets.Scripts.GameMechanics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class GeneralizedInstruction
{
    public enum ComparisonMode
    {
        None,
        Greater,
        Less
    }

    public enum InstructionType
    {
        None,
        Wait,
        Rotate,
        ChangeBehaviour,
        Point,
        Shoot,
        ShootSquare,
        ShootCircle,
        Die
    }

    public InstructionType instructionType = InstructionType.None;

    public float waitTime = 0f;

    /*Rotate, Die, Shoot*/
    public GameObject enemy;
    public float rotationAngle = 0;

    /*Point*/
    public Transform target;

    /*Shoot*/
    public GameObject bulletPrefab;

    /*Comparison*/
    private EnemyBehavior script;
    public ComparisonMode realDistanceToTarget = ComparisonMode.None;
    public float distanceToTarget;

    public EnemyBehavior.MovementMode newMovementMode = EnemyBehavior.MovementMode.Idle;

    [HideInInspector, NonSerialized]
    public GameObject gunObject;
    public Transform defaultTarget;
    
    public IEnumerator Wait()
    {
        yield return new WaitForSeconds(waitTime);
    }

    public void Rotate()
    {
        enemy.transform.RotateAround(enemy.transform.position, new Vector3(0, 0, 1), rotationAngle);
        Flip();
    }

    public void Point()
    {
        if (!target) { target = defaultTarget; }
        if (!target) { return; }
        Vector2 dir = new Vector2(target.position.x - gunObject.transform.position.x, target.position.y - gunObject.transform.position.y);
        gunObject.transform.up = dir;
        Flip();
    }

    public void Shoot()
    {
        if (!target) { target = defaultTarget; }
        if (!target) { return; }
        Vector2 dir = new Vector2(target.position.x - enemy.transform.position.x, target.position.y - enemy.transform.position.y);

        GameObject newBullet = GameObject.Instantiate(bulletPrefab);
        newBullet.transform.up = dir;
        newBullet.transform.position = enemy.transform.position.ConvertTo<Vector2>();
        //newBullet.GetComponent<BulletInterface>().OnSpawn(enemy);  //rasj: data transfer for charger
    }

    public void ShootSquare(int size)
    {
        if (!target) { target = defaultTarget; }
        if (!target) { return; }

        Vector2 dir = new Vector2(target.position.x - enemy.transform.position.x, target.position.y - enemy.transform.position.y);
        float halfSize = (float)Math.Floor((decimal)size / 2);

        for (int x = 0;  x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                GameObject newBullet = GameObject.Instantiate(bulletPrefab);

                newBullet.transform.up = dir;   //rasj: set bullet direction
                newBullet.transform.position = enemy.transform.position.ConvertTo<Vector2>() - new Vector2(x - halfSize, y - halfSize);
                newBullet.GetComponent<BulletInterface>().OnSpawn(enemy);   //rasj: data transfer from old enemy to new
            }
        }
    }

    public void ShootCircle(float radius, int outerRingAmount)
    {
        if (!target) { target = defaultTarget; }
        Vector2 dir = new Vector2(target.position.x - enemy.transform.position.x, target.position.y - enemy.transform.position.y);
        float angle;
        GameObject newBullet = GameObject.Instantiate(bulletPrefab);
        int bulletRadius = (int)Math.Ceiling(newBullet.gameObject.GetComponent<CircleCollider2D>().radius);  //rasj: get bullet radius as an int

        float circumference = radius * 2 * Mathf.PI;
        float c;
        int amount;
        float densityC = outerRingAmount / circumference;  //rasj: bullets per unit based on circumference
        //float densityR = outerRingAmount / radius;

        

        for (int r = 0;  r < radius; r += bulletRadius)
        {
            c = r * 2 * Mathf.PI;  //rasj: local circumference
            //amount = (int)Math.Floor(densityR * r);  //rasj: amount based on local radius
            amount = (int)Math.Floor(densityC * c);  //rasj: amount based on local circumference

            if (r + bulletRadius >= radius)  //rasj: if last in loop
            {
                amount = outerRingAmount;
            }

            for (int i = 0; i < amount; i++)
            {
                newBullet = GameObject.Instantiate(bulletPrefab);
                angle = i * Mathf.PI * 2 / amount;  //rasj: angle in radians

                newBullet.transform.up = dir;
                newBullet.transform.position = enemy.transform.position.ConvertTo<Vector2>() - new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                newBullet.GetComponent<BulletInterface>().OnSpawn(enemy);
            }
        }
    }

    public void ChangeBehaviour()
    {
        bool condition = distanceToTarget < 0; //rasj: if "none" and distance is negative, always run, as negative distance don't exist

        if (!condition)  //rasj: if distance is not negative; if condition should not be ignored
        {
            float dist = Vector3.Distance(enemy.transform.position, target.transform.position);
            switch (realDistanceToTarget)
            {
                case ComparisonMode.Less:       //rasj: expected distance to target is more than real distance to target 
                    condition = distanceToTarget > dist;
                    break;
                case ComparisonMode.Greater:    //rasj: expected distance to target is less than real distance to target
                    condition = distanceToTarget < dist;
                    break;
            }
        }
        //rasj: another if here, cuz condition could change above
        if (condition)
        {
            script = enemy.GetComponent<EnemyBehavior>();
            script.ChangeBehaviour(newMovementMode);
        }
    }

    private void Flip()
    {
        //rasj: tried to make the gun not opside-down when enemy looking left, idk what went wrong
        float rotation = gunObject.transform.rotation.z;
        if (rotation < 0)  //rasj: left
        {
            gunObject.transform.GetChild(0).localScale = new Vector3(1, -1, 1);  //rasj: sets gun height to negative
        }
        else if (rotation > 0) //rasj: right
        {
            gunObject.transform.GetChild(0).localScale = new Vector3(1, 1, 1);
        }
    }
    /*
    private float lerp(float start, float end, float t)
    {
        return (1 - t) * start + t * end;
    }
    */
}