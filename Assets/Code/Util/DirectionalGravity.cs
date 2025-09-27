using Unity.Android.Gradle.Manifest;
using Unity.VisualScripting;
using UnityEngine;

public class DirectionalGravity : MonoBehaviour
{
    [SerializeField]
    private Rigidbody body;
    private Vector3? gravityDirection;

    private void Awake()
    {
        EnsureBodyReference();
    }

    private void EnsureBodyReference()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }
    }

    public void AssignGravityDirection(Vector3 direction)
    {
        //ok. so we are going to assign our gravity!
        EnsureBodyReference();
        if (body != null)
        {
            body.useGravity = false;
            gravityDirection = direction.normalized;
        }
    }

    private void FixedUpdate()
    {
        if (body != null && gravityDirection.HasValue)
        {
            body.AddForce(gravityDirection.Value * Game.GRAVITY_ACCELERATION * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }
    }
}
