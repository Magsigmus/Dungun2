using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Assets.Scripts.GameMechanics;
using Unity.VisualScripting;


[CustomEditor(typeof(EnemyCombatBehaviour))]
[CanEditMultipleObjects]
public class EnemyCombatBehaviourEditor : Editor
{
    SerializedProperty OnStart; //InstructionTypeListProp
    SerializedProperty Main;    //InstructionTypeListProp
    SerializedProperty OnDeath; //InstructionTypeListProp

    private void OnEnable()
    {
        //rasj: finds the "InstructionTypeList" property within the serialized object
        OnStart = serializedObject.FindProperty("OnStart");
        Main = serializedObject.FindProperty("Main");
        OnDeath = serializedObject.FindProperty("OnDeath");   
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        /*
        serializedObject.Update();
        
        if (onStartInstructionTypeListProp.arraySize > 0)   //rasj: don't show if list is empty
        {
            EditorGUILayout.LabelField("On Start:", EditorStyles.boldLabel);

            for (int i = 0; i < onStartInstructionTypeListProp.arraySize; i++)
            {
                SerializedProperty instructionTypeProp = onStartInstructionTypeListProp.GetArrayElementAtIndex(i); //rasj: get SerializedProperty for current element

                InstructionType instructionType = (InstructionType)instructionTypeProp.enumValueIndex; //rasj: get InstructionType enum value from property

                DrawInstructionsUI(instructionType);

                serializedObject.ApplyModifiedProperties();

                if (instructionType == InstructionType.Die) { break; }  //rasj: break if die, as no instruction afterwards will run anyways
            }

            EditorGUILayout.Separator();
        }

        if (mainInstructionTypeListProp.arraySize > 0)
        {
            EditorGUILayout.LabelField("Main:", EditorStyles.boldLabel);

            for (int i = 0; i < mainInstructionTypeListProp.arraySize; i++)
            {
                SerializedProperty instructionTypeProp = mainInstructionTypeListProp.GetArrayElementAtIndex(i); //rasj: get SerializedProperty for current element

                InstructionType instructionType = (InstructionType)instructionTypeProp.enumValueIndex; //rasj: get InstructionType enum value from property

                DrawInstructionsUI(instructionType);

                serializedObject.ApplyModifiedProperties();

                if (instructionType == InstructionType.Die) { break; }  //rasj: break if die, as no instruction afterwards will run anyways
            }
            EditorGUILayout.Separator();
        }

        if (onDeathInstructionTypeListProp.arraySize > 0)
        {
            EditorGUILayout.LabelField("On Death:", EditorStyles.boldLabel);

            for (int i = 0; i < onDeathInstructionTypeListProp.arraySize; i++)
            {
                SerializedProperty instructionTypeProp = onDeathInstructionTypeListProp.GetArrayElementAtIndex(i); //rasj: get SerializedProperty for current element

                InstructionType instructionType = (InstructionType)instructionTypeProp.enumValueIndex; //rasj: get InstructionType enum value from property

                if (instructionType != InstructionType.Die) { DrawInstructionsUI(instructionType); }  //rasj: should not try to die when dying
                else { break; } //rasj: break if die, as no instruction afterwards will run anyways

                serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.Separator();
        }
    }

    static void DrawInstructionsUI(InstructionType instructionType)
    {
        switch (instructionType)
        {
            case InstructionType.Wait:
                EditorGUILayout.LabelField("Wait (seconds)");
                GeneralizedInstruction.time = EditorGUILayout.FloatField(GeneralizedInstruction.time);
                break;
            case InstructionType.Rotate:
                EditorGUILayout.LabelField("Rotate");
                GeneralizedInstruction.angle = EditorGUILayout.FloatField(GeneralizedInstruction.angle);
                break;
            case InstructionType.Point:
                EditorGUILayout.LabelField("Point to");
                GeneralizedInstruction.target = (Transform)EditorGUILayout.ObjectField("Target: ", GeneralizedInstruction.target, typeof(Transform), true);
                break;
            case InstructionType.Shoot:
                EditorGUILayout.LabelField("Shoot");
                GeneralizedInstruction.bulletPrefab = (GameObject)EditorGUILayout.ObjectField("Bullet: ", GeneralizedInstruction.bulletPrefab, typeof(GameObject), true);
                break;
            case InstructionType.ChangeBehaviour:
                EditorGUILayout.LabelField("ChangeBehaviour");
                //rasj: set var to input from the floatfield/enumpopup, where the default is the var
                GeneralizedInstruction.distToPlayer = EditorGUILayout.FloatField(GeneralizedInstruction.distToPlayer);
                GeneralizedInstruction.comparisonMode = (GeneralizedInstruction.ComparisonMode)EditorGUILayout.EnumPopup(GeneralizedInstruction.comparisonMode);
                EditorGUILayout.LabelField("Distance to player");
                GeneralizedInstruction.newMovementMode = (EnemyBehavior.MovementMode)EditorGUILayout.EnumPopup(GeneralizedInstruction.newMovementMode);
                break;
            case InstructionType.Die:
                EditorGUILayout.LabelField("Die.");
                break;
        }*/
    }
}
