using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SineColor : MonoBehaviour
{
    public Gradient gradient;
    public float gradientSpeed = 1.0f;

    Material material;
    float startTime;

    void Start()
    {
        material = GetComponent<MeshRenderer>().materials[0];
        material.color = gradient.Evaluate(0);
        startTime = Time.time;
    }

    void Update()
    {
        var lifetime = Time.time - startTime;
        var sine = Mathf.Sin(lifetime * gradientSpeed);

        material.color = gradient.Evaluate(Mathf.Abs(sine));
    }
}
