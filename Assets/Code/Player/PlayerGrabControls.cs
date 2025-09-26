using UnityEngine;

public class PlayerGrabControls : PlayerComponentControls
{

    private float crosshairwidth = 20;
    private float crosshairheight = 20;

    public float grabDistance = 10f;
    public float forceStrength = 10f;

    private class GrabState
    {
        public Rigidbody body;
        public float dist;

    }
    private GrabState grab = null;

    public void UpdateFireInput(bool fire)
    {
        //bang

        if (grab == null)
        {
            if (fire)
            {
                var ray = player.PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

                var hit = Physics.Raycast(ray, out RaycastHit rayhitinfo, 10f);
                if (hit)
                {
                    var body = rayhitinfo.collider.attachedRigidbody;

                    grab = new GrabState() { body = body, dist = rayhitinfo.distance };
                }
            }
        }
        else
        {
            if (fire == false)
            {
                grab = null;
            }

           
        }

    }

    public void UpdateFixedPhysics()
    {
        if (grab != null)
        {
            if (grab.body == null)
                return;

            Vector3 newTargetPos;

            var ray = player.PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // var hit = Physics.Raycast(ray, out RaycastHit rayhitinfo, grabDistance);
            // if (hit)
            // {
            //     newTargetPos = rayhitinfo.point;
            // }
            // else
            // {
                newTargetPos = ray.origin + ray.direction * grabDistance;
            // }

            //now apply some forces...

            var offset = newTargetPos - grab.body.position;
            grab.body.AddForce( offset * forceStrength, ForceMode.VelocityChange );

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
