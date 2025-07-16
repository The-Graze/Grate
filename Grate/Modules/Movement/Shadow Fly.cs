using Grate.Extensions;
using Grate.Networking;
using Grate.Patches;
using UnityEngine;
using NetworkPlayer = NetPlayer;
namespace Grate.Modules.Movement;

public class ShadowFly : Fly
{
    private GameObject? tmpWings;
    private GameObject? enabledWings;
    protected override void Start()
    {
        base.Start();
        tmpWings = Plugin.assetBundle?.LoadAsset<GameObject>("ShadowWings");
        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
    }

    private void OnRigCached(NetPlayer arg1, VRRig arg2)
    {
        if(enabledWings != null) enabledWings.Obliterate();
    }

    private void OnPlayerModStatusChanged(NetworkPlayer player, string mod, bool modEnabled)
    {
        if (mod != DisplayName /*|| player.UserId != "AE10C04744CCF6E7"*/) return;

        if (modEnabled)
        {
            enabledWings = Instantiate(tmpWings, player.Rig()?.transform);
        }
        else if (enabledWings != null)
        {
            enabledWings.Obliterate();
        }
    }
}