using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Jump", menuName = "ScriptableObjects/Jump", order = 2)]
public class Jump : ScriptableObject
{
    public MovementState transition;

    [Header("Conditions")]
    public string button;
    public bool needsUnpress;
    public bool needsDown;

    public int resource;

    public bool needDirectionalInput;

    [Header("Bounce")]
    public bool bounceOffSurface;
    public float bounceVel;
    public float speedAddedToBounce;

    public float upBounce;
    public float normalBounce;
    public float nearbyBounce;
    public bool normalizeBounce;

    [Header("Dash")]
    public bool dash;
    public float dashSpeed;
    public bool keepSpeedOnDash;

    public bool flatDash;
    public bool additiveDash;

    [Header("Nearby Walls")]
    public bool nearby;
    public float maxNearbyDistance;
    public Vector2 nearbyDistances;
    public Vector2 nearbyMultipliers;
    public float nearbyVelocity;

    [Header("Charge")]
    public bool useCharge;
    public bool idleCharge;
    public float maxCharge;
    public Vector2 charges;
    public Vector2 chargeMultipliers;

    public float charge;

    [Header("Delay")]
    public bool useDelay;
    public float maxDelay;
    
    public float delay;
}
