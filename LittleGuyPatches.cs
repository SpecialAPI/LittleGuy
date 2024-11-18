using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using System.Collections;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace LittleGuy
{
    [HarmonyPatch]
    public class LittleGuyPatches
    {
        public static MethodInfo ccbflg_s_ccb = AccessTools.Method(typeof(LittleGuyPatches), nameof(ChangeCoopBosscardForLittleGuy_Slides_ChangeCoopBosscard));
        public static MethodInfo ccbflg_v_ccb = AccessTools.Method(typeof(LittleGuyPatches), nameof(ChangeCoopBosscardForLittleGuy_Visibility_ChangeCoopBosscard));
        public static MethodInfo dcswlg_cgt = AccessTools.Method(typeof(LittleGuyPatches), nameof(DoCoopSlideWithLittleGuy_ChangeGameType));

        [HarmonyPatch(typeof(BossCardUIController), nameof(BossCardUIController.HandleCharacterSlides), MethodType.Enumerator)]
        [HarmonyILManipulator]
        public static void ChangeCoopBosscardForLittleGuy_Slides_Transpiler(ILContext ctx)
        {
            var crs = new ILCursor(ctx);

            if (!crs.JumpToNext(x => x.MatchCallOrCallvirt<dfControl>($"set_{nameof(dfControl.IsVisible)}"), 2))
                return;

            crs.Emit(OpCodes.Ldarg_0);
            crs.Emit(OpCodes.Call, ccbflg_s_ccb);
        }

        public static void ChangeCoopBosscardForLittleGuy_Slides_ChangeCoopBosscard(IEnumerator enumerator)
        {
            if (!HasLittleGuy(out var gal))
                return;

            var ui = enumerator.EnumeratorGetField<BossCardUIController>("$this");

            var vismain = ui.playerSprite.IsVisible;
            var viscoop = ui.coopSprite.IsVisible;

            if (vismain && viscoop)
                return;

            var lilGuyTargetSprite = vismain ? ui.coopSprite : ui.playerSprite;

            lilGuyTargetSprite.texture = gal ? Plugin.lilguybosscard2 : Plugin.lilguybosscard;
            lilGuyTargetSprite.IsVisible = true;
            lilGuyTargetSprite.ZOrder = ui.bossSprite.ZOrder + 1;
        }

        [HarmonyPatch(typeof(BossCardUIController), nameof(BossCardUIController.ToggleCoreVisiblity))]
        [HarmonyILManipulator]
        public static void ChangeCoopBosscardForLittleGuy_Visibility_Transpiler(ILContext ctx)
        {
            var crs = new ILCursor(ctx);

            if (!crs.JumpToNext(x => x.MatchCallOrCallvirt<dfControl>($"set_{nameof(dfControl.IsVisible)}"), 6))
                return;

            crs.Emit(OpCodes.Ldarg_0);
            crs.Emit(OpCodes.Call, ccbflg_v_ccb);
        }

        public static void ChangeCoopBosscardForLittleGuy_Visibility_ChangeCoopBosscard(BossCardUIController ui)
        {
            if (!HasLittleGuy(out var gal))
                return;

            var vismain = ui.playerSprite.IsVisible;
            var viscoop = ui.coopSprite.IsVisible;

            if (vismain && viscoop)
                return;

            var lilGuyTargetSprite = vismain ? ui.coopSprite : ui.playerSprite;

            lilGuyTargetSprite.texture = gal ? Plugin.lilguybosscard2 : Plugin.lilguybosscard;
            lilGuyTargetSprite.IsVisible = true;
            lilGuyTargetSprite.ZOrder = ui.bossSprite.ZOrder + 1;
        }

        [HarmonyPatch(typeof(SpawnObjectPlayerItem), nameof(SpawnObjectPlayerItem.DoSpawn))]
        [HarmonyPostfix]
        public static void NotEnoughSynergy_Postfix(SpawnObjectPlayerItem __instance)
        {
            if (__instance.PickupObjectId != 155)
                return;

            if (__instance.LastOwner == null || !__instance.LastOwner.HasActiveBonusSynergy(Plugin.notEnoughSynergy))
                return;

            if (__instance.spawnedPlayerObject == null || __instance.spawnedPlayerObject.GetComponent<BlackHoleDoer>() == null)
                return;

            __instance.spawnedPlayerObject.GetComponent<BlackHoleDoer>().coreDuration += 2f;
        }

        [HarmonyPatch(typeof(CompanionController), nameof(CompanionController.StopPet))]
        [HarmonyPrefix]
        public static void PlayPostPetAnim_Prefix(CompanionController __instance)
        {
            if (__instance.m_pettingDoer == null || __instance.aiActor == null)
                return;

            if (__instance.aiActor.EnemyGuid != "lg_lilguy" && __instance.aiActor.EnemyGuid != "lg_lilgal")
                return;

            __instance.aiAnimator.PlayUntilFinished("pet_end");
        }

        [HarmonyPatch(typeof(BossCardUIController), nameof(BossCardUIController.HandleDelayedCoopCharacterSlide), MethodType.Enumerator)]
        [HarmonyILManipulator]
        public static void DoCoopSlideWithLittleGuy_Transpiler(ILContext ctx)
        {
            var crs = new ILCursor(ctx);
            Debug.Log("agh");

            if (!crs.JumpToNext(x => x.MatchCallOrCallvirt<GameManager>($"get_{nameof(GameManager.CurrentGameType)}")))
                return;

            crs.Emit(OpCodes.Call, dcswlg_cgt);
            Debug.Log("im gonna kill you");
        }

        public static GameManager.GameType DoCoopSlideWithLittleGuy_ChangeGameType(GameManager.GameType curr)
        {
            Debug.Log("GRAAAAAAHHH");
            if (HasLittleGuy(out _))
            {
                Debug.Log("AAAAAAAAAAAAAAH");
                return GameManager.GameType.COOP_2_PLAYER;
            }

            Debug.Log("WERE ALL GONNA DIE");

            return curr;
        }

        public static bool HasLittleGuy(out bool hasgal)
        {
            hasgal = false;
            var hasguy = false;

            if (!GameManager.HasInstance || GameManager.Instance.AllPlayers == null)
                return false;

            foreach (var p in GameManager.Instance.AllPlayers)
            {
                if (p == null || p.healthHaver == null || p.healthHaver.IsDead)
                    continue;

                foreach (var i in p.passiveItems)
                {
                    if (i == null || i is not StrangeRoot ci)
                        continue;

                    var c = ci.ExtantCompanion;

                    if (c == null || c.GetComponent<AIActor>() == null || c.GetComponent<AIActor>().EnemyGuid != "lg_lilguy")
                        continue;

                    hasgal = ci.littlegal != null;
                    hasguy = true;

                    if (hasgal && hasguy)
                        return true;
                }
            }

            return hasguy;
        }
    }
}
