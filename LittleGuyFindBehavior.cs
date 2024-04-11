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
			if (m_aiActor.CompanionOwner != null)
			{
				m_aiActor.CompanionOwner.OnRoomClearEvent += HandleRoomCleared;
			}
		}

		public override void Destroy()
		{
			if (m_aiActor.CompanionOwner != null)
			{
				m_aiActor.CompanionOwner.OnRoomClearEvent -= HandleRoomCleared;
			}
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
				{
					flag = true;
				}
				List<WeightedGameObject> compiledRawItems = tableToUse.GetCompiledRawItems();
				List<KeyValuePair<WeightedGameObject, float>> list = new List<KeyValuePair<WeightedGameObject, float>>();
				float num = 0f;
				List<KeyValuePair<WeightedGameObject, float>> list2 = new List<KeyValuePair<WeightedGameObject, float>>();
				float num2 = 0f;
				for (int i = 0; i < compiledRawItems.Count; i++)
				{
					if (!(compiledRawItems[i].gameObject != null))
					{
						continue;
					}
					PickupObject component = compiledRawItems[i].gameObject.GetComponent<PickupObject>();
					if (component == null || (bossStyle && component is GungeonMapItem))
					{
						continue;
					}
					if(itemselect != null && !itemselect.Invoke(component))
                    {
						continue;
                    }
					bool flag2 = RewardManager.CheckQualityForItem(component, player, targetQuality, forceSynergyCompletion, rewardSource);
					if ((component.ItemSpansBaseQualityTiers || component.ItemRespectsHeartMagnificence) && targetQuality != PickupObject.ItemQuality.D && targetQuality != 0 && targetQuality != PickupObject.ItemQuality.S)
					{
						flag2 = true;
					}
					if (!ignorePlayerTraits && component is SpiceItem && (bool)player && player.spiceCount > 0)
					{
						Debug.Log("BAM spicing it up");
						flag2 = true;
					}
					if (!(component != null) || !flag2)
					{
						continue;
					}
					bool flag3 = true;
					float num3 = compiledRawItems[i].weight;
					if (excludedObjects != null && excludedObjects.Contains(component.gameObject))
					{
						continue;
					}
					if (additionalExcludedObjects != null && additionalExcludedObjects.Contains(component.gameObject))
					{
						continue;
					}
					if (!component.PrerequisitesMet())
					{
						flag3 = false;
					}
					if (component is Gun)
					{
						Gun gun = component as Gun;
						if (gun.InfiniteAmmo && !gun.CanBeDropped && gun.quality == PickupObject.ItemQuality.SPECIAL)
						{
							continue;
						}
						GunClass gunClass = gun.gunClass;
						if (!ignorePlayerTraits && gunClass != 0)
						{
							int num4 = ((!(player == null) && player.inventory != null) ? player.inventory.ContainsGunOfClass(gunClass, respectsOverrides: true) : 0);
							float modifierForClass = LootDataGlobalSettings.Instance.GetModifierForClass(gunClass);
							num3 *= Mathf.Pow(modifierForClass, num4);
						}
					}
					if (!ignorePlayerTraits)
					{
						float multiplierForItem = RewardManager.GetMultiplierForItem(component, player, forceSynergyCompletion);
						num3 *= multiplierForItem;
					}
					bool flag4 = !GameManager.Instance.IsSeeded;
					EncounterTrackable component2 = component.GetComponent<EncounterTrackable>();
					if (component2 != null && flag4)
					{
						int num5 = 0;
						if (Application.isPlaying)
						{
							num5 = GameStatsManager.Instance.QueryEncounterableDifferentiator(component2);
						}
						if (num5 > 0 || (Application.isPlaying && GameManager.Instance.ExtantShopTrackableGuids.Contains(component2.EncounterGuid)))
						{
							flag3 = false;
							num2 += num3;
							KeyValuePair<WeightedGameObject, float> item = new KeyValuePair<WeightedGameObject, float>(compiledRawItems[i], num3);
							list2.Add(item);
						}
						else if (Application.isPlaying && GameStatsManager.Instance.QueryEncounterable(component2) == 0 && GameStatsManager.Instance.QueryEncounterableAnnouncement(component2.EncounterGuid))
						{
							num3 *= 10f;
						}
					}
					if (component.ItemSpansBaseQualityTiers || component.ItemRespectsHeartMagnificence)
					{
						if (RewardManager.AdditionalHeartTierMagnificence >= 3f)
						{
							num3 *= GameManager.Instance.RewardManager.ThreeOrMoreHeartMagMultiplier;
						}
						else if (RewardManager.AdditionalHeartTierMagnificence >= 1f)
						{
							num3 *= GameManager.Instance.RewardManager.OneOrTwoHeartMagMultiplier;
						}
					}
					if (flag3)
					{
						num += num3;
						KeyValuePair<WeightedGameObject, float> item2 = new KeyValuePair<WeightedGameObject, float>(compiledRawItems[i], num3);
						list.Add(item2);
					}
				}
				if (list.Count == 0 && list2.Count > 0)
				{
					list = list2;
					num = num2;
				}
				if (num > 0f && list.Count > 0)
				{
					float num6;
					if (ignorePlayerTraits)
					{
						float num7 = (float)safeRandom.NextDouble();
						Debug.LogError("safe random: " + num7);
						num6 = num * num7;
					}
					else
					{
						num6 = num * UnityEngine.Random.value;
					}
					for (int j = 0; j < list.Count; j++)
					{
						num6 -= list[j].Value;
						if (num6 <= 0f)
						{
							return list[j].Key.gameObject;
						}
					}
					return list[list.Count - 1].Key.gameObject;
				}
				targetQuality--;
				if (targetQuality < PickupObject.ItemQuality.COMMON && !flag)
				{
					targetQuality = PickupObject.ItemQuality.D;
				}
			}
			return null;
		}

		private void HandleRoomCleared(PlayerController obj)
        {
			var actualFailedAttempts = failedAttempts;

			if (PassiveItem.IsFlagSetAtAll(typeof(BattleStandardItem)))
			{
				actualFailedAttempts += 15;
			}

			if(obj != null && obj.inventory != null && obj.inventory.AllGuns != null && obj.inventory.AllGuns.Exists(x => x.IsLuteCompanionBuff))
			{
				actualFailedAttempts += 15;
			}

            if (UnityEngine.Random.value * Mathf.Pow(chanceMult, actualFailedAttempts) < (m_aiActor.CompanionOwner.HasActiveBonusSynergy(Plugin.heavyweightSynergy) ? FindChanceSynergy : FindChance))
            {
				if(m_aiActor.transform.position.GetAbsoluteRoom() != m_aiActor.CompanionOwner.CurrentRoom || m_aiAnimator.IsPlaying("sleep"))
                {
					m_aiActor.CompanionWarp(m_aiActor.CompanionOwner.CenterPosition);
                }
                var val = UnityEngine.Random.value;
                var quality = PickupObject.ItemQuality.D;
				var hassynergy = m_aiActor.CompanionOwner.HasActiveBonusSynergy(Plugin.rareLootSynergy);
				var hightiermult = 1f;
                if (hassynergy)
                {
					hightiermult = HighTierSynergyMult;
				}
				if (val < SChance * hightiermult)
                {
                    quality = PickupObject.ItemQuality.S;
                }
                else if (val < SChance * hightiermult + AChance * hightiermult)
                {
                    quality = PickupObject.ItemQuality.A;
                }
                else if (val < SChance * hightiermult + AChance * hightiermult + BChance * hightiermult)
                {
                    quality = PickupObject.ItemQuality.B;
                }
                else if (val < SChance * hightiermult + AChance * hightiermult + BChance * hightiermult + CChance * (hassynergy ? CTierSynergyMult : 1f))
                {
                    quality = PickupObject.ItemQuality.C;
                }
                m_findTimer = 2.6f;
                m_aiAnimator.PlayUntilFinished("find");
                var item = GetItemForPlayer(m_aiActor.CompanionOwner, GameManager.Instance.RewardManager.ItemsLootTable, quality, null, x => x is PlayerItem);
                GameManager.Instance.Dungeon.StartCoroutine(DelayedSpawnItem(m_aiActor.CenterPosition, item));
				failedAttempts = 0;
				AkSoundEngine.PostEvent("Play_PET_dog_bark_01", m_aiActor.gameObject);
            }
            else
            {
				failedAttempts++;
            }
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
            {
				m_aiActor.ClearPath();
            }
			else if((!m_aiAnimator.IsPlaying("idle_right") && !m_aiAnimator.IsPlaying("idle_left") && !m_aiAnimator.IsPlaying("sleep")) || m_aiActor.CompanionOwner.IsInCombat)
            {
				idleTimer = 0f;
				hadSleptThisIdle = false;
            }
            if (m_aiAnimator.IsPlaying("pet_end"))
            {
				m_aiActor.ClearPath();
            }
			return base.Update();
		}
	}
}
