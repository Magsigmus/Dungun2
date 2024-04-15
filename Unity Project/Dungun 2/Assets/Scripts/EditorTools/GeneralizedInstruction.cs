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

    public IEnumerator Wait()  //rasj: run as ienumerator (to make to a coroutine)
    {
        yield return new WaitForSeconds(waitTime);  //rasj: wait the correct amount of time
    }

    public void Rotate()
    {
        enemy.transform.RotateAround(enemy.transform.position, new Vector3(0, 0, 1), rotationAngle);
    }

    public void Point()
    {
        if (!target) { target = defaultTarget; }
        Vector2 dir = new Vector2(target.position.x - gunObject.transform.position.x, target.position.y - gunObject.transform.position.y);
        gunObject.transform.up = dir;
    }

    /*
    public void Die()
    {
        //EnemyBehavior.Death();
        //rasj: y u no run?
    }
    */

    public void Shoot()
    {
        if (!target) { target = defaultTarget; }
        Vector2 dir = new Vector2(target.position.x - enemy.transform.position.x, target.position.y - enemy.transform.position.y);
        GameObject newBullet = GameObject.Instantiate(bulletPrefab);
        newBullet.transform.up = dir;
        newBullet.transform.position = enemy.transform.position.ConvertTo<Vector2>();
        newBullet.GetComponent<BulletInterface>().OnSpawn(enemy);
    }

    public void ChangeBehaviour()
    {
        float dist = Vector3.Distance(enemy.transform.position, target.transform.position);
        bool condition = false;

        switch (realDistanceToTarget)
        {
            case ComparisonMode.Less:       //rasj: expected distance to target is more than real distance to target 
                condition = distanceToTarget > dist;
                break;
            case ComparisonMode.Greater:    //rasj: expected distance to target is less than real distance to target
                condition = distanceToTarget < dist;
                break;
        }
        if (condition)
        {
            script = enemy.GetComponent<EnemyBehavior>();
            script.ChangeBehaviour(newMovementMode);
        }
    }
}