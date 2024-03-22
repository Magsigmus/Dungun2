using Assets.Scripts.GameMechanics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyCombatBehaviour : MonoBehaviour
{
    public List<Instruction> Main = new List<Instruction>();
    public List<Instruction> OnStart = new List<Instruction>();
    public List<Instruction> OnDeath = new List<Instruction>();
    public List<InstructionType> test = new List<InstructionType>();

    // Start is called before the first frame update
    void Start()
    {
        runInstructions(OnStart);
    }

    // Update is called once per frame
    void Update()
    {
        runInstructions(Main);
    }

    void runInstructions(List<Instruction> instructions)
    {
        foreach (Instruction instruction in instructions)  //rasj: loop over each instruction
        {
            if (instruction.type == InstructionType.Die && instructions.Find(i => i.type == InstructionType.Die) == null) //rasj: if gonna die, and OnDeath does not contain die (avoid infinite loop), run
            { 
                runInstructions(OnDeath);
                return;
            }
            instruction.ExecuteInstruction();
        }
    }

    public enum InstructionLists
    {
        Main,
        OnStart,
        OnDeath
    }
}
