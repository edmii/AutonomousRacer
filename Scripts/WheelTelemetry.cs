// using UnityEngine;
// using NWH.WheelController3D;

// public class NwhWheelTelemetry : MonoBehaviour
// {
//     [Header("NWH WheelControllers")]
//     public WheelController FL, FR, RL, RR;
//     public Rigidbody rb;

//     [Header("On-track via LAYER (required)")]
//     public LayerMask trackSurfaceMask;        // set to your TrackSurface layer(s)
//     public float raycastFallbackDist = 5f;  // for rare fallback when HitCollider is null

//     [Header("Normalization (torques)")]
//     public float maxMotorTorqueNm = 1000f;    // set to your car’s peak wheel torque
//     public float maxBrakeTorqueNm = 5500f;    // set to your car’s peak brake torque per wheel

//     [Header("Aggregates (read-only)")]
//     public bool WheelsOnTrack { get; private set; }
//     public bool anyWheelLocked   { get; private set; }
//     public bool anyWheelSpinning { get; private set; }
//     public float maxAbsLatSlip   { get; private set; }
//     public float maxAbsLongSlip  { get; private set; }
//     public float normMotorTorque { get; private set; } // [0..1]
//     public float normBrakeTorque { get; private set; } // [0..1]

//     public bool onTrack;

//     // Per-wheel (optional diagnostics)
//     public float latFL, latFR, latRL, latRR;
//     public float lonFL, lonFR, lonRL, lonRR;
//     public bool  onTrackFL, onTrackFR, onTrackRL, onTrackRR;
//     public bool  lockedFL,  lockedFR,  lockedRL,  lockedRR;
//     public bool  spinFL,    spinFR,    spinRL,    spinRR;

//     void Reset(){ if(!rb) rb = GetComponent<Rigidbody>(); }

//     void FixedUpdate()
//     {
//         float vLong = rb ? Vector3.Dot(rb.linearVelocity, transform.forward) : 0f;

//         ReadWheel(FL, ref latFL, ref lonFL, ref onTrackFL, ref lockedFL, ref spinFL, vLong);
//         ReadWheel(FR, ref latFR, ref lonFR, ref onTrackFR, ref lockedFR, ref spinFR, vLong);
//         ReadWheel(RL, ref latRL, ref lonRL, ref onTrackRL, ref lockedRL, ref spinRL, vLong);
//         ReadWheel(RR, ref latRR, ref lonRR, ref onTrackRR, ref lockedRR, ref spinRR, vLong);

//         maxAbsLatSlip   = Mathf.Max(Mathf.Abs(latFL), Mathf.Abs(latFR), Mathf.Abs(latRL), Mathf.Abs(latRR));
//         maxAbsLongSlip  = Mathf.Max(Mathf.Abs(lonFL), Mathf.Abs(lonFR), Mathf.Abs(lonRL), Mathf.Abs(lonRR));
//         anyWheelLocked  = lockedFL || lockedFR || lockedRL || lockedRR;
//         anyWheelSpinning= spinFL   || spinFR   || spinRL   || spinRR;
//         WheelsOnTrack= onTrackFL || onTrackFR || onTrackRL || onTrackRR;


//         // Normalize torques (use absolute and clamp)
//         float motor = 0f, brake = 0f;
//         AccumulateTorques(FL, ref motor, ref brake);
//         AccumulateTorques(FR, ref motor, ref brake);
//         AccumulateTorques(RL, ref motor, ref brake);
//         AccumulateTorques(RR, ref motor, ref brake);
//         // average per-wheel then normalize
//         motor /= 4f; brake /= 4f;
//         normMotorTorque = Mathf.Clamp01(Mathf.Abs(motor) / Mathf.Max(1f, maxMotorTorqueNm));
//         normBrakeTorque = Mathf.Clamp01(Mathf.Abs(brake) / Mathf.Max(1f, maxBrakeTorqueNm));
//     }

//     void AccumulateTorques(WheelController w, ref float motor, ref float brake)
//     {
//         if (!w) return;
//         motor += w.MotorTorque;
//         brake += w.BrakeTorque;
//     }

//     const float speedDeadband = 0.15f;   // m/s  (~15 cm/s)
//     const float slipDeadband  = 0.02f;   // slip units (dimensionless)
//     void ReadWheel(
//         WheelController wc,
//         ref float latSlip, ref float lonSlip,
//         ref bool onTrack, ref bool locked, ref bool spinning,
//         float vLong)
//     {
//         latSlip = lonSlip = 0f; onTrack = locked = spinning = false;
//         if (!wc) return;

//         // Raw slips
//         float lon = wc.LongitudinalSlip;
//         float lat = wc.LateralSlip;

//         // Deadband for near-standstill or tiny slips
//         bool nearlyStopped = Mathf.Abs(vLong) < speedDeadband;
//         if (nearlyStopped) { lon = 0f; lat = 0f; }
//         if (Mathf.Abs(lon) < slipDeadband) lon = 0f;
//         if (Mathf.Abs(lat) < slipDeadband) lat = 0f;

//         lonSlip = lon;
//         latSlip = lat;

//         // On-track: LAYER via HitCollider first, raycast fallback
//         Debug.Log($"Wheel {wc.name} grounded={wc.IsGrounded} hit={wc.HitCollider}");
//         if (wc.IsGrounded)
//         {
//             int ts = LayerMask.NameToLayer("TrackSurface");
//             Debug.Log($"[Diag] TrackSurface index={ts}  maskVal={trackSurfaceMask.value}  expectedBit={(1 << ts)}");

//             // ray start: wheel position slightly above
//             Vector3 origin = wc.transform.position + Vector3.up * 0.1f;
//             Debug.DrawRay(origin, Vector3.down * raycastFallbackDist, Color.cyan, 0.02f, false);
//             // make sure this distance reaches your asphalt if it's below the lines
//             float dist = Mathf.Max(2.0f, raycastFallbackDist);  // e.g., 1–2 m

//             if (Physics.Raycast(origin, Vector3.down, out var hit, dist, trackSurfaceMask,
//                                 QueryTriggerInteraction.Ignore))
//             {
//                 onTrack = true;
                
//             }
            
//             Debug.Log($"[Diag] OnTrack: {WheelsOnTrack}");
            
            
//             // Lock / spin heuristics
//             float radius = wc.wheel.radius;
//             float omega  = wc.AngularVelocity;              // rad/s
//             float tangential = Mathf.Abs(omega) * radius;   // m/s

//             locked   = (Mathf.Abs(vLong) > 3f) && (tangential < 0.5f) && (wc.BrakeTorque > 0.1f);
//             spinning = (vLong > 3f) && (omega * radius > vLong + 2f) && (wc.MotorTorque > 0.1f);
//         }
//         else
//         {
            
//         }
//     }
// }

using UnityEngine;
using NWH.WheelController3D;

public class NwhWheelTelemetry : MonoBehaviour
{
    [Header("NWH WheelControllers")]
    public WheelController FL, FR, RL, RR;
    public Rigidbody rb;

    [Header("On-track via LAYER (required)")]
    public LayerMask trackSurfaceMask;        // set to your TrackSurface layer(s)
    public float raycastFallbackDist = 0.5f;  // for rare fallback when HitCollider is null

    [Header("Normalization (torques)")]
    public float maxMotorTorqueNm = 1000f;    // set to your car’s peak wheel torque
    public float maxBrakeTorqueNm = 5500f;    // set to your car’s peak brake torque per wheel

    [Header("Aggregates (read-only)")]
    public bool WheelsOnTrack { get; private set; }
    public bool isGrounded { get; private set; }
    public bool anyWheelLocked   { get; private set; }
    public bool anyWheelSpinning { get; private set; }
    public float maxAbsLatSlip   { get; private set; }
    public float maxAbsLongSlip  { get; private set; }
    public float normMotorTorque { get; private set; } // [0..1]
    public float normBrakeTorque { get; private set; } // [0..1]

    // Per-wheel (optional diagnostics)
    public float latFL, latFR, latRL, latRR;
    public float lonFL, lonFR, lonRL, lonRR;
    public bool  onTrackFL, onTrackFR, onTrackRL, onTrackRR;
    public bool  groundedFL, groundedFR, groundedRL, groundedRR;
    public bool  lockedFL,  lockedFR,  lockedRL,  lockedRR;
    public bool  spinFL,    spinFR,    spinRL,    spinRR;

    void Reset(){ if(!rb) rb = GetComponent<Rigidbody>(); }

    void FixedUpdate()
    {
        float vLong = rb ? Vector3.Dot(rb.linearVelocity, transform.forward) : 0f;

        ReadWheel(FL, ref latFL, ref lonFL, ref onTrackFL, ref groundedFL, ref lockedFL, ref spinFL, vLong);
        ReadWheel(FR, ref latFR, ref lonFR, ref onTrackFR, ref groundedFR, ref lockedFR, ref spinFR, vLong);
        ReadWheel(RL, ref latRL, ref lonRL, ref onTrackRL, ref groundedRL, ref lockedRL, ref spinRL, vLong);
        ReadWheel(RR, ref latRR, ref lonRR, ref onTrackRR, ref groundedRR, ref lockedRR, ref spinRR, vLong);

        maxAbsLatSlip   = Mathf.Max(Mathf.Abs(latFL), Mathf.Abs(latFR), Mathf.Abs(latRL), Mathf.Abs(latRR));
        maxAbsLongSlip  = Mathf.Max(Mathf.Abs(lonFL), Mathf.Abs(lonFR), Mathf.Abs(lonRL), Mathf.Abs(lonRR));
        anyWheelLocked  = lockedFL || lockedFR || lockedRL || lockedRR;
        anyWheelSpinning= spinFL   || spinFR   || spinRL   || spinRR;
        WheelsOnTrack= onTrackFL || onTrackFR || onTrackRL || onTrackRR;
        isGrounded = groundedFL || groundedFR || groundedRL || groundedRR;

        // Debug.Log($"WheelsOnTrack: {WheelsOnTrack} | FL: {onTrackFL} | FR: {onTrackFR} | RL: {onTrackRL} | RR: {onTrackRR}");

        // Normalize torques (use absolute and clamp)
        float motor = 0f, brake = 0f;
        AccumulateTorques(FL, ref motor, ref brake);
        AccumulateTorques(FR, ref motor, ref brake);
        AccumulateTorques(RL, ref motor, ref brake);
        AccumulateTorques(RR, ref motor, ref brake);
        // average per-wheel then normalize
        motor /= 4f; brake /= 4f;
        normMotorTorque = Mathf.Clamp01(Mathf.Abs(motor) / Mathf.Max(1f, maxMotorTorqueNm));
        normBrakeTorque = Mathf.Clamp01(Mathf.Abs(brake) / Mathf.Max(1f, maxBrakeTorqueNm));
    }

    void AccumulateTorques(WheelController w, ref float motor, ref float brake)
    {
        if (!w) return;
        motor += w.MotorTorque;
        brake += w.BrakeTorque;
    }

    const float speedDeadband = 0.15f;   // m/s  (~15 cm/s)
    const float slipDeadband  = 0.02f;   // slip units (dimensionless)
    void ReadWheel(
        WheelController wc,
        ref float latSlip, ref float lonSlip,
        ref bool onTrack, ref bool grounded, ref bool locked, ref bool spinning,
        float vLong)
    {
        // latSlip = lonSlip = 0f; onTrack = locked = spinning = false;
        latSlip = lonSlip = 0f; onTrack = grounded = locked = spinning = false;
        if (!wc) return;
        
        // Check if wheel is grounded
        grounded = wc.IsGrounded;
        // Debug.Log($"Grounded: {grounded}");

        // Raw slips
        float lon = wc.LongitudinalSlip;
        float lat = wc.LateralSlip;

        // Deadband for near-standstill or tiny slips
        bool nearlyStopped = Mathf.Abs(vLong) < speedDeadband;
        if (nearlyStopped) { lon = 0f; lat = 0f; }
        if (Mathf.Abs(lon) < slipDeadband) lon = 0f;
        if (Mathf.Abs(lat) < slipDeadband) lat = 0f;

        lonSlip = lon;
        latSlip = lat;

        // On-track: LAYER via HitCollider first, raycast fallback
        if (wc.IsGrounded && wc.HitCollider)
        {
            int layer = wc.HitCollider.gameObject.layer;
            onTrack = ((trackSurfaceMask.value & (1 << layer)) != 0);
        }
        else
        {
            // Fallback short raycast straight down from the wheel
            Vector3 origin = wc.transform.position;
            if (Physics.Raycast(origin, Vector3.down, out var hit, raycastFallbackDist, trackSurfaceMask, QueryTriggerInteraction.Ignore))
                onTrack = true;
        }

        // Lock / spin heuristics
        if (wc.IsGrounded)
        {
            float radius = wc.wheel.radius;
            float omega  = wc.AngularVelocity;              // rad/s
            float tangential = Mathf.Abs(omega) * radius;   // m/s

            locked   = (Mathf.Abs(vLong) > 3f) && (tangential < 0.5f) && (wc.BrakeTorque > 0.1f);
            spinning = (vLong > 3f) && (omega * radius > vLong + 2f) && (wc.MotorTorque > 0.1f);
        }
    }
}

