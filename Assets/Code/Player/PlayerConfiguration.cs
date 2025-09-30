using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfiguration", menuName = "Scriptable Objects/PlayerConfiguration")]
public class PlayerConfiguration : ScriptableObject
{
    public float horizontalTurnSpeed;
    public float verticalLookSpeed;
    public float forwardMoveSpeed;
    public float backwardMoveSpeed;
    public float sidewaysMoveSpeed;
    public float forwardSwingSpeed;
    public float backwardSwingSpeed;
    public float sidewaysSwingSpeed;
    public float jumpForce;
    public float swingJumpDirectionalForce;
    public float linearVelocityDrag;
}
