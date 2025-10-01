using Unity.VisualScripting;
using UnityEngine;
using static Logging;

public class PlayerPhysicsControls : PlayerComponentControls
{
    private Rigidbody Body => player.Body;

    public override void UpdateMoveInput(Vector2 moveInput, bool jumpInput)
    {
        var forwardStrength = config.forwardMoveSpeed;
        var backwardStrength = config.backwardMoveSpeed;
        var sidewaysStrength = config.sidewaysMoveSpeed;

        if (player.RopeSwinging)
        {
            forwardStrength = config.forwardSwingSpeed;
            backwardStrength = config.backwardSwingSpeed;
            sidewaysStrength = config.sidewaysSwingSpeed;
        }

        var forwardForce = Mathf.Clamp(moveInput.y, 0f, 1f) * PlayerDT * forwardStrength;
        var backwardForce = Mathf.Clamp(moveInput.y, -1f, 0f) * PlayerDT * backwardStrength;

        var sidewaysForce = moveInput.x * PlayerDT * sidewaysStrength;

        var force = new Vector3(sidewaysForce, 0, forwardForce + backwardForce);
        Body.AddRelativeForce(force, ForceMode.VelocityChange);


        //jump?
        if (player.CanJump() && jumpInput)
        {
            var jumpUpwardsForce = Vector3.up * config.jumpForce;
            var jumpDirectionalForce = Vector3.zero;
            if (player.RopeSwinging)
            {
                jumpDirectionalForce = new Vector3(moveInput.x * config.swingJumpDirectionalForce, 0, moveInput.y * config.swingJumpDirectionalForce);
            }
            Body.AddRelativeForce(jumpUpwardsForce + jumpDirectionalForce, ForceMode.VelocityChange);
            player.Jump();
        }
    }

    public override void UpdateFixedPhysics()
    {
        var gravity = -1f * Game.GRAVITY_ACCELERATION * PlayerUp;
        Body.AddForce(gravity, ForceMode.Acceleration);

        //drag
        var velocity = Body.linearVelocity;
        //don't drag falling
        var fallingComponent = Vector3.Dot(PlayerUp, velocity) * PlayerUp.normalized;
        velocity -= fallingComponent;
        velocity *= config.linearVelocityDrag;
        velocity += fallingComponent;
        Body.linearVelocity = velocity;
    }
}
