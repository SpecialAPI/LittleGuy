using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using System.Reflection.Emit;
using System.Collections;
using UnityEngine;

namespace LittleGuy
{
    [HarmonyPatch]
    public class TheBosscardPatch
    {
        public static Type coopslidetype = AccessTools.TypeByName("BossCardUIController+<HandleCharacterSlides>c__Iterator5");
        public static MethodInfo visiblethingy = AccessTools.PropertySetter(typeof(dfControl), nameof(dfControl.IsVisible));
        public static MethodInfo changebosscard = AccessTools.Method(typeof(TheBosscardPatch), nameof(MaybeChangeCoopBosscard));
        public static FieldInfo thisincoopslide = AccessTools.Field(coopslidetype, "$this");

        [HarmonyPatch]
        public static class ThePatchThatNeedsATranspiler
        {
            [HarmonyTargetMethod]
            public static MethodBase Method()
            {
                return AccessTools.Method(coopslidetype, "MoveNext");
            }

            [HarmonyPatch]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> AddTheStuff(IEnumerable<CodeInstruction> instructions)
            {
                int encounteredvisiblecount = 0;
                foreach (var i in instructions)
                {
                    yield return i;
                    if (i.Calls(visiblethingy))
                    {
                        encounteredvisiblecount++;
                        if (encounteredvisiblecount == 2)
                        {
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, thisincoopslide);
                            yield return new CodeInstruction(OpCodes.Call, changebosscard);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SpawnObjectPlayerItem), nameof(SpawnObjectPlayerItem.DoSpawn))]
        [HarmonyPostfix]
        public static void ThisIsTechnicallyNotABosscardPatchButShhhhh(SpawnObjectPlayerItem __instance)
        {
            if(__instance.PickupObjectId == 155 && __instance.LastOwner != null && __instance.LastOwner.HasActiveBonusSynergy(Plugin.notEnoughSynergy) && __instance.spawnedPlayerObject != null && __instance.spawnedPlayerObject.GetComponent<BlackHoleDoer>() != null)
            {
                __instance.spawnedPlayerObject.GetComponent<BlackHoleDoer>().coreDuration += 2f;
            }
        }

        [HarmonyPatch(typeof(CompanionController), nameof(CompanionController.StopPet))]
        [HarmonyPrefix]
        public static void AnotherNonBosscardPatch(CompanionController __instance)
        {
            if(__instance.m_pettingDoer != null && __instance.aiActor != null && (__instance.aiActor.EnemyGuid == "lg_lilguy" || __instance.aiActor.EnemyGuid == "lg_lilgal"))
            {
                __instance.aiAnimator.PlayUntilFinished("pet_end");
            }
        }

        public static void MaybeChangeCoopBosscard(BossCardUIController ui)
        {
            if (HasLittleGuy(out var gal))
            {
                var vismain = ui.playerSprite.IsVisible;
                var viscoop = ui.coopSprite.IsVisible;
                if (vismain && viscoop)
                {
                    return;
                }
                dfTextureSprite turnThisIntoLilGuy;
                if (vismain)
                {
                    turnThisIntoLilGuy = ui.coopSprite;
                }
                else
                {
                    turnThisIntoLilGuy = ui.playerSprite;
                }
                turnThisIntoLilGuy.texture = gal ? Plugin.lilguybosscard2 : Plugin.lilguybosscard;
                turnThisIntoLilGuy.IsVisible = true;
                turnThisIntoLilGuy.ZOrder = ui.bossSprite.ZOrder + 1;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BossCardUIController), nameof(BossCardUIController.HandleDelayedCoopCharacterSlide))]
        public static IEnumerator Slide(IEnumerator rat, BossCardUIController __instance)
        {
            if(GameManager.Instance.CurrentGameType == 0 && HasLittleGuy(out _))
            {
                float initialMoveDuration = __instance.CHARACTER_INITIAL_MOVE_DURATION;
                float slideDuration = __instance.CHARACTER_SLIDE_DURATION - __instance.CHARACTER_INITIAL_MOVE_DURATION;
                float elapsed2 = 0f;
                Vector3 playerVec = __instance.playerTarget.position - __instance.playerStart.position;
                __instance.coopSprite.transform.position = __instance.playerStart.position + __instance.GetCoopOffset();
                float p2u = __instance.playerSprite.PixelsToUnits();
                while (elapsed2 < initialMoveDuration)
                {
                    elapsed2 += __instance.m_deltaTime;
                    Vector3 calcedPlayerPos = Vector3.Lerp(t: elapsed2 / initialMoveDuration, a: __instance.playerStart.position, b: __instance.playerTarget.position);
                    __instance.coopSprite.transform.position = calcedPlayerPos + __instance.GetCoopOffset();
                    yield return null;
                }
                elapsed2 = 0f;
                Vector3 currentRealPlayerPosition = __instance.playerTarget.position;
                while (elapsed2 < slideDuration)
                {
                    elapsed2 += __instance.m_deltaTime;
                    currentRealPlayerPosition += playerVec.normalized * __instance.m_deltaTime * __instance.BOSS_SLIDE_SPEED;
                    __instance.coopSprite.transform.position = currentRealPlayerPosition.Quantize(p2u) + __instance.GetCoopOffset();
                    yield return null;
                }
            }
            else
            {
                yield return rat;
            }
        }

        public static bool HasLittleGuy(out bool hasgal)
        {
            if(GameManager.HasInstance && GameManager.Instance.AllPlayers != null)
            {
                foreach(var p in GameManager.Instance.AllPlayers)
                {
                    if(p != null && p.healthHaver != null && !p.healthHaver.IsDead)
                    {
                        foreach(var i in p.passiveItems)
                        {
                            if(i != null && i is StrangeRoot ci)
                            {
                                var c = ci.ExtantCompanion;
                                if(c != null && c.GetComponent<AIActor>() != null && c.GetComponent<AIActor>().EnemyGuid == "lg_lilguy")
                                {
                                    hasgal = ci.littlegal != null;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            hasgal = false;
            return false;
        }
    }
}
