using UnityEngine;
using UnityEngine;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.Tools;
using HarmonyLib;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Valve.VR;

namespace Grate.Gestures;

public class GestureTracker : MonoBehaviour
{
    public const string localRigPath =
        "Player Objects/Local VRRig/Local Gorilla Player";

    public const string palmPath =
        "/GorillaPlayerNetworkedRigAnchor/rig/body/shoulder.{0}/upper_arm.{0}/forearm.{0}/hand.{0}/palm.01.{0}";

    public const string pointerFingerPath =
        palmPath + "/f_index.01.{0}/f_index.02.{0}/f_index.03.{0}";

    public const string thumbPath =
        palmPath + "/thumb.01.{0}/thumb.02.{0}/thumb.03.{0}";

    public static GestureTracker Instance;

    private readonly float illProximityThreshold = .1f;
    private readonly Queue<int> meatBeatCollisions = new();

    public float camOffset = -45f;

    public GameObject
        chest,
        leftPointerObj,
        rightPointerObj,
        leftHand,
        rightHand;

    public List<InputTracker?> inputs;
    public bool isIlluminatiing, isChargingKamehameha;
    private float lastBeat;

    public InputDevice leftController, rightController;

    public InputTracker<float>?
        leftGrip;

    public BodyVectors leftHandVectors, rightHandVectors, headVectors;

    public GrateInteractor
        leftPalmInteractor,
        rightPalmInteractor,
        leftPointerInteractor,
        rightPointerInteractor;

    public Transform leftPointerTransform, rightPointerTransform, leftThumbTransform, rightThumbTransform;

    public InputTracker<bool>?
        leftPrimary;

    public InputTracker<bool>?
        leftSecondary;

    public InputTracker<bool>?
        leftStick;

    public InputTracker<Vector2>?
        leftStickAxis;

    public InputTracker<float>?
        leftTrigger;

    public Action<Vector3> OnGlide;
    public Action OnIlluminati, OnKamehameha;


    // Gesture Actions
    public Action OnMeatBeat;

    public InputTracker<float>?
        rightGrip;

    public InputTracker<bool>?
        rightPrimary;

    public InputTracker<bool>?
        rightSecondary;

    public InputTracker<bool>?
        rightStick;

    public InputTracker<Vector2>?
        rightStickAxis;

    public InputTracker<float>?
        rightTrigger;

    private void Awake()
    {
        Logging.Debug("Awake");
        Instance = this;
        leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        var poller = Traverse.Create(ControllerInputPoller.instance);
        var pollerExt = Traverse.Create(new ControllerInputPollerExt());

        leftGrip = new InputTracker<float>(poller.Field("leftControllerGripFloat"), XRNode.LeftHand);
        rightGrip = new InputTracker<float>(poller.Field("rightControllerGripFloat"), XRNode.RightHand);

        leftTrigger = new InputTracker<float>(poller.Field("leftControllerIndexFloat"), XRNode.LeftHand);
        rightTrigger = new InputTracker<float>(poller.Field("rightControllerIndexFloat"), XRNode.RightHand);

        leftPrimary = new InputTracker<bool>(poller.Field("leftControllerPrimaryButton"), XRNode.LeftHand);
        rightPrimary = new InputTracker<bool>(poller.Field("rightControllerPrimaryButton"), XRNode.RightHand);

        leftSecondary = new InputTracker<bool>(poller.Field("leftControllerSecondaryButton"), XRNode.LeftHand);
        rightSecondary = new InputTracker<bool>(poller.Field("rightControllerSecondaryButton"), XRNode.RightHand);

        leftStick = new InputTracker<bool>(pollerExt.Field("leftControllerStickButton"), XRNode.LeftHand);
        rightStick = new InputTracker<bool>(pollerExt.Field("rightControllerStickButton"), XRNode.RightHand);

        leftStickAxis = new InputTracker<Vector2>(pollerExt.Field("leftControllerStickAxis"), XRNode.LeftHand);
        rightStickAxis = new InputTracker<Vector2>(pollerExt.Field("rightControllerStickAxis"), XRNode.RightHand);

        inputs = new List<InputTracker?>
        {
            leftGrip, rightGrip,
            leftTrigger, rightTrigger,
            leftPrimary, rightPrimary,
            leftSecondary, rightSecondary,
            leftStick, rightStick,
            leftStickAxis, rightStickAxis
        };
        BuildColliders();
        var observer = chest.AddComponent<CollisionObserver>();
        observer.OnTriggerEntered += OnChestBeat;
    }

    private void Update()
    {
        ControllerInputPollerExt.Instance.Update();
        UpdateValues();
        TrackBodyVectors();
        TrackGlideGesture();
        isIlluminatiing = TrackIlluminatiGesture();
        isChargingKamehameha = TrackKamehamehaGesture();
    }

    private void FixedUpdate()
    {
        // If it's been more than one second since you last beat your chest, reset
        if (Time.time - lastBeat > 1f)
            meatBeatCollisions.Clear();
    }

    public void OnDestroy()
    {
        Logging.Debug("Gesture Tracker Destroy");
        leftHand?.Obliterate();
        rightHand?.Obliterate();
        leftPointerObj?.Obliterate();
        rightPointerObj?.Obliterate();
        Instance = null;
        OnMeatBeat = null;
    }

    public void UpdateValues()
    {
        if (NetworkSystem.Instance.InRoom)
        {
            if (NetworkSystem.Instance.GameModeString.Contains("MODDED_"))
                foreach (var input in inputs)
                    input.UpdateValues();
            else
                NetworkSystem.Instance.ReturnToSinglePlayer();
        }
    }

    private void TrackBodyVectors()
    {
        var left = leftHand.transform;
        leftHandVectors = new BodyVectors
        {
            pointerDirection = left.forward,
            palmNormal = left.right,
            thumbDirection = left.up
        };
        var right = rightHand.transform;
        rightHandVectors = new BodyVectors
        {
            pointerDirection = right.forward,
            palmNormal = right.right * -1,
            thumbDirection = right.up
        };

        var head = GTPlayer.Instance.headCollider.transform;
        headVectors = new BodyVectors
        {
            pointerDirection = head.forward,
            palmNormal = head.right,
            thumbDirection = head.up
        };
    }

    private bool TrackIlluminatiGesture()
    {
        var scale = GTPlayer.Instance.scale;
        // Check if thumb and pointer are touching
        if (Vector3.Distance(
                leftPointerTransform.position, rightPointerTransform.position
            ) > illProximityThreshold * scale) return false;
        if (Vector3.Distance(
                leftThumbTransform.position, rightThumbTransform.position
            ) > illProximityThreshold * scale) return false;

        if (PalmsFacingSameWay())
        {
            OnIlluminati?.Invoke();
            return true;
        }

        return false;
    }

    private bool TrackKamehamehaGesture()
    {
        var scale = GTPlayer.Instance.scale;
        // Check if palms are too far away. If so, leave.
        if (
            Vector3.Distance(
                leftPalmInteractor.transform.position,
                rightPalmInteractor.transform.position
            ) > .25f * scale)
            return false;

        if (PalmsFacingEachOther() && FingersFacingAway())
        {
            OnKamehameha?.Invoke();
            return true;
        }

        return false;
    }

    private void TrackGlideGesture()
    {
        if (FingersFacingAway() && PalmsFacingSameWay())
        {
            // Check that the glide direction is toward where the player is facing
            var direction = (leftHandVectors.thumbDirection + rightHandVectors.thumbDirection) / 2;
            if (Vector3.Dot(direction, headVectors.pointerDirection) > 0f)
                OnGlide?.Invoke(direction);
        }
    }

    public bool PalmsFacingEachOther()
    {
        var relativePosition = leftHand.transform.InverseTransformPoint(rightHand.transform.position);
        if (relativePosition.x < 0f) return false;
        return Vector3.Dot(leftHandVectors.palmNormal, rightHandVectors.palmNormal) < -.5f;
    }

    public bool PalmsFacingSameWay()
    {
        return Vector3.Dot(leftHandVectors.palmNormal, rightHandVectors.palmNormal) > .5f;
    }

    public bool FingersFacingAway()
    {
        var relativePosition = leftHand.transform.InverseTransformPoint(rightHand.transform.position);
        if (relativePosition.z > 0f) return false;
        return Vector3.Dot(leftHandVectors.pointerDirection, rightHandVectors.pointerDirection) < -.5f;
    }

    private void OnChestBeat(GameObject obj, Collider collider)
    {
        if (!XRSettings.isDeviceActive)
            return;

        if (collider.gameObject != leftHand &&
            collider.gameObject != rightHand) return;

        lastBeat = Time.time;

        if (meatBeatCollisions.Count > 3)
            meatBeatCollisions.Dequeue();
        if (collider.gameObject == leftHand)
            meatBeatCollisions.Enqueue(0);
        else if (collider.gameObject == rightHand)
            meatBeatCollisions.Enqueue(1);
        if (meatBeatCollisions.Count < 4) return;
        int current, last = -1;
        for (var i = 0; i < meatBeatCollisions.Count; i++)
        {
            current = meatBeatCollisions.ElementAt(i);
            if (last == current) return;
            last = current;
        }

        meatBeatCollisions.Clear();
        OnMeatBeat?.Invoke();
    }

    private void BuildColliders()
    {
        Logging.Debug("BuildColliders");

        var player = GTPlayer.Instance;
        chest = new GameObject("Body Gesture Collider");
        chest.AddComponent<CapsuleCollider>().isTrigger = true;
        chest.AddComponent<Rigidbody>().isKinematic = true;
        chest.transform.SetParent(player.transform.FindChildRecursive("Body Collider"), false);
        chest.layer = LayerMask.NameToLayer("Water");
        float
            height = 1 / 8f,
            radius = 1 / 4f;
        chest.transform.localScale = new Vector3(radius, height, radius);

        var leftPalm = GameObject.Find(string.Format(localRigPath + palmPath, "L")).transform;
        leftPalmInteractor = CreateInteractor("Left Palm Interactor", leftPalm, 1 / 16f);
        leftHand = leftPalmInteractor.gameObject;
        leftHand.transform.localRotation = Quaternion.Euler(-90, -90, 0);

        var rightPalm = GameObject.Find(string.Format(localRigPath + palmPath, "R")).transform;
        rightPalmInteractor = CreateInteractor("Right Palm Interactor", rightPalm, 1 / 16f);
        rightHand = rightPalmInteractor.gameObject;
        rightHand.transform.localRotation = Quaternion.Euler(-90, 0, 0);


        leftPointerTransform = GameObject.Find(string.Format(localRigPath + pointerFingerPath, "L")).transform;
        leftPointerInteractor = CreateInteractor("Left Pointer Interactor", leftPointerTransform, 1 / 32f);
        leftPointerObj = leftPointerInteractor.gameObject;

        rightPointerTransform = GameObject.Find(string.Format(localRigPath + pointerFingerPath, "R")).transform;
        rightPointerInteractor = CreateInteractor("Right Pointer Interactor", rightPointerTransform, 1 / 32f);
        rightPointerObj = rightPointerInteractor.gameObject;

        leftThumbTransform = GameObject.Find(string.Format(localRigPath + thumbPath, "L")).transform;
        rightThumbTransform = GameObject.Find(string.Format(localRigPath + thumbPath, "R")).transform;
    }

    public GrateInteractor CreateInteractor(string name, Transform parent, float scale)
    {
        var obj = new GameObject(name);
        var interactor = obj.AddComponent<GrateInteractor>();
        obj.transform.SetParent(parent, false);
        obj.transform.localScale = Vector3.one * scale;
        return interactor;
    }

    public XRController GetController(bool isLeft)
    {
        foreach (var controller in FindObjectsOfType<XRController>())
        {
            if (isLeft && controller.name.ToLowerInvariant().Contains("left")) return controller;
            if (!isLeft && controller.name.ToLowerInvariant().Contains("right")) return controller;
        }

        return null;
    }

    public void HapticPulse(bool isLeft, float strength = .5f, float duration = .05f)
    {
        var hand = isLeft ? leftController : rightController;
        hand.SendHapticImpulse(0u, strength, duration);
    }

    public InputTracker? GetInputTracker(string name, XRNode node)
    {
        switch (name)
        {
            case "grip":
                return node == XRNode.LeftHand ? leftGrip : rightGrip;
            case "trigger":
                return node == XRNode.LeftHand ? leftTrigger : rightTrigger;
            case "stick":
                return node == XRNode.LeftHand ? leftStick : rightStick;
            case "primary":
                return node == XRNode.LeftHand ? leftPrimary : rightPrimary;
            case "secondary":
                return node == XRNode.LeftHand ? leftSecondary : rightSecondary;
            case "a/x":
                return node == XRNode.LeftHand ? leftPrimary : rightPrimary;
            case "b/y":
                return node == XRNode.LeftHand ? leftSecondary : rightSecondary;
            case "a":
                return rightPrimary;
            case "b":
                return rightSecondary;
            case "x":
                return leftPrimary;
            case "y":
                return leftSecondary;
        }

        return null;
    }

    public struct BodyVectors
    {
        public Vector3 pointerDirection, palmNormal, thumbDirection;
    }
}

public abstract class InputTracker
{
    public string name;
    public XRNode node;
    public Action<InputTracker> OnPressed, OnReleased;
    public bool pressed, wasPressed;
    public Quaternion quaternionValue;
    public Traverse traverse;
    public Vector3 vector3Value;

    public abstract void UpdateValues();
}

public class InputTracker<T> : InputTracker
{
    public InputTracker(Traverse traverse, XRNode node)
    {
        this.traverse = traverse;
        this.node = node;
    }

    public T GetValue()
    {
        return traverse.GetValue<T>();
    }

    public override void UpdateValues()
    {
        wasPressed = pressed;
        if (typeof(T) == typeof(bool))
            pressed = traverse.GetValue<bool>();
        else if (typeof(T) == typeof(float))
            pressed = traverse.GetValue<float>() > .5f;

        if (!wasPressed && pressed)
            OnPressed?.Invoke(this);
        if (wasPressed && !pressed)
            OnReleased?.Invoke(this);
    }
}

public class ControllerInputPollerExt
{
    public static ControllerInputPollerExt Instance;
    public Vector2 rightControllerStickAxis, leftControllerStickAxis;
    public bool rightControllerStickButton, leftControllerStickButton;
    private bool steam;

    public ControllerInputPollerExt()
    {
        Instance = this;
    }

    public void Update()
    {
        if (Plugin.IsSteam)
        {
            leftControllerStickButton = SteamVR_Actions.gorillaTag_LeftJoystickClick.state;
            rightControllerStickButton = SteamVR_Actions.gorillaTag_RightJoystickClick.state;
            leftControllerStickAxis = SteamVR_Actions.gorillaTag_LeftJoystick2DAxis.axis;
            rightControllerStickAxis = SteamVR_Actions.gorillaTag_RightJoystick2DAxis.axis;
        }
        else
        {
            var left = GestureTracker.Instance.leftController;
            var right = GestureTracker.Instance.rightController;
            left.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out leftControllerStickButton);
            right.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out rightControllerStickButton);
            left.TryGetFeatureValue(CommonUsages.primary2DAxis, out leftControllerStickAxis);
            right.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightControllerStickAxis);
        }
    }
}