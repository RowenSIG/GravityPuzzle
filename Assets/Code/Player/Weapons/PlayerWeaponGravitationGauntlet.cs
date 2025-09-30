using UnityEngine;

public class PlayerWeaponGravitationGauntlet : PlayerWeapon
{
    private enum eState
    {
        INVALID = 0,

        NO_GRAB = 10,

        MISSED_GRAB = 20,

        GRAB = 30,
    }

    [SerializeField]
    private float grabRange;

    private eState state;

    private Rigidbody grabBody;
    private Vector3 grabHitPos;
    private float grabHitDist;
    private Vector3 currentGrabPos;

    public override void Setup(Player player)
    {
        base.Setup(player);
        state = eState.NO_GRAB;
    }

    public override void UpdateWeapon(float deltaTime, bool leftFire, bool rightFire)
    {
        switch (state)
        {
            case eState.NO_GRAB:
                {
                    if (rightFire)
                    {
                        TryGrab();
                    }
                }
                break;

            case eState.MISSED_GRAB:
                if (rightFire == false)
                {
                    state = eState.NO_GRAB;
                    ClearGrab();
                }
                break;

            case eState.GRAB:
                if (rightFire == false)
                {
                    ReleaseGrab();
                    ClearGrab();
                }
                else
                {
                    MaintainGrab();
                }
                break;
        }
    }

    private void TryGrab()
    {
        state = eState.MISSED_GRAB;
        var ray = player.PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        var hit = Physics.Raycast(ray, out RaycastHit rayhitinfo, grabRange);
        if (hit)
        {
            var body = rayhitinfo.collider.attachedRigidbody;
            if (body != null)
            {
                grabBody = body;
                grabHitPos = rayhitinfo.point;
                grabHitDist = Vector3.Distance(rayhitinfo.point, transform.position);

                state = eState.GRAB;
            }
        }
    }
    private void MaintainGrab()
    {
        currentGrabPos = transform.position + player.PlayerCamera.transform.forward * grabHitDist;
    }
    private void ReleaseGrab()
    {
        //we're gonna resolve our direction onto a principle axis.
        var directionalGravity = grabBody.gameObject.GetComponent<DirectionalGravity>();

        var directionDelta = currentGrabPos - grabHitPos;

        float absx = Mathf.Abs(directionDelta.x);
        float absy = Mathf.Abs(directionDelta.y);
        float absz = Mathf.Abs(directionDelta.z);

        if (absx >= absy && absx >= absz)
        {
            directionDelta = Mathf.Sign(directionDelta.x) * Vector3.right;
        }
        else if (absy >= absx && absy >= absz)
        {
            directionDelta = Mathf.Sign(directionDelta.y) * Vector3.up;
        }
        else if (absz >= absx && absz >= absy)
        {
            directionDelta = Mathf.Sign(directionDelta.z) * Vector3.forward;
        }

        directionalGravity.AssignGravityDirection(directionDelta);
        state = eState.NO_GRAB;
    }

    private void ClearGrab()
    {
        grabBody = null;
        grabHitPos = Vector3.zero;
        grabHitDist = 0;
        currentGrabPos = Vector3.zero;
    }


    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(grabHitPos, 0.1f);
        Gizmos.DrawWireSphere(currentGrabPos, 0.1f);
        Gizmos.DrawLine(grabHitPos, currentGrabPos);
    }
}
