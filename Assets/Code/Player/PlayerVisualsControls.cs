using UnityEngine;
using static Logging;

public class PlayerVisualsControls : PlayerComponentControls
{
    [SerializeField]
    private Rigidbody rigidBody;

    [SerializeField]
    private Transform camLookTransform;

    public override void Setup(PlayerConfiguration config, Player player)
    {
        base.Setup(config, player);
        player.OnNewPlayerUp += OnNewPlayerUp;
    }
    protected void OnDestroy()
    {
        player.OnNewPlayerUp -= OnNewPlayerUp;
    }

    public override void UpdateLookInput(Vector2 input)
    {
        float x = input.x;
        //jitter!?
        if (Mathf.Abs(x) < 1)
        {
            x = 0;
        }

        var up = PlayerUp;
        var rotX = Quaternion.AngleAxis(x * config.horizontalTurnSpeed, up);
        var look = transform.rotation;
        look = rotX * look;
        transform.rotation = look;

        EnsureUpwardsVector();
        // Log($"[PlayerVisualsControls] rotate [{input.x}]");
    }

    private void EnsureUpwardsVector()
    {
        var up = PlayerUp;
        //compute our look along the plane our up is normal to:

        //we know our 'up'. to determine our look and right we need... something.
        if (shiftTimer > 0)
        {
            up = Vector3.Lerp(shiftFromUp, PlayerUp, 1f - (shiftTimer / SHIFT_TIME));
            shiftTimer -= Time.deltaTime;


            Vector3 projectedForwardX = Vector3.ProjectOnPlane(transform.forward + transform.up * 2f, up).normalized;

            transform.rotation = Quaternion.LookRotation(projectedForwardX, up);
            rigidBody.rotation = transform.rotation;
            return;
        }

        Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, up).normalized;

        transform.rotation = Quaternion.LookRotation(projectedForward, up);
        rigidBody.rotation = transform.rotation;


    }

    private Vector3 shiftFromUp;
    private float shiftTimer = 0f;
    private const float SHIFT_TIME = 0.3f;

    private void OnNewPlayerUp()
    {
        if (Vector3.Dot(PlayerUp, transform.up) > 0.99f)
            return;


        if (Vector3.Dot(PlayerUp, transform.up) < 0.5f)
        {
            shiftFromUp = transform.up;
            shiftTimer = SHIFT_TIME;
        }
        else
        {
            //instant shift... better?

            Vector3 projectedForward = Vector3.ProjectOnPlane(camLookTransform.forward + camLookTransform.up * 2f, PlayerUp).normalized;
            transform.rotation = Quaternion.LookRotation(projectedForward, PlayerUp);
            rigidBody.rotation = transform.rotation;
        }
    }

}
