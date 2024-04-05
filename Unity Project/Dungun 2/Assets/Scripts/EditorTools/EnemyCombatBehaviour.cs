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
        //runInstructions(OnStart);
    }

    // Update is called once per frame
    void Update()
    {
        
        //runInstructions(Main);
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
