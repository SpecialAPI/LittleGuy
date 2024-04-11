using BepInEx;
using HarmonyLib;
using SGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LittleGuy
{
    [BepInPlugin(GUID, "Little Guy", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "spapi.etg.littleguy";
        public static AssetBundle bundle;
        public static Texture2D lilguybosscard;
        public static Texture2D lilguybosscard2;
        public static CustomSynergyType heavyweightSynergy;
        public static CustomSynergyType rareLootSynergy;
        public static CustomSynergyType notEnoughSynergy;
        public static CustomSynergyType wholeZooSynergy;
        public static GungeonFlags completedSinisterCountdown;

        public void Awake()
        {
            using(var strem = Assembly.GetExecutingAssembly().GetManifestResourceStream("LittleGuy.LittleGuy.Assets.AssetBundles.littleguybundle"))
            {
                bundle = AssetBundle.LoadFromStream(strem);
            }
            lilguybosscard = bundle.LoadAsset<Texture2D>("little_guy_bosscard_001");
            lilguybosscard2 = bundle.LoadAsset<Texture2D>("little_guy_and_gal_bosscard_001");
            ETGModMainBehaviour.WaitForGameManagerStart(GMStart);
            new Harmony(GUID).PatchAll();
        }

        public void GMStart(GameManager manager)
        {
            completedSinisterCountdown = ETGModCompatibility.ExtendEnum<GungeonFlags>(GUID, "SINISTER_COUNTDOWN_COMPLETE");
            var littleGalSeen = GameStatsManager.HasInstance && GameStatsManager.Instance.GetFlag(completedSinisterCountdown);

            StrangeRoot.Init();

            GameManager.Instance.SynergyManager.synergies.FirstOrDefault(x => x.bonusSynergies.Contains(CustomSynergyType.TEA_FOR_TWO)).OptionalItemIDs.Add(StrangeRoot.rootid);
            GameManager.Instance.SynergyManager.synergies.FirstOrDefault(x => x.bonusSynergies.Contains(CustomSynergyType.ELEPHANT_GUN)).OptionalItemIDs.Add(StrangeRoot.rootid);

            GameManager.Instance.SynergyManager.synergies = GameManager.Instance.SynergyManager.synergies.AddRangeToArray(new AdvancedSynergyEntry[]
            {
                new AdvancedSynergyEntry()
                {
                    NameKey = "#LG_HEAVYWEIGHT",
                    MandatoryItemIDs = new List<int>()
                    {
                        StrangeRoot.rootid
                    },
                    OptionalItemIDs = new List<int>()
                    {
                        133
                    },
                    OptionalGunIDs = new List<int>()
                    {
                        539
                    },
                    bonusSynergies = new List<CustomSynergyType>()
                    {
                        (heavyweightSynergy = ETGModCompatibility.ExtendEnum<CustomSynergyType>(GUID, "HEAVYWEIGHT"))
                    },
                    statModifiers = new List<StatModifier>()
                },
                new AdvancedSynergyEntry()
                {
                    NameKey = "#LG_RARELOOT",
                    MandatoryItemIDs = new List<int>()
                    {
                        StrangeRoot.rootid
                    },
                    OptionalItemIDs = new List<int>()
                    {
                        605,
                        289
                    },
                    bonusSynergies = new List<CustomSynergyType>()
                    {
                        (rareLootSynergy = ETGModCompatibility.ExtendEnum<CustomSynergyType>(GUID, "RARE_LOOT"))
                    },
                    statModifiers = new List<StatModifier>()
                },
                new AdvancedSynergyEntry()
                {
                    NameKey = "#LG_NOTENOUGHSPACE",
                    MandatoryItemIDs = new List<int>()
                    {
                        StrangeRoot.rootid
                    },
                    OptionalItemIDs = new List<int>()
                    {
                        155
                    },
                    OptionalGunIDs = new List<int>()
                    {
                        169
                    },
                    bonusSynergies = new List<CustomSynergyType>()
                    {
                        (notEnoughSynergy = ETGModCompatibility.ExtendEnum<CustomSynergyType>(GUID, "NOT_ENOUGH_SPACE"))
                    },
                    statModifiers = new List<StatModifier>()
                    {
                        StatModifier.Create(PlayerStats.StatType.AdditionalItemCapacity, StatModifier.ModifyMethod.ADDITIVE, 2)
                    }
                },
                new AdvancedSynergyEntry()
                {
                    NameKey = "#LG_WHOLEZOO",
                    MandatoryItemIDs = new List<int>()
                    {
                        StrangeRoot.rootid
                    },
                    OptionalGunIDs = new List<int>()
                    {
                        369,
                        176,
                        599,
                        406
                    },
                    bonusSynergies = new List<CustomSynergyType>()
                    {
                        (wholeZooSynergy = ETGModCompatibility.ExtendEnum<CustomSynergyType>(GUID, "WHOLE_ZOO"))
                    },
                    statModifiers = new List<StatModifier>()
                    {
                        StatModifier.Create(PlayerStats.StatType.Damage, StatModifier.ModifyMethod.MULTIPLICATIVE, 0.75f),
                        StatModifier.Create(PlayerStats.StatType.Accuracy, StatModifier.ModifyMethod.MULTIPLICATIVE, 1.1f)
                    },
                    ActiveWhenGunUnequipped = false
                }
            });

            ETGMod.Databases.Strings.Synergy.Set("#LG_HEAVYWEIGHT", "Heavy Weight");
            ETGMod.Databases.Strings.Synergy.Set("#LG_RARELOOT", "Rare Loot");
            ETGMod.Databases.Strings.Synergy.Set("#LG_NOTENOUGHSPACE", "Not Enough Space");
            ETGMod.Databases.Strings.Synergy.Set("#LG_WHOLEZOO", "Whole Zoo");

            PickupObjectDatabase.GetById(539).gameObject.AddComponent<HeavyWeightSynergyForBoxingGlove>();

            PickupObjectDatabase.GetById(369).gameObject.AddComponent<WholeZooSynergy>();
            PickupObjectDatabase.GetById(176).gameObject.AddComponent<WholeZooSynergy>();
            PickupObjectDatabase.GetById(599).gameObject.AddComponent<WholeZooSynergy>();
            PickupObjectDatabase.GetById(406).gameObject.AddComponent<WholeZooSynergy>();

            var txt = "Little Guy has successfully infiltrated the game.";

            if (littleGalSeen)
            {
                txt = "Little Guy (and Little Gal) have successfully infiltrated the game.";
            }

            var groupHeight = 48;
            var group = new SGroup() { Size = new Vector2(20000, groupHeight), AutoLayoutPadding = 0, Background = Color.clear, AutoLayout = x => x.AutoLayoutHorizontal };
            var scale = 0.3f;

            var color1 = new Color32(93, 174, 13, 255);
            var color2 = new Color32(177, 202, 19, 255);
            
            if(littleGalSeen)
            {
                color2 = new Color32(0, 140, 198, 255);
            }

            var grad = new Gradient()
            {
                colorKeys = new GradientColorKey[]
                {
                    new(color1, 0f),
                    new(color2, 0.5f),
                    new(color1, 1f)
                }
            };

            for(int i = 0; i < txt.Length; i++)
            {
                var c = txt[i];
                var color = grad.Evaluate((float)i / (txt.Length - 1) / 2f);

                if (c == ' ')
                {
                    group.Children.Add(new SRect(Color.clear) { Size = Vector2.one * 10 });
                }
                else
                {
                    group.Children.Add(new SLabel(c.ToString()) { Foreground = color, With = { new GradientThingy(grad, (float)i / (txt.Length - 1) / 2f) } });
                }
            }

            group.Children.Add(new SRect(Color.clear) { Size = Vector2.one * 10 });

            if (littleGalSeen)
            {
                var galTexture = bundle.LoadAsset<Texture2D>("gal");
                group.Children.Add(new SLabel() { Icon = galTexture, IconScale = Vector2.one * scale, With = { new MovementThingy(0.5f, 2.5f) } });
                group.Children.Add(new SRect(Color.clear) { Size = new(galTexture.width * scale, groupHeight) });
            }

            var guyTexture = bundle.LoadAsset<Texture2D>("guy");
            group.Children.Add(new SLabel() { Icon = guyTexture, IconScale = Vector2.one * scale, With = { new MovementThingy(0, 2.5f) } });
            group.Children.Add(new SRect(Color.clear) { Size = new(guyTexture.width * scale, groupHeight) });

            ETGModConsole.Instance.GUI[0].Children.Add(group);
        }
    }

    public class GradientThingy(Gradient gradient, float offs) : SModifier
    {
        public Gradient gradient = gradient;
        public float offs = offs;

        public override void Update()
        {
            Elem.Foreground = gradient.Evaluate((Time.realtimeSinceStartup * 0.25f + offs) % 1f);
        }
    }

    public class MovementThingy(float offs, float amplitude) : SModifier
    {
        public float offs = offs;
        public float amplitude = amplitude;

        public override void Update()
        {
            Elem.Position = Elem.Position.WithY(Mathf.Sin((Time.realtimeSinceStartup * 0.5f + offs) * Mathf.PI * 2) * amplitude);
        }
    }
}
