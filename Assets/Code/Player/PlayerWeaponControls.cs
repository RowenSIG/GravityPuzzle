using UnityEngine;

public class PlayerWeaponControls : PlayerComponentControls
{
    [SerializeField]
    private PlayerWeaponRopeProjectile ropeProjectileWeapon;

    [SerializeField]
    private PlayerWeaponGravitationGauntlet gravitationGauntletWeapon;


    public override void Setup(PlayerConfiguration config, Player player)
    {
        base.Setup(config, player);

        ropeProjectileWeapon.Setup(player);
        gravitationGauntletWeapon.Setup(player);
    }

    public override void UpdateFireInput(bool leftInput, bool rightInput)
    {
        //bang

        // ropeProjectileWeapon.UpdateWeapon(PlayerDT, leftInput, rightInput);

        gravitationGauntletWeapon.UpdateWeapon(PlayerDT, leftInput, rightInput);

    }
}
