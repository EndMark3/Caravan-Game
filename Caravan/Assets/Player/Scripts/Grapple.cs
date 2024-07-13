using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grapple : MonoBehaviour
{
	[Header("Refs")]
	public Transform cam;
	public AltPlayerMovement move;
	public GameObject test;

	public Color debugLineColor = Color.white;
	
	[Header("Shoot")]
	public LayerMask ground;
	public float maxLength;
	public float radStep;
	public int radSteps;
	
	[Header("Move")]
	public float pullForce;
	public float maxPullSpeed;
	public float idleDamp;
	
	public float jumpForce;
	public Vector2 jumpMax;
	public Vector2 jumpFraction;
	
	bool grappling;
	Vector3 anchorPoint;
	float grappleLength;
	
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {       
		if (grappling)
		{
			ApplyForces();
			if (InputManager.GetButtonDown("Jump"))
			{
				Jump();
				grappling = false;
				//move.velocity += Vector3.up * jumpForce;
			}
			
			Debug.DrawLine(transform.position, anchorPoint, debugLineColor);
		}	
		else
		{
			if (InputManager.GetButtonDown("Click"))
			{
				RaycastHit hit = grapplePoint();
				if (hit.collider)
				{
					grappling = true;
					anchorPoint = hit.point;
					grappleLength = (anchorPoint - transform.position).magnitude;

					GrappleInteraction interaction = hit.collider.GetComponent<GrappleInteraction>();
                    if (interaction != null)
                    {
						interaction.OnGrappleHit();
                    }
                }
			}
		}
    }
	
	RaycastHit grapplePoint()
	{
		RaycastHit hit = new RaycastHit();
		
		for (int i = 0; i < radSteps; i++)
		{
			float start = i / radSteps * maxLength;
			
			Physics.SphereCast(transform.position + cam.forward * start, radStep * i, cam.forward, out hit, maxLength - start, ground);
			if (hit.collider)
			{ break; }
		}
		
		return hit;
	}
	
	void ApplyForces()
	{
		Vector3 vel = move.velocity;
		Vector3 line = (anchorPoint - transform.position);
		float length = line.magnitude;
		line = line.normalized;
		float speed = Vector3.Dot(vel, line);
		vel -= line * speed;
		
		bool pulling = InputManager.GetButton("Click");
		
		
		
		if (pulling)
		{
			if (length > grappleLength)
			{ 
				float currPullForce = (length - grappleLength) * pullForce;
				speed += currPullForce * Time.fixedDeltaTime;
			}
			else { grappleLength = length; }
		}
		else
		{
			grappling = false;
		}
		
		vel += line * speed;
		move.velocity = vel;
	}
	
	void Jump()
	{
		Vector3 line = (anchorPoint - transform.position);
		line = line.normalized;
		
		Vector3 vel = move.velocity;
		float speed = Vector3.Dot(vel, line);
		float height = vel.y;
		
		if (speed < jumpMax.x)
		{ move.velocity += line * (jumpMax.x - speed) * jumpFraction.x; }
		if (height < jumpMax.y)
		{ move.velocity += Vector3.up * (jumpMax.y - height) * jumpFraction.y; }
	}
}
