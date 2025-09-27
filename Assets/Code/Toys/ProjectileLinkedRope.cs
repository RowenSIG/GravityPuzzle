using System.Collections;
using UnityEngine;

public class ProjectileLinkedRope : LinkedRope
{
    //we're going to 'shoot' our rope to a place and then it creates links:

    [SerializeField]
    private Rigidbody hardPoint;



    protected override void Awake()
    {
        //don't do base awake.
    }

    public void Shoot(Vector3 targetPos, Vector3 direction, Vector3 hitGravity)
    {
        //instant transmission, for now
        transform.position = targetPos;
        transform.up = direction;

        hardPoint.isKinematic = true;
        StartCoroutine( CoCreateLinks(hitGravity) );

    }

    IEnumerator CoCreateLinks(Vector3 gravityVector )
    {
        CreateLinks();

        yield return new WaitForEndOfFrame();

        foreach (var link in links)
        {
            var grav = link.AddComponent<DirectionalGravity>();
            grav.AssignGravityDirection(gravityVector);
        }
    }
}

