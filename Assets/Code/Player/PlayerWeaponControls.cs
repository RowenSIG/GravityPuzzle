using UnityEngine;

public class PlayerWeaponControls : PlayerComponentControls
{
    [SerializeField]
    private PlayerWeaponRopeProjectile ropeProjectileWeapon;

    [SerializeField]
    private PlayerWeaponGravitationGauntlet gravitationGauntletWeapon;

    [SerializeField]
    private PlayerWeaponZeroSword zeroSwordWeapon;

    private PlayerWeapon currentWeapon;

    public override void Setup(PlayerConfiguration config, Player player)
    {
        base.Setup(config, player);

        ropeProjectileWeapon.Setup(player);
        gravitationGauntletWeapon.Setup(player);
        zeroSwordWeapon.Setup(player);

        SetCurrentWeapon(gravitationGauntletWeapon);
    }

    private void SetCurrentWeapon(PlayerWeapon weapon)
    {
        ropeProjectileWeapon.gameObject.EnsureActive(false);
        gravitationGauntletWeapon.gameObject.EnsureActive(false);
        zeroSwordWeapon.gameObject.EnsureActive(false);

        currentWeapon = weapon;
        currentWeapon.gameObject.EnsureActive(true);
    }

    public override void UpdateFireInput(bool leftInput, bool rightInput)
    {
        //bang
        currentWeapon.UpdateWeapon(PlayerDT, leftInput, rightInput);
    }

    public override void UpdatePrevNextInput(bool prev, bool next, float scrollInput)
    {
        if (scrollInput < 0)
            prev |= true;
        if (scrollInput > 0)
            next |= true;    
        
        if (prev)
            {
                switch (currentWeapon)
                {
                    default:
                    case PlayerWeaponZeroSword: SetCurrentWeapon(gravitationGauntletWeapon); break;
                    case PlayerWeaponGravitationGauntlet: SetCurrentWeapon(ropeProjectileWeapon); break;
                    case PlayerWeaponRopeProjectile: SetCurrentWeapon(zeroSwordWeapon); break;
                }
            }
            else if (next)
            {
                switch (currentWeapon)
                {
                    default:
                    case PlayerWeaponZeroSword: SetCurrentWeapon(ropeProjectileWeapon); break;
                    case PlayerWeaponGravitationGauntlet: SetCurrentWeapon(zeroSwordWeapon); break;
                    case PlayerWeaponRopeProjectile: SetCurrentWeapon(gravitationGauntletWeapon); break;
                }
            }
    }

}
