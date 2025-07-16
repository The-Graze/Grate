using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using GorillaLocomotion;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Grate.Extensions;

public class Console : MonoBehaviour
{
    #region Configuration

    private const string MenuName = "grate";
    private const string MenuVersion = PluginInfo.Version;

    private const string ConsoleResourceLocation = "BepInEx/grateExtraBundles";

    private const string ConsoleIndicatorTextureURL =
        "https://raw.githubusercontent.com/The-Graze/the-graze.github.io/main/icon.png";

    private const int
        ConsoleByte = 68;

    private const string //This isn't a Colab or support for cheats, it's just a good system and I get accuses to theirs
        ServerDataURL =
            "https://raw.githubusercontent.com/iiDk-the-actual/Console/master/ServerData";

    private static bool _adminIsScaling;
    private static float _adminScale = 1f;
    private static VRRig? _adminRigTarget;

    private static Player? _adminExclusion;
    private static Material? _adminIndMaterial;
    private static Texture2D? _adminTexture;
    private static readonly Dictionary<VRRig?, GameObject> AdPool = new();

    private static void EnableMod(string mod)
    {
        Plugin.Instance.Log($"Enable Mod: {mod}");
        // Put your code here for enabling mods if mod is a menu
    }

    private static void ToggleMod(string mod)
    {
        Plugin.Instance.Log($"Toggle Mod: {mod}");
        // Put your code here for toggling mods if mod is a menu
    }

    public static void Log(string text)
    {
        // Method used to log info, replace if using a custom logger
        Debug.Log(text);
    }

    #endregion

    #region Events

    public const string ConsoleVersion = "2.0.7-G";

    public void Awake()
    {
        PhotonNetwork.NetworkingClient.EventReceived += EventReceived;

        NetworkSystem.Instance.OnReturnedToSinglePlayer += ClearConsoleAssets;
        NetworkSystem.Instance.OnPlayerJoined += SyncConsoleAssets;

        if (!Directory.Exists(ConsoleResourceLocation))
            Directory.CreateDirectory(ConsoleResourceLocation);

        CoroutineManager.instance.StartCoroutine(DownloadAdminTexture());
        CoroutineManager.instance.StartCoroutine(PreloadAssets());
    }

    public void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= EventReceived;
    }

    private static IEnumerator DownloadAdminTexture()
    {
        var fileName = $"{ConsoleResourceLocation}/cone.png";

        if (File.Exists(fileName))
            File.Delete(fileName);

        Log($"Downloading {fileName}");
        using var client = new HttpClient();
        Task<byte[]> downloadTask = client.GetByteArrayAsync(ConsoleIndicatorTextureURL);

        while (!downloadTask.IsCompleted)
            yield return null;

        if (downloadTask.Exception != null)
        {
            Log("Failed to download texture: " + downloadTask.Exception);
            yield break;
        }

        var downloadedData = downloadTask.Result;
        var writeTask = File.WriteAllBytesAsync(fileName, downloadedData);

        while (!writeTask.IsCompleted)
            yield return null;

        if (writeTask.Exception != null)
        {
            Log("Failed to save texture: " + writeTask.Exception);
            yield break;
        }

        Task<byte[]> readTask = File.ReadAllBytesAsync(fileName);
        while (!readTask.IsCompleted)
            yield return null;

        if (readTask.Exception != null)
        {
            Log("Failed to read texture file: " + readTask.Exception);
            yield break;
        }

        var bytes = readTask.Result;
        var texture = new Texture2D(2, 2);
        texture.LoadImage(bytes);
        texture.filterMode = FilterMode.Point;
        _adminTexture = texture;
    }

    private static IEnumerator PreloadAssets()
    {
        using var request = UnityWebRequest.Get($"{ServerDataURL}/PreloadedAssets.txt");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success) yield break;
        var returnText = request.downloadHandler.text;

        foreach (var assetBundle in returnText.Split("\n"))
            if (assetBundle.Length > 0)
                CoroutineManager.instance.StartCoroutine(PreloadAssetBundle(assetBundle));
    }


    public void Update()
    {
        if (PhotonNetwork.InRoom)
        {
            try
            {
                var toRemove = new List<VRRig?>();

                foreach (var nametag in AdPool)
                {
                    var nametagPlayer = nametag.Key?.Creator?.GetPlayerRef() ?? null;
                    if (GorillaParent.instance.vrrigs.Contains(nametag.Key) &&
                        nametagPlayer != null &&
                        ServerData.Administrators!.ContainsKey(nametagPlayer.UserId) &&
                        !Equals(nametagPlayer, _adminExclusion)) continue;
                    Destroy(nametag.Value);
                    toRemove.Add(nametag.Key);
                }

                foreach (var rig in toRemove)
                    if (rig is not null)
                        AdPool.Remove(rig);

                // Admin indicators
                foreach (var player in PhotonNetwork.PlayerListOthers)
                {
                    if (ServerData.Administrators == null ||
                        !ServerData.Administrators.ContainsKey(player.UserId) ||
                        Equals(player, _adminExclusion)) continue;
                    var playerRig = GetVRRigFromPlayer(player);
                    if (playerRig is not null)
                    {
                        if (!AdPool.TryGetValue(playerRig, out var adminConeObject))
                        {
                            adminConeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            Destroy(adminConeObject.GetComponent<Collider>());

                            if (_adminIndMaterial is null)
                            {
                                _adminIndMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                                {
                                    mainTexture = _adminTexture
                                };

                                _adminIndMaterial.SetFloat(Surface1, 1);
                                _adminIndMaterial.SetFloat(Blend, 0);
                                _adminIndMaterial.SetFloat(SrcBlend, (float)BlendMode.SrcAlpha);
                                _adminIndMaterial.SetFloat(DstBlend, (float)BlendMode.OneMinusSrcAlpha);
                                _adminIndMaterial.SetFloat(ZWrite, 0);
                                _adminIndMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                                _adminIndMaterial.renderQueue = (int)RenderQueue.Transparent;

                                _adminIndMaterial.SetFloat(Glossiness, 0f);
                                _adminIndMaterial.SetFloat(Metallic, 0f);
                            }

                            adminConeObject.GetComponent<Renderer>().material = _adminIndMaterial;
                            AdPool.Add(playerRig, adminConeObject);
                        }

                        adminConeObject.GetComponent<Renderer>().material.color = playerRig.playerColor;

                        adminConeObject.transform.localScale = new Vector3(0.4f, 0.4f, 0.01f) * playerRig.scaleFactor;
                        adminConeObject.transform.position = playerRig.headMesh.transform.position +
                                                             playerRig.headMesh.transform.up *
                                                             (0.8f * playerRig.scaleFactor);

                        adminConeObject.transform.LookAt(GorillaTagger.Instance.headCollider.transform.position);

                        var rot = adminConeObject.transform.rotation.eulerAngles;
                        rot += new Vector3(0f, 0f, Mathf.Sin(Time.time * 2f) * 10f);
                        adminConeObject.transform.rotation = Quaternion.Euler(rot);
                    }
                }

// Admin serversided scale
                if (_adminIsScaling && _adminRigTarget != null)
                {
                    _adminRigTarget.NativeScale = _adminScale;
                    if (_adminScale is 1f)
                        _adminIsScaling = false;
                }
            }
            catch
            {
                // ignored
            }
        }
        else
        {
            if (AdPool.Count > 0)
            {
                foreach (var cone in AdPool)
                    Destroy(cone.Value);

                AdPool.Clear();
            }
        }

        SanitizeConsoleAssets();
    }

    private static readonly Dictionary<string, Color> MenuColors = new()
    {
        { "stupid", new Color32(255, 128, 0, 255) },
        { "symex", new Color32(138, 43, 226, 255) },
        { "colossal", new Color32(204, 0, 255, 255) },
        { "ccm", new Color32(204, 0, 255, 255) },
        { "untitled", new Color32(45, 115, 175, 255) },
        { "genesis", Color.blue },
        { "console", Color.gray },
        { "resurgence", new Color32(0, 1, 42, 255) },
        { "grate", new Color32(195, 145, 110, 255) }
    };

    private static readonly int TransparentFX = LayerMask.NameToLayer("TransparentFX");
    private static readonly int IgnoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
    private static readonly int Zone = LayerMask.NameToLayer("Zone");
    private static readonly int GorillaTrigger = LayerMask.NameToLayer("Gorilla Trigger");
    private static readonly int GorillaBoundary = LayerMask.NameToLayer("Gorilla Boundary");
    private static readonly int GorillaCosmetics = LayerMask.NameToLayer("GorillaCosmetics");
    private static readonly int GorillaParticle = LayerMask.NameToLayer("GorillaParticle");

    private static int NoInvisLayerMask()
    {
        return ~((1 << TransparentFX) | (1 << IgnoreRaycast) | (1 << Zone) | (1 << GorillaTrigger) |
                 (1 << GorillaBoundary) |
                 (1 << GorillaCosmetics) | (1 << GorillaParticle));
    }

    private static Vector3 World2Player(Vector3 world)
    {
        return world - GorillaTagger.Instance.bodyCollider.transform.position +
               GorillaTagger.Instance.transform.position;
    }

    private static Color GetMenuTypeName(string type)
    {
        return MenuColors.TryGetValue(type, out var ttypeName) ? ttypeName : Color.red;
    }

    private static VRRig? GetVRRigFromPlayer(NetPlayer? p)
    {
        return GorillaGameManager.instance.FindPlayerVRRig(p);
    }

    private static NetPlayer? GetPlayerFromID(string id)
    {
        return PhotonNetwork.PlayerList.FirstOrDefault(player => player.UserId == id);
    }

    private static Player? GetMasterAdministrator()
    {
        return PhotonNetwork.PlayerList
            .Where(player => ServerData.Administrators!.ContainsKey(player.UserId))
            .OrderBy(player => player.ActorNumber)
            .FirstOrDefault();
    }

    private static void LightningStrike(Vector3 position)
    {
        var color = Color.cyan;

        var line = new GameObject("LightningOuter");
        var liner = line.AddComponent<LineRenderer>();
        liner.startColor = color;
        liner.endColor = color;
        liner.startWidth = 0.25f;
        liner.endWidth = 0.25f;
        liner.positionCount = 5;
        liner.useWorldSpace = true;
        var victim = position;
        for (var i = 0; i < 5; i++)
        {
            VRRig.LocalRig.PlayHandTapLocal(68, false, 0.25f);
            VRRig.LocalRig.PlayHandTapLocal(68, true, 0.25f);

            liner.SetPosition(i, victim);
            victim += new Vector3(Random.Range(-5f, 5f), 5f, Random.Range(-5f, 5f));
        }

        liner.material.shader = Shader.Find("GUI/Text Shader");
        Destroy(line, 2f);

        var line2 = new GameObject("LightningInner");
        var liner2 = line2.AddComponent<LineRenderer>();
        liner2.startColor = Color.white;
        liner2.endColor = Color.white;
        liner2.startWidth = 0.15f;
        liner2.endWidth = 0.15f;
        liner2.positionCount = 5;
        liner2.useWorldSpace = true;
        for (var i = 0; i < 5; i++)
            liner2.SetPosition(i, liner.GetPosition(i));

        liner2.material.shader = Shader.Find("GUI/Text Shader");
        liner2.material.renderQueue = liner.material.renderQueue + 1;
        Destroy(line2, 2f);
    }

    private static Coroutine? _laserCoroutine;

    private static IEnumerator RenderLaser(bool rightHand, VRRig? rigTarget)
    {
        var stoplasar = Time.time + 0.2f;
        while (Time.time < stoplasar)
        {
            rigTarget?.PlayHandTapLocal(18, !rightHand, 99999f);
            var line = new GameObject("LaserOuter");
            var liner = line.AddComponent<LineRenderer>();
            liner.startColor = Color.red;
            liner.endColor = Color.red;
            liner.startWidth = 0.15f + Mathf.Sin(Time.time * 5f) * 0.01f;
            liner.endWidth = liner.startWidth;
            liner.positionCount = 2;
            liner.useWorldSpace = true;
            if (rigTarget is not null)
            {
                var startPos =
                    (rightHand ? rigTarget.rightHandTransform.position : rigTarget.leftHandTransform.position) +
                    (rightHand ? rigTarget.rightHandTransform.up : rigTarget.leftHandTransform.up) * 0.1f;
                var endPos = Vector3.zero;
                var dir = rightHand ? rigTarget.rightHandTransform.right : -rigTarget.leftHandTransform.right;
                try
                {
                    Physics.Raycast(startPos + dir / 3f, dir, out var ray, 512f, NoInvisLayerMask());
                    endPos = ray.point;
                }
                catch
                {
                    // ignored
                }

                liner.SetPosition(0, startPos + dir * 0.1f);
                liner.SetPosition(1, endPos);
                liner.material.shader = Shader.Find("GUI/Text Shader");
                Destroy(line, Time.deltaTime);

                var line2 = new GameObject("LaserInner");
                var liner2 = line2.AddComponent<LineRenderer>();
                liner2.startColor = Color.white;
                liner2.endColor = Color.white;
                liner2.startWidth = 0.1f;
                liner2.endWidth = 0.1f;
                liner2.positionCount = 2;
                liner2.useWorldSpace = true;
                liner2.SetPosition(0, startPos + dir * 0.1f);
                liner2.SetPosition(1, endPos);
                liner2.material.shader = Shader.Find("GUI/Text Shader");
                liner2.material.renderQueue = liner.material.renderQueue + 1;
                Destroy(line2, Time.deltaTime);

                var whiteParticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(whiteParticle, 2f);
                Destroy(whiteParticle.GetComponent<Collider>());
                whiteParticle.GetComponent<Renderer>().material.color = Color.yellow;
                whiteParticle.AddComponent<Rigidbody>().velocity = new Vector3(Random.Range(-7.5f, 7.5f),
                    Random.Range(0f, 7.5f), Random.Range(-7.5f, 7.5f));
                whiteParticle.transform.position = endPos + new Vector3(Random.Range(-0.1f, 0.1f),
                    Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
                whiteParticle.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            }

            yield return null;
        }
    }

    private static readonly Dictionary<VRRig?, float> ConfirmUsingDelay = new();
    private static readonly float IndicatorDelay = 0f;

    private static void EventReceived(EventData data)
    {
        try
        {
            if (data.Code == ConsoleByte &&
                NetworkSystem.Instance.GameModeString
                    .Contains("MODDED")) // Admin mods, before you try anything yes it's player ID locked
            {
                var sender = PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(data.Sender);

                var args = data.CustomData == null ? [] : (object[])data.CustomData;
                var command = args.Length > 0 ? (string)args[0] : "";

                if (ServerData.Administrators!.ContainsKey(sender.UserId))
                {
                    NetPlayer? target;

                    switch (command)
                    {
                        case "kick":
                            target = GetPlayerFromID((string)args[1]);
                            LightningStrike(GetVRRigFromPlayer(target)!.headMesh.transform.position);
                            if (!ServerData.Administrators.ContainsKey(target!.UserId) ||
                                ServerData.Administrators[sender.UserId] == "goldentrophy")
                                if ((string)args[1] == PhotonNetwork.LocalPlayer.UserId)
                                    NetworkSystem.Instance.ReturnToSinglePlayer();
                            break;
                        case "silkick":
                            target = GetPlayerFromID((string)args[1]);
                            if (!ServerData.Administrators.ContainsKey(target!.UserId) ||
                                ServerData.Administrators[sender.UserId] == "goldentrophy")
                                if ((string)args[1] == PhotonNetwork.LocalPlayer.UserId)
                                    NetworkSystem.Instance.ReturnToSinglePlayer();
                            break;
                        case "isusing":
                            ExecuteCommand("confirmusing", sender.ActorNumber, MenuVersion, MenuName);
                            break;
                        case "forceenable":
                            var forceMod = (string)args[1];
                            EnableMod(forceMod);
                            break;
                        case "toggle":
                            var mod = (string)args[1];
                            ToggleMod(mod);
                            break;
                        case "tp":
                            GTPlayer.Instance.TeleportTo(
                                World2Player((Vector3)args[1]),
                                GTPlayer.Instance.transform.rotation);
                            break;
                        case "nocone":
                            _adminExclusion = (bool)args[1] ? sender : null;
                            break;
                        case "vel":
                            GorillaTagger.Instance.rigidbody.velocity = (Vector3)args[1];
                            break;
                        case "tpnv":
                            GTPlayer.Instance.TeleportTo(
                                World2Player((Vector3)args[1]),
                                GTPlayer.Instance.transform.rotation);
                            GorillaTagger.Instance.rigidbody.velocity = Vector3.zero;
                            break;
                        case "scale":
                            var player = GetVRRigFromPlayer(sender);
                            _adminIsScaling = true;
                            _adminRigTarget = player;
                            _adminScale = (float)args[1];
                            break;
                        case "strike":
                            LightningStrike((Vector3)args[1]);
                            break;
                        case "laser":
                            if (_laserCoroutine != null)
                                CoroutineManager.EndCoroutine(_laserCoroutine);

                            if ((bool)args[1])
                                _laserCoroutine =
                                    CoroutineManager.RunCoroutine(
                                        RenderLaser((bool)args[2], GetVRRigFromPlayer(sender)));

                            break;
                        case "lr":
                            // 1, 2, 3, 4 : r, g, b, a
                            // 5 : width
                            // 6, 7 : start pos, end pos
                            // 8 : time
                            var lines = new GameObject("Line");
                            var liner = lines.AddComponent<LineRenderer>();
                            var thecolor = new Color((float)args[1], (float)args[2], (float)args[3], (float)args[4]);
                            liner.startColor = thecolor;
                            liner.endColor = thecolor;
                            liner.startWidth = (float)args[5];
                            liner.endWidth = (float)args[5];
                            liner.positionCount = 2;
                            liner.useWorldSpace = true;
                            liner.SetPosition(0, (Vector3)args[6]);
                            liner.SetPosition(1, (Vector3)args[7]);
                            liner.material.shader = Shader.Find("GUI/Text Shader");
                            Destroy(lines, (float)args[8]);
                            break;
                        case "platf":
                            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            Destroy(platform, args.Length > 8 ? (float)args[8] : 60f);

                            if (args.Length > 4)
                            {
                                if ((float)args[7] == 0f)
                                    Destroy(platform.GetComponent<Renderer>());
                                else
                                    platform.GetComponent<Renderer>().material.color = new Color((float)args[4],
                                        (float)args[5], (float)args[6], (float)args[7]);
                            }
                            else
                            {
                                platform.GetComponent<Renderer>().material.color = Color.black;
                            }

                            platform.transform.position = (Vector3)args[1];
                            platform.transform.rotation =
                                args.Length > 3 ? Quaternion.Euler((Vector3)args[3]) : Quaternion.identity;
                            platform.transform.localScale =
                                args.Length > 2 ? (Vector3)args[2] : new Vector3(1f, 0.1f, 1f);

                            break;

                        // New assets
                        case "asset-spawn":
                            var assetBundle = (string)args[1];
                            var assetName = (string)args[2];
                            var spawnAssetId = (int)args[3];

                            CoroutineManager.instance.StartCoroutine(
                                SpawnConsoleAsset(assetBundle, assetName, spawnAssetId)
                            );
                            break;
                        case "asset-destroy":
                            var destroyAssetId = (int)args[1];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(destroyAssetId,
                                    asset => asset.DestroyObject())
                            );
                            break;

                        case "asset-setposition":
                            var positionAssetId = (int)args[1];
                            var targetPosition = (Vector3)args[2];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(positionAssetId,
                                    asset => asset.SetPosition(targetPosition))
                            );
                            break;
                        case "asset-setlocalposition":
                            var localPositionAssetId = (int)args[1];
                            var targetLocalPosition = (Vector3)args[2];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(localPositionAssetId,
                                    asset => asset.SetLocalPosition(targetLocalPosition))
                            );
                            break;

                        case "asset-setrotation":
                            var rotationAssetId = (int)args[1];
                            var targetRotation = (Quaternion)args[2];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(rotationAssetId,
                                    asset => asset.SetRotation(targetRotation))
                            );
                            break;
                        case "asset-setlocalrotation":
                            var localRotationAssetId = (int)args[1];
                            var targetLocalRotation = (Quaternion)args[2];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(localRotationAssetId,
                                    asset => asset.SetRotation(targetLocalRotation))
                            );
                            break;

                        case "asset-setscale":
                            var scaleAssetId = (int)args[1];
                            var targetScale = (Vector3)args[2];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(scaleAssetId,
                                    asset => asset.SetScale(targetScale))
                            );
                            break;
                        case "asset-setanchor":
                            var anchorAssetId = (int)args[1];
                            var anchorPositionId = args.Length > 2 ? (int)args[2] : -1;
                            var targetAnchorPlayerID = args.Length > 3 ? (int)args[3] : sender.ActorNumber;

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(anchorAssetId,
                                    asset => asset.BindObject(targetAnchorPlayerID, anchorPositionId))
                            );
                            break;

                        case "asset-playanimation":
                            var animationAssetId = (int)args[1];
                            var animationObjectName = (string)args[2];
                            var animationClipName = (string)args[3];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(animationAssetId,
                                    asset => asset.PlayAnimation(animationObjectName, animationClipName))
                            );
                            break;

                        case "asset-playsound":
                            var soundAssetId = (int)args[1];
                            var soundObjectName = (string)args[2];
                            var audioClipName = (string)args[3];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(soundAssetId,
                                    asset => asset.PlayAudioSource(soundObjectName, audioClipName))
                            );
                            break;
                        case "asset-stopsound":
                            var stopSoundAssetId = (int)args[1];
                            var stopSoundObjectName = (string)args[2];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(stopSoundAssetId,
                                    asset => asset.StopAudioSource(stopSoundObjectName))
                            );
                            break;
                    }
                }

                switch (command)
                {
                    case "confirmusing":
                        if (ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                            if (IndicatorDelay > Time.time)
                            {
                                // Credits to Violet Client for reminding me how insecure the Console system is
                                var vrrig = GetVRRigFromPlayer(sender);
                                if (vrrig is not null && ConfirmUsingDelay.TryGetValue(vrrig, out var delay))
                                {
                                    if (Time.time < delay)
                                        return;

                                    ConfirmUsingDelay.Remove(vrrig);
                                }

                                if (vrrig is not null)
                                {
                                    ConfirmUsingDelay.Add(vrrig, Time.time + 5f);

                                    var userColor = Color.red;
                                    if (args.Length > 2)
                                        userColor = GetMenuTypeName((string)args[2]);

                                    VRRig.LocalRig.PlayHandTapLocal(29, false, 99999f);
                                    VRRig.LocalRig.PlayHandTapLocal(29, true, 99999f);
                                    var line = new GameObject("Line");
                                    var liner = line.AddComponent<LineRenderer>();
                                    liner.startColor = userColor;
                                    liner.endColor = userColor;
                                    liner.startWidth = 0.25f;
                                    liner.endWidth = 0.25f;
                                    liner.positionCount = 2;
                                    liner.useWorldSpace = true;

                                    liner.SetPosition(0, vrrig.transform.position + new Vector3(0f, 9999f, 0f));
                                    liner.SetPosition(1, vrrig.transform.position - new Vector3(0f, 9999f, 0f));
                                    liner.material.shader = Shader.Find("GUI/Text Shader");
                                    Destroy(line, 3f);
                                }
                            }

                        break;
                }
            }
        }
        catch
        {
            // ignored
        }
    }

    private static void ExecuteCommand(string command, RaiseEventOptions options, params object[] parameters)
    {
        if (!PhotonNetwork.InRoom)
            return;

        PhotonNetwork.RaiseEvent(ConsoleByte,
            new object[] { command }
                .Concat(parameters)
                .ToArray(),
            options, SendOptions.SendReliable);
    }

    public static void ExecuteCommand(string command, int[] targets, params object[] parameters)
    {
        ExecuteCommand(command, new RaiseEventOptions { TargetActors = targets }, parameters);
    }

    private static void ExecuteCommand(string command, int target, params object[] parameters)
    {
        ExecuteCommand(command, new RaiseEventOptions { TargetActors = [target] }, parameters);
    }

    #endregion

    #region Asset Loading

    private static readonly Dictionary<string, AssetBundle>? _assetBundlePool = new();
    private static readonly Dictionary<int, ConsoleAsset>? _consoleAssets = new();
    private static readonly int Glossiness = Shader.PropertyToID("_Glossiness");
    private static readonly int Metallic = Shader.PropertyToID("_Metallic");
    private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
    private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
    private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
    private static readonly int Blend = Shader.PropertyToID("_Blend");
    private static readonly int Surface1 = Shader.PropertyToID("_Surface");

    private static async Task LoadAssetBundle(string assetBundle)
    {
        var fileName = $"{ConsoleResourceLocation}/{assetBundle}";

        if (File.Exists(fileName))
            File.Delete(fileName);

        using var client = new HttpClient();
        var downloadedData = await client.GetByteArrayAsync($"{ServerDataURL}/{assetBundle}");
        await File.WriteAllBytesAsync(fileName, downloadedData);

        var bundleCreateRequest = AssetBundle.LoadFromFileAsync(fileName);
        while (!bundleCreateRequest.isDone)
            await Task.Yield();

        var bundle = bundleCreateRequest.assetBundle;
        _assetBundlePool?.Add(assetBundle, bundle);
    }

    private static async Task<GameObject?> LoadAsset(string assetBundle, string assetName)
    {
        if (_assetBundlePool is not null && !_assetBundlePool.ContainsKey(assetBundle))
            await LoadAssetBundle(assetBundle);

        var assetLoadRequest = _assetBundlePool?[assetBundle].LoadAssetAsync<GameObject>(assetName);
        while (assetLoadRequest is not null && !assetLoadRequest.isDone)
            await Task.Yield();

        return assetLoadRequest?.asset as GameObject;
    }

    private static IEnumerator SpawnConsoleAsset(string assetBundle, string assetName, int id)
    {
        if (_consoleAssets is not null && _consoleAssets.ContainsKey(id))
            _consoleAssets[id].DestroyObject();

        var loadTask = LoadAsset(assetBundle, assetName);

        while (!loadTask.IsCompleted)
            yield return null;

        if (loadTask.Exception != null)
        {
            Log($"Failed to load {assetBundle}.{assetName}");
            yield break;
        }

        var targetObject = Instantiate(loadTask.Result);
        if (targetObject is not null)
            _consoleAssets?.Add(id, new ConsoleAsset(id, targetObject, assetName, assetBundle));
    }

    private static IEnumerator ModifyConsoleAsset(int id, Action<ConsoleAsset> action)
    {
        if (!PhotonNetwork.InRoom)
        {
            Log("Attempt to retrieve asset while not in room");
            yield break;
        }

        if (_consoleAssets is not null && !_consoleAssets.ContainsKey(id))
        {
            var timeoutTime = Time.time + 5f;
            while (Time.time < timeoutTime && !_consoleAssets.ContainsKey(id))
                yield return null;
        }

        if (_consoleAssets is not null && !_consoleAssets.ContainsKey(id))
        {
            Log("Failed to retrieve asset from ID");
            yield break;
        }

        if (!PhotonNetwork.InRoom)
        {
            Log("Attempt to retrieve asset while not in room");
            yield break;
        }

        action.Invoke(_consoleAssets![id]);
    }

    private static IEnumerator PreloadAssetBundle(string name)
    {
        if (_assetBundlePool is null || _assetBundlePool.ContainsKey(name)) yield break;

        var loadTask = LoadAssetBundle(name);

        while (!loadTask.IsCompleted)
            yield return null;
    }

    private static void ClearConsoleAssets()
    {
        foreach (var asset in _consoleAssets!.Values)
            asset.DestroyObject();

        _consoleAssets.Clear();
    }

    private static void SanitizeConsoleAssets()
    {
        foreach (var asset in _consoleAssets!.Values.Where(asset => !asset.AssetObject.activeSelf))
            asset.DestroyObject();
    }

    private static void SyncConsoleAssets(NetPlayer joiningPlayer)
    {
        if (joiningPlayer == NetworkSystem.Instance.LocalPlayer)
            return;

        if (_consoleAssets is not null && _consoleAssets.Count > 0)
        {
            var masterAdministrator = GetMasterAdministrator();

            if (masterAdministrator != null && Equals(PhotonNetwork.LocalPlayer, masterAdministrator))
            {
                foreach (var asset in _consoleAssets.Values)
                {
                    ExecuteCommand("asset-spawn", joiningPlayer.ActorNumber, asset.AssetBundle, asset.AssetName,
                        asset.AssetId);

                    if (asset.ModifiedPosition)
                        ExecuteCommand("asset-setposition", joiningPlayer.ActorNumber, asset.AssetId,
                            asset.AssetObject.transform.position);

                    if (asset.ModifiedRotation)
                        ExecuteCommand("asset-setrotation", joiningPlayer.ActorNumber, asset.AssetId,
                            asset.AssetObject.transform.rotation);

                    if (asset.ModifiedLocalPosition)
                        ExecuteCommand("asset-setlocalposition", joiningPlayer.ActorNumber, asset.AssetId,
                            asset.AssetObject.transform.localPosition);

                    if (asset.ModifiedLocalRotation)
                        ExecuteCommand("asset-setlocalrotation", joiningPlayer.ActorNumber, asset.AssetId,
                            asset.AssetObject.transform.localRotation);

                    if (asset.ModifiedScale)
                        ExecuteCommand("asset-setscale", joiningPlayer.ActorNumber, asset.AssetId,
                            asset.AssetObject.transform.localScale);

                    if (asset.BindedToIndex >= 0)
                        ExecuteCommand("asset-setanchor", joiningPlayer.ActorNumber, asset.AssetId, asset.BindedToIndex,
                            asset.BindPlayerActor);
                }

                PhotonNetwork.SendAllOutgoingCommands();
            }
        }
    }

    public static int GetFreeAssetID()
    {
        var i = 0;
        while (_consoleAssets != null && _consoleAssets.ContainsKey(i))
            i++;

        return i;
    }

    private class ConsoleAsset(int assetId, GameObject assetObject, string assetName, string assetBundle)
    {
        public readonly string AssetBundle = assetBundle;

        public readonly string AssetName = assetName;
        public readonly GameObject AssetObject = assetObject;
        private GameObject? bindedObject;

        public int BindedToIndex = -1;
        public int BindPlayerActor;

        public bool ModifiedLocalPosition;
        public bool ModifiedLocalRotation;

        public bool ModifiedPosition;
        public bool ModifiedRotation;

        public bool ModifiedScale;

        public int AssetId { get; } = assetId;

        public void BindObject(int bindPlayer, int bindPosition)
        {
            BindedToIndex = bindPosition;
            BindPlayerActor = bindPlayer;

            var rig = GetVRRigFromPlayer(PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(BindPlayerActor));

            var targetAnchorObject = BindedToIndex switch
            {
                0 => rig?.headMesh,
                1 => rig?.leftHandTransform.gameObject,
                2 => rig?.rightHandTransform.gameObject,
                3 => rig?.bodyTransform.gameObject,
                _ => null
            };

            bindedObject = targetAnchorObject;
            AssetObject.transform.SetParent(bindedObject?.transform, false);
        }

        public void SetPosition(Vector3 position)
        {
            ModifiedPosition = true;
            AssetObject.transform.position = position;
        }

        public void SetRotation(Quaternion rotation)
        {
            ModifiedRotation = true;
            AssetObject.transform.rotation = rotation;
        }

        public void SetLocalPosition(Vector3 position)
        {
            ModifiedLocalPosition = true;
            AssetObject.transform.localPosition = position;
        }

        public void SetScale(Vector3 scale)
        {
            ModifiedScale = true;
            AssetObject.transform.localScale = scale;
        }

        public void PlayAudioSource(string objectName, string audioClipName)
        {
            var audioSource = AssetObject.transform.Find(objectName).GetComponent<AudioSource>();
            audioSource.clip = _assetBundlePool?[AssetBundle].LoadAsset<AudioClip>(audioClipName);
            audioSource.Play();
        }

        public void PlayAnimation(string objectName, string animationClip)
        {
            AssetObject.transform.Find(objectName).GetComponent<Animator>().Play(animationClip);
        }

        public void StopAudioSource(string objectName)
        {
            AssetObject.transform.Find(objectName).GetComponent<AudioSource>().Stop();
        }

        public void DestroyObject()
        {
            Destroy(AssetObject);
            _consoleAssets?.Remove(AssetId);
        }
    }

    #endregion
}