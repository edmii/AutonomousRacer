
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour
{
    [Tooltip("Order around the lap (0..N-1). Will be auto-filled by the manager if left as -1).")]
    public int Index = -1;

    public CheckpointManager Manager;    // drag your Checkpoints parent (with manager) here
    CheckpointTrigger trig;

    void Awake()
    {
        trig = GetComponent<CheckpointTrigger>();
        if (!Manager) Manager = GetComponentInParent<CheckpointManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        var rb = other.attachedRigidbody;
        var agent = rb ? rb.GetComponent<CarAgent>() : null;
        if (!agent || !Manager) return;

        Manager.ReportHit(agent, trig.Index, rb.linearVelocity, transform.forward);
    }

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnDrawGizmos()
    {
        var col = GetComponent<Collider>();
        if (!col) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        Gizmos.color = Color.green;               // direction you expect the car to cross
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}
