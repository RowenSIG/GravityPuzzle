using UnityEngine;
using static Logging;

public class Game : MonoBehaviour
{
    public const float GRAVITY_ACCELERATION = 9.81f;

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
