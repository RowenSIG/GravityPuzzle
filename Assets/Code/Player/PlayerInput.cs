using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class PlayerInput : MonoBehaviour
{

    private InputUser inputUser;
    public InputAction MoveAction { get; private set; }

    public InputAction LookAction { get; private set; }
    public InputAction JumpAction { get; private set; }
    public InputAction FireAction { get; private set; }
    public InputAction AltFireAction { get; private set; }
    public InputAction NextWeaponAction { get; private set; }
    public InputAction PrevWeaponAction { get; private set; }


    public void Initialise(InputDevice[] devices, int playerIndex, InputSystem_Actions inputControls)
    {
        inputUser = InputUser.CreateUserWithoutPairedDevices();

        foreach (var device in devices)
        {
            InputUser.PerformPairingWithDevice(device, inputUser);
        }

        InputActionMap actionMap = null;

        switch (playerIndex)
        {
            case 0:
                actionMap = inputControls.Player1Game;
                MoveAction = inputControls.Player1Game.Move;
                LookAction = inputControls.Player1Game.Look;
                JumpAction = inputControls.Player1Game.Jump;
                FireAction = inputControls.Player1Game.Attack;
                AltFireAction = inputControls.Player1Game.AltAttack;
                NextWeaponAction = inputControls.Player1Game.Next;
                PrevWeaponAction = inputControls.Player1Game.Previous;
                break;
            case 1:
                actionMap = inputControls.Player2Game;
                MoveAction = inputControls.Player2Game.Move;
                LookAction = inputControls.Player2Game.Look;
                JumpAction = inputControls.Player2Game.Jump;
                FireAction = inputControls.Player2Game.Attack;
                AltFireAction = inputControls.Player2Game.AltAttack;
                NextWeaponAction = inputControls.Player2Game.Next;
                PrevWeaponAction = inputControls.Player2Game.Previous;
                break;
                // Add more cases for additional players
        }

        actionMap.Enable();
        inputUser.AssociateActionsWithUser(actionMap);
    }
    
    

}
