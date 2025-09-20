using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Logging;

public class PlayerGravRaycastControl : PlayerComponentControls
{
    [SerializeField]
    private Transform rayOrigin;

    [SerializeField]
    private CollisionReactor collisionReactor;
    

    private bool firing = false;

    protected void Awake()
    {
        collisionReactor.OnOnCollisionEnter += CollisionEnter;
    }
    protected void OnDestroy()
    {
        collisionReactor.OnOnCollisionEnter -= CollisionEnter;
    }

    public void UpdateFireInput(bool fire)
    {
        return;

        if (fire && firing == false)
        {
            firing = true;
            //cast the ray!
            var ray = new Ray(rayOrigin.position, transform.forward);
            var hit = Physics.Raycast(ray, out RaycastHit hitinfo, 100f);

            if (hit)
            {

                //we just want a normal, right?
                var normal = hitinfo.normal;
                Log($"[PlayerGravRaycastControl] hitNormal [{normal}]");
                player.SetNewPlayerUp(normal);
            }
        }
        else if (fire == false)
        {
            firing = false;
        }
    }

    private Collider currentCollider = null;
    private Collider previousCollider = null;
    private float previousColliderIgnoreTimer = 0f;
    private const float IGNORE_TIMER = 0.3f;

    public void UpdateFixedPhysics()
    {
        //how do we figure out what our floor is?
        previousColliderIgnoreTimer -= Time.deltaTime;
    }

    private void DeriveUpFromCollision(Collision collision)
    {
        if (collision.collider != currentCollider)
        {
            previousCollider = currentCollider;
            previousColliderIgnoreTimer = IGNORE_TIMER;
        }

        currentCollider = collision.collider;

        contacts.Clear();
        contacts.AddRange(collision.contacts);
    
        var up = collision.contacts[0].normal;
        player.SetNewPlayerUp(up);
        return;
    }

    protected void CollisionEnter(Collision collision)
    {
        Log($"[PlayerGravRaycastControl] OnCollisionEnter[{collision}]");

        if (collision.collider == previousCollider)
        {
            if (previousColliderIgnoreTimer > 0)
                return;
        }
        DeriveUpFromCollision(collision);
    }

    protected void CollisionStay(Collision collision)
    {
        //not quite working right.

        // if(collision.collider == currentCollider)
        //     DeriveUpFromCollision(collision);

    }


    private List<ContactPoint> contacts = new(64);

#if DEBUG

    protected void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        foreach (var contact in contacts)
        {
            Gizmos.DrawWireCube(contact.point, Vector3.one * 0.1f);
            Gizmos.DrawLine(contact.point, contact.point + contact.normal * 3f);
        }
    }
#endif

}
