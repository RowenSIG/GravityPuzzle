using System.Collections;
using UnityEngine;

public class BodyPhysicsJointConfig : MonoBehaviour
{
    private float timerPeriod = 1f;
    private float timer = 0f;
    private Rigidbody body;
    private Joint joint;
    
    private Vector3 lastTorque;
    private Vector3 lastForce;
    private bool broken = false;

    [System.Serializable]
    private class FixedJointConfig
    {
        public Rigidbody targetBody;
        public float breakForce = Mathf.Infinity;
        public float breakTorque = Mathf.Infinity;
        public bool enableCollision;
        public bool enablePreprocessing = true;
        public float massScale = 1f;
        public float connectedMassScale = 1f;
    }

    [SerializeField]
    private FixedJointConfig jointConfig;

    public Vector3 anchorOverridePosition;
    public Vector3 anchorOverrideAxis;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        joint = gameObject.AddComponent<FixedJoint>();

        var fixedJoint = joint as FixedJoint;
        if(fixedJoint != null)
        {
            joint.autoConfigureConnectedAnchor = true;
            fixedJoint.connectedBody = jointConfig.targetBody;

            fixedJoint.breakForce = jointConfig.breakForce;
            fixedJoint.breakTorque = jointConfig.breakTorque;
            fixedJoint.enableCollision = jointConfig.enableCollision;
            fixedJoint.enablePreprocessing = jointConfig.enablePreprocessing;
            fixedJoint.massScale = jointConfig.massScale;
            fixedJoint.connectedMassScale = jointConfig.connectedMassScale;
            fixedJoint.anchor = anchorOverridePosition;
            fixedJoint.axis = anchorOverrideAxis.normalized;

        }
    }

    private void FixedUpdate()
    {
        if(body == null || joint == null)
            return;

        if(broken)
            return;

        timer += Time.fixedDeltaTime;
        if(timer > timerPeriod)
        {
            timer = 0f;
        }
        else
        {
            return;
        }
        

        //report the force and torque:

        var bodyforce = body.GetAccumulatedForce();
        var bodyTorque = body.GetAccumulatedTorque();

        var jointforce = joint.currentForce;
        var breakforce = joint.breakForce;

        var jointTorque = joint.currentTorque;
        var breakTorque = joint.breakTorque;

        lastTorque = jointTorque;
        lastForce = jointforce;

       // Debug.Log($"[BodyPhysicsJointReporter] bodyForce[{bodyforce}] bodyTorque[{bodyTorque}] || jointForce[{jointforce}] breakForce[{breakforce}] jointTorque[{jointTorque}] breakTorque[{breakTorque}]");

    }

    private void OnJointBreak(float breakForce)
    {
        broken = true;
        Debug.Log($"[BodyPhysicsJointReporter] breakForce[{breakForce}] lastForce[{lastForce}] lastTorque[{lastTorque}]");
        
    }

    private void OnDrawGizmos()
    {
        var fixedJoint = joint as FixedJoint;
        if(fixedJoint != null)
        {
            var worldAnchor = transform.TransformPoint(fixedJoint.anchor);
            var worldAxis = transform.TransformDirection(fixedJoint.axis);

            Gizmos.DrawWireCube( worldAnchor, Vector3.one * 0.1f);       

            Gizmos.DrawLine( worldAnchor, worldAnchor + worldAxis);
            Gizmos.DrawWireCube( worldAnchor + worldAxis, Vector3.one * 0.02f);

        }

        Gizmos.color = Color.green;
        var worldAnchorOverride = transform.TransformPoint(anchorOverridePosition);
        var worldAxisOverride = transform.TransformDirection(anchorOverrideAxis);
        Gizmos.DrawWireCube(worldAnchorOverride, Vector3.one * 0.085f);

        Gizmos.DrawLine(worldAnchorOverride, worldAnchorOverride + worldAxisOverride);
        Gizmos.DrawWireCube(worldAnchorOverride + worldAxisOverride, Vector3.one * 0.025f);
    }
}
