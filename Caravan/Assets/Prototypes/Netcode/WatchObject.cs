using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class WatchObject
{
    public ushort playerId; // Who owns this Object?
    public ushort objectId; // What Object?
    public GameObject gameObject;
    public Vector3 position;
    public float timeout; // Only applies to object 0
}
