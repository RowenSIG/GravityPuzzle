using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static Logging;

public class Player : MonoBehaviour
{
    [SerializeField]
    private PlayerCameraControls cameraControls;
    [SerializeField]
    private PlayerPhysicsControls physicsControls;
    [SerializeField]
    private PlayerVisualsControls visualsControls;
    [SerializeField]
    private PlayerWeaponControls weaponControls;
    [SerializeField]
    private PlayerInput input;

    [SerializeField]
    private PlayerGravRaycastControl gravControls;
    [SerializeField]
    private PlayerGrabControls grabControls;

    [SerializeField]
    private Camera playerCamera;
    public Camera PlayerCamera => playerCamera;

    [SerializeField]
    private Rigidbody body;
    public Rigidbody Body => body;

    private PlayerConfiguration config;

    private Vector3 playerUp = Vector3.up;
    public Vector3 PlayerUp => playerUp;

    private int playerIndex;
    public int PlayerIndex => playerIndex;

    public Action OnNewPlayerUp = () => { };

    public bool RopeSwinging { get; private set; }

    private List<PlayerComponentControls> allControls;
    public void Setup(PlayerConfiguration config)
    {
        this.config = config;

        var found = gameObject.GetComponentsInChildren<PlayerComponentControls>();
        allControls = new List<PlayerComponentControls>(found);

        foreach (var control in allControls)
        {
            control.Setup(config, this);
        }
    }

    public void InitialiseInput(InputDevice[] devices, int playerIndex, InputSystem_Actions inputControls)
    {
        this.playerIndex = playerIndex;
        input.Initialise(devices, playerIndex, inputControls);

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Vector2 move = input.MoveAction.ReadValue<Vector2>();
        Vector2 look = input.LookAction.ReadValue<Vector2>();
        bool jump = input.JumpAction.WasPerformedThisFrame();
        bool fire = input.FireAction.IsPressed();
        bool altFire = input.AltFireAction.IsPressed();

        bool previousWeapon = input.PrevWeaponAction.WasPressedThisFrame();
        bool nextWeapon = input.NextWeaponAction.WasPressedThisFrame();

        // Log($"[Player] Update move[{move}] look[{look}] jump[{jump}] fire[{fire}]");

        foreach (var control in allControls)
        {
            control.UpdateLookInput(look);
            control.UpdateMoveInput(move, jump);
            control.UpdateFireInput(fire, altFire);
            control.UpdatePrevNextInput(previousWeapon, nextWeapon,  input.ScrollWeaponAction.ReadValue<Vector2>().y );
        }
    }

    void FixedUpdate()
    {

        foreach (var control in allControls)
        {
            control.UpdateFixedPhysics();
        }
    }

    public void SetNewPlayerUp(Vector3 up)
    {
        canJump = true;
        if (Vector3.Dot(up, playerUp) > 0.999f)
            return;

        Log($"[PlayerComponentControls] SetNewPlayerUp [{up}]");
        playerUp = up;
        OnNewPlayerUp.Invoke();

    }

    private bool canJump = true;
    public bool CanJump()
    {
        //umm.
        if (RopeSwinging)
        {
            return true;
        }
        return canJump;
    }
    public void Jump()
    {
        canJump = false;
        ReleaseRope();
    }

    private Joint ropeSwingJoint;
    public void StartedRopeSwinging(Joint swingJoint)
    {
        ropeSwingJoint = swingJoint;
        RopeSwinging = true;
    }
    public void ReleaseRope()
    {
        canJump = false;
        Destroy(ropeSwingJoint);
        RopeSwinging = false;
        ropeSwingJoint = null;
    }
}
