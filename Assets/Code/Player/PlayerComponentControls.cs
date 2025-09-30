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

    public virtual void UpdateLookInput(Vector2 input) { }
    public virtual void UpdateMoveInput(Vector2 moveInput, bool jumpInput) { }
    public virtual void UpdateFireInput(bool leftInput, bool rightInput) { }
    public virtual void UpdateFixedPhysics() { }
    public virtual void UpdatePrevNextInput(bool prev, bool next) { }

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

