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
    private float timer = 0.0f;  //rasj: for time management; ignore

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

    public void Wait()
    {
        while (timer < waitTime)  //rasj: wait until time has passed
        {
            timer += Time.deltaTime;
        }
    }

    public void Rotate()
    {
        enemy.transform.RotateAround(enemy.transform.position, new Vector3(0, 0, 1), rotationAngle);
    }

    public void Point()
    {
        enemy.transform.up = target.position - enemy.transform.position;
    }

    public void Die()
    {
        //EnemyBehavior.Death();
        //rasj: y u no run?
    }

    public void Shoot()
    {
        GameObject newBullet = GameObject.Instantiate(bulletPrefab);
        newBullet.transform.up = enemy.transform.up;
        newBullet.transform.position = enemy.transform.position;
    }

    public void ChangeBehaviour()
    {
        float dist = Vector3.Distance(enemy.transform.position, target.transform.position);
        bool condition = false;

        switch (realDistanceToTarget)
        {
            case ComparisonMode.Less:       //rasj: expected distance to target > real distance to target 
                condition = distanceToTarget > dist;
                break;
            case ComparisonMode.Greater:    //rasj: expected distance to target < real distance to target
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