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

    public float waitTime = 1f;
    private float timer = 0.0f;

    /*Rotate, Die, Shoot*/
    public Transform enemy;
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


    void Main()
    {
        switch (instructionType)
        {
            case InstructionType.Wait:
                Wait();
                break;
            case InstructionType.Rotate:
                Rotate(); 
                break;
            case InstructionType.Point:
                Point(); 
                break;
            case InstructionType.Shoot:
                Shoot(); 
                break;
            case InstructionType.ChangeBehaviour:
                ChangeBehaviour(); 
                break;
            case InstructionType.Die: 
                Die(); 
                break;
        }
    }

    public void Wait()
    {
        while (timer < waitTime)  //rasj: wait until time has passes
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
        enemy.transform.up = target.position - enemy.position;
    }

    public void Die()
    {
        GameObject.Destroy(enemy.gameObject);
    }

    public void Shoot()
    {
        GameObject newBullet = GameObject.Instantiate(bulletPrefab);
        newBullet.transform.up = enemy.transform.up;
        newBullet.transform.position = enemy.transform.position + newBullet.transform.up;
    }

    public void ChangeBehaviour()
    {
        float dist = Vector3.Distance(enemy.transform.position, target.transform.position);
        bool condition = false;

        switch (realDistanceToTarget)
        {
            case ComparisonMode.Less:
                condition = distanceToTarget < dist;
                break;
            case ComparisonMode.Greater:
                condition = distanceToTarget > dist;
                break;
        }
        if (condition)
        {
            script.ChangeBehaviour(newMovementMode);
        }
    }
}