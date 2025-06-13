using System;
using System.Collections;
using System.Collections.Generic;
using GorillaNetworking;
using Grate.Extensions;
using Grate.Modules;
using Grate.Tools;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Grate.Networking;

public class NetworkPropertyHandler : MonoBehaviourPunCallbacks
{
    private const float refreshRate = 1f;

    public static NetworkPropertyHandler Instance;
    public static string versionKey = "GrateVersion";
    private readonly Hashtable properties = new();

    private float lastPropertyUpdate;
    public Dictionary<NetPlayer, NetworkedPlayer> networkedPlayers = new();
    public Action<NetPlayer> OnPlayerJoined, OnPlayerLeft;
    public Action<NetPlayer, string, bool> OnPlayerModStatusChanged;

    private void Awake()
    {
        Instance = this;
        ChangeProperty(versionKey, PluginInfo.Version);
    }

    private void Start()
    {
        Logging.Debug("Found", GorillaParent.instance.vrrigs.Count, "vrrigs and ", PhotonNetwork.PlayerList.Length,
            "players.");
        foreach (var player in PhotonNetwork.PlayerList) StartCoroutine(CreateNetworkedPlayer(player));
        NetworkSystem.Instance.OnPlayerJoined += OnPlayerEnteredRoom;
        NetworkSystem.Instance.OnPlayerLeft += OnPlayerLeftRoom;
    }

    private void FixedUpdate()
    {
        if (properties.Count == 0 || Time.time - lastPropertyUpdate < refreshRate) return;
        Logging.Debug($"Updated properties ({properties.Count}):");
        foreach (var property in properties)
        {
            Logging.Debug(property.Key, ":", property.Value);
            if ((string)property.Key == GrateModule.enabledModulesKey)
                foreach (var mod in (Dictionary<string, bool>)property.Value)
                    if (mod.Value)
                        Logging.Debug("    ", property.Key, "is enabled");
        }

        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
        properties.Clear();
        lastPropertyUpdate = Time.time;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        var targetNetPlayer = NetworkSystem.Instance.GetPlayer(targetPlayer.ActorNumber);
        if (targetNetPlayer != NetworkSystem.Instance.LocalPlayer)
            if (changedProps.ContainsKey(GrateModule.enabledModulesKey))
            {
                networkedPlayers[targetPlayer].hasGrate = true;
                var enabledModules = (Dictionary<string, bool>)changedProps[GrateModule.enabledModulesKey];
                //Logging.Debug(targetPlayer.NickName, "toggled mods:");
                foreach (var mod in enabledModules)
                    //Logging.Debug(mod.Value ? "  +" : "  -", mod.Key, mod.Value);
                    OnPlayerModStatusChanged?.Invoke(targetPlayer, mod.Key, mod.Value);
            }
    }

    public void OnPlayerLeftRoom(NetPlayer otherPlayer)
    {
        OnPlayerLeft?.Invoke(otherPlayer);
        if (networkedPlayers.ContainsKey(otherPlayer))
        {
            Destroy(networkedPlayers[otherPlayer]);
            networkedPlayers.Remove(otherPlayer);
        }
    }

    public void OnPlayerEnteredRoom(NetPlayer newPlayer)
    {
        try
        {
            OnPlayerJoined?.Invoke(newPlayer);
            StartCoroutine(CreateNetworkedPlayer(newPlayer));
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private IEnumerator CreateNetworkedPlayer(NetPlayer player = null, VRRig rig = null)
    {
        if (player is null && rig is null)
            throw new Exception("Both player and rig are null");

        if (player is null)
            player = rig.OwningNetPlayer;
        else if (rig is null)
            for (var i = 0; i < 10; i++)
            {
                rig = player.Rig();
                if (rig is null) yield return new WaitForSeconds(.1f);
            }

        var np = rig?.gameObject.GetOrAddComponent<NetworkedPlayer>();
        np.owner = player;
        np.rig = rig;
        networkedPlayers.AddOrUpdate(player, np);
    }

    public void ChangeProperty(string key, object value)
    {
        if (properties.ContainsKey(key))
            properties[key] = value;
        else
            properties.Add(key, value);
    }
}