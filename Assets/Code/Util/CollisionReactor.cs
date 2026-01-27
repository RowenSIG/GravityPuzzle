using System;
using UnityEngine;
using static Logging;

public class CollisionReactor : MonoBehaviour
{
    public Action<Collision> OnOnCollisionEnter = (p) => { };
    public Action<Collision> OnOnCollisionExit = (p) => { };
    public Action<Collision> OnOnCollisionStay = (p) => { };

    public Action<Collider> OnOnTriggerEnter = (p) => { };
    public Action<Collider> OnOnTriggerExit = (p) => { };
    public Action<Collider> OnOnTriggerStay = (p) => { };

    void OnCollisionEnter(Collision collision)
    {
        OnOnCollisionEnter.Invoke(collision);
    }
    void OnCollisionExit(Collision collision)
    {
        OnOnCollisionExit.Invoke(collision);
    }
    void OnCollisionStay(Collision collision)
    {
        OnOnCollisionStay.Invoke(collision);
    }

    void OnTriggerEnter(Collider other)
    {
        OnOnTriggerEnter.Invoke(other);   
    }
    void OnTriggerExit(Collider other)
    {
        OnOnTriggerExit.Invoke(other);   
    }
    void OnTriggerStay(Collider other)
    {
        OnOnTriggerStay.Invoke(other);   
    }

    public Action OnZeroSwordHit = () => {};
}
