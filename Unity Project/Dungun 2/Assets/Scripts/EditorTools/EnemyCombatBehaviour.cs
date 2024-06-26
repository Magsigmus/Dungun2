using Assets.Scripts.GameMechanics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class EnemyCombatBehaviour : MonoBehaviour
{
    //public Transform defaultTarget = GameObject.FindWithTag("Player").transform;
    public List<GeneralizedInstruction> OnStart = new List<GeneralizedInstruction>();
    public List<GeneralizedInstruction> Main = new List<GeneralizedInstruction>();
    public List<GeneralizedInstruction> OnDeath = new List<GeneralizedInstruction>();

    List<InstructionType> OnStartInstructionTypeList = new List<InstructionType>();
    List<InstructionType> MainInstructionTypeList = new List<InstructionType>();
    List<InstructionType> OnDeathInstructionTypeList = new List<InstructionType>();

    bool mainDone = false;
    public GameObject gunObject;
    private Animator gunAnimator;

    // Start is called before the first frame update
    void Start()
    {
        //gunObject = gameObject.transform.GetChild(0).gameObject;
        gunAnimator = gunObject.GetComponentInChildren<Animator>();
        StartCoroutine(RunInstructions(OnStart));
    }

    // Update is called once per frame
    void Update()
    {
        //rasj: todo: make code wait till this is done
        if (mainDone)
        {
            mainDone = false;
            StartCoroutine(RunInstructions(Main));
        }

        FlipGun();
    }

    public IEnumerator RunInstructions(List<GeneralizedInstruction> instructions) //rasj: todo: run as ienumerator (to make to a coroutine)
    {
        //rasj: both cannot be OnDeath, so if both are true, instructions is different from ondeath, and thereby ok to stop running
        if (instructions.Count < 1 && OnDeath.Count > 1)  { yield return null; }  //rasj: if instruction list is shorter than 1, don't run the foreach (LIKE YOU'RE FUCKING DESIGNED TO)
        foreach (GeneralizedInstruction instruction in instructions)
        {
            instruction.gunObject = gunObject;
            instruction.target = GameObject.FindGameObjectWithTag("Player").transform;

            switch (instruction.instructionType)
            {
                case GeneralizedInstruction.InstructionType.Wait:
                    //rasj: only yield return on ienumerators cuz then it waits to be done
                    yield return instruction.Wait();
                    break;
                case GeneralizedInstruction.InstructionType.Rotate:
                    instruction.Rotate();
                    break;
                case GeneralizedInstruction.InstructionType.ChangeBehaviour:
                    instruction.ChangeBehaviour();
                    break;
                case GeneralizedInstruction.InstructionType.Point:
                    instruction.Point();
                    break;
                case GeneralizedInstruction.InstructionType.Shoot:
                    instruction.Shoot();
                    StartCoroutine(StartShootAnimation());
                    break;
                case GeneralizedInstruction.InstructionType.ShootSquare:
                    instruction.ShootSquare(4);
                    break;
                case GeneralizedInstruction.InstructionType.ShootCircle:
                    instruction.ShootCircle(2, 15);
                    break;
                case GeneralizedInstruction.InstructionType.Die:
                    //rasj: if die is not in ondeath
                    if (!OnDeath.Any(x => x.instructionType == GeneralizedInstruction.InstructionType.Die)) { //rasj: prevents infinte loop by not running ondeath if ondeath has die
                        if (OnDeath.Count < 1 || OnDeath[^1].instructionType != GeneralizedInstruction.InstructionType.Die)  //if last instruction not death
                        {
                            /*rasj: adds a die instruction, as it is the easiest way
                                    to make sure it dies, as it'll just run that instruction*/
                            GeneralizedInstruction dieInstruction = new GeneralizedInstruction();
                            dieInstruction.instructionType = GeneralizedInstruction.InstructionType.Die;

                            OnDeath.Add(dieInstruction);
                        }
                        yield return RunInstructions(OnDeath);
                        yield return null;
                    }
                    else if (OnDeath[^1].instructionType == GeneralizedInstruction.InstructionType.Die)
                    {
                        GetComponent<EnemyBehavior>().Death(); //rasj: kill john lennon >:)
                    }
                    break;
            }
        }
        mainDone = true;
        yield return null;
    }

    private void FlipGun()
    {
        GameObject gunSpriteGameObject = gunObject.transform.GetChild(0).gameObject;
        int side = Math.Sign(gunObject.transform.eulerAngles.z - 180);
        side = ((side == 0) ? 1 : side);
        Vector3 newScale = gunSpriteGameObject.transform.localScale;
        newScale.y = Math.Abs(newScale.y) * side;
        gunSpriteGameObject.transform.localScale = newScale;
    }

    public IEnumerator StartShootAnimation()
    {
        gunAnimator.SetBool("Shoot", true);
        yield return new WaitForEndOfFrame();
        gunAnimator.SetBool("Shoot", false);
    }

    /*
    void runInstructions(List<GeneralizedInstruction.InstructionType> instructions)
    {
        foreach (GeneralizedInstruction.InstructionType instruction in instructions)  //rasj: loop over each instruction
        {
            switch (instruction)
            {
                case GeneralizedInstruction.InstructionType.Rotate:
                    GeneralizedInstruction.Rotate();
                    break;
                case GeneralizedInstruction.InstructionType.Point:
                    GeneralizedInstruction.Point();
                    break;
                case GeneralizedInstruction.InstructionType.Shoot:
                    GeneralizedInstruction.Shoot();
                    break;
                case GeneralizedInstruction.InstructionType.ChangeBehaviour:
                    GeneralizedInstruction.ChangeBehaviour();
                    break;
                case GeneralizedInstruction.InstructionType.Die:
                    if (OnDeath.Any(i => i == GeneralizedInstruction.InstructionType.Die)) {
                        runInstructions(OnDeath);
                    }
                    break;
            }
        }
    }*/

    public enum InstructionLists
    {
        Main,
        OnStart,
        OnDeath
    }
}
