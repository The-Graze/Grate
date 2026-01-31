using UnityEngine;
using UnityEngine;
﻿using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace Grate.Patches;

public class NetworkStatePatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
            if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 10f)
            {
                Console.WriteLine("Shortening state stuck time");
                yield return new CodeInstruction(OpCodes.Ldc_R4, 1f); // Replace 10f with 1f
            }
            else
            {
                yield return instruction;
            }
    }
}