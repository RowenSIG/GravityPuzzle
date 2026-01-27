using Unity.Android.Gradle.Manifest;
using Unity.VisualScripting;
using UnityEngine;

public class DirectionalGravity : MonoBehaviour
{
    [SerializeField]
    private Rigidbody body;
    public Rigidbody Body => body;

    [SerializeField]
    private Vector3 gravityDirection;
    public Vector3 GravityDirection => gravityDirection;

    [SerializeField]
    private bool assignGravityOnAwake;
    private bool gravityActive = true;
    public bool GravityActive => gravityActive;

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

    public void SetGravityActive(bool active)
    {
        this.gravityActive = active;
    }

    private void FixedUpdate()
    {
        if ( body != null && gravityActive )
        {
            body.AddForce(gravityDirection * Game.GRAVITY_ACCELERATION * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }
    }

}
