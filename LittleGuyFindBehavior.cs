using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LittleGuy
{
    public class LittleGuyFindBehavior : BehaviorBase
    {
        private float m_findTimer;
        private float idleTimer;
        private bool hadSleptThisIdle;

        public float FindChance = 0.0475f;
        public float FindChanceSynergy = 0.07f;

        public float CChance = 0.189f;
        public float BChance = 0.063f;
        public float AChance = 0.017f;
        public float SChance = 0.015f;

        public float chanceMult = 0.95f;

        public float HighTierSynergyMult = 7;
        public float CTierSynergyMult = 0.37f;

        private int failedAttempts;

        public override void Start()
        {
            base.Start();

            if (m_aiActor.CompanionOwner == null)
                return;

            m_aiActor.CompanionOwner.OnRoomClearEvent += HandleRoomCleared;
        }

        public override void Destroy()
        {
            if (m_aiActor.CompanionOwner != null)
                m_aiActor.CompanionOwner.OnRoomClearEvent -= HandleRoomCleared;

            base.Destroy();
        }

        private IEnumerator DelayedSpawnItem(Vector2 spawnPoint, GameObject item)
        {
            yield return new WaitForSeconds(2.84f);
            LootEngine.SpawnItem(item, spawnPoint, Vector2.down, 1f);
        }

        public GameObject GetItemForPlayer(PlayerController player, GenericLootTable tableToUse, PickupObject.ItemQuality targetQuality, List<GameObject> excludedObjects, Predicate<PickupObject> itemselect, bool ignorePlayerTraits = false, System.Random safeRandom = null, bool bossStyle = false, List<GameObject> additionalExcludedObjects = null, bool forceSynergyCompletion = false, RewardManager.RewardSource rewardSource = RewardManager.RewardSource.UNSPECIFIED)
        {
            bool flag = false;

            while (targetQuality >= PickupObject.ItemQuality.COMMON)
            {
                if (targetQuality > PickupObject.ItemQuality.COMMON)
                    flag = true;

                var compiledRawItems = tableToUse.GetCompiledRawItems();

                var itemsAndWeights = new List<KeyValuePair<WeightedGameObject, float>>();
                var totalWeight = 0f;

                var itemsAndWeights2 = new List<KeyValuePair<WeightedGameObject, float>>();
                var totalWeight2 = 0f;

                for (int i = 0; i < compiledRawItems.Count; i++)
                {
                    if (compiledRawItems[i].gameObject == null)
                        continue;

                    var item = compiledRawItems[i].gameObject.GetComponent<PickupObject>();

                    if (item == null || (bossStyle && item is GungeonMapItem))
                        continue;

                    if (itemselect != null && !itemselect.Invoke(item))
                        continue;

                    var matchQuality = RewardManager.CheckQualityForItem(item, player, targetQuality, forceSynergyCompletion, rewardSource);

                    if ((item.ItemSpansBaseQualityTiers || item.ItemRespectsHeartMagnificence) && targetQuality != PickupObject.ItemQuality.D && targetQuality != PickupObject.ItemQuality.COMMON && targetQuality != PickupObject.ItemQuality.S)
                        matchQuality = true;

                    if (!ignorePlayerTraits && item is SpiceItem && player && player.spiceCount > 0)
                        matchQuality = true;

                    if (item == null || !matchQuality)
                        continue;

                    var addWeight = true;
                    var weight = compiledRawItems[i].weight;

                    if (excludedObjects != null && excludedObjects.Contains(item.gameObject))
                        continue;

                    if (additionalExcludedObjects != null && additionalExcludedObjects.Contains(item.gameObject))
                        continue;

                    if (!item.PrerequisitesMet())
                        addWeight = false;

                    if (item is Gun gun)
                    {
                        if (gun.InfiniteAmmo && !gun.CanBeDropped && gun.quality == PickupObject.ItemQuality.SPECIAL)
                            continue;

                        var gunClass = gun.gunClass;

                        if (!ignorePlayerTraits && gunClass != 0)
                        {
                            var numGunsOfClass = (player != null && player.inventory != null) ? player.inventory.ContainsGunOfClass(gunClass, true) : 0;
                            var modifierForClass = LootDataGlobalSettings.Instance.GetModifierForClass(gunClass);

                            weight *= Mathf.Pow(modifierForClass, numGunsOfClass);
                        }
                    }

                    if (!ignorePlayerTraits)
                    {
                        var multiplierForItem = RewardManager.GetMultiplierForItem(item, player, forceSynergyCompletion);
                        weight *= multiplierForItem;
                    }

                    var unseeded = !GameManager.Instance.IsSeeded;
                    var trackable = item.GetComponent<EncounterTrackable>();

                    if (trackable != null && unseeded)
                    {
                        var differentiator = GameStatsManager.Instance.QueryEncounterableDifferentiator(trackable);

                        if (differentiator > 0 || GameManager.Instance.ExtantShopTrackableGuids.Contains(trackable.EncounterGuid))
                        {
                            addWeight = false;
                            totalWeight2 += weight;

                            var itemAndWeight2 = new KeyValuePair<WeightedGameObject, float>(compiledRawItems[i], weight);
                            itemsAndWeights2.Add(itemAndWeight2);
                        }

                        else if (GameStatsManager.Instance.QueryEncounterable(trackable) == 0 && GameStatsManager.Instance.QueryEncounterableAnnouncement(trackable.EncounterGuid))
                            weight *= 10f;
                    }

                    if (item.ItemSpansBaseQualityTiers || item.ItemRespectsHeartMagnificence)
                    {
                        if (RewardManager.AdditionalHeartTierMagnificence >= 3f)
                            weight *= GameManager.Instance.RewardManager.ThreeOrMoreHeartMagMultiplier;

                        else if (RewardManager.AdditionalHeartTierMagnificence >= 1f)
                            weight *= GameManager.Instance.RewardManager.OneOrTwoHeartMagMultiplier;
                    }

                    if (!addWeight)
                        continue;

                    totalWeight += weight;

                    var itemAndWeight = new KeyValuePair<WeightedGameObject, float>(compiledRawItems[i], weight);
                    itemsAndWeights.Add(itemAndWeight);
                }

                if (itemsAndWeights.Count == 0 && itemsAndWeights2.Count > 0)
                {
                    itemsAndWeights = itemsAndWeights2;
                    totalWeight = totalWeight2;
                }

                if (totalWeight > 0f && itemsAndWeights.Count > 0)
                {
                    var rngMult = ignorePlayerTraits ? (float)safeRandom.NextDouble() : UnityEngine.Random.value;
                    var target = totalWeight * rngMult;

                    for (int j = 0; j < itemsAndWeights.Count; j++)
                    {
                        target -= itemsAndWeights[j].Value;

                        if (target <= 0f)
                            return itemsAndWeights[j].Key.gameObject;
                    }

                    return itemsAndWeights[itemsAndWeights.Count - 1].Key.gameObject;
                }

                targetQuality--;

                if (targetQuality < PickupObject.ItemQuality.COMMON && !flag)
                    targetQuality = PickupObject.ItemQuality.D;
            }

            return null;
        }

        private void HandleRoomCleared(PlayerController obj)
        {
            var actualFailedAttempts = failedAttempts;

            if (PassiveItem.IsFlagSetAtAll(typeof(BattleStandardItem)))
                actualFailedAttempts += 15;

            if(obj != null && obj.inventory != null && obj.inventory.AllGuns != null && obj.inventory.AllGuns.Exists(x => x.IsLuteCompanionBuff))
                actualFailedAttempts += 15;

            if (UnityEngine.Random.value * Mathf.Pow(chanceMult, actualFailedAttempts) >= (m_aiActor.CompanionOwner.HasActiveBonusSynergy(Plugin.heavyweightSynergy) ? FindChanceSynergy : FindChance))
            {
                failedAttempts++;
                return;
            }

            if (m_aiActor.transform.position.GetAbsoluteRoom() != m_aiActor.CompanionOwner.CurrentRoom || m_aiAnimator.IsPlaying("sleep"))
                m_aiActor.CompanionWarp(m_aiActor.CompanionOwner.CenterPosition);

            var val = UnityEngine.Random.value;
            var quality = PickupObject.ItemQuality.D;
            var hassynergy = m_aiActor.CompanionOwner.HasActiveBonusSynergy(Plugin.rareLootSynergy);
            var hightiermult = 1f;

            if (hassynergy)
                hightiermult = HighTierSynergyMult;

            if (val < SChance * hightiermult)
                quality = PickupObject.ItemQuality.S;

            else if (val < SChance * hightiermult + AChance * hightiermult)
                quality = PickupObject.ItemQuality.A;

            else if (val < SChance * hightiermult + AChance * hightiermult + BChance * hightiermult)
                quality = PickupObject.ItemQuality.B;

            else if (val < SChance * hightiermult + AChance * hightiermult + BChance * hightiermult + CChance * (hassynergy ? CTierSynergyMult : 1f))
                quality = PickupObject.ItemQuality.C;

            m_findTimer = 2.6f;
            m_aiAnimator.PlayUntilFinished("find");

            var item = GetItemForPlayer(m_aiActor.CompanionOwner, GameManager.Instance.RewardManager.ItemsLootTable, quality, null, x => x is PlayerItem);
            GameManager.Instance.Dungeon.StartCoroutine(DelayedSpawnItem(m_aiActor.CenterPosition, item));

            failedAttempts = 0;

            AkSoundEngine.PostEvent("Play_PET_dog_bark_01", m_aiActor.gameObject);
        }

        public override BehaviorResult Update()
        {
            if (m_findTimer > 0f)
            {
                DecrementTimer(ref m_findTimer);
                m_aiActor.ClearPath();
            }

            if ((m_aiAnimator.IsPlaying("idle_right") || m_aiAnimator.IsPlaying("idle_left")) && !m_aiAnimator.IsPlaying("sleep") && !hadSleptThisIdle && !m_aiActor.CompanionOwner.IsInCombat)
            {
                idleTimer += m_deltaTime;
                if(idleTimer > 8f)
                {
                    m_aiAnimator.PlayUntilFinished("sleep");
                    hadSleptThisIdle = true;
                }
            }

            else if (m_aiAnimator.IsPlaying("sleep"))
                m_aiActor.ClearPath();

            else if((!m_aiAnimator.IsPlaying("idle_right") && !m_aiAnimator.IsPlaying("idle_left") && !m_aiAnimator.IsPlaying("sleep")) || m_aiActor.CompanionOwner.IsInCombat)
            {
                idleTimer = 0f;
                hadSleptThisIdle = false;
            }

            if (m_aiAnimator.IsPlaying("pet_end"))
                m_aiActor.ClearPath();

            return base.Update();
        }
    }
}
