using System;
using UnityEngine;
using static Logging;

public class CollisionReactor : MonoBehaviour
{
    public Action<Collision> OnOnCollisionEnter = (p) => { };
    public Action<Collision> OnOnCollisionExit = (p) => { };
    public Action<Collision> OnOnCollisionStay = (p) => { };

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
}
