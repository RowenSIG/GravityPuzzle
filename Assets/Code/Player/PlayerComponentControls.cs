using UnityEngine;
using static Logging;

public abstract class PlayerComponentControls : MonoBehaviour
{
    protected PlayerConfiguration config;
    protected Player player;
    public virtual void Setup(PlayerConfiguration config, Player player)
    {
        this.config = config;
        this.player = player;
    }

    protected Vector3 PlayerUp
    {
        get
        {
            return player.PlayerUp;
        }
    }
    protected float PlayerDT
    {
        get
        {
            return Time.deltaTime;
        }
    }
}

