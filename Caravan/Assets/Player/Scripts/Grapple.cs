using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public class Grapple : MonoBehaviour
{
	[Header("Refs")]
	public Transform cam;
	public AltPlayerMovement move;
	public GameObject test;
    public Transform anchorPoint;

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
	float grappleLength;

	GameMenu menu;
	
    void Start()
    {
        menu = GameObject.FindObjectOfType<GameMenu>();
    }

    void FixedUpdate()
    {
		if (menu.PauseMenu.activeSelf) return;

		if (grappling)
		{
			ApplyForces();
			if (InputManager.GetButtonDown("Jump"))
			{
				Jump();
				CancelGrapple();
				//move.velocity += Vector3.up * jumpForce;
			}
			
			Debug.DrawLine(transform.position, anchorPoint.position, debugLineColor);
		}	
		else
		{
			if (InputManager.GetButtonDown("Click"))
			{
				RaycastHit hit = grapplePoint();
				if (hit.collider)
                {
                    grappling = true;

                    GrappleInteraction interaction = hit.collider.GetComponent<GrappleInteraction>();
                    if (interaction != null)
                    {
                        grappling = interaction.OnGrappleHit();
                    }

					if(grappling)
                    {
                        anchorPoint.position = hit.point;
                        anchorPoint.SetParent(hit.transform);
                        grappleLength = (anchorPoint.position - transform.position).magnitude;
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
		Vector3 line = (anchorPoint.position - transform.position);
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
			CancelGrapple();
		}
		
		vel += line * speed;
		move.velocity = vel;
	}
	
	void Jump()
	{
		Vector3 line = (anchorPoint.position - transform.position);
		line = line.normalized;
		
		Vector3 vel = move.velocity;
		float speed = Vector3.Dot(vel, line);
		float height = vel.y;
		
		if (speed < jumpMax.x)
		{ move.velocity += line * (jumpMax.x - speed) * jumpFraction.x; }
		if (height < jumpMax.y)
		{ move.velocity += Vector3.up * (jumpMax.y - height) * jumpFraction.y; }
	}

	void CancelGrapple()
    {
        grappling = false;
		anchorPoint.SetParent(null);
    }
}
