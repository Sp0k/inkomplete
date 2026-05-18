using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;

public class Drawbridge : MonoBehaviour
{
    public NavMeshSurface Surface;
    public GameObject BridgeToObject;
    public float ForceMultiplier = 1f;

    private Rigidbody rb;
    private bool _knockedOver = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    public void PushObject()
    {
        if (_knockedOver) return;

        var forceVector = BridgeToObject.transform.position - transform.position;
        forceVector.y = 0;
        forceVector.Normalize();

        Vector3 torqueAxis = Vector3.Cross(Vector3.up, forceVector);

        rb.isKinematic = false;
        rb.AddTorque(torqueAxis * ForceMultiplier, ForceMode.Impulse);
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            PushObject();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == BridgeToObject.gameObject)
        {
            _knockedOver = true;
            Surface.BuildNavMesh();
            rb.isKinematic = true;
        }
    }
}