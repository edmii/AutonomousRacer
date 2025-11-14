using UnityEngine;

public class RaycastSensor : MonoBehaviour
{
    [Header("Geometry")]
    public int rays = 11;                 // odd is enforced in OnValidate()
    [Range(10f, 270f)] public float fov = 170f;
    public float height = 0.25f;
    public Vector3 localOffset = new Vector3(0f, 0f, 0.3f);

    [Header("Per-ray ranges (3-tier)")]
    [Tooltip("Meters for the outer rays.")]
    public float sideRange   = 40f;
    [Tooltip("Meters for the rays near the center (each side).")]
    public float middleRange = 60f;
    [Tooltip("How many rays on EACH side of center use middleRange.")]
    public int middleHalfWidth = 2; // e.g., 2 → 4 middle rays total (not counting the center)
    [Tooltip("Meters for the exact center ray (straight ahead).")]
    public float centerRange = 90f;

    [Header("Physics")]
    public LayerMask detectableLayers;
    public bool hitTriggers = false;

    [Header("Debug")]
    public bool drawGizmos = true;

    // Outputs
    public float[]  Distances  { get; private set; } // normalized 0..1 by each ray's max
    public Vector3[] HitNormals { get; private set; }
    public float[]  PerRayMax   { get; private set; } // optional: expose scale used for each ray

    // --- Index helpers ---
    public int CenterIndex => rays / 2;
    public int LeftmostIndex  => 0;
    public int RightmostIndex => Mathf.Max(0, rays - 1);

    // Main generic accessor 
    public float GetNormalizedDistance(int i)
    {
        if (Distances == null || i < 0 || i >= rays) return 1f;
        return Distances[i];
    }

    // Convenience wrappers (mirrors the center one)
    public float GetCenterNormalizedDistance()      => GetNormalizedDistance(CenterIndex);
    public float GetLeftmostNormalizedDistance()    => GetNormalizedDistance(LeftmostIndex);
    public float GetRightmostNormalizedDistance()   => GetNormalizedDistance(RightmostIndex);

    // Offsets relative to center (k = 1 is immediately left/right of center)
    public float GetLeftOfCenter(int k = 1)  => GetNormalizedDistance(Mathf.Clamp(CenterIndex - k, 0, rays-1));
    public float GetRightOfCenter(int k = 1) => GetNormalizedDistance(Mathf.Clamp(CenterIndex + k, 0, rays-1));

    // --- Angle-based helpers ---
    // Angle of ray i, in degrees relative to forward (negative = left, positive = right)
    public float GetRayAngleDeg(int i)
    {
        if (rays == 1) return 0f;
        float t = (float)i / (rays - 1);
        return -fov * 0.5f + t * fov;
    }

    // Map an angle to the nearest ray index
    public int GetIndexForAngle(float angleDeg)
    {
        float t = Mathf.InverseLerp(-fov * 0.5f, fov * 0.5f, angleDeg);
        return Mathf.Clamp(Mathf.RoundToInt(t * (rays - 1)), 0, rays - 1);
    }

    // Read by angle directly (still 0..1)
    public float GetNormalizedByAngle(float angleDeg) => GetNormalizedDistance(GetIndexForAngle(angleDeg));

    // --- Sector summaries (great for rewards / safety logic) ---
    // Returns min (most dangerous) distance in an angular window centered at angleDeg with halfWidthDeg
    public float GetSectorMin(float angleDeg, float halfWidthDeg)
    {
        int i0 = GetIndexForAngle(angleDeg - halfWidthDeg);
        int i1 = GetIndexForAngle(angleDeg + halfWidthDeg);
        if (i0 > i1) { var t = i0; i0 = i1; i1 = t; }

        float m = 1f;
        for (int i = i0; i <= i1; i++) m = Mathf.Min(m, GetNormalizedDistance(i));
        return m; // 0..1, lower means closer obstacle in that sector
    }


    void OnValidate()
    {
        if (rays < 1) rays = 1;
        // Enforce odd so we have a single center ray.
        if ((rays % 2) == 0) rays += 1;

        sideRange   = Mathf.Max(0.01f, sideRange);
        middleRange = Mathf.Max(0.01f, middleRange);
        centerRange = Mathf.Max(0.01f, centerRange);

        int center = rays / 2;
        middleHalfWidth = Mathf.Clamp(middleHalfWidth, 0, center);
    }

    void Awake()
    {
        Distances  = new float[rays];
        HitNormals = new Vector3[rays];
        PerRayMax  = new float[rays];
        for (int i = 0; i < rays; i++) { Distances[i] = 1f; HitNormals[i] = Vector3.zero; PerRayMax[i] = sideRange; }
    }

    void FixedUpdate() => SampleRays();

    float RangeForIndex(int i)
    {
        int center = rays / 2;
        int d = Mathf.Abs(i - center);
        if (d == 0) return centerRange;                       // straight-ahead
        if (d <= middleHalfWidth) return middleRange;         // near-center
        return sideRange;                                     // outer/side
    }

    public void SampleRays()
    {
        if (Distances == null || Distances.Length != rays) { Awake(); }

        Vector3 origin = transform.TransformPoint(localOffset) + Vector3.up * height;

        for (int i = 0; i < rays; i++)
        {
            float t = (rays == 1) ? 0.5f : (float)i / (rays - 1);
            float angleDeg = -fov * 0.5f + t * fov;
            Vector3 dir = Quaternion.Euler(0f, angleDeg, 0f) * transform.forward;

            float perRayMax = RangeForIndex(i);
            PerRayMax[i] = perRayMax;

            RaycastHit hit;
            bool gotHit = Physics.Raycast(
                origin, dir, out hit, perRayMax, detectableLayers,
                hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore
            );

            if (gotHit)
            {
                Distances[i]  = hit.distance / perRayMax; // 0..1
                HitNormals[i] = hit.normal;
            }
            else
            {
                Distances[i]  = 1f;
                HitNormals[i] = Vector3.zero;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || rays < 1) return;

        if (Distances == null || Distances.Length != rays)
        {
            Distances = new float[rays];
            for (int i = 0; i < rays; i++) Distances[i] = 1f;
        }

        Vector3 origin = transform.TransformPoint(localOffset) + Vector3.up * height;

        for (int i = 0; i < rays; i++)
        {
            float t = (rays == 1) ? 0.5f : (float)i / (rays - 1);
            float angleDeg = -fov * 0.5f + t * fov;
            Vector3 dir = Quaternion.Euler(0f, angleDeg, 0f) * transform.forward;

            float perRayMax = RangeForIndex(i);
            float dWorld = Mathf.Clamp01(Distances[i]) * Mathf.Max(0.01f, perRayMax);
            Vector3 end = origin + dir * dWorld;

            Color c = Color.Lerp(Color.red, Color.green, Distances[i]);
            Gizmos.color = c;
            Gizmos.DrawLine(origin, end);
            Gizmos.DrawSphere(end, 0.03f);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, 0.04f);
    }
}
