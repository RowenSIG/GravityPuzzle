using UnityEngine;
using static Logging;

public class PlayerPhysicsControls : PlayerComponentControls
{
    [SerializeField]
    private Rigidbody body;


    public override void UpdateMoveInput(Vector2 moveInput, bool jumpInput)
    {
        var forwardForce = Mathf.Clamp(moveInput.y, 0f, 1f) * PlayerDT * config.forwardMoveSpeed;
        var backwardForce = Mathf.Clamp(moveInput.y, -1f, 0f) * PlayerDT * config.backwardMoveSpeed;

        var sidewaysForce = moveInput.x * PlayerDT * config.sidewaysMoveSpeed;

        var force = new Vector3(sidewaysForce, 0, forwardForce + backwardForce);
        body.AddRelativeForce(force, ForceMode.VelocityChange);

    }

    public override void UpdateFixedPhysics()
    {
        var gravity = -1f * Game.GRAVITY_ACCELERATION * PlayerUp;
        body.AddForce(gravity * body.mass, ForceMode.Acceleration);

        //drag
        var velocity = body.linearVelocity;
        //don't drag falling
        var fallingComponent = Vector3.Dot(PlayerUp, velocity) * PlayerUp.normalized;
        velocity -= fallingComponent;
        velocity *= 0.9f;
        velocity += fallingComponent;
        body.linearVelocity = velocity;
    }
}
