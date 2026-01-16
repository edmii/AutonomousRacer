// using UnityEngine;
// using System.Text;

// public class RaycastSensorConsoleLog : MonoBehaviour
// {
//     public RaycastSensor sensor;
//     [Tooltip("Seconds between logs to avoid spamming the Console.")]
//     public float interval = 0.5f;

//     float nextLog;

//     void Reset() { if (sensor == null) sensor = GetComponent<RaycastSensor>(); }

//     void Update()
//     {
//         if (!sensor || sensor.Distances == null) return;
//         if (Time.unscaledTime < nextLog) return;
//         nextLog = Time.unscaledTime + interval;

//         var arr = sensor.Distances;
//         var sb = new StringBuilder(8 * arr.Length);
//         for (int i = 0; i < arr.Length; i++)
//         {
//             sb.Append(arr[i].ToString("0.00"));
//             if (i < arr.Length - 1) sb.Append(" | ");
//         }
//         Debug.Log($"[Rays 0..1] center={sensor.GetCenterNormalizedDistance():0.00} :: {sb}");
//     }
// }

using UnityEngine;
using System.Text;

public class RaycastSensorConsoleLog : MonoBehaviour
{
    public RaycastSensor sensor;
    [Tooltip("Seconds between logs to avoid spamming the Console.")]
    public float interval = 0.5f;

    float nextLog;

    void Reset() { if (sensor == null) sensor = GetComponent<RaycastSensor>(); }

    void Update()
    {
        if (!sensor || sensor.Distances == null) return;
        if (Time.unscaledTime < nextLog) return;
        nextLog = Time.unscaledTime + interval;

        var arr = sensor.Distances;
        var sb = new StringBuilder(8 * arr.Length);
        for (int i = 0; i < arr.Length; i++)
        {
            sb.Append(arr[i].ToString("0.00"));
            if (i < arr.Length - 1) sb.Append(" | ");
        }
        // Debug.Log($"[Rays 0..1] center={sensor.GetCenterNormalizedDistance():0.00} :: {sb}");
    }
}
