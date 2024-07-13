using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Soul : MonoBehaviour, GrappleInteraction
{
    public float Concentration;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool OnGrappleHit()
    {
        Debug.Log("Soul clicked! Value: " + Concentration);
        Destroy(this.gameObject);

        return false;
    }
}
