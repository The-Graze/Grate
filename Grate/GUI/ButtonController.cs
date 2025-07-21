using System;
using System.Collections.Generic;
using Grate;
using Grate.Gestures;
using Grate.Tools;
using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    public enum Blocker
    {
        MENU_FALLING,
        NOCLIP_BOUNDARY,
        PIGGYBACKING,
        BUTTON_PRESSED,
        MOD_INCOMPAT,
        MOD_CONTROLL
    }

    public float buttonPushDistance = 0.03f; // Distance the button travels when pushed
    public Canvas canvas;
    public Text text;
    private readonly List<Blocker> blockers = new();

    private readonly Dictionary<Blocker, string> blockerText = new()
    {
        { Blocker.MENU_FALLING, "" },
        { Blocker.NOCLIP_BOUNDARY, "YOU ARE TOO CLOSE TO A WALL TO ACTIVATE THIS" },
        { Blocker.PIGGYBACKING, "NO COLLIDE CANNOT BE TOGGLED WHILE PIGGYBACK IS ACTIVE" },
        { Blocker.BUTTON_PRESSED, "" },
        { Blocker.MOD_INCOMPAT, "YOU HAVE A MOD THAT DOSN't WORK WITH THIS MOD" },
        { Blocker.MOD_CONTROLL, "UNDER CONTROLL OF ANOTHER MOD" }
    };

    private readonly float cooldown = .1f;
    private bool _isPressed;
    private Transform buttonModel;
    private float lastPressed;
    private Material material;
    public Action<ButtonController, bool> OnPressed;

    public bool IsPressed
    {
        get => _isPressed;
        set
        {
            _isPressed = value;
            material.color = value ? Color.red : Color.white * .75f;
        }
    }

    public bool Interactable
    {
        get => blockers.Count == 0;
        private set
        {
            if (value)
                material.color = IsPressed ? Color.red : Color.white * .75f;
            else
                material.color = IsPressed ? new Color(.5f, .3f, .3f) : Color.gray;
        }
    }

    protected void Awake()
    {
        var progress = "";
        try
        {
            buttonModel = transform.GetChild(0);
            material = buttonModel.GetComponent<Renderer>().material;
            gameObject.layer = GrateInteractor.InteractionLayer;
            var observer = gameObject.AddComponent<CollisionObserver>();
            observer.OnTriggerEntered += Press;
            observer.OnTriggerExited += Unpress;
            text = GetComponentInChildren<Text>();
            if (text)
                text.fontSize = 26;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
            Logging.Debug("Reached", progress);
        }
    }

    public void Press(GameObject self, Collider collider)
    {
        try
        {
            if (!Interactable && blockerText[blockers[0]].Length > 0)
            {
                Plugin.menuController.helpText.text = blockerText[blockers[0]];
                return;
            }

            if (!Interactable ||
                (collider.gameObject != GestureTracker.Instance.leftPointerInteractor.gameObject &&
                 collider.gameObject != GestureTracker.Instance.rightPointerInteractor.gameObject)
               ) return;

            if (Time.time - lastPressed < cooldown) return;

            lastPressed = Time.time;
            IsPressed = !IsPressed;
            OnPressed?.Invoke(this, IsPressed);
            var isLeft = collider.name.ToLower().Contains("left");
            GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(67, isLeft, 0.05f);
            var hand = isLeft ? GestureTracker.Instance.leftController : GestureTracker.Instance.rightController;
            GestureTracker.Instance.HapticPulse(isLeft);
            Plugin.menuController.AddBlockerToAllButtons(Blocker.BUTTON_PRESSED);
            Invoke(nameof(RemoveCooldownBlocker), .1f);
            buttonModel.localPosition = Vector3.up * -buttonPushDistance;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    protected void Unpress(GameObject self, Collider collider)
    {
        if (!collider.name.Contains("Pointer")) return;
        buttonModel.localPosition = Vector3.zero;
    }

    private void RemoveCooldownBlocker()
    {
        Plugin.menuController.RemoveBlockerFromAllButtons(Blocker.BUTTON_PRESSED);
    }

    public void SetText(string text)
    {
        this.text.text = text.ToUpper();
    }

    public void AddBlocker(Blocker blocker)
    {
        try
        {
            if (!NetworkSystem.Instance.GameModeString.Contains("MODDED_"))
                NetworkSystem.Instance.ReturnToSinglePlayer();
            if (blockers.Contains(blocker)) return;
            Interactable = false;
            blockers.Add(blocker);
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    public void RemoveBlocker(Blocker blocker)
    {
        try
        {
            if (blockers.Contains(blocker))
            {
                blockers.Remove(blocker);
                Interactable = blockers.Count == 0;
            }
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }
}