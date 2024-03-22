using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;

namespace Assets.Scripts.GameMechanics
{
    [Serializable]
    abstract public class Instruction
    {
        abstract public IEnumerator ExecuteInstruction();
        public InstructionType type;
    }

    [Serializable]
    public class WaitInstruction : Instruction
    {
        public float time = 0f;
        override public IEnumerator ExecuteInstruction()
        {
            yield return new WaitForSeconds(time);
        }
    }

    [Serializable]
    public class RotateInstructon : Instruction
    {
        [SerializeField] Transform enemy;
        public float angle = 0;

        public override IEnumerator ExecuteInstruction()
        {
            enemy.transform.RotateAround(enemy.transform.position, new Vector3 (0, 0, 1), angle);
            yield return null;  //rasj: just stop
        }
    }

    [Serializable]
    public class PointInstruction : Instruction
    {
        [SerializeField] Transform enemy;
        [SerializeField] Transform target;

        public override IEnumerator ExecuteInstruction()
        {
            enemy.transform.up = target.position - enemy.position;
            yield return null;
        }
    }

    [Serializable]
    public class DieInstruction : Instruction
    {
        [SerializeField] Transform enemy;

        public override IEnumerator ExecuteInstruction()
        {
            GameObject.Destroy(enemy.gameObject);
            yield return null;
        }
    }

    [Serializable]
    public class ShootInstruction : Instruction
    {
        [SerializeField] Transform enemy;
        [SerializeField] GameObject bulletPrefab;

        public override IEnumerator ExecuteInstruction()
        {
            GameObject newBullet = GameObject.Instantiate(bulletPrefab);
            newBullet.transform.up = enemy.transform.up;
            newBullet.transform.position = enemy.transform.position + newBullet.transform.up;
            yield return null;
        }
    }

    [Serializable]
    public class ChangeBehaviourtInstruction : Instruction
    {
        [SerializeField] Transform enemy;
        [SerializeField] Transform target;
        public ComparisonMode comparisonMode;
        public EnemyBehavior script;
        public EnemyBehavior.MovementMode newMovementMode;
        public float distToPlayer;

        public override IEnumerator ExecuteInstruction()
        {
            float dist = Vector3.Distance(enemy.transform.position, target.transform.position);
            bool condition = false;
            switch (comparisonMode)
            {
                case ComparisonMode.Less:
                    if (distToPlayer < dist) { condition = true; }
                    break;
                case ComparisonMode.Greater:
                    if (distToPlayer > dist) { condition = true; }
                    break;
            }
            if (condition)
            {
                script.ChangeBehaviour(newMovementMode);
            }
            yield return null;
        }
    }

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
}
