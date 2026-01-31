using GorillaLocomotion;
using UnityEngine;
using UnityEngine;
using Grate.Gestures;
using UnityEngine;

namespace Grate.Interaction;

public class GrateGrabbable : GrateInteractable
{
    private readonly Vector3 mirrorScale = new(-1, 1, 1);
    private Vector3 _localPos;
    private bool kinematicCache;

    public Vector3 LocalRotation = Vector3.zero;
    public float throwForceMultiplier = 1f;
    public bool throwOnDetach;
    private GorillaVelocityEstimator velEstimator;

    public Vector3 LocalPosition
    {
        get => _localPos;
        set
        {
            _localPos = value;
            MirroredLocalPosition = Vector3.Scale(value, mirrorScale);
        }
    }

    public Vector3 MirroredLocalPosition { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        var gt = GestureTracker.Instance;
        validSelectors = new[] { gt.leftPalmInteractor, gt.rightPalmInteractor };
        velEstimator = gameObject.AddComponent<GorillaVelocityEstimator>();
    }

    public override void OnSelect(GrateInteractor interactor)
    {
        if (GetComponent<Rigidbody>() is Rigidbody rb)
        {
            kinematicCache = rb.isKinematic;
            rb.isKinematic = true;
        }

        transform.SetParent(interactor.transform);
        if (interactor.IsLeft)
            transform.localPosition = LocalPosition;
        else
            transform.localPosition = MirroredLocalPosition;
        transform.localRotation = Quaternion.Euler(LocalRotation);
        base.OnSelect(interactor);
    }

    public override void OnDeselect(GrateInteractor interactor)
    {
        transform.SetParent(null);
        if (GetComponent<Rigidbody>() is Rigidbody rb)
        {
            if (throwOnDetach)
            {
                rb.isKinematic = false;
                rb.useGravity = true;

                // Apply the force to the rigidbody
                rb.velocity = GTPlayer.Instance.GetComponent<Rigidbody>().velocity +
                              velEstimator.linearVelocity * throwForceMultiplier;
                rb.velocity *= 1 / GTPlayer.Instance.scale;
                rb.angularVelocity = velEstimator.angularVelocity;
            }
            else
            {
                rb.isKinematic = kinematicCache;
            }
        }

        base.OnDeselect(interactor);
    }
}