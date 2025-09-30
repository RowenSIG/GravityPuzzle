using Unity.Android.Gradle.Manifest;
using Unity.VisualScripting;
using UnityEngine;

public class DirectionalGravity : MonoBehaviour
{
    [SerializeField]
    private Rigidbody body;

    [SerializeField]
    private Vector3 gravityDirection;
    public Vector3 GravityDirection => gravityDirection;

    [SerializeField]
    private bool assignGravityOnAwake;

    private void Awake()
    {
        EnsureBodyReference();
        if (assignGravityOnAwake)
        {
            AssignGravityDirection(gravityDirection);
        }
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
        if ( body != null )
        {
            body.AddForce(gravityDirection * Game.GRAVITY_ACCELERATION * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }
    }
}
