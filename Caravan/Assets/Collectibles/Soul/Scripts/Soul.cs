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

    public void OnGrappleHit()
    {
        Debug.Log("Soul clicked! Value: " + Concentration);
        Destroy(this.gameObject);
    }
}
