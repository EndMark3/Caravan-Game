using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AltPlayerMovement : MonoBehaviour
{
    [Header("Controls")]    
    public float idleZone = 0.01f;
    public float fullZone = 0.95f;
    public bool invertedControls;

    [System.Serializable]    
    public class JumpResource
    {
        public int index;
        public int count;
        public bool free;
        public float coyoteTimer;
    }

    [Header("Jumping")]
    public float coyoteTime;
    public List<JumpResource> jumpResources;

    [Header("Ground")]
    public LayerMask ground;
    [Range(0, 180)]
    public float maxWalkSlope;
    [Range(0, 90)]
    public float maxMomentumSlopeIncrease;
    [Range(0, 90)]
    public float maxMomentumSlopeDecrease;
    public Vector3 momentumLoss;
    public float momentumCatchupPower;
    public float momentumGainPerTurn;

    float maxWalkDot;
    Vector2 momentumDots;
    float lostMomentum = 0;   

    public bool snapUpToNormal;
    public bool snapBodyToUp;
    public bool snapBodyToNormal;

    [Header("Body")]
    public float radius = 0.5f;
    public float height = 1f;
    public float baseHeight = 1f;
    public Vector3 bodyDirection = new Vector3(0, 1, 0);

    [Header("Collisions")]
    public float maxLedgeHeight;
    public int maxStepChecks;

    [Header("Steps")]
    public float maxCornerRadius;
    public float maxDownSnapRadius;
    public float maxDownSnapHeight;

    [Header("Current State")]
    public MovementState currentState;
    public Vector3 up = Vector3.up;
    Vector3[] upMatrix;      
    public Vector3 velocity;

    Vector3 inputDir;

    [Header("Surface")]
    public Transform surface;
    public Vector3 normal = Vector3.up;
    public Vector3 surfacePoint;
    public Vector3 localPos;
    public Vector3 localVel;
    public Vector3 localNormal;

    [Header("Visuals")]
    public Transform ball1;
    public Transform ball2;
    public Transform shaft;

    void Start()
    {
        OnValidate();
    }    

    void Update()
    {
        if (new Vector2(Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal")).magnitude < idleZone)
        { invertedControls = (currentState.grounded && Vector3.Dot(normal, up) < 0); }
    }   

    private void FixedUpdate()
    {
        Globalize();

        CheckInsideGround();

        Camera cam = Camera.main;
        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));        

        Vector3 oldVelocity = velocity;
        velocity = ModifyVelocity(velocity, cam.transform.forward, moveInput, normal, currentState);

        HandleJumping();

        ApplyVelocity(oldVelocity, velocity);       

        if (surface && currentState.grounded)
        {
            float dot = Vector3.Dot(up, normal);

            if ((dot > maxWalkDot))
            { lostMomentum = 0; }
            else
            {
                float y = Vector3.Dot(velocity.normalized, up);
                bool goingUp = (y > 0);

                y *= y;
                float x = 1f - y;

                y *= goingUp ? momentumLoss.z : momentumLoss.x;
                x *= momentumLoss.y;

                lostMomentum += Mathf.Sqrt(x + y);
                if (lostMomentum > Mathf.Pow(velocity.magnitude, momentumCatchupPower))
                {
                    SetGrounded(false);
                    DetachFromSurface();
                }
            }
        }

        if (snapBodyToUp)
        { TryTurningBody(up); }
        if (snapBodyToNormal)
        { TryTurningBody(normal); }

        Localize();
    }

    bool CheckInsideGround()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, radius, ground);

        if (cols.Length > 0)
        {
            if (!CheckLedge(transform.position, Vector3.zero))
            {
                Vector3 dir = new Vector3();
                float dist = 0;

                for (int i = 0; i < cols.Length; i++)
                {
                    if (Physics.ComputePenetration(GetComponent<Collider>(), transform.position, transform.rotation, cols[i], cols[i].transform.position, cols[i].transform.rotation, out dir, out dist))
                    {
                        bool grounded = (IsStandable(dir));
                        SetGrounded(grounded);

                        AttachToSurface(cols[i].transform, transform.position + dir * (dist - radius), dir, grounded, !grounded);
                    }
                }
            }

            return true;
        }

        return false;
    }

    Vector3 ModifyVelocity(Vector3 velocity, Vector3 camDir, Vector2 moveInput, Vector3 normal, MovementState state)
    {
        // Applying gravity
        velocity -= up * state.gravity * Time.fixedDeltaTime;

        // Constructing matrix for the surface    
        Vector3[] matrix = Formulas.defaultMatrix();

        if (state.grounded)
        {            
            normal = Formulas.world2PlanarVector(normal, upMatrix);
            matrix = Formulas.PlaneMatrix(normal);
        }
        else
        { normal = Vector3.up; }

        for (int i = 0; i < 3; i++)
        { matrix[i] = Formulas.planar2WorldVector(matrix[i], upMatrix); }

        // Converting the current velocity and camera direction
        // into local coordinates for the surface you're walking on.
        // (X goes perpendicular to the incline, Y matches the surface's normal, Z goes up the incline)
        velocity = Formulas.world2PlanarVector(velocity, matrix);

        Vector3 forward = new Vector3();
        Vector3 side = new Vector3();
        Vector3 desiredDir = new Vector3();

        if (state.flatMovement)
        {
            camDir = Formulas.world2PlanarVector(camDir, upMatrix);
            camDir = new Vector3(camDir.x, 0, camDir.z).normalized;

            if (normal != Vector3.up)
            {
                Vector3 planeForward = new Vector3(normal.x, 0, -normal.z).normalized;                
                Vector3 planeRight = new Vector3(planeForward.z, 0, -planeForward.x);

                forward = planeForward * camDir.z + planeRight * camDir.x;
                side = new Vector3(forward.z, 0, -forward.x);

                if (normal.y < 0)
                { 
                    forward = new Vector3(-forward.x, 0, forward.z);
                    side = new Vector3(-side.x, 0, side.z);
                }
            }
            else
            {
                forward = camDir;
                side = new Vector3(camDir.z, 0, -camDir.x);
                desiredDir = camDir * moveInput.y + side * moveInput.x;
            }            
        }
        else
        { 
            forward = Formulas.world2PlanarVector(camDir, matrix);
            side = new Vector3(forward.z, 0, -forward.x).normalized;
        }

        if (invertedControls)
        {
            //if (Mathf.Abs(forward.z) > Mathf.Abs(forward.x))
            { forward = -forward; }
            //else
            { side = -side; }
        }
        desiredDir = forward * moveInput.y + side * moveInput.x;
        inputDir = Formulas.planar2WorldVector(desiredDir, matrix);

        // Preparing to start moving

        // Removing the Y component for movement that only happens on a plane
        // Can be skipped for full 3-axis movement
        float yVel = velocity.y;
        velocity = new Vector3(velocity.x, (state.flatMovement ? 0 : velocity.y), velocity.z);

        // Snapping desired direction to between 0 and 1
        // fullMove determines weather the player should keep extra speed above max
        float speed = velocity.magnitude;
        float inputMag = moveInput.magnitude;
        bool fullMove = (inputMag >= fullZone);
        bool idleMove = (inputMag <= idleZone);

        if (idleMove)
        {
            desiredDir = Vector3.zero;
            inputMag = 0;            
        }
        else if (fullMove)
        {
            desiredDir /= inputMag;
            inputMag = 1;
        }

        if (speed > state.maxSpeed + 0.001f && fullMove && Vector3.Dot(velocity, desiredDir) > 0)
        { }
        else
        { desiredDir *= state.maxSpeed; }

        // If you're not pressing anything with idleSlide,
        // The script acts as if you're trying to move in the same direction you're already moving.
        if (state.idleSlide && idleMove)
        {
            idleMove = false;
            fullMove = (speed > state.maxSpeed + 0.001f);
            desiredDir = velocity;
        }

        // Beginning actual movement        

        if (speed > state.maxSpeed + 0.001f && fullMove && Vector3.Dot(velocity, desiredDir) > 0)
        {
            if (Vector3.Dot(velocity, desiredDir) > 0 && fullMove)
            { RoundVelocityChange(ref velocity, desiredDir, state, Time.fixedDeltaTime); }
        }
        else
        { LinearVelocityChange(ref velocity, desiredDir, (idleMove ?state.idleDecel : state.turnDecel), state.accel); }

        // Adding back the Y component
        if (state.flatMovement && !state.removeYComponent)
        { velocity = new Vector3(velocity.x, yVel, velocity.z); }

        // TODO: Gravity

        // Applying the matrix to the result to return it to world space
        return Formulas.planar2WorldVector(velocity, matrix);
    }

    void HandleJumping()
    {
        foreach (JumpResource res in jumpResources)
        {
            if (res.free)
            { res.coyoteTimer = coyoteTime; }
            else
            { res.coyoteTimer -= Mathf.Min(res.coyoteTimer, Time.fixedDeltaTime); }
        }

        for (int i = 0; i < currentState.jumps.Count; i++)
        {
            Jump jump = currentState.jumps[i];

            if (workWithCharge(jump, out float charge))
            {
                if (jump.needDirectionalInput && new Vector2(Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal")).magnitude < idleZone)
                { continue; }

                if (jump.resource > -1)
                {
                    if (!FindResource(jump.resource))
                    { continue; }
                }

                if (!CanScaleBody(jump.transition.heightMultiplier))
                { continue; }

                bool hasInput = jump.needsUnpress ? (jump.needsDown ? InputManager.GetButtonUp(jump.button) : !InputManager.GetButton(jump.button))
                : (jump.needsDown ? InputManager.GetButtonDown(jump.button) : InputManager.GetButton(jump.button));

                ExecuteJump(jump, charge);
                return;
            }
        }

        if (currentState.minSpeedTransition && velocity.magnitude < currentState.minSpeed && Vector3.Angle(normal, up) < currentState.minSlope)
        {
            if (!currentState.minSpeedTransition.grounded)
            { DetachFromSurface(); }
            StateTransition(currentState.minSpeedTransition);
        }
    }

    bool workWithCharge(Jump jump, out float charge)
    {
        if (jump.useDelay && jump.delay > 0)
        {
            jump.delay -= Time.fixedDeltaTime;
            jump.charge = 0;
            charge = 0;
            return false;
        }

        charge = jump.charge;

        bool hasInput = jump.needsUnpress ? (jump.needsDown ? InputManager.GetButtonUp(jump.button, false) : !InputManager.GetButton(jump.button))
                : (jump.needsDown ? InputManager.GetButtonDown(jump.button, false) : InputManager.GetButton(jump.button));

        if (hasInput)
        {
            jump.charge = 0;
            return true;
        }

        if (jump.useCharge)
        {
            if (jump.idleCharge || (jump.needsUnpress ? InputManager.GetButton(jump.button) : !InputManager.GetButton(jump.button)))
            {
                jump.charge += Time.deltaTime;

                if (jump.charge > jump.maxCharge && jump.maxCharge > 0)
                {
                    charge = jump.charge;
                    jump.charge = 0;
                    return true;
                }
            }
            else
            { jump.charge = 0; }
        }

        return false;
    }

    bool FindResource(int resource)
    {
        JumpResource res = jumpResources[resource];
        return (res.free || res.coyoteTimer > 0 || res.count > 0);
    }

    void spendResource(int resource)
    {
        JumpResource res = jumpResources[resource];
        res.coyoteTimer = 0;
        if (!res.free)
        {
            if (res.coyoteTimer == 0)
            { res.count--; }
        }       
    }

    void ChangeResource(JumpResource resource)
    {
        JumpResource res = jumpResources[resource.index];
        res.free = resource.free;

        if (resource.free)
        { res.coyoteTimer = coyoteTime; }

        if (resource.count > -1)
        { res.count = resource.count; }
    }

    void ExecuteJump(Jump jump, float charge)
    {
        if (jump.resource > -1)
        { spendResource(jump.resource); }

        if (jump.useDelay)
        { jump.delay = jump.maxDelay; }

        float multiplier = 1f;

        if (jump.useCharge)
        {
            multiplier *= newBoundaries(charge, jump.charges, jump.chargeMultipliers);
            jump.charge = 0;
        }

        Vector3 nearbyVel = Vector3.zero; 

        if (jump.nearby)
        {
            float minDist = jump.maxNearbyDistance;
            Vector3 minDir = Vector3.zero;
            Collider[] cols = Physics.OverlapSphere(transform.position, jump.maxNearbyDistance, ground);            
            
            for (int i = 0; i < cols.Length; i++)
            {
                Vector3 dir = transform.position - cols[i].ClosestPoint(transform.position);
                float dist = dir.magnitude;

                if (dir != Vector3.zero && dist != 0)
                {
                    if (dist <= minDist)
                    {
                        minDir = dir;
                        minDist = dist;
                    }
                }
            }

            Debug.Log(cols.Length + " appropriate colliders found.");
            Debug.Log("MinDir is " + minDir);
            multiplier *= newBoundaries(minDist, jump.nearbyDistances, jump.nearbyMultipliers);

            if (cols.Length > 0)
            { nearbyVel = minDir.normalized; }
        }

        if (jump.dash)
        {
            Vector3 dir = Camera.main.transform.forward;

            if (inputDir.magnitude > idleZone)
            { dir = inputDir; }

            if (jump.flatDash)
            { dir -= normal * Vector3.Dot(normal, dir); }

            dir = dir.normalized;

            float sped = 0;

            if (jump.keepSpeedOnDash)
            { sped = Vector3.Dot(dir, velocity); }

            Vector3 dashVel = dir * Mathf.Max(jump.dashSpeed * multiplier, sped);

            if (jump.additiveDash)
            { velocity += dashVel; }
            else
            { velocity = dashVel; }
        }

        if (jump.bounceOffSurface)
        {
            Vector3 dir = up * jump.upBounce + normal * jump.normalBounce + nearbyVel * jump.nearbyBounce;
            float dirMag = jump.normalizeBounce ? 1f : dir.magnitude;
            dir.Normalize();

            float vel = jump.bounceVel + velocity.magnitude * jump.speedAddedToBounce;
            vel *= multiplier;

            float currentVel = Vector3.Dot(dir, velocity);
            velocity += dir * (vel * dirMag - currentVel);
        }

        velocity += nearbyVel * jump.nearbyVelocity * multiplier;

        if (jump.transition && !jump.transition.grounded)
        { DetachFromSurface(); }
        StateTransition(jump.transition);
    }

    float newBoundaries(float input, Vector2 start, Vector2 end)
    {
        if (start.x == start.y)
        { input = (input > start.x) ? 1 : 0; }
        else
        {
            input -= start.x;
            input /= (start.y - start.x);

            input = Mathf.Max(Mathf.Min(input, 1), 0);
        }

        return (end.x + (end.y - end.x) * input);
    }

    void LinearVelocityChange(ref Vector3 velocity, Vector3 desired, float deccel, float accel)
    {       
        if (velocity != desired)
        {
            float time = Time.fixedDeltaTime;           
            Vector3 diff = desired - velocity;
            float dif = diff.magnitude;
            diff.Normalize();

            // Decceleration part.
            // Goes in a straight line towards desired velocity
            // until the dot product is = 0.
            if (Vector3.Dot(diff, velocity) < 0)
            {                
                float neededDist = Mathf.Min(Vector3.Dot(diff, -velocity), dif);
                float neededTime = Mathf.Min(time, neededDist / deccel);
                velocity += diff.normalized * neededTime * deccel;
                dif -= neededTime * deccel;
                time -= neededTime;
            }

            // Acceleration part
            if (time > 0 && dif > 0)
            { velocity += diff * Mathf.Min(time * accel, dif); }
        }
    }

    //Change direction while preserving extra speed
    void RoundVelocityChange(ref Vector3 velocity, Vector3 desired, MovementState state, float time)
    {
        float startSpeed = velocity.magnitude;
        float endSpeed = Mathf.Max(state.maxSpeed, startSpeed - state.extraDecel * time);
        velocity = Vector3.RotateTowards(velocity, desired, state.turnSpeed * time / (startSpeed + endSpeed) * 2f, 0);
        velocity = velocity.normalized * endSpeed;
    }

    bool refSurf = false;
    Vector3 refNormal = Vector3.up;

    public void ApplyVelocity(Vector3 startVelocity, Vector3 endVelocity)
    {
        Vector3 posChange = (endVelocity + startVelocity) * 0.5f * Time.fixedDeltaTime;
        if (!currentState.grounded)
        { DetachFromSurface(); }

        refSurf = currentState.grounded;
        refNormal = normal;

        for (int i = 0; i < maxStepChecks; i++)
        {
            if (!refSurf && currentState.grounded)
            {
                refSurf = currentState.grounded;
                refNormal = normal;
            }

            if (posChange == Vector3.zero)
            {
                if (currentState.grounded)
                { CheckStep(posChange); }
                break;
            }

            if (!CheckCollisions(ref posChange))
            {
                Debug.Log("Didn't collide.");

                if (currentState.grounded)
                { CheckStep(posChange); }
                else
                { transform.position += posChange; }

                break;
            }
        }
    }   


    bool CheckCollisions(ref Vector3 posChange, Transform ignoreSurface = null)
    {
        RaycastHit hit = new RaycastHit();

        if (!currentState.grounded)
        {
            Physics.CapsuleCast(transform.position, transform.position + bodyDirection * height, radius, posChange, out hit, posChange.magnitude + radius, ground);

            if (hit.collider && hit.distance != 0 && hit.point != Vector3.zero)
            {
                if (IsLedge(hit.point + hit.normal * radius, hit.transform, hit.point, hit.normal, out Vector3 ledgePoint, out Vector3 ledgeNormal))
                {
                    Vector3 legPoint = ledgePoint + ledgeNormal * (radius + 0.001f);
                    float dist = (legPoint - transform.position).magnitude;

                    if (dist < posChange.magnitude)
                    {
                        SetGrounded(true);
                        AttachToSurface(hit.transform, ledgePoint, ledgeNormal, false, false);

                        float fraction = 1f - dist / posChange.magnitude;
                        posChange = velocity * fraction * Time.fixedDeltaTime;

                        return true;
                    }
                }
            }
        }

        RaycastHit[] hits = Physics.CapsuleCastAll(transform.position, transform.position + bodyDirection * height, radius, posChange, posChange.magnitude, ground);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform == ignoreSurface)
            { continue; }

            if (hits[i].distance == 0 && hits[i].point == Vector3.zero)
            { continue; }

            Vector3 newPos = transform.position + posChange.normalized * (hits[i].distance - 0.001f);

            if (IsStandable(hits[i].normal))
            {
                AttachToSurface(hits[i].transform, hits[i].point, hits[i].normal, currentState.grounded);
                transform.position = newPos;
                SetGrounded(true);

                float fraction = 1f - hits[i].distance / posChange.magnitude;
                posChange = velocity * fraction * Time.fixedDeltaTime;
            }
            else
            {
                Vector3 ledgeCheckPoint = transform.position + posChange;
                ledgeCheckPoint = hits[i].point + up * Vector3.Dot(up, ledgeCheckPoint - hits[i].point);

                if (!CheckLedge(ledgeCheckPoint, posChange))
                {
                    SlideOnSurface(posChange, hits[i], ref refSurf, ref refNormal);

                    transform.position = newPos;

                    float fraction = 1f - hits[i].distance / posChange.magnitude;
                    posChange = velocity * fraction * Time.fixedDeltaTime;

                    Debug.DrawLine(hits[i].point, hits[i].point + posChange, Color.blue, 500);
                }
                else
                { posChange = Vector3.zero; }
            }

            return true;
        }

        return false;
    }

    bool CheckLedge(Vector3 point, Vector3 posChange)
    {
        point += up * maxLedgeHeight;
        RaycastHit[] hits = Physics.SphereCastAll(point, radius, -up, maxLedgeHeight, ground);

        Vector3 checkDir = posChange - up * Vector3.Dot(up, posChange);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].distance == 0 && hits[i].point == Vector3.zero)
            { continue; }

            Debug.DrawLine(point, hits[i].point);

            bool canStand = false;
            Vector3 newPoint = hits[i].point;
            Vector3 newNormal = hits[i].normal;
            Vector3 capsuleStart = newPoint + newNormal * radius;

            if (IsLedge(capsuleStart, hits[i].transform, newPoint, newNormal, out Vector3 ledgePoint, out Vector3 ledgeNormal))
            {
                canStand = true;
                newPoint = ledgePoint;
                newNormal = ledgeNormal;
                capsuleStart = newPoint + newNormal * radius;
            }
            else { canStand = (IsStandable(hits[i].normal)); }

            capsuleStart += newNormal * 0.001f;

            if (canStand)
            {                
                if (!Physics.CheckCapsule(capsuleStart, capsuleStart + bodyDirection * height, radius, ground))
                {
                    Vector3 rayDir = capsuleStart - transform.position;

                    if (Vector3.Dot(checkDir, rayDir) >= 0)
                    {
                        if (!Physics.Raycast(transform.position, rayDir, rayDir.magnitude, ground))
                        {
                            Debug.DrawLine(transform.position, capsuleStart, Color.green);

                            AttachToSurface(hits[i].transform, newPoint, newNormal, currentState.grounded);
                            SetGrounded(true);
                            return true;
                        }
                        else
                        { 
                            Debug.DrawLine(transform.position, capsuleStart, Color.red, 500);
                            Debug.Log("Failed line-of-sight check.");
                        }
                    }
                    else
                    { Debug.Log("Failed dot product check."); }
                }
            }
            else
            { Debug.Log("Can't stand."); }
        }

        return false;
    }    

    void CheckStep(Vector3 posChange)
    {
        Vector3 checkOrigin = transform.position + posChange;

        if (Physics.SphereCast(checkOrigin, radius, -normal, out RaycastHit hit, maxDownSnapHeight + radius + posChange.magnitude, ground))
        {
            if (hit.distance != 0 || hit.point != Vector3.zero)
            {
                if (IsLedge(checkOrigin, hit.transform, hit.point, hit.normal, out Vector3 point, out Vector3 newNormal))
                {
                    AttachToSurface(hit.transform, point, newNormal, currentState.grounded, false);
                    return;
                }
                else
                {
                    if (hit.distance <= maxDownSnapHeight && IsStandable(hit.normal))
                    {
                        AttachToSurface(hit.transform, hit.point, hit.normal, true);
                        return;
                    }
                }
            }
        }

        DetachFromSurface();
        SetGrounded(false);
        transform.position = checkOrigin;
    }

    void SlideOnSurface(Vector3 dir, RaycastHit hit, ref bool prev, ref Vector3 prevNormal)
    {
        transform.position += dir * hit.distance + hit.normal * 0.0001f;

        if (prev && prevNormal != hit.normal && Vector3.Dot(prevNormal, hit.normal) < 0)
        {
            transform.position += prevNormal * 0.0001f;
            velocity = SlideVelocity(velocity, prevNormal, hit.normal, false);           
        }
        else
        { velocity = AttachVelocity(velocity, hit.normal, false); }

        prev = true;
        prevNormal = hit.normal;
    }  

    void AttachToSurface(Transform newSurface, Vector3 contactPoint, Vector3 newNormal, bool keepSpeed, bool airborne = false)
    {
        if (newSurface != surface)
        {
            DetachFromSurface();
            surface = newSurface;

            Platform plat = surface.GetComponent<Platform>();
            if (plat)
            { velocity -= plat.velocityAtPoint(contactPoint); }
        }

        Vector3 prevVel = velocity;

        transform.position = contactPoint + newNormal * (radius + 0.001f);
        velocity = AttachVelocity(velocity, newNormal, keepSpeed);

        if (currentState.grounded)
        {
            float posCurve = (Vector3.Dot(prevVel, newNormal) < 0) ? 1f : -1f;
            lostMomentum = Mathf.Max(0, lostMomentum - Vector3.Angle(prevVel, velocity) / 180f * posCurve * momentumGainPerTurn * Mathf.Pow(velocity.magnitude, momentumCatchupPower - 1f));
        }

        normal = newNormal;
        surfacePoint = contactPoint;

        if (snapUpToNormal && !airborne)
        { SetNewUp(normal); }
    }

    Vector3 SlideVelocity(Vector3 velocity, Vector3 normal1, Vector3 normal2, bool keepSpeed)
    {
        Vector3 cross = Vector3.Cross(normal1, normal2).normalized;
        float spd = Vector3.Dot(cross, velocity);

        return cross * (keepSpeed ? velocity.magnitude * Mathf.Sign(spd) : spd);
    }

    Vector3 AttachVelocity(Vector3 velocity, Vector3 normal, bool keepSpeed)
    {
        float speed = velocity.magnitude;
        velocity -= normal * Vector3.Dot(velocity, normal);
        if (keepSpeed)
        { velocity = velocity.normalized * speed; }

        return velocity;
    }

    void DetachFromSurface()
    {
        if (surface)
        {
            Platform plat = surface.GetComponent<Platform>();
            if (plat)
            { velocity += plat.velocityAtPoint(transform.position - normal * radius); }
        }

        surface = null;
        normal = up;
    }

    void Localize()
    {
        if (surface)
        {
            Platform plat = surface.GetComponent<Platform>();

            Vector3 surfPos = surface.position;
            Vector3[] surfMat = new Vector3[] { surface.right, surface.up, surface.forward };

            localNormal = Formulas.world2PlanarVector(normal, surfMat);
            localPos = Formulas.world2PlanarVector(surfacePoint - surfPos, surfMat);
            localVel = Formulas.world2PlanarVector(velocity, surfMat); ;            
        }
    }

    void Globalize()
    {
        if (surface)
        {
            Platform plat = surface.GetComponent<Platform>();

            Vector3 surfPos = plat ? plat.conveyorPos : surface.position;
            Vector3[] surfMat = plat ? plat.toWorldMatrix : new Vector3[] { surface.right, surface.up, surface.forward };

            normal = Formulas.planar2WorldVector(localNormal, surfMat).normalized;
            velocity = Formulas.planar2WorldVector(localVel, surfMat);

            Vector3 globalPoint = Formulas.planar2WorldVector(localPos, surfMat) + surfPos;
            Vector3 targetPos = globalPoint + normal * (radius + 0.001f);
            Vector3 posChange = targetPos - transform.position;

            float heightDiff = Vector3.Dot(posChange, normal);
            if (heightDiff > 0)
            { 
                transform.position += normal * heightDiff;
                posChange -= normal * heightDiff;
            }

            Vector3 platVel = Vector3.zero;
            if (plat)
            {
                platVel = plat.velocityAtPoint(globalPoint);
                velocity += platVel;
            }

            if (!CheckCollisions(ref posChange, surface))
            { 
                transform.position = targetPos;
            //    Debug.Log("Didn't hit Something!");
            }
            //else
            //{ Debug.Log("Hit Something!"); }

            velocity -= platVel;
        }
    }

    bool IsLedge(Vector3 currentPos, Transform newSurface, Vector3 point, Vector3 ledgeNormal, out Vector3 standablePoint, out Vector3 standableNormal)
    {
        //This is for debugging. Remove later
        if ((point - transform.position).magnitude > 5f)
        {  }

        bool isLedge = false;

        if (ledgeNormal == up)
        {
            isLedge = CheckLedgeness(point, upMatrix[0], out standableNormal);

            if (!isLedge)
            { isLedge = CheckLedgeness(point, upMatrix[2], out standableNormal); }
        }
        else
        {
            float y = Vector3.Dot(up, ledgeNormal);
            Vector3 flat = ledgeNormal - up * y;
            float x = flat.magnitude;
            flat = flat.normalized;

            Vector3 checkDir = flat * y - up * x;

            isLedge = CheckLedgeness(point, checkDir, out standableNormal);
        }

        if (isLedge)
        {
            Vector3 dirFromLedge = currentPos - point;
            dirFromLedge -= standableNormal * Vector3.Dot(standableNormal, dirFromLedge);

            if (dirFromLedge.magnitude <= radius)
            {
                Vector3 capsuleStart = point + dirFromLedge + standableNormal * (radius + 0.001f);

                if (!Physics.CheckCapsule(capsuleStart, capsuleStart + bodyDirection * height, radius, ground))
                {
                    standablePoint = point + dirFromLedge;
                    return true;
                }
            }
        }

        standableNormal = Vector3.zero;
        standablePoint = Vector3.zero;
        return false;
    }

    bool CheckLedgeness(Vector3 point, Vector3 checkDirection, out Vector3 standableNormal)
    {
        Vector3 raisedPoint = point + up * 0.001f;

        bool stand1 = false;
        bool stand2 = false;

        standableNormal = Vector3.zero;
        RaycastHit hit = new RaycastHit();

        for (int i = 0; i < 2; i++)
        {
            bool stand = false;

            if (Physics.Raycast(raisedPoint + checkDirection * 0.001f, -up, out hit, maxDownSnapHeight, ground))
            {
                if (IsStandable(hit.normal))
                {
                    stand = (Vector3.Dot(point - hit.point, hit.normal) < maxCornerRadius);
                    if (stand)
                    {
                        Debug.DrawLine(hit.point, hit.point + hit.normal, Color.green);
                        standableNormal = hit.normal;
                    }
                }
                if (!stand)
                { Debug.DrawLine(hit.point, hit.point + hit.normal, Color.red); }
            }
            else
            { Debug.DrawLine(raisedPoint, raisedPoint + checkDirection, Color.blue); }

            if (i == 0)
            { stand1 = stand; }
            else
            { stand2 = stand; }

            checkDirection = -checkDirection;
        }

        return ((stand1 && !stand2) || (!stand1 && stand2));
    }

    bool IsStandable(Vector3 pointNormal)
    { 
        if ((Vector3.Dot(up, pointNormal) > maxWalkDot))
        { return true; }

        if (currentState.grounded)
        {
            if (Vector3.Dot(normal, pointNormal) >= ((Vector3.Dot(velocity, pointNormal) <= 0) ? momentumDots.x : momentumDots.y))
            { return true; }
        }

        return false;
    }     

    private void OnValidate()
    {
        SetNewUp(up);
        normal.Normalize();

        if (maxWalkSlope >= 180)
        { maxWalkDot = -2f; }
        else
        { maxWalkDot = Mathf.Cos(maxWalkSlope * Mathf.Deg2Rad); }

        momentumDots = new Vector2(Mathf.Cos(maxMomentumSlopeIncrease * Mathf.Deg2Rad), Mathf.Cos(maxMomentumSlopeDecrease * Mathf.Deg2Rad));
    }

    public void SetNewUp(Vector3 newUp)
    {
        up = newUp.normalized;
        upMatrix = Formulas.PlaneMatrix(up);
    }

    bool TryTurningBody(Vector3 direction)
    {
        if (!Physics.CheckCapsule(transform.position, transform.position + direction * height, radius, ground))
        {
            TurnBody(direction);
            return true;
        }

        return false;
    }

    bool CanScaleBody(float newHeight)
    {
        if (newHeight < height)
        { return true; }
        //TODO: Make it so that the body could potentially move down
        else return (!Physics.CheckCapsule(transform.position, transform.position + bodyDirection * newHeight * baseHeight, radius, ground));
    }

    void ScaleBody(float newHeight)
    {
        height = newHeight * baseHeight;

        ball2.localPosition = Vector3.up * height;
        shaft.localPosition = Vector3.up * height * 0.5f;
        shaft.localScale = new Vector3(1, height * 0.5f, 1);
    }

    void TurnBody(Vector3 direction)
    {
        Vector3 lookDir = Formulas.PlaneMatrix(direction)[0];
        bodyDirection = direction;
        transform.rotation = Quaternion.LookRotation(lookDir, direction);
    }

    void SetGrounded(bool newGrounded)
    {
        MovementState newState = newGrounded ? currentState.groundedTransition : currentState.airborneTransition;
        if (newState && CanScaleBody(newState.heightMultiplier))
        { StateTransition(newState); }
    }

    public void StateTransition(MovementState newState)
    {
        foreach (JumpResource res in jumpResources)
        { res.free = false; }
        foreach (JumpResource res in newState.resources)
        { ChangeResource(res); }

        foreach (Jump jump in currentState.jumps)
        { jump.charge = 0; }

        ScaleBody(newState.heightMultiplier);
        currentState = newState;
    }
}
