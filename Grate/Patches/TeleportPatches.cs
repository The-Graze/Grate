// Stolen from https://github.com/Graicc/PracticeMod/blob/617a9f758077ea06cf0407a776580d6b021bcc35/PracticeMod/Patches/PlayerTeleportPatch.cs#L61
// Used without permission, but what are you gonna do, sue me?

using System;
using System.Threading.Tasks;
using Grate.Modules.Physics;
using Grate.Tools;
using HarmonyLib;
using UnityEngine;
using Player = GorillaLocomotion.GTPlayer;

namespace Grate.Patches;

[HarmonyPatch(typeof(Player), nameof(Player.AntiTeleportTechnology))]
internal class PlayerAntiAntiTeleport
{
    private static bool Prefix()
    {
        return !Plugin.WaWaGrazeDotCc;
    }
}

[HarmonyPatch(typeof(Player))]
[HarmonyPatch(nameof(Player.LateUpdate), MethodType.Normal)]
internal class TeleportPatch
{
    private static bool _isTeleporting,
        _rotate;

    private static Vector3 _teleportPosition;
    private static float _teleportRotation;
    private static bool _killVelocity;

    public static bool Prefix(Player __instance)
    {
        try
        {
            if (_isTeleporting)
            {
                Player.Instance.locomotionEnabledLayers = NoClip.layerMask;
                Player.Instance.bodyCollider.isTrigger = true;
                Player.Instance.headCollider.isTrigger = true;
                var playerRigidBody = __instance.GetComponent<Rigidbody>();
                if (playerRigidBody != null)
                {
                    var correctedPosition = _teleportPosition - __instance.bodyCollider.transform.position +
                                            __instance.transform.position;

                    if (_killVelocity)
                        playerRigidBody.velocity = Vector3.zero;

                    __instance.transform.position = correctedPosition;
                    if (_rotate)
                        __instance.transform.rotation = Quaternion.Euler(0, _teleportRotation, 0);


                    Traverse.Create(__instance).Field("lastLeftHandPosition")
                        .SetValue(__instance.leftHand.handFollower.transform.position);
                    Traverse.Create(__instance).Field("lastRightHandPosition")
                        .SetValue(__instance.rightHand.handFollower.transform.position);

                    Traverse.Create(__instance).Field("lastPosition").SetValue(correctedPosition);
                    Traverse.Create(__instance).Field("lastOpenHeadPosition")
                        .SetValue(__instance.headCollider.transform.position);

                    GorillaTagger.Instance.offlineVRRig.transform.position = correctedPosition;
                }

                Player.Instance.headCollider.isTrigger = NoClip.Instance.baseHeadIsTrigger;
                Player.Instance.bodyCollider.isTrigger = NoClip.Instance.baseBodyIsTrigger;
                Task.Delay(250).ContinueWith(delegate
                {
                    Player.Instance.locomotionEnabledLayers = NoClip.Instance.baseMask;
                });
                _isTeleporting = false;
                return true;
            }
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }

        return true;
    }

    internal static void TeleportPlayer(Vector3 destinationPosition, float destinationRotation,
        bool killVelocity = true)
    {
        if (_isTeleporting)
            return;
        _killVelocity = killVelocity;
        _teleportPosition = destinationPosition;
        _teleportRotation = destinationRotation;
        _isTeleporting = true;
        _rotate = true;
    }

    internal static void TeleportPlayer(Vector3 destinationPosition, bool killVelocity = true)
    {
        if (_isTeleporting)
            return;

        _killVelocity = killVelocity;
        _teleportPosition = destinationPosition;
        _isTeleporting = true;
        _rotate = false;
    }
}

/* broke teleporting to v-stump
 [HarmonyPatch(typeof(Player), nameof(Player.TeleportTo), new Type[] { typeof(Vector3), typeof(Quaternion) })]
 internal class TeleportPatch
 {
     private static bool Prefix(Player __instance, Vector3 position, Quaternion rotation)
     {
         var playerRigidBody = __instance.GetComponent<Rigidbody>();
         if (playerRigidBody != null)
         {
             Vector3 correctedPosition = position - __instance.bodyCollider.transform.position + __instance.transform.position;
             playerRigidBody.velocity = Vector3.zero;
             __instance.transform.position = correctedPosition;
             __instance.transform.rotation = rotation;
             __instance.transform.rotation = rotation;
             __instance.leftHandFollower.position = __instance.leftControllerTransform.position;
             __instance.leftHandFollower.rotation = __instance.leftControllerTransform.rotation;
             __instance.rightHandFollower.position = __instance.rightControllerTransform.position;
             __instance.rightHandFollower.rotation = __instance.rightControllerTransform.rotation;
             __instance.lastHeadPosition = __instance.headCollider.transform.position;

             Traverse.Create(__instance).Field("lastLeftHandPosition").SetValue(__instance.leftHandFollower.transform.position);
             Traverse.Create(__instance).Field("lastRightHandPosition").SetValue(__instance.rightHandFollower.transform.position);

             Traverse.Create(__instance).Field("lastPosition").SetValue(position);
             Traverse.Create(__instance).Field("lastOpenHeadPosition").SetValue(__instance.headCollider.transform.position);

             GorillaTagger.Instance.offlineVRRig.transform.position = position;
         }
         return false;
     }
 }*/