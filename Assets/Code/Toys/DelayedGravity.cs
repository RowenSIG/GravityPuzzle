using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class DelayedGravity : MonoBehaviour
{
    public class GravityDelayAction
    {
        public Rigidbody body;
        public DirectionalGravity directionalGravity;
        public float mass;
        public bool active;
        public float delay;
        public bool restoreDamping;

        public bool cancelled = false;
    }

    private List<GravityDelayAction> actions = new();

    private void CancelLikeActions(GravityDelayAction action)
    {
        foreach(var existingAction in actions)
        {
            bool matched = false;
            if(existingAction.directionalGravity != null && existingAction.directionalGravity == action.directionalGravity)
            {
                matched = true;
            }
            else if(existingAction.body != null && existingAction.body == action.body)
            {
                matched = true;
            }

            if(matched)
            {
                existingAction.cancelled = true;
                action.mass = existingAction.mass;
                action.active = existingAction.active;
                action.restoreDamping = existingAction.restoreDamping;
            }
        }
    }
    
    public void RestoreGravityWithDelay(Rigidbody body, float mass, bool active, float delay, bool restoreDamping)
    {
        if(body.GetComponent<DirectionalGravity>() is var dirgrav && dirgrav != null)
        {
            RestoreGravityWithDelay(dirgrav, mass, active, delay, restoreDamping);
            return;    
        }

        var action = new GravityDelayAction()
        {
            body = body,
            mass = mass,
            active = active,
            delay = delay,
            restoreDamping = restoreDamping
        }   ;
        CancelLikeActions(action);

        RestoreDamping(action);
        actions.Add(action);
        StartCoroutine(CoSetGravityDelay(action));
    }
    public void RestoreGravityInstantly(Rigidbody body, float mass, bool active, bool restoreDamping)
    { 
        if(body.GetComponent<DirectionalGravity>() is var dirgrav && dirgrav != null)
        {
            RestoreGravityInstantly(dirgrav, mass, active, restoreDamping);
            return;    
        }

        var action = new GravityDelayAction()
        {
            body = body,
            mass = mass,
            active = active,
            restoreDamping = restoreDamping,
        };
        CancelLikeActions(action);
        RestoreBodyGravity(action);
    }

    public void RestoreGravityWithDelay(DirectionalGravity directionalGravity, float mass, bool active, float delay, bool restoreDamping)
    {
        var action = new GravityDelayAction()
        {
            directionalGravity = directionalGravity,
            mass = mass,
            active = active,
            delay = delay,
            restoreDamping = restoreDamping,
        };

        CancelLikeActions(action);

        RestoreDamping(action);
        actions.Add(action);
        StartCoroutine(CoSetGravityDelay(action));
    }
    public void RestoreGravityInstantly(DirectionalGravity directionalGravity, float mass, bool active, bool restoreDamping)
    {
        var action = new GravityDelayAction()
        {
            directionalGravity = directionalGravity,
            mass = mass,
            active = active,
            restoreDamping = restoreDamping,
        };

        CancelLikeActions(action);
        RestoreBodyGravity(action);
    }

    public void CancelGravityDelay(Rigidbody body)
    {
        if(body.GetComponent<DirectionalGravity>() is var dirgrav && dirgrav != null)
        {
            CancelGravityDelay(dirgrav);
            return;    
        }
        var action = new GravityDelayAction()
        {
            body = body,
        };
        CancelLikeActions(action);
    }

    public void CancelGravityDelay(DirectionalGravity directionalGravity)
    {
        var action = new GravityDelayAction()
        {
            directionalGravity = directionalGravity,
        };

        CancelLikeActions(action);
    }

    private IEnumerator CoSetGravityDelay(GravityDelayAction action)
    {
        yield return new WaitForSeconds(action.delay);

        if(action.cancelled == false)
        {
            RestoreBodyGravity(action);
            actions.Remove(action);
        }
    }
    private void RestoreBodyGravity(GravityDelayAction action)
    {
        var rigidBody = action.body;
        if(action.directionalGravity)
        {
            action.directionalGravity.SetGravityActive(action.active);
            rigidBody = action.directionalGravity.Body;
        }
        else if(rigidBody != null)
        {
            rigidBody.useGravity = action.active;
        }

        if(rigidBody != null)
        {
            rigidBody.mass = action.mass;
        }

        RestoreDamping(action);

    }

    private void RestoreDamping(GravityDelayAction action)
    {
        if(action.restoreDamping)
        {
            var rigidBody = action.body;
            if(action.directionalGravity)
            {
                rigidBody = action.directionalGravity.Body;
            }
            if(rigidBody != null)
            {
                rigidBody.linearDamping = 0.1f;
                rigidBody.angularDamping = 0.1f;
            }
        }
    }
}
