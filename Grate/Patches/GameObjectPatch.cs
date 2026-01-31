using UnityEngine;
using UnityEngine;
using HarmonyLib;

namespace Grate.Patches;

[HarmonyPatch(typeof(GameObject))]
[HarmonyPatch(nameof(GameObject.CreatePrimitive), MethodType.Normal)]
internal class GameObjectPatch
{
    private static void Postfix(GameObject __result)
    {
        __result.GetComponent<Renderer>().material.shader = Shader.Find("GorillaTag/UberShader");
        __result.GetComponent<Renderer>().material.color = Color.white;
    }
}