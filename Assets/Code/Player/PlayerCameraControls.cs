using UnityEngine;

public class PlayerCameraControls : PlayerComponentControls
{
    public void UpdateLookInput(Vector2 input)
    {
        var right = Vector3.right;
        var rotY = Quaternion.AngleAxis(-input.y * config.verticalLookSpeed, right);
        var look = transform.localRotation;
        look = rotY * look;
        transform.localRotation = look;

        //but it could have gone wonky like. :()
    }
}
