using HarmonyLib;
using UnityEngine;

namespace Grate.Patches;

[HarmonyPatch(typeof(GameObject))]
[HarmonyPatch("CreatePrimitive", MethodType.Normal)]
internal class GameObjectPatch
{
    private static void Postfix(GameObject __result)
    {
        __result.GetComponent<Renderer>().material.shader = Shader.Find("GorillaTag/UberShader");
        __result.GetComponent<Renderer>().material.color = Color.grey;
    }
}