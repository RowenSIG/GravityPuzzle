using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponRopeProjectile : PlayerWeapon
{
    [SerializeField]
    private ProjectileLinkedRope projectileRopePrefab;

    [SerializeField]
    private float reloadTime = 3f;

    [SerializeField]
    private int maxNumRopes;

    private float reloadTimer = 0f;
    private bool canFire = true;

    private List<ProjectileLinkedRope> ropes = new List<ProjectileLinkedRope>();

    private bool CanFire
    {
        get
        {
            if (canFire == false)
                return false;

            if (reloadTimer <= 0f)
                return true;
            return false;
        }
    }

    public override void UpdateWeapon(float deltaTime, bool leftFire, bool rightFire)
    {
        reloadTimer -= deltaTime;

        if (CanFire && rightFire)
        {
            Fire();
        }

        if (rightFire == false)
        {
            canFire = true;
        }
    }

    private void Fire()
    {
        //where?...
        canFire = false;
        reloadTimer = reloadTime;

        var ray = player.PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var hit = Physics.Raycast(ray, out var hitinfo, 100f);

        if (hit)
        {
            //we're going to just plonk a rope there...
            FireProjectileAtLocation(hitinfo.point, ray.direction);

            RemoveOldestRopeIfNecessary();
        }
    }

    private void FireProjectileAtLocation(Vector3 targetPoint, Vector3 direction)
    {
        var clone = Instantiate(projectileRopePrefab);
        clone.Shoot(targetPoint, direction);
        ropes.Add(clone);
    }

    private void RemoveOldestRopeIfNecessary()
    {
        if (ropes.Count > maxNumRopes)
        {
            var rope = ropes[0];
            ropes.RemoveAt(0);
            GameObject.Destroy(rope.gameObject);
        }
    }

}


