using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform : MonoBehaviour
{
    [Header("Movement")]
    public Vector3 linearVelocity;
    public Vector3 turnAxis;
    public float turnSpeed;

    [Header("Conveyor")]
    public Vector3 conveyorVelocity;
    public Vector3 conveyorAxis;
    public float conveyorTurnSpeed;

    public Vector3[] toWorldMatrix = Formulas.defaultMatrix();
    public Vector3 conveyorPos;

    [System.Serializable]
    public class SinMove
    {
        public Vector3 direction;
        public float magnitude;
        public float frequency;
        public float offset;
    }

    [Header("Sine Movement")]
    public List<SinMove> sinMoves = new List<SinMove>();
    public Vector3 defaultPos;

    public virtual Vector3 velocityAtPoint(Vector3 point)
    {
        Vector3 dir = point - transform.position;

        Vector3 totalVelocity = linearVelocity + conveyorVelocity;
        totalVelocity -= Vector3.Cross(dir, turnAxis).normalized * turnSpeed * Mathf.Deg2Rad * dir.magnitude;
        totalVelocity -= Vector3.Cross(dir, conveyorAxis).normalized * conveyorTurnSpeed * Mathf.Deg2Rad * dir.magnitude;

        return totalVelocity;
    }

    private void FixedUpdate()
    {
        if (sinMoves.Count > 0)
        { DoSineMovement(); }

        if (turnSpeed != 0)
        {
            transform.Rotate(turnAxis, turnSpeed * Time.fixedDeltaTime);
        }

        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        conveyorPos = pos + conveyorVelocity * Time.fixedDeltaTime;

        transform.Rotate(conveyorAxis, conveyorTurnSpeed * Time.fixedDeltaTime);
        toWorldMatrix = new Vector3[] { transform.right, transform.up, transform.forward };
        
        transform.rotation = rot;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
        {
            //transform.position = conveyorPos;
            //rb.MovePosition(pos);

            rb.position = pos - conveyorVelocity * Time.fixedDeltaTime;
            rb.MovePosition(pos);

            transform.Rotate(conveyorAxis, -conveyorTurnSpeed * Time.fixedDeltaTime);
            Quaternion modRot = transform.rotation;
            transform.rotation = rot;

            rb.rotation = modRot;
            rb.MoveRotation(rot);           
        }
    }

    void DoSineMovement()
    {
        transform.position = defaultPos;
        linearVelocity = Vector3.zero;

        foreach (SinMove sin in sinMoves)
        {
            float tim = (sin.frequency * Time.time + sin.offset) * Mathf.PI * 2f;
            transform.position += sin.direction * Mathf.Sin(tim) * sin.magnitude;
            linearVelocity += sin.direction * Mathf.Cos(tim) * sin.magnitude * sin.frequency * 2f * Mathf.PI;
        }
    }

    void OnValidate()
    {
        foreach (SinMove sin in sinMoves)
        { sin.direction = sin.direction.normalized; }
    }
}
