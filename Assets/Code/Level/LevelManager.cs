using UnityEngine;
using static Logging;

public class LevelManager : MonoBehaviour
{
    private static LevelManager instance;
    public static LevelManager Instance => instance;

    private Level level;

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


    public Level GetLevel()
    {
        if (level == null)
        {
            var levelsFound = GameObject.FindObjectsByType<Level>(FindObjectsSortMode.None);
            if (levelsFound.Length != 1)
            {
                LogError($"[LevelManager] Non '1' levels found... count[{levelsFound.Length}]");
            }
            level = levelsFound[0];
        }
        return level;
    }
}
