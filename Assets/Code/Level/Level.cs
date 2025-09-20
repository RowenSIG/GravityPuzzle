using UnityEngine;

public class Level : MonoBehaviour
{

    [SerializeField]
    private LevelSpawnPoint spawnPoint;
    public LevelSpawnPoint SpawnPoint => spawnPoint;
    
}
