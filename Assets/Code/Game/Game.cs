using UnityEngine;
using static Logging;

public class Game : MonoBehaviour
{
    private static Game instance;
    public static Game Instance => instance;

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

    void Start()
    {
        Log($"[Game] Start - initialise input");
        DynamicMultiplayerManager.Instance.Initialise();
    }
    
}
