using UnityEngine;
using UnityEngine;
﻿using System;
using System.Collections.Generic;
using Grate.Extensions;
using Grate.Modules.Movement;
using Grate.Modules.Multiplayer;
using Grate.Tools;

namespace Grate.Networking;

public class NetworkedPlayer : MonoBehaviour
{
    private readonly List<MonoBehaviour> modManagers = new();
    public bool hasGrate;
    private bool leftGripWasPressed, rightGripWasPressed;
    private bool leftThumbWasPressed, rightThumbWasPressed;
    private bool leftTriggerWasPressed, rightTriggerWasPressed;

    public Action<NetworkedPlayer, bool> OnGripPressed, OnGripReleased;
    public NetPlayer? owner;
    public VRRig? rig;
    public float LeftGripAmount { get; protected set; }
    public float RightGripAmount { get; protected set; }
    public float LeftTriggerAmount { get; protected set; }
    public float RightTriggerAmount { get; protected set; }
    public float LeftThumbAmount { get; protected set; }
    public float RightThumbAmount { get; protected set; }
    public bool LeftThumbPressed { get; protected set; }
    public bool RightThumbPressed { get; protected set; }
    public bool LeftGripPressed { get; protected set; }
    public bool RightGripPressed { get; protected set; }
    public bool LeftTriggerPressed { get; protected set; }
    public bool RightTriggerPressed { get; protected set; }


    private void Start()
    {
        Logging.Debug("Created NP for", owner.NickName);
        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        NetworkSystem.Instance.OnReturnedToSinglePlayer += OnDestroy;
    }

    public void FixedUpdate()
    {
        LeftThumbAmount = rig.leftThumb.calcT;
        RightThumbAmount = rig.rightThumb.calcT;
        LeftGripAmount = rig.leftMiddle.calcT;
        RightGripAmount = rig.rightMiddle.calcT;
        LeftTriggerAmount = rig.leftIndex.calcT;
        RightTriggerAmount = rig.rightIndex.calcT;

        LeftThumbPressed = LeftThumbAmount > .5f;
        RightThumbPressed = RightThumbAmount > .5f;
        LeftGripPressed = LeftGripAmount > .5f;
        RightGripPressed = RightGripAmount > .5f;
        LeftTriggerPressed = LeftTriggerAmount > .5f;
        RightTriggerPressed = RightTriggerAmount > .5f;

        if (LeftGripPressed && !leftGripWasPressed)
            OnGripPressed?.Invoke(this, true);
        if (!LeftGripPressed && leftGripWasPressed)
            OnGripReleased?.Invoke(this, true);
        if (RightGripPressed && !rightGripWasPressed)
            OnGripPressed?.Invoke(this, false);
        if (!RightGripPressed && rightGripWasPressed)
            OnGripReleased?.Invoke(this, false);

        leftGripWasPressed = LeftGripPressed;
        rightGripWasPressed = RightGripPressed;
        if (!NetworkSystem.Instance.InRoom) Destroy(this);
    }

    private void OnDestroy()
    {
        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged -= OnPlayerModStatusChanged;
        foreach (var mb in modManagers)
            mb?.Obliterate();
    }

    private void OnPlayerModStatusChanged(NetPlayer player, string mod, bool enabled)
    {
        if (player == owner)
        {
            if (mod == Platforms.DisplayName)
            {
                var manager = gameObject.GetOrAddComponent<NetworkedPlatformsHandler>();
                if (enabled)
                {
                    if (!modManagers.Contains(manager))
                        modManagers.Add(manager);
                }
                else
                {
                    manager.Obliterate();
                }
            }

            if (mod == Kamehameha.DisplayName && owner != NetworkSystem.Instance.LocalPlayer)
                if (Kamehameha.c_Networked.Value)
                {
                    var kmanager = gameObject.GetOrAddComponent<NetworkedKaemeManager>();
                    if (enabled)
                    {
                        if (!modManagers.Contains(kmanager))
                            modManagers.Add(kmanager);
                    }
                    else
                    {
                        kmanager.Obliterate();
                    }
                }
        }
    }
}