using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Formulas
{
    static public void SmoothMove(ref float value, ref float velocity, float target, float acceleration, float speed, float time)
    {
        float desiredVelocity = (target - value) * speed;

        desiredVelocity -= velocity;
        velocity += Mathf.Sign(desiredVelocity) * Mathf.Min(Mathf.Abs(desiredVelocity), acceleration / time);
        value += velocity * Time.fixedDeltaTime;
    }

    static public void SmoothMove(ref Vector2 value, ref Vector2 velocity, Vector2 target, float acceleration, float speed, float time)
    {
        Vector2 desiredVelocity = (target - value) * speed;

        desiredVelocity -= velocity;
        velocity += desiredVelocity.normalized * Mathf.Min(desiredVelocity.magnitude, acceleration / time);
        value += velocity * Time.fixedDeltaTime;
    }

    static public void SmoothMove(ref Vector3 value, ref Vector3 velocity, Vector3 target, float acceleration, float speed, float time)
    {
        Vector3 desiredVelocity = (target - value) * speed;

        desiredVelocity -= velocity;
        velocity += desiredVelocity.normalized * Mathf.Min(desiredVelocity.magnitude, acceleration / time);
        value += velocity * Time.fixedDeltaTime;
    }

    static public void DragMove(ref float value, float drag, float time)
    {
        if (value != 0f)
        { value = Mathf.Sign(value) * drag / (drag / (Mathf.Abs(value)) + time); }
    }

    static public Vector3 world2PlanarVector(Vector3 world, Vector3 normal, out Vector3[] matrix)
    {
        matrix = PlaneMatrix(normal);

        if (normal == Vector3.up)
        { return world; }
        if (normal == Vector3.down)
        { return new Vector3(world.x, -world.y, -world.z); }

        return world2PlanarVector(world, matrix);
    }

    static public Vector3 world2PlanarVector(Vector3 world, Vector3 normal)
    {
        if (normal == Vector3.up)
        { return world; }
        if (normal == Vector3.down)
        { return new Vector3(world.x, -world.y, -world.z); }

        Vector3[] matrix = PlaneMatrix(normal);
        return world2PlanarVector(world, matrix);
    }

    static public Vector3 world2PlanarVector(Vector3 world, Vector3[] matrix)
    {
        return new Vector3(
            Vector3.Dot(world, matrix[0]),
            Vector3.Dot(world, matrix[1]),
            Vector3.Dot(world, matrix[2])
            );
    }

    static public Vector3 planar2WorldVector(Vector3 planar, Vector3 normal, out Vector3[] matrix)
    {
        matrix = PlaneMatrix(normal);

        if (normal == Vector3.up)
        { return planar; }
        if (normal == Vector3.down)
        { return new Vector3(planar.x, -planar.y, -planar.z); }

        return planar2WorldVector(planar, matrix);
    }

    static public Vector3 planar2WorldVector(Vector3 planar, Vector3 normal)
    {
        if (normal == Vector3.up)
        { return planar; }
        if (normal == Vector3.down)
        { return new Vector3(planar.x, -planar.y, -planar.z); }

        Vector3[] matrix = PlaneMatrix(normal);
        return planar2WorldVector(planar, matrix);
    }

    static public Vector3 planar2WorldVector(Vector3 planar, Vector3[] matrix)
    {
        return matrix[0] * planar.x + matrix[1] * planar.y + matrix[2] * planar.z;
    }

    static public Vector3[] PlaneMatrix(Vector3 normal)
    {
        if (normal == Vector3.up)
        { return new Vector3[] { Vector3.right, Vector3.up, Vector3.forward }; }
        if (normal == Vector3.down)
        { return new Vector3[] { Vector3.right, Vector3.down, -Vector3.forward }; }

        float horLength = Mathf.Sqrt(1f - normal.y * normal.y);
        Vector3 horVector = new Vector3(normal.x, 0, normal.z).normalized;
        Vector3 xVector = new Vector3(-horVector.z, 0, horVector.x);
        Vector3 zVector = -horVector * normal.y + Vector3.up * horLength;

        return new Vector3[] { xVector, normal, zVector };
    }

    public static Vector3[] defaultMatrix()
    { return new Vector3[] { Vector3.right, Vector3.up, Vector3.forward }; }

    static public Vector2 RelativeCoordinates(Vector2 abs, Vector2 A, Vector2 B)
    {
        float b = (abs.y - abs.x * A.y / A.x) / (B.y - A.y / A.x * B.x);
        float a = (abs.x - B.x * b) / A.x;

        return new Vector2(a, b);
    }

    static public Vector3 RelativeCoordinates(Vector3 abs, Vector3 A, Vector3 B, Vector3 C)
    {
        float a = ((C.y * abs.x - C.x * abs.y) * (B.x * C.z - B.z * C.x) - (C.z * abs.x - C.x * abs.z) * (B.x * C.y - B.y * C.x)) / ((A.x * C.y - A.y * C.x) * (B.x * C.z - B.z * C.x) - (A.x * C.z - A.z * C.x) * (B.x * C.y - B.y * C.x));
        abs -= A * a;
        float b = (C.y * abs.x - C.x * abs.y) / (B.x * C.y - B.y * C.x);
        abs -= B * b;
        float c = abs.x / C.x;

        return new Vector3(a, b, c);
    }

    public static Vector2 CircleOverlapPoint(Vector2 pos1, Vector2 pos2, float rad1, float rad2, bool second)
    {
        return CircleOverlapPoints(pos1, pos2, rad1, rad2)[second ? 1 : 0];
    }

    static public Vector2[] CircleOverlapPoints(Vector2 pos1, Vector2 pos2, float rad1, float rad2)
    {
        float one_x = pos1.x;
        float one_y = pos1.y;
        float one_r = rad1;
        float two_x = pos2.x;
        float two_y = pos2.y;
        float two_r = rad2;

        float hyp = Hypotenuse(two_x - one_x, two_y - one_y);

        float ex = (two_x - one_x) / hyp;
        float ey = (two_y - one_y) / hyp;

        float x = (one_r * one_r - two_r * two_r + hyp * hyp) / (2 * hyp);
        float y = Mathf.Sqrt(one_r * one_r - x * x);

        Vector2 connect1 = new Vector2(one_x + (x * ex) - (y * ey), one_y + (x * ey) + (y * ex));
        Vector2 connect2 = new Vector2(one_x + (x * ex) + (y * ey), one_y + (x * ey) - (y * ex));

        return new Vector2[] { connect1, connect2 };
    }

    static public float Hypotenuse(float a, float b)
    {
        a *= a;
        b *= b;
        return Mathf.Sqrt(a + b);
    }

    static public Vector2[] CircleTangents(Vector3 circle, Vector2 point)
    {
        Vector2 dir = (Vector2)circle - point;
        float dist = dir.magnitude;

        if (dist > circle.z)
        {
            dir /= dist;
            Vector2 per = new Vector2(dir.y, -dir.x);

            float r2 = circle.z * circle.z;
            float b = r2 / dist;
            float a = Mathf.Sqrt(r2 - (b * b));
            dist -= b;

            return new Vector2[]
            {
            point + dir * dist + per * a,
            point + dir * dist - per * a
            };
        }

        return new Vector2[0];
    }

    public static Vector3 TangentCircle(Vector2 tangentPoint, Vector2 normal, Vector2 secondPoint)
    {
        normal = normal.normalized;
        Vector2 perp = new Vector2(normal.y, -normal.x);

        Vector2 target = Formulas.RelativeCoordinates(secondPoint - tangentPoint, normal, perp);
        float radius = (target.y * target.y + target.x * target.x) / target.x * 0.5f;
        Vector2 circlePos = tangentPoint + normal * radius;

        return new Vector3(circlePos.x, circlePos.y, Mathf.Abs(radius));
    }

    public static Vector3[] Arc(Vector3 circle, Vector2 angles, float resolution, int minPoints = 3)
    {
        float length = (angles.y - angles.x);
        int steps = Mathf.Max(Mathf.CeilToInt(Mathf.Abs(length) * resolution * circle.z), minPoints);
        float step = length / steps;

        Vector3[] newArc = new Vector3[steps + 1];

        for (int i = 0; i <= steps; i++)
        {
            float angle = angles.x + step * i;
            newArc[i] = new Vector2(circle.x, circle.y) + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * circle.z;
        }

        return newArc;
    }

    public static float AngleDifference(float ang1, float ang2)
    {
        float diff = ang2 - ang1;
        diff = diff / Mathf.PI * 0.5f;
        diff += 0.5f;
        diff -= Mathf.Floor(diff);
        diff -= 0.5f;
        return diff * Mathf.PI * 2f;
    }

    public static Vector2 BezierPoint2(Vector2[] points, float time)
    {
        if (points.Length < 2)
        { return points.Length == 1 ? points[0] : Vector2.zero; }

        Vector2[] newPoints = (Vector2[])points.Clone();

        for (int pointCount = points.Length; pointCount > 1; pointCount--)
        {
            for (int i = 0; i < pointCount - 1; i++)
            { newPoints[i] = time * newPoints[i + 1] + (1f - time) * newPoints[i]; }
        }

        return newPoints[0];
    }

    public static Vector3 BezierPoint3(Vector3[] points, float time)
    {
        if (points.Length < 2)
        { return points.Length == 1 ? points[0] : Vector3.zero; }

        Vector3[] newPoints = (Vector3[])points.Clone();

        for (int pointCount = points.Length; pointCount > 1; pointCount--)
        {
            for (int i = 0; i < pointCount - 1; i++)
            { newPoints[i] = time * newPoints[i + 1] + (1f - time) * newPoints[i]; }
        }

        return newPoints[0];
    }

    public static Vector3 Bezier3Point3(Vector3 x, Vector3 y, Vector3 z, float time)
    {
        float inv = 1f - time;
        x = y * time + x * inv;
        y = z * time + y * inv;
        return y * time + x * inv;
    }

    [System.Serializable]
    public class Follower3
    {
        public Vector3 pos, vel, prevInput;

        public float frequency = 1f, damping = 1f, response = 1f;
        float k1, k2, k3;

        public void Apply(Vector3 input)
        {
            k1 = damping / (Mathf.PI * frequency);
            k2 = 1 / (Mathf.Pow(2f * Mathf.PI * frequency, 2));
            k3 = response * damping / (2 * Mathf.PI * frequency);

            prevInput = input;
        }

        public void Update(float time, Vector3 input, Vector3 inVel, bool knownVel = false)
        {
            if (!knownVel)
            { inVel = (input - prevInput) / time; }
            prevInput = input;

            pos = pos + time * vel;
            vel = vel + time * (input + k3 * inVel - pos - k1 * vel) / k2;
        }
    }

    [System.Serializable]
    public class Follower2
    {
        public Vector2 pos, vel, prevInput;

        public float frequency = 1f, damping = 1f, response = 1f;
        float k1, k2, k3;

        public void Apply(Vector2 input)
        {
            k1 = damping / (Mathf.PI * frequency);
            k2 = 1 / (Mathf.Pow(2f * Mathf.PI * frequency, 2));
            k3 = response * damping / (2 * Mathf.PI * frequency);

            prevInput = input;
        }

        public void Update(float time, Vector2 input, Vector2 inVel, bool knownVel = false)
        {
            if (!knownVel)
            { inVel = (input - prevInput) / time; }
            prevInput = input;

            pos = pos + time * vel;
            vel = vel + time * (input + k3 * inVel - pos - k1 * vel) / k2;
        }
    }

    [System.Serializable]
    public class FollowerR
    {
        public Vector3 pos, prevInput;
        public Vector4 vel;

        public float frequency = 1f, damping = 1f, response = 1f;
        public float maxSpeed = 50f;
        float k1, k2, k3;

        public void Apply(Vector3 input)
        {
            k1 = damping / (Mathf.PI * frequency);
            k2 = 1 / (Mathf.Pow(2f * Mathf.PI * frequency, 2));
            k3 = response * damping / (2 * Mathf.PI * frequency);

            prevInput = input;
        }

        public void Update(float time, Vector3 input, Vector4 inVel, bool knownVel = false)
        {
            if (pos.sqrMagnitude < 0.00000000000001f)
            { pos = Vector3.right; }
            pos.Normalize();

            if (((Vector3)vel).sqrMagnitude < 0.00000000000001f)
            { vel = new Vector4(1, 0, 0, 0); }
            vel = new Vector4(0, 0, 0, vel.w) + (Vector4)((Vector3)vel.normalized);

            if (!knownVel)
            {
                inVel = GetTurn(prevInput, input);
                inVel.w = inVel.w / time;
            }
            prevInput = input;

            pos = RotateVector3(pos, (Vector3)vel, vel.w * time);

            Vector3 og = RotateVector3(pos, (Vector3)vel, vel.w * k1);
            Vector3 dest = RotateVector3(input, (Vector3)inVel, inVel.w * -k3);
            Vector3 addVel = Turn2Vel(GetTurn(dest, og), pos) * time / k2;
            vel = Vel2Turn(Turn2Vel(vel, pos) + addVel, pos);

            vel.w = Mathf.Min(vel.w, maxSpeed);
        }
    }

    public static Vector4 GetTurn(Vector3 og, Vector3 dest)
    {
        if (Vector3.Distance(og, dest) < 0.00000000000001f)
        { return new Vector4(0, 1, 0, 0); }

        Vector3 axis = Vector3.Cross(og, dest).normalized;
        float angle = Vector3.Angle(og, dest) * Mathf.Deg2Rad;

        return new Vector4(0, 0, 0, angle) + (Vector4)axis;
    }

    public static Vector3 Turn2Vel(Vector4 turn, Vector3 og)
    {
        return Vector3.Cross((Vector3)turn, og).normalized * turn.w;
    }

    public static Vector4 Vel2Turn(Vector3 vel, Vector3 og)
    {
        Vector3 axis = Vector3.Cross(og, vel).normalized;
        return new Vector4(0, 0, 0, vel.magnitude) + (Vector4)axis;
    }

    public static Vector3 RotateVector3(Vector3 og, Vector4 turn)
    {
        return RotateVector3(og, turn, turn.w);
    }

    public static Vector3 RotateVector3(Vector3 og, Vector3 axis, float angle)
    {
        if (Mathf.Abs(angle) < 0.00000000000001f || axis.magnitude < 0.00000000000001f)
        { return og; }

        axis.Normalize();

        if (Mathf.Abs(Vector3.Dot(og, axis)) > 9.9999999999999999f)
        { return og; }

        Vector3 x = Vector3.Cross(axis, og).normalized;
        Vector3 y = Vector3.Cross(x, axis).normalized;

        Vector3 local = new Vector3(Vector3.Dot(x, og), Vector3.Dot(y, og), Vector3.Dot(axis, og));

        return axis * local.z + Mathf.Sin(angle) * (-local.y * x + local.x * y) + Mathf.Cos(angle) * (local.x * x + local.y * y);
    }
}
