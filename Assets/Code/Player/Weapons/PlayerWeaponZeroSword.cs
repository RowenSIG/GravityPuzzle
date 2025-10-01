using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using static Logging;

public class PlayerWeaponZeroSword : PlayerWeapon
{
    public enum eSwipeDirection
    {
        INVALID = -1,

        RIGHT_TO_LEFT = 10,
        LEFT_TO_RIGHT = 20,
    }

    [SerializeField]
    private float weaponRange;
    [SerializeField]
    private float arcAngle;
    [SerializeField]
    private float arcTime;
    [SerializeField]
    private Vector3 arcAxis;
    [SerializeField]
    private float arcThickness;

    [SerializeField]
    private float strikeForce;

    [SerializeField]
    private float reloadTime = 3f;

    private float reloadTimer = 0f;
    private bool canFire = true;

    private eSwipeDirection nextSwipeDirection = eSwipeDirection.RIGHT_TO_LEFT;

    private bool CanFire
    {
        get
        {
            if (canFire == false)
                return false;

            if (reloadTimer <= 0f)
                return true;
            return false;
        }
    }

    public override void UpdateWeapon(float deltaTime, bool leftFire, bool rightFire)
    {
        reloadTimer -= deltaTime;

        if (CanFire && rightFire)
        {
            Fire();
        }

        if (rightFire == false)
        {
            canFire = true;
        }
    }

    private void Fire()
    {
        //where?...
        canFire = false;
        reloadTimer = reloadTime;

        StartCoroutine(CoSwingWeapon());
    }

    private RaycastHit[] capsuleCastHitArray = new RaycastHit[64];
    private HashSet<Collider> alreadyHitColliders = new HashSet<Collider>();

    private IEnumerator CoSwingWeapon()
    {
        // i'll just do a sort of fan of raycasts
        var startTime = Time.realtimeSinceStartupAsDouble;
        alreadyHitColliders.Clear();
        hitpoints.Clear();

        var rightNow = 0d;
        Vector3? lastCapsuleOriginB = null;

        capsA.Clear();
        capsB.Clear();

        while (rightNow - startTime < arcTime)
        {
            rightNow = Time.realtimeSinceStartupAsDouble;

            var timeProp = rightNow - startTime;
            var angleDelta = (timeProp / arcTime) * arcAngle;
            angleDelta -= arcAngle / 2f;

            var origin = player.PlayerCamera.transform.position;
            var cameraForward = player.PlayerCamera.transform.forward;
            var arcUp = Vector3.zero;

            switch (nextSwipeDirection)
            {
                case eSwipeDirection.LEFT_TO_RIGHT:
                    arcUp = transform.TransformDirection(arcAxis);
                    break;
                case eSwipeDirection.RIGHT_TO_LEFT:
                    angleDelta = -angleDelta; //actually it needs to be negative to  go right to left.
                    var invArcAxis = arcAxis;
                    invArcAxis.x = -invArcAxis.x;
                    arcUp = transform.TransformDirection(invArcAxis);
                    break;
            }

            var capsuleAxis = Quaternion.AngleAxis((float)angleDelta, arcUp) * cameraForward;

            var radius = arcThickness;

            var capsuleOriginA = origin + capsuleAxis.normalized * radius;
            var capsuleOriginB = origin + capsuleAxis.normalized * (weaponRange - radius);

            capsA.Add(capsuleOriginA);
            capsB.Add(capsuleOriginB);

            if (lastCapsuleOriginB.HasValue)
            {
                var sweepDirection = capsuleOriginB - lastCapsuleOriginB.Value;
                int hitCount = Physics.CapsuleCastNonAlloc(capsuleOriginA, capsuleOriginB, radius, sweepDirection.normalized, capsuleCastHitArray, sweepDirection.magnitude);

                for (int i = 0; i < hitCount; i++)
                {
                    var hit = capsuleCastHitArray[i];
                    hitpoints.Add(hit.point);

                    var coll = hit.collider;
                    var body = coll.attachedRigidbody;
                    if (body != null && player.Body == body)
                        continue;

                    if (alreadyHitColliders.Contains(coll))
                        continue;

                    alreadyHitColliders.Add(coll);
                    var grav = coll.gameObject.GetComponent<DirectionalGravity>();

                    if (grav != null)
                    {
                        var objectUp = grav.GravityDirection;
                        grav.AssignGravityDirection(Vector3.zero);
                        body = grav.GetComponent<Rigidbody>();

                        if (body != null)
                        {
                            if (objectUp.sqrMagnitude < Mathf.Epsilon)
                            {
                                //has no direction gravity - force it backwards!
                                var direction = (grav.transform.position - player.transform.position).normalized;
                                body.AddForce(direction * strikeForce, ForceMode.VelocityChange);
                            }
                            else
                            {
                                body.AddForce(objectUp * -1f * 0.2f, ForceMode.VelocityChange);
                            }
                        }
                    }
                }
            }


            lastCapsuleOriginB = capsuleOriginB;
            yield return new WaitForEndOfFrame();
        }

        alreadyHitColliders.Clear();

        switch (nextSwipeDirection)
        {
            default:
            case eSwipeDirection.LEFT_TO_RIGHT: nextSwipeDirection = eSwipeDirection.RIGHT_TO_LEFT; break;
            case eSwipeDirection.RIGHT_TO_LEFT : nextSwipeDirection = eSwipeDirection.LEFT_TO_RIGHT; break;
        }
    }

    private List<Vector3> capsA = new List<Vector3>();
    private List<Vector3> capsB = new List<Vector3>();
    private List<Vector3> hitpoints = new List<Vector3>();

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        if (capsA != null && capsB != null)
        {
            Vector3? prevEnd = null;
            for (int i = 0; i < capsA.Count; i++)
            {
                var start = capsA[i];
                var end = capsB[i];
                Gizmos.DrawLine(start, end);

                Gizmos.DrawLine(end, end + (end - start).normalized * arcThickness);

                if (prevEnd.HasValue)
                {
                    Gizmos.DrawLine(end, prevEnd.Value);
                }
                prevEnd = end;
            }
        }
        if (hitpoints != null)
        {
            foreach (var point in hitpoints)
            {
                Gizmos.DrawWireCube(point, Vector3.one * 0.1f);
            }
        }
    }
#endif
}
