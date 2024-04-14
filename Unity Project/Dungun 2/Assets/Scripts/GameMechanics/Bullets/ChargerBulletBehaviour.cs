using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface BulletInterface
{
    void OnSpawn(GameObject shooter);
}

public class ChargerBulletBehaviour : MonoBehaviour, BulletInterface
{
    [SerializeField] GameObject chargerPrefab;

    public float startVelocity = 1f;
    public float desctructionTime = 5f;
    public int chargerHealth = 0;


    void Start()
    {
        GetComponent<Rigidbody2D>().velocity = transform.up * startVelocity;
        if (desctructionTime >= 0)  //rasj: if i.e. -1, then don't destroy after some time
        {
            Destroy(this.gameObject, desctructionTime);
        }
    }

    public void OnSpawn(GameObject shooter)
    {
        chargerHealth = shooter.GetComponent<EnemyBehavior>().healthPoints;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //rasj: spawn the enemy prefab
        GameObject newCharger = Instantiate(chargerPrefab);
        newCharger.transform.position = transform.position;  //rasj: spawn charger at current location
        newCharger.GetComponent<EnemyBehavior>().healthPoints = chargerHealth;  //rasj: update health to be correct

        Destroy(this.gameObject);   
    }
}
