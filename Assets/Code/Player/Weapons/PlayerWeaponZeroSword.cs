using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using static Logging;

public class PlayerWeaponZeroSword : PlayerWeapon
{
    [SerializeField]
    private float weaponRange;
    [SerializeField]
    private float arcAngle;
    [SerializeField]
    private float arcTime;
    [SerializeField]
    private Vector3 arcAxis;

    [SerializeField]
    private float strikeForce;

    [SerializeField]
    private float reloadTime = 3f;

    private float reloadTimer = 0f;
    private bool canFire = true;

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
            var arcUp = transform.TransformDirection(arcAxis);
            var capsuleAxis = Quaternion.AngleAxis((float)angleDelta, arcUp) * cameraForward;

            var radius = 0.1f;

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
            for (int i = 0; i < capsA.Count; i++)
            {
                var start = capsA[i];
                var end = capsB[i];
                Gizmos.DrawLine(start, end);
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
