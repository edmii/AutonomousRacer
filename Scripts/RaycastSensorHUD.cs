using UnityEngine;
using System.Text;

public class RaycastSensorHUD : MonoBehaviour
{
    public RaycastSensor sensor;

    void Reset() { if (sensor == null) sensor = GetComponent<RaycastSensor>(); }

    void OnGUI()
    {
        if (!sensor || sensor.Distances == null) return;

        var d = sensor.Distances;
        var sb = new StringBuilder("Ray distances (0..1)\n");
        for (int i = 0; i < d.Length; i++)
        {
            sb.Append(i).Append(':').Append(d[i].ToString("0.00"));
            if (i < d.Length - 1) sb.Append("  ");
        }

        const int pad = 10;
        GUILayout.BeginArea(new Rect(10, 10, Screen.width * 0.6f, 80), GUI.skin.box);
        GUILayout.Label(sb.ToString());
        GUILayout.EndArea();
    }
}
