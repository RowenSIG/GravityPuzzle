using UnityEngine;

public class PlayerWeaponControls : PlayerComponentControls
{
    [SerializeField]
    private PlayerWeaponRopeProjectile ropeProjectileWeapon;

    public override void Setup(PlayerConfiguration config, Player player)
    {
        base.Setup(config, player);

        ropeProjectileWeapon.Setup(player);
    }

    public void UpdateFireInput(bool leftInput, bool rightInput)
    {
        //bang

        ropeProjectileWeapon.UpdateWeapon(PlayerDT, leftInput, rightInput);

    }
}
