using System.Collections.Generic;
using System.Linq;
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

    private float ropeClimbTime = 0.8f;
    private float ropeClimbTimer = 0f;

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

        if (rightFire)
        {
            if (CanFire)
                Fire();
            else
                FireHeld();
        }

        if (rightFire == false)
        {
            FireReleased();
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
            FireProjectileAtLocation(hitinfo.point, ray.direction, hitinfo.normal);

            RemoveOldestRopeIfNecessary();
        }
    }

    private void FireReleased()
    {
        canFire = true;
        ropeClimbTimer = 0f;

        if (player.RopeSwinging)
        {
            player.ReleaseRope();
        }
    }

    private void FireHeld()
    {
        //we need to somehow swing off a rope...

        //1 - we must move towards our rope anchor
        //2 - our player becomes linked to the end of the rope

        if (player.RopeSwinging == false)
        {
            if (ropeClimbTimer < ropeClimbTime)
            {
                ropeClimbTimer += Time.deltaTime;
                return;
            }
        }

        if (player.RopeSwinging == false && ropeClimbTimer > ropeClimbTime)
        {
            //set to very low. it'll never time up
            ropeClimbTimer = Mathf.NegativeInfinity;

            var lastRope = ropes.Last();
            var lastLink = lastRope.LastLink();

            var ropeEndPos = lastLink.transform.position;
            player.transform.position = ropeEndPos;

            var joint = lastLink.AddComponent<CharacterJoint>();
            joint.connectedBody = player.Body;
            player.Body.mass = 1;
            player.StartedRopeSwinging(joint);
        }
    }

    private void FireProjectileAtLocation(Vector3 targetPoint, Vector3 direction, Vector3 gravityNormal)
    {
        var clone = Instantiate(projectileRopePrefab);
        clone.Shoot(targetPoint, direction, gravityNormal);
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


