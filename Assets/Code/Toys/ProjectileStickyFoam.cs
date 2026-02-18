using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ProjectileStickyFoam : MonoBehaviour
{
    [SerializeField]
    private float projectileFlySpeed;
    [SerializeField]
    private float projectileSettleSpeed;
    [SerializeField]
    private float projectileSettleGrow;

    [SerializeField]
    private float postFirstHitContinuation;

    [SerializeField]
    private float noHitSelfDestructTime;
    private float timeOfSpawn;

    [SerializeField]
    private int bondLimit;

    [SerializeField]
    private float breakForce;
    [SerializeField]
    private float breakTorque;

    private Rigidbody body;
    private Collider ourCollider;
    
    private List<Collider> otherHitColliders = new();
    private List<FixedJoint> joints = new();

    private bool firstHitMade = false;
    private bool settled = false;
    private float timeOfFirstHit = 0f;

    private Vector3 direction;
    private Collider ignoreWhileFlyingCollider = null;

    private void Awake()
    {
        body = GetComponentInChildren<Rigidbody>();
        timeOfSpawn = Time.realtimeSinceStartup;
        ourCollider = GetComponentInChildren<Collider>();
        var collisionChecker = ourCollider.AddComponent<CollisionReactor>();
        collisionChecker.OnOnTriggerEnter += OnTriggerEnter;
        collisionChecker.OnOnTriggerExit += OnTriggerExit;
        collisionChecker.OnZeroSwordHit += Remove;
    }

    public void Shoot(Vector3 direction, Collider ignoreCollider)
    {
        ignoreWhileFlyingCollider = ignoreCollider;
        Physics.IgnoreCollision(ourCollider, ignoreWhileFlyingCollider);
        this.direction = direction;
    }

    public void OnTriggerEnter(Collider other)
    {
        //we have hit something! yay.
        if(settled)
            return;

        //well?
        if(other == ourCollider)
            return;

        if(otherHitColliders.Count >= bondLimit)
            return;

        if(otherHitColliders.Contains(other))
            return;

        if(otherHitColliders.Exists((p) => p.attachedRigidbody == other.attachedRigidbody))
            return;

        if(otherHitColliders.Count == 0)
        {
            firstHitMade = true;
            timeOfFirstHit = Time.time;
        }

        otherHitColliders.Add(other);
    }

    public void OnTriggerExit(Collider other)
    {
        if(otherHitColliders.Contains(other))
            otherHitColliders.Remove(other);
    }

    public void FixedUpdate()
    {
        //we move in our direction, but physics-style
        if(settled)
            return;
        
        if(firstHitMade == false)
        {
            //we just keep going

            if(Time.time - timeOfSpawn > noHitSelfDestructTime)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                body.MovePosition(body.position + direction * Time.fixedDeltaTime * projectileFlySpeed);
            }
        }
        else
        {
            //we will move forward at our other speed until our time is up, then we stop and form bonds
            if(Time.time - timeOfFirstHit < postFirstHitContinuation)
            {
                body.MovePosition(body.position + direction * Time.fixedDeltaTime * projectileSettleSpeed);
                body.transform.localScale = body.transform.localScale + Vector3.one * Time.fixedDeltaTime * projectileSettleGrow;
            }
            else
            {
                settled = true;

                CreateBonds();
                Harden();
            }
        }
    }

    private void CreateBonds()
    {
        //shuld we attach any/all bodies to ourselves? yeah why not.

        var worldOrigin = transform.position;
        var worldAxis = transform.forward; //why not.

        foreach(var otherCollider in otherHitColliders)
        {
            if(otherCollider.attachedRigidbody == body)
                continue;

            var stickyFoam = otherCollider.GetComponentInParent<ProjectileStickyFoam>();
            if(stickyFoam)
            {
                Physics.IgnoreCollision(ourCollider, otherCollider);
                continue;
            }
                
            var origin = otherCollider.transform.InverseTransformPoint(worldOrigin);
            var axis = otherCollider.transform.InverseTransformDirection(worldAxis);

            var fixedJoint = body.gameObject.AddComponent<FixedJoint>();
            fixedJoint.autoConfigureConnectedAnchor = true;

            if(otherCollider.attachedRigidbody != null)
            {
                fixedJoint.connectedBody = otherCollider.attachedRigidbody;
            }
            else
            {
                //we're sticking to a collider without an rb...
                body.isKinematic = true;
            }

            fixedJoint.breakForce = breakForce;
            fixedJoint.breakTorque = breakTorque;
            fixedJoint.enableCollision = false;
            fixedJoint.enablePreprocessing = true;
            fixedJoint.massScale = 1f;
            fixedJoint.connectedMassScale = 1f;
            fixedJoint.anchor = origin;
            fixedJoint.axis = axis.normalized;

            joints.Add(fixedJoint);
        }
    }

    private void Harden()
    {
        //become a non trigger!?

        //what if we're sticking to other sticky foam?

        Physics.IgnoreCollision(ourCollider, ignoreWhileFlyingCollider, false);


        //we could sub parent, remove our own rb
        var tempJoints = new List<FixedJoint>(joints);

        foreach(var otherCollider in otherHitColliders)
        {
            var stickyFoam = otherCollider.GetComponentInParent<ProjectileStickyFoam>();
            if(stickyFoam)
            {
                var otherbody = otherCollider.attachedRigidbody;
                
                //ahha. ok...
                transform.SetParent(otherCollider.transform);
                
                //uh oh. can't remove body cos of the fixed joint...
                foreach(var joint in tempJoints)
                {
                    if(joint == null) //just shows as an error...
                        continue; 
                    if(joint.connectedBody == otherbody)
                    {
                        Destroy(joint);
                        joints.Remove(joint);
                    }
                    else
                    {
                        //we have to transfer them over...
                        TransferJointFromUsToTargetBody(joint, otherCollider, otherbody, stickyFoam);
                        Destroy(joint);
                        joints.Remove(joint);
                    }
                }

                Destroy(body);
                break;
                //i wonder if we'll have to do something clever here to rebuild the physics?
            }
        }

        ourCollider.isTrigger = false;
    }

    private void TransferJointFromUsToTargetBody(FixedJoint joint, Collider collider, Rigidbody owner, ProjectileStickyFoam stickyFoam)
    {
        var newJoint = owner.gameObject.AddComponent<FixedJoint>();
        newJoint.connectedBody = joint.connectedBody;
        newJoint.breakForce = joint.breakForce;
        newJoint.breakTorque = joint.breakTorque;
        newJoint.enableCollision = joint.enableCollision;
        newJoint.enablePreprocessing = joint.enablePreprocessing;
        newJoint.massScale = joint.massScale;
        newJoint.connectedMassScale = joint.connectedMassScale;
        newJoint.anchor = joint.anchor;
        newJoint.axis = joint.axis.normalized;
        stickyFoam.AddedJoint(collider, newJoint);
    }

    private void AddedJoint(Collider collider, FixedJoint joint)
    {
        if(otherHitColliders.Contains(collider) == false)
            otherHitColliders.Add(collider);
        
        joints.Add(joint);
    }

    public void Remove()
    {
        Destroy(gameObject);
    }
}
