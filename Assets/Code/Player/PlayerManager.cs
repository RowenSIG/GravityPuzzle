using System.Collections.Generic;
using UnityEngine;
using static Logging;

public class PlayerManager : MonoBehaviour
{
    private static PlayerManager instance;

    public static PlayerManager Instance => instance;

    private GameObject playerContainer = null;

    [SerializeField]
    private Player playerPrefab;
    [SerializeField]
    private PlayerConfiguration playerConfigPrefab;

    private List<Player> players = new List<Player>();

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

    public Player InstantiatePlayer()
    {
        if (playerContainer == null)
        {
            playerContainer = new GameObject();
            playerContainer.name = "PlayerContainer";
        }

        Log($"[PlayerManager] InstantiatePlayer");
        var player = Instantiate(playerPrefab, playerContainer.transform);
        player.Setup(playerConfigPrefab);
        player.gameObject.name = $"Player[{players.Count}]";

        var spawn = LevelManager.Instance.GetLevel().SpawnPoint;
        player.transform.position = spawn.transform.position;
        player.transform.rotation = spawn.transform.rotation;
        return player;
    }

    public void RemovePlayer(Player player)
    {
        if (player != null && players.Contains(player))
        {
            players.Remove(player);
            Destroy(player.gameObject);
        }
    }

    public int NumPlayers => players.Count;
}
