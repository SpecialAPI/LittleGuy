using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using HarmonyLib;

namespace LittleGuy
{
    [HarmonyPatch]
    public class PetOffsetHolder : MonoBehaviour
    {
        [HarmonyPatch(typeof(CompanionController), nameof(CompanionController.DoPet))]
        [HarmonyPostfix]
        public static void ApplyOverridePetOffset_Postfix(CompanionController __instance, PlayerController player)
        {
            var hold = __instance.GetComponent<PetOffsetHolder>();
            if (hold == null)
                return;

            if (__instance.specRigidbody.UnitCenter.x > player.specRigidbody.UnitCenter.x)
                __instance.m_petOffset = hold.petOffsetRight;

            else
                __instance.m_petOffset = hold.petOffsetLeft;
        }

        public Vector2 petOffsetRight;
        public Vector2 petOffsetLeft;
    }
}
