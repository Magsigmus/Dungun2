using Assets.Scripts.GameMechanics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class EnemyCombatBehaviour : MonoBehaviour
{
    public List<GeneralizedInstruction> OnStart = new List<GeneralizedInstruction>();
    public List<GeneralizedInstruction> Main = new List<GeneralizedInstruction>();
    public List<GeneralizedInstruction> OnDeath = new List<GeneralizedInstruction>();

    List<InstructionType> OnStartInstructionTypeList = new List<InstructionType>();
    List<InstructionType> MainInstructionTypeList = new List<InstructionType>();
    List<InstructionType> OnDeathInstructionTypeList = new List<InstructionType>();


    // Start is called before the first frame update
    void Start()
    {
        RunInstructions(OnStart, "start");
    }

    

    // Update is called once per frame
    void Update()
    {
        RunInstructions(Main, "main");
    }

    public void RunInstructions(List<GeneralizedInstruction> instructions, string type)    //rasj: type is needed to make sure that death is not run infinitly
    {
        foreach (GeneralizedInstruction instruction in instructions)
        {
            switch (instruction.instructionType)
            {
                case GeneralizedInstruction.InstructionType.Wait:
                    instruction.Wait();
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
                    break;
                case GeneralizedInstruction.InstructionType.Die:
                    if (type != "death") { RunInstructions(OnDeath, "death"); }     //rasj: actually makes sure that death is not run infinitly
                    else {
                        Debug.Log("ded");
                        GetComponent<EnemyBehavior>().Death();
                        //instruction.Die();  //rasj: if not already wanna die, 
                    }     

                    break;
            }
        }
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
