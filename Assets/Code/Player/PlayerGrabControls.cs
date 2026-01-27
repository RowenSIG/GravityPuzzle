using Unity.Mathematics;
using UnityEngine;

public class PlayerGrabControls : PlayerComponentControls
{

    private float crosshairwidth = 20;
    private float crosshairheight = 20;

    public float grabDistance = 10f;
    public float forceStrength = 10f;
    public float torqueStrength = 1f;

    private class GrabState
    {
        public Rigidbody body;
        public float dist;

        public float bodyMass;
        public bool bodyUseGravity;
    }
    private GrabState grab = null;

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
                        grab.bodyUseGravity = body.useGravity;

                        body.linearDamping = 10f;
                        body.angularDamping = 10f;
                        body.useGravity = false;
                        body.mass = 1f;

                    }
                }
            }
        }
        else
        {
            if (leftFire == false)
            {
                if (grab != null && grab.body != null)
                {
                    grab.body.linearDamping = 0.1f;
                    grab.body.angularDamping = 0.1f;
                    
                    grab.body.mass = grab.bodyMass;
                    grab.body.useGravity = grab.bodyUseGravity;
                }
                grab = null;
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
