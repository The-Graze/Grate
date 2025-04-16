﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Grate.Gestures;
using Grate.Modules;
using Grate.Modules.Movement;
using Grate.Modules.Physics;
using Grate.Modules.Multiplayer;
using Grate.Modules.Teleportation;
using Grate.Tools;
using Grate.Interaction;
using Grate.Extensions;
using Player = GorillaLocomotion.GTPlayer;
using BepInEx.Configuration;
using UnityEngine.XR;
using Grate.Modules.Misc;
using Photon.Pun;
using UnityEngine.InputSystem;
using System.Threading;
using System.Collections;
using UnityEngine.Networking;
using PlayFab.ClientModels;

namespace Grate.GUI
{
    public class MenuController : GrateGrabbable
    {
        public static MenuController Instance;
        public bool Built { get; private set; }
        public Vector3
        initialMenuOffset = new Vector3(0, .035f, .65f),
        btnDimensions = new Vector3(.3f, .05f, .05f);
        public Rigidbody _rigidbody;
        private List<Transform> modPages;
        public List<ButtonController> buttons;
        public List<GrateModule> modules = new List<GrateModule>();
        public GameObject modPage, settingsPage;
        public Text helpText;
        public static InputTracker SummonTracker;
        public static ConfigEntry<string> SummonInput;
        public static ConfigEntry<string> SummonInputHand;
        public static ConfigEntry<string> Theme;
        public static ConfigEntry<int> GhostLvl;

        public Material[] ghost, blood;

        bool docked;

        protected override void Awake()
        {
            if (NetworkSystem.Instance.GameModeString.Contains("MODDED_"))
            {
                Instance = this;
                try
                {
                    Logging.Debug("Awake");
                    base.Awake();
                    this.throwOnDetach = true;
                    gameObject.AddComponent<PositionValidator>();
                    Plugin.configFile.SettingChanged += SettingsChanged;
                    List<GrateModule> TooAddmodules = new List<GrateModule>();

                    if (GhostLvl.Value >= 0)
                    {
                        TooAddmodules.Add(gameObject.AddComponent<JumpScare>());
                    }
                    if (GhostLvl.Value >= 1)
                    {
                        TooAddmodules.Add(gameObject.AddComponent<DisableWind>());
                        TooAddmodules.Add(gameObject.AddComponent<Fly>());
                    }
                    if (GhostLvl.Value >= 2)
                    {
                        TooAddmodules.Add(gameObject.AddComponent<Platforms>());
                        TooAddmodules.Add(gameObject.AddComponent<NoClip>());
                        TooAddmodules.Add(gameObject.AddComponent<SpeedBoost>());
                    }
                    if (GhostLvl.Value >= 3)
                    {
                        TooAddmodules.Add(gameObject.AddComponent<Climb>());
                        TooAddmodules.Add(gameObject.AddComponent<DoubleJump>());
                        TooAddmodules.Add(gameObject.AddComponent<Teleport>());
                    }
                    if (GhostLvl.Value >= 4)
                    {
                        TooAddmodules.Add(gameObject.AddComponent<ESP>());
                    }
                    if (GhostLvl.Value >= 5)
                    {
                        TooAddmodules.Add(gameObject.AddComponent<Telekinesis>());
                        TooAddmodules.Add(gameObject.AddComponent<Throw>());
                        TooAddmodules.Add(gameObject.AddComponent<RatSword>());
                    }
                    if (GhostLvl.Value >= 10)
                    {
                        TooAddmodules.Add(gameObject.AddComponent<BANGUN>());
                    }
                    gameObject.AddComponent<Boxing>();
                    modules.AddRange(TooAddmodules);
                    ReloadConfiguration();
                }
                catch (Exception e) { Logging.Exception(e); }
            }
        }
        private void Start()
        {
            this.Summon();
            base.transform.SetParent(null);
            base.transform.position = Vector3.zero;
            this._rigidbody.isKinematic = false;
            this._rigidbody.useGravity = true;
            base.transform.SetParent(null);
            this.AddBlockerToAllButtons(ButtonController.Blocker.MENU_FALLING);
            this.docked = false;
        }

        private void ThemeChanged()
        {
            Debug.Log("Theme value: " + Theme.Value);
            if (ghost == null)
            {
                ghost = new Material[]
                {
                    Plugin.assetBundle.LoadAsset<Material>("Zipline Rope Material"),
                    Plugin.assetBundle.LoadAsset<Material>("Metal Material")
                };
                blood = new Material[]
                {
                    Plugin.assetBundle.LoadAsset<Material>("m_Menu Outer"),
                    Plugin.assetBundle.LoadAsset<Material>("m_Menu Inner")

                };
            }
            string ThemeName = Theme.Value.ToLower();
            if (ThemeName == "ghost")
            {
                gameObject.GetComponent<MeshRenderer>().materials = ghost;
            }
            if (ThemeName == "blood")
            {
                gameObject.GetComponent<MeshRenderer>().materials = blood;
            }
            transform.GetChild(5).gameObject.SetActive(false);
        }

        private void ReloadConfiguration()
        {
            if (SummonTracker != null)
                SummonTracker.OnPressed -= Summon;
            GestureTracker.Instance.OnMeatBeat -= Summon;

            var hand = SummonInputHand.Value == "left"
                ? XRNode.LeftHand : XRNode.RightHand;

            if (SummonInput.Value == "gesture")
            {
                GestureTracker.Instance.OnMeatBeat += Summon;
            }
            else
            {
                SummonTracker = GestureTracker.Instance.GetInputTracker(
                    SummonInput.Value, hand
                );
                if (SummonTracker != null)
                    SummonTracker.OnPressed += Summon;
            }
        }

        void SettingsChanged(object sender, SettingChangedEventArgs e)
        {
            if (e.ChangedSetting == SummonInput || e.ChangedSetting == SummonInputHand)
            {
                ReloadConfiguration();
            }
            if (e.ChangedSetting == Theme)
            {
                ThemeChanged();
            }
        }

        void Summon(InputTracker _) { Summon(); }

        public void Summon()
        {
            if (!Built)
                BuildMenu();
            else
                ResetPosition();
        }

        void FixedUpdate()
        {
            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                if (!docked)
                {
                    Summon();
                }
                else
                {
                    _rigidbody.isKinematic = false;
                    _rigidbody.useGravity = true;
                    transform.SetParent(null);
                    AddBlockerToAllButtons(ButtonController.Blocker.MENU_FALLING);
                    docked = false;
                }
            }
            // The potions tutorial needs to be updated frequently to keep the current size
            // up-to-date, even when the mod is disabled
            if (GrateModule.LastEnabled && GrateModule.LastEnabled == Potions.Instance)
            {
                helpText.text = Potions.Instance.Tutorial();
            }
        }

        void ResetPosition()
        {
            _rigidbody.isKinematic = true;
            _rigidbody.velocity = Vector3.zero;
            transform.SetParent(Player.Instance.bodyCollider.transform);
            transform.localPosition = initialMenuOffset;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            foreach (var button in buttons)
            {
                button.RemoveBlocker(ButtonController.Blocker.MENU_FALLING);
            }
            docked = true;
        }

        IEnumerator VerCheck()
        {
            this.gameObject.transform.Find("Version Canvas").GetComponentInChildren<Text>().text =
            $"Ghost Menu\n Ghost LVL: {GhostLvl.Value}";
            yield return "wawa";
        }

        void BuildMenu()
        {
            Logging.Debug("Building menu...");
            try
            {

                helpText = this.gameObject.transform.Find("Help Canvas").GetComponentInChildren<Text>();
                helpText.text = "Enable a module to see its tutorial.";
                StartCoroutine(VerCheck());
                var collider = this.gameObject.GetOrAddComponent<BoxCollider>();
                collider.isTrigger = true;
                _rigidbody = gameObject.GetComponent<Rigidbody>();
                _rigidbody.isKinematic = true;

                SetupInteraction();
                SetupModPages();
                SetupSettingsPage();

                transform.SetParent(Player.Instance.bodyCollider.transform);
                ResetPosition();
                Logging.Debug("Build successful.");
                ReloadConfiguration();
            }
            catch (Exception ex) { Logging.Warning(ex.Message); Logging.Warning(ex.StackTrace); return; }
            Built = true;
        }

        private void SetupSettingsPage()
        {
            GameObject button = this.gameObject.transform.Find("Settings Button").gameObject;
            ButtonController btnController = button.AddComponent<ButtonController>();
            buttons.Add(btnController);
            btnController.OnPressed += (obj, pressed) =>
            {
                settingsPage.SetActive(pressed);
                if (pressed)
                    settingsPage.GetComponent<SettingsPage>().UpdateText();
                modPage.SetActive(!pressed);
            };

            settingsPage = this.transform.Find("Settings Page").gameObject;
            settingsPage.AddComponent<SettingsPage>();
            settingsPage.SetActive(false);
        }

        public static bool debugger = true;
        public void SetupModPages()
        {
            var modPageTemplate = this.gameObject.transform.Find("Mod Page");
            int buttonsPerPage = modPageTemplate.childCount - 2; // Excludes the prev/next page btns
            int numPages = ((modules.Count - 1) / buttonsPerPage) + 1;
            if (Plugin.DebugMode)
                numPages++;

            modPages = new List<Transform>() { modPageTemplate };
            for (int i = 0; i < numPages - 1; i++)
                modPages.Add(Instantiate(modPageTemplate, this.gameObject.transform));

            buttons = new List<ButtonController>();
            for (int i = 0; i < modules.Count; i++)
            {
                var module = modules[i];

                var page = modPages[i / buttonsPerPage];
                var button = page.Find($"Button {i % buttonsPerPage}").gameObject;

                ButtonController btnController = button.AddComponent<ButtonController>();
                buttons.Add(btnController);
                btnController.OnPressed += (obj, pressed) =>
                {
                    module.enabled = pressed;
                    if (pressed)
                        helpText.text = module.GetDisplayName().ToUpper() +
                            "\n\n" + module.Tutorial().ToUpper();
                };
                module.button = btnController;
                btnController.SetText(module.GetDisplayName().ToUpper());
            }

            AddDebugButtons();

            foreach (Transform modPage in modPages)
            {
                foreach (Transform button in modPage)
                {
                    if (button.name == "Button Left" && modPage != modPages[0])
                    {
                        var btnController = button.gameObject.AddComponent<ButtonController>();
                        btnController.OnPressed += PreviousPage;
                        btnController.SetText("Prev Page");
                        buttons.Add(btnController);
                        continue;
                    }
                    else if (button.name == "Button Right" && modPage != modPages[modPages.Count - 1])
                    {
                        var btnController = button.gameObject.AddComponent<ButtonController>();
                        btnController.OnPressed += NextPage;
                        btnController.SetText("Next Page");
                        buttons.Add(btnController);
                        continue;
                    }
                    else if (!button.GetComponent<ButtonController>())
                        button.gameObject.SetActive(false);

                }
                modPage.gameObject.SetActive(false);
            }
            modPageTemplate.gameObject.SetActive(true);
            modPage = modPageTemplate.gameObject;
        }

        private void AddDebugButtons()
        {
            AddDebugButton("Debug Log", (btn, isPressed) =>
            {
                debugger = isPressed;
                Logging.Debug("Debugger", debugger ? "active" : "inactive");
                Plugin.debugText.text = "";
            });

            AddDebugButton("Close game", (btn, isPressed) =>
            {
                debugger = isPressed;
                if (btn.text.text == "You sure?")
                {
                    Application.Quit();
                }
                else
                {
                    btn.text.text = "You sure?";
                }
            });

            AddDebugButton("Show Colliders", (btn, isPressed) =>
            {
                if (isPressed)
                {
                    foreach (var c in FindObjectsOfType<Collider>())
                        c.gameObject.AddComponent<ColliderRenderer>();
                }
                else
                {
                    foreach (var c in FindObjectsOfType<ColliderRenderer>())
                        c.Obliterate();
                }
            });
        }

        int debugButtons = 0;
        private void AddDebugButton(string title, Action<ButtonController, bool> onPress)
        {
            if (!Plugin.DebugMode) return;
            var page = modPages.Last();
            var button = page.Find($"Button {debugButtons}").gameObject;
            var btnController = button.gameObject.AddComponent<ButtonController>();
            btnController.OnPressed += onPress;
            btnController.SetText(title);
            buttons.Add(btnController);
            debugButtons++;
        }

        private int pageIndex = 0;
        public void PreviousPage(ButtonController button, bool isPressed)
        {
            button.IsPressed = false;
            pageIndex--;
            for (int i = 0; i < modPages.Count; i++)
            {
                modPages[i].gameObject.SetActive(i == pageIndex);
            }
            modPage = modPages[pageIndex].gameObject;
        }
        public void NextPage(ButtonController button, bool isPressed)
        {
            button.IsPressed = false;
            pageIndex++;
            for (int i = 0; i < modPages.Count; i++)
            {
                modPages[i].gameObject.SetActive(i == pageIndex);
            }
            modPage = modPages[pageIndex].gameObject;
        }

        public void SetupInteraction()
        {
            this.throwOnDetach = true;
            this.priority = 100;
            this.OnSelectExit += (_, __) =>
            {
                AddBlockerToAllButtons(ButtonController.Blocker.MENU_FALLING);
                docked = false;
            };
            this.OnSelectEnter += (_, __) =>
            {
                RemoveBlockerFromAllButtons(ButtonController.Blocker.MENU_FALLING);
            };

        }

        public Material GetMaterial(string name)
        {
            foreach (var renderer in FindObjectsOfType<Renderer>())
            {
                string _name = renderer.material.name.ToLower();
                if (_name.Contains(name))
                {
                    return renderer.material;
                }
            }
            return null;
        }

        public void AddBlockerToAllButtons(ButtonController.Blocker blocker)
        {
            foreach (var button in buttons)
            {
                button.AddBlocker(blocker);
            }
        }

        public void RemoveBlockerFromAllButtons(ButtonController.Blocker blocker)
        {
            foreach (var button in buttons)
            {
                button.RemoveBlocker(blocker);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Plugin.configFile.SettingChanged -= SettingsChanged;
        }

        public static void BindConfigEntries()
        {
            try
            {
                ConfigDescription inputDesc = new ConfigDescription(
                    "Which button you press to open the menu",
                    new AcceptableValueList<string>("gesture", "stick", "a/x", "b/y")
                );
                SummonInput = Plugin.configFile.Bind("General",
                    "open menu",
                    "gesture",
                    inputDesc
                );

                ConfigDescription handDesc = new ConfigDescription(
                    "Which hand can open the menu",
                    new AcceptableValueList<string>("left", "right")
                );
                SummonInputHand = Plugin.configFile.Bind("General",
                    "open hand",
                    "right",
                    handDesc
                );
                ConfigDescription ThemeDesc = new ConfigDescription(
                   "Which Theme Should Ghost Menu Use?",
                   new AcceptableValueList<string>("ghost","blood")
               );
                Theme = Plugin.configFile.Bind("General",
                    "theme",
                    "ghost",
                    ThemeDesc
                );
                GhostLvl = Plugin.configFile.Bind("General", "What Ghost LVl are you?", 0);
            }
            catch (Exception e) { Logging.Exception(e); }
        }
    }
}
