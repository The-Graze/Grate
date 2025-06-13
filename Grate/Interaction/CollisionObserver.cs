using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CollisionObserver : MonoBehaviour
{
    public LayerMask layerMask = ~0;
    public Action<GameObject, Collision> OnCollisionEntered, OnCollisionStayed, OnCollisionExited;
    public Action<GameObject, Collider> OnTriggerEntered, OnTriggerStayed, OnTriggerExited;

    private void OnCollisionEnter(Collision collision)
    {
        if (layerMask == (layerMask | (1 << collision.gameObject.layer)))
            OnCollisionEntered?.Invoke(gameObject, collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (layerMask == (layerMask | (1 << collision.gameObject.layer)))
            OnCollisionExited?.Invoke(gameObject, collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (layerMask == (layerMask | (1 << collision.gameObject.layer)))
            OnCollisionStayed?.Invoke(gameObject, collision);
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (layerMask == (layerMask | (1 << collider.gameObject.layer))) OnTriggerEntered?.Invoke(gameObject, collider);
    }

    private void OnTriggerExit(Collider collider)
    {
        if (layerMask == (layerMask | (1 << collider.gameObject.layer))) OnTriggerExited?.Invoke(gameObject, collider);
    }

    private void OnTriggerStay(Collider collider)
    {
        if (layerMask == (layerMask | (1 << collider.gameObject.layer))) OnTriggerStayed?.Invoke(gameObject, collider);
    }
}