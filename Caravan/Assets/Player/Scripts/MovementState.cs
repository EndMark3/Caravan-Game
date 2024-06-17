using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "State", menuName = "ScriptableObjects/MovementState", order = 1)]
public class MovementState : ScriptableObject
{
    [Header("Controls")]
    public bool grounded;
    public bool airborne;
    public float gravity;

    public float maxSpeed;
    public float accel;    
    public float idleDecel; // Controls how fast you slow down when you're not pressing anything    
    public float turnDecel; // Controls how fast you slow down before turning in the opposite direction    
    public float turnSpeed; // When speed exceeds max, velocity will "rotate" towards the desired direction at this speed while keeping its magnitude            
    public float extraDecel; // Controls how fast you lose extra speed above max

    [Header("Surface")]        
    public bool flatMovement; // If false, allows for full 3D motion instead of being stuck to a surface
    // Useful for flying or swimming
    // Note that it should still be True while falling, since you don't control your vertical velocity

    public bool removeYComponent; // If true, all vertical speed will be removed. Makes sense on surfaces, not so much when falling    
    public bool idleSlide; // If true, you won't deccelerate if you're not pressing anything   

    [Header("Whatever")]
    public float heightMultiplier = 1f;

    [Header("Transitions")]
    public MovementState groundedTransition;
    public MovementState airborneTransition;

    public float minSpeed;
    public float minSlope;
    public MovementState minSpeedTransition;

    [Header("Jumps")]
    public List<AltPlayerMovement.JumpResource> resources;
    public List<Jump> jumps;
}
