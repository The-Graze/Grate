using GorillaLocomotion;
using GorillaLocomotion.Swimming;
using Grate.GUI;
using UnityEngine;

namespace Grate.Modules;

public class Swim : GrateModule
{
    public static readonly string DisplayName = "Swim";
    public GameObject? waterVolume;

    protected override void Start()
    {
        base.Start();
        waterVolume = Plugin.Water;
        waterVolume.transform.localScale = new Vector3(5f, 1000f, 5f);
        waterVolume.transform.SetParent(GTPlayer.Instance.transform, false);
        waterVolume.transform.localPosition = new Vector3(0, 50, 0);
        waterVolume.SetActive(false);
        if (waterVolume.GetComponent<Renderer>()) waterVolume.GetComponent<Renderer>().enabled = false;
        if (waterVolume.GetComponentInChildren<Renderer>())
            waterVolume.GetComponentInChildren<Renderer>().enabled = false;
    }

    private void LateUpdate()
    {
        GTPlayer.Instance.audioManager.UnsetMixerSnapshot();
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        waterVolume?.SetActive(true);
    }

    protected override void Cleanup()
    {
        if (!MenuController.Instance.Built) return;
        waterVolume?.SetActive(false);
        GTPlayer.Instance.audioManager.UnsetMixerSnapshot();
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Effect: Surrounds you with invisible water.";
    }
}