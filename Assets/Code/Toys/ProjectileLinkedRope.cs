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

    public void Shoot(Vector3 targetPos, Vector3 direction)
    {
        //instant transmission, for now
        transform.position = targetPos;
        transform.up = direction;

//        StartCoroutine(CoCreateLinks());
        hardPoint.isKinematic = true;
        CreateLinks();

    }

    IEnumerator CoCreateLinks()
    {
        yield return new WaitForSeconds(1f);

        CreateLinks();
    }
}

