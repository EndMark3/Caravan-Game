using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TPSCamera : MonoBehaviour
{
    [Header("Movement")]
    public Vector2 mouseSens;

    public bool smooth;

    public float camAccel;
    public float camSpeed;
    public Vector2 minMaxViewPitch;
    public float slowDownBounds;
    public Vector3 offset;
    public Vector3 aimOffset;
    public float aimSpeed;
    public AnimationCurve distanceOverPitch;
    public AnimationCurve fovOverDistance;
    public float aimFOV;

    Vector3 cameraRotation;
    Vector3 camPos;
    Vector3 realRotation;
    Vector2 testRotation;
    Vector2 camVelocity;

    [Header("Up Turn")]
    public float upTurnSpeed;

    AltPlayerMovement move;
    Vector3 up = new Vector3(0, 1, 0);
    Vector3 desiredUp = new Vector3(0, 1, 0);
    Vector3[] upMatrix = new Vector3[0];

    [Header("Zoom")]
    public LayerMask mask;
    public float camRadius;
    public QueryTriggerInteraction what;   

    float launchTimer;
    Transform camera;
    public float foresight;
    public int foresightSteps;

    float camDistance;
    float desiredDistance;
    float timeToZoom;
    float zoomVelocity;
    float aimStep;
    bool localplayer;
    GameMenu menu;

    void Start()
    {
        camera = Camera.main.transform;
        move = GetComponent<AltPlayerMovement>();
        SetNewUp(up);
        Cursor.lockState = CursorLockMode.Locked;
        menu = GameObject.FindObjectOfType<GameMenu>();
    }

    private void Update()
    {
        Debug.DrawLine(camera.position, camera.position + upMatrix[0], Color.green);
        Debug.DrawLine(camera.position, camera.position + upMatrix[1], Color.red);
        Debug.DrawLine(camera.position, camera.position + upMatrix[2], Color.blue);

        RotateUp();
        Rotate();
        Zoom();
    }

    void RotateUp()
    {
        if (move)
        {
            desiredUp = move.up;

            if (up != desiredUp)
            { SetNewUp(Vector3.RotateTowards(up, desiredUp, upTurnSpeed * Time.deltaTime, 0)); }
        }
    }

    public void SetNewUp(Vector3 newUp)
    {
        up = newUp;

        Vector3 world = Vector3.forward;
        Vector3 realWorld = Vector3.forward;
        float speed = camVelocity.magnitude;

        if (upMatrix.Length != 0)
        {
            world = cam2WorldPos(cameraRotation);
            realWorld = cam2WorldPos(realRotation);
            Debug.DrawRay(camera.position, world);
        }

        upMatrix = Formulas.PlaneMatrix(up);

        cameraRotation = World2CamPos(world);
        realRotation = World2CamPos(realWorld);

        realRotation = new Vector3(realRotation.x, Formulas.AngleDifference(cameraRotation.y, realRotation.y) + cameraRotation.y, 0);

        cameraRotation *= Mathf.Rad2Deg;
        realRotation *= Mathf.Rad2Deg;

        Vector2 diff = cameraRotation - realRotation;
        camVelocity = diff.normalized * speed;
    }

    Vector3 cam2WorldPos(Vector2 cam)
    {
        cam *= Mathf.Deg2Rad;
        Vector3 hor = upMatrix[0] * Mathf.Sin(cam.y) + upMatrix[2] * Mathf.Cos(cam.y);
        hor *= Mathf.Cos(cam.x);
        hor -= upMatrix[1] * Mathf.Sin(cam.x);

        return hor;
    }

    Vector2 World2CamPos(Vector3 world)
    {
        world = Formulas.world2PlanarVector(world, upMatrix);
        float y = Mathf.Atan2(world.x, world.z);
        float x = -Mathf.Atan2(world.y, Mathf.Sqrt(1f - world.y * world.y));

        return new Vector2(x, y);
    }

    void Rotate()
    {
        camera = Camera.main.transform;

        if (cameraRotation.x > 180)
        { cameraRotation.x = -360 + cameraRotation.x; }

        if (!menu.PauseMenu.activeSelf)
        {
            if (slowDownBounds != 0 && Input.GetAxis("Mouse Y") > 0 && cameraRotation.x < minMaxViewPitch.x + slowDownBounds)
            { cameraRotation += new Vector3(-Input.GetAxis("Mouse Y") * mouseSens.y * (cameraRotation.x - minMaxViewPitch.x) / slowDownBounds, Input.GetAxis("Mouse X") * mouseSens.x, 0); }
            else if (slowDownBounds != 0 && Input.GetAxis("Mouse Y") < 0 && cameraRotation.x > minMaxViewPitch.y - slowDownBounds)
            { cameraRotation += new Vector3(-Input.GetAxis("Mouse Y") * mouseSens.y * (minMaxViewPitch.y - cameraRotation.x) / slowDownBounds, Input.GetAxis("Mouse X") * mouseSens.x, 0); }
            else
            { cameraRotation += new Vector3(-Input.GetAxis("Mouse Y") * mouseSens.y, Input.GetAxis("Mouse X") * mouseSens.x, 0); }
        }

        if (cameraRotation.x > minMaxViewPitch.y)
        { cameraRotation.x = minMaxViewPitch.y; }
        else if (cameraRotation.x < minMaxViewPitch.x)
        { cameraRotation.x = minMaxViewPitch.x; }

        if (smooth)
        {
            Vector2 desiredVelocity = (cameraRotation - realRotation) * camSpeed;
            if (desiredVelocity.y / camSpeed > 180)
            { desiredVelocity += new Vector2(0, -360f); }
            else if (desiredVelocity.y / camSpeed < -180)
            { desiredVelocity += new Vector2(0, 360f); }

            desiredVelocity -= camVelocity;
            camVelocity += desiredVelocity.normalized * Mathf.Min(desiredVelocity.magnitude, camAccel * Time.deltaTime);
            realRotation += new Vector3(camVelocity.x, camVelocity.y, 0f) * Time.deltaTime;
        }
        else
        { realRotation = cameraRotation; }

        // Initialize
        camera.rotation = Quaternion.LookRotation(cam2WorldPos(realRotation), up);

        if (Input.GetButton("Fire2"))
        {
            aimStep = 1 - aimSpeed / (aimSpeed / (1 - aimStep) + Time.deltaTime);
        }
        else
        {
            if (aimStep != 0)
            { aimStep = aimSpeed / (aimSpeed / aimStep + Time.deltaTime); }
        }
        
        camera.position = transform.position + camera.forward * (offset.z * (1 - aimStep) + aimOffset.z * aimStep) + camera.right * (offset.x * (1 - aimStep) + aimOffset.x * aimStep) + up * (offset.y * (1 - aimStep) + aimOffset.y * aimStep);
    }

    void Zoom()
    {
        RaycastHit hit;
        bool obstacleFound = false;
        desiredDistance = distanceOverPitch.Evaluate((realRotation.x - minMaxViewPitch.x) / minMaxViewPitch.y);

        if (Physics.CheckSphere(transform.position + camera.right * offset.x + new Vector3(0, offset.y, 0), camRadius, mask))
        {
            camDistance = 0;
        }
        else if (Physics.SphereCast(transform.position + camera.right * offset.x + new Vector3(0, offset.y, 0), camRadius, -camera.forward, out hit, camDistance, mask, what))
        {
            camDistance = hit.distance;
        }

        camDistance = desiredDistance;

        Camera.main.fieldOfView = fovOverDistance.Evaluate(camDistance) * (1 - aimStep) + aimFOV * aimStep;
        Camera.main.transform.position -= Camera.main.transform.forward * camDistance * (1 - aimStep);
    }

    public void localPlayerEnable()
    {
        localplayer = true;
    }
}
