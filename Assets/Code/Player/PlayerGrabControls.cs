using UnityEngine;

public class PlayerGrabControls : PlayerComponentControls
{
    private DelayedGravity delayedGravity;

    private float crosshairwidth = 20;
    private float crosshairheight = 20;

    public float grabDistance = 10f;
    public float forceStrength = 10f;
    public float torqueStrength = 1f;

    public float bodyMoveSpeedThresholdForDelayedGravity = 1f;
    public float bodyDelayedGravityDelay = 3f;

    private class GrabState
    {
        public Rigidbody body;
        public float dist;

        public float bodyMass;
        public bool bodyUseGravity;
    }
    private GrabState grab = null;

    public override void Setup(PlayerConfiguration config, Player player)
    {
        base.Setup(config, player);
        delayedGravity = GetComponent<DelayedGravity>();
    }

    public override void UpdateFireInput(bool leftFire, bool rightFire)
    {
        //bang

        if (grab == null)
        {
            if (leftFire)
            {
                var ray = player.PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

                var hit = Physics.Raycast(ray, out RaycastHit rayhitinfo, 10f);
                if (hit)
                {
                    var body = rayhitinfo.collider.attachedRigidbody;
                    

                    grab = new GrabState() { body = body, dist = rayhitinfo.distance };

                    if(body != null)
                    {
                        grab.bodyMass = body.mass;

                        body.linearDamping = 10f;
                        body.angularDamping = 10f;
                        body.mass = 1f;

                        if(body.GetComponent<DirectionalGravity>() is var dirgrav && dirgrav != null)
                        {
                            delayedGravity.CancelGravityDelay(grab.body);

                            grab.bodyUseGravity = dirgrav.GravityActive;
                            dirgrav.SetGravityActive(false);
                        }
                        else
                        {
                            delayedGravity.CancelGravityDelay(grab.body);

                            grab.bodyUseGravity = body.useGravity;
                            body.useGravity = false;
                        }

                    }
                }
            }
        }
        else
        {
            if (leftFire == false)
            {
                //release our object:
                ReleaseGrabbedObject();
                grab = null;
            }
        }
    }

    private void ReleaseGrabbedObject()
    {
        if (grab != null && grab.body != null)
        {
            if(grab.body.linearVelocity.magnitude < bodyMoveSpeedThresholdForDelayedGravity)
            {
                delayedGravity.RestoreGravityWithDelay(grab.body, mass: grab.bodyMass, active: grab.bodyUseGravity, delay: bodyDelayedGravityDelay, restoreDamping: true);
            }
            else
            {
                delayedGravity.RestoreGravityInstantly(grab.body, mass: grab.bodyMass, active: grab.bodyUseGravity, restoreDamping: true);
            }
        }
    }

    public override void UpdateFixedPhysics()
    {
        if (grab != null)
        {
            if (grab.body == null)
                return;

            Vector3 newTargetPos;

            var ray = player.PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            newTargetPos = ray.origin + ray.direction * grabDistance;

            var offset = newTargetPos - grab.body.position;
            grab.body.AddForce( offset * forceStrength, ForceMode.Impulse );

            var desiredOrientation = player.PlayerCamera.transform.rotation;
            var bodyOrientation = grab.body.transform.rotation;
            var rotationDelta = desiredOrientation * Quaternion.Inverse(bodyOrientation) ;

            rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);

            if (angle > 180f)
                angle -= 360f;

            Vector3 torque = axis.normalized * angle * torqueStrength * Mathf.Deg2Rad;
            grab.body.AddTorque(torque, ForceMode.Impulse);
        }
    }

    void OnGUI()
    {
        //crosshair!
        float halfw = crosshairwidth / 2f;
        float halfh = crosshairheight / 2f;
        GUI.Box(new Rect(Screen.width / 2f - halfw, Screen.height / 2f - halfh, crosshairwidth, crosshairheight), "");
    }
}
