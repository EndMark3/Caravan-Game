using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Caravan : MonoBehaviour
{
    [Header("Refs")]
    public GameObject[] Wagons;
    public float WagonDistance;

    [Header("Movement")]
    public float Speed;
    public Vector3 ForwardDirection = new Vector3(-1f, 0f ,0f);

    void Start()
    {
        
    }

    void Update()
    {
        MoveFirstWagon();
        MoveOtherWagons();
    }

    void MoveFirstWagon()
    {
        Wagons[0].transform.position += ForwardDirection * Speed;
    }

    void MoveOtherWagons()
    {
        for(int i=1; i<Wagons.Length; i++)
        {
            if (Vector3.Distance(Wagons[i].transform.position, Wagons[i-1].transform.position) < WagonDistance)
            {
                var direction = Wagons[i-1].transform.position - Wagons[i].transform.position;
                Wagons[i].transform.position += direction.normalized * Speed;
            }
        }
    }
}
