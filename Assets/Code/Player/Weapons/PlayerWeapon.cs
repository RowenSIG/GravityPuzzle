using UnityEngine;

public abstract class PlayerWeapon : MonoBehaviour
{
    protected Player player;
    public virtual void Setup(Player player)
    {
        this.player = player;
    }

    public virtual void UpdateWeapon(float deltaTime, bool leftFire, bool rightFire)
    {

    }
}

