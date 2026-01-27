using UnityEngine;

public class PlayerWeaponStickyFoam : PlayerWeapon
{
    [SerializeField]
    private ProjectileStickyFoam stickyFoamPrefab;

    [SerializeField]
    private Transform foamSpawnPoint;
    private Vector3 FoamSpawnOrigin => foamSpawnPoint.position;
    
    [SerializeField]
    private int foamsPerMagazine;
    private int foamsRemainingInMagazine = 0;

    [SerializeField]
    private float repeatTime;
    private float repeatTimer = 0f;

    [SerializeField]
    private float reloadTime;
    private float reloadTimer = 0f;

    private bool CanFire
    {
        get
        {
            if (reloadTimer > 0f)
                return false;
            if(repeatTimer > 0f)
                return false;
            return true;
        }
    }

    public override void Setup(Player player)
    {
        base.Setup(player);
        foamsRemainingInMagazine = foamsPerMagazine;
    }

    public override void UpdateWeapon(float deltaTime, bool leftFire, bool rightFire)
    {
        bool reloading = reloadTimer > 0f;
        reloadTimer -= deltaTime;

        if(reloading && reloadTimer <= 0f)
        {
            foamsRemainingInMagazine = foamsPerMagazine;
        }

        repeatTimer -= deltaTime;
        
        //we shall fire foam at our target...
        if(rightFire)
        {
            if(CanFire)
                Fire();
        }
    }

      private void Fire()
    {
        //where?...
        repeatTimer = repeatTime;

        foamsRemainingInMagazine -= 1;
        if(foamsRemainingInMagazine == 0)
        {
            reloadTimer = reloadTime;
        }

        
        var ray = player.PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var hit = Physics.Raycast(ray, out var hitinfo, 100f);

        if (hit)
        {
            var dir = hitinfo.point - FoamSpawnOrigin;
            var clone = Instantiate(stickyFoamPrefab);
            clone.transform.position = FoamSpawnOrigin;
            clone.transform.rotation = transform.rotation;
            clone.Shoot(dir.normalized, player.Collider);
        }

    }


}
