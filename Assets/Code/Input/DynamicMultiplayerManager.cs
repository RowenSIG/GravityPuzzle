using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DynamicMultiplayerManager : MonoBehaviour
{
    private static DynamicMultiplayerManager instance;
    public static DynamicMultiplayerManager Instance => instance;

    public InputSystem_Actions inputControls;

    private Dictionary<InputDevice, Player> deviceToPlayer = new Dictionary<InputDevice, Player>();

    private List<Player> playersWithoutInputs = new List<Player>();

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
        DetectExistingGamepads();
    }

    void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    public void Initialise()
    {
        inputControls = new InputSystem_Actions();
        AssignKeyboardAndMouseToPlayerZero();
        DetectExistingGamepads();
    }
    
    void AssignKeyboardAndMouseToPlayerZero()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard != null && mouse != null)
        {
            AddNewPlayer( new InputDevice[] { keyboard, mouse } );
        }
    }


    void DetectExistingGamepads()
    {
        // foreach (var gamepad in Gamepad.all)
        // {
        //     if (deviceToPlayer.ContainsKey(gamepad) == false)
        //     {
        //         AddNewPlayer( new InputDevice[] { gamepad });
        //     }
        // }
    }


    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        switch (change)
        {
            case InputDeviceChange.Added:
                DetectExistingGamepads();
                break;
            case InputDeviceChange.Disconnected:
            case InputDeviceChange.Removed:
                RemovePlayerInput(device);
                break;
            case InputDeviceChange.Reconnected:
                ReassignPlayer(device);
                break;
        }
    }

    void AddNewPlayer(InputDevice[] devices)
    {
        var player = PlayerManager.Instance.InstantiatePlayer();
        int playerIndex = PlayerManager.Instance.NumPlayers;
        player.InitialiseInput(devices, playerIndex, inputControls);
        foreach (var device in devices)
        {
            deviceToPlayer[device] = player;
        }
    }

    void RemovePlayerInput(InputDevice device)
    {
        if (deviceToPlayer.TryGetValue(device, out var player))
        {
            deviceToPlayer.Remove(device);
            playersWithoutInputs.Add(player);
        }
    }

    void ReassignPlayer(InputDevice device)
    {
        if (deviceToPlayer.ContainsKey(device) == false)
        {
            if (playersWithoutInputs.Count > 0)
            {

                var player = playersWithoutInputs[0];
                playersWithoutInputs.Remove(player);
                player.InitialiseInput(new InputDevice[] { device }, player.PlayerIndex, inputControls);
                deviceToPlayer[device] = player;
            }
        }
    }
}
