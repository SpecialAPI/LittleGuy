using Dungeonator;
using FullInspector;
using Gungeon;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LittleGuy
{
    public class StrangeRoot : CompanionItem
    {
        public static GameObject guy;
        public static GameObject gal;
        public static int rootid;
        public static int rootsid;
        public static tk2dSpriteCollectionData rootscoll;
        public GameObject littlegal;
        public bool hasgal;
        public float timer = 450f;
        public float lastSinisterCountdownTime = 450f;
        public bool doSinisterCooldown;
        public bool cachedCanBeDropped;
        public static Dictionary<float, string> sinisterCountdown = new Dictionary<float, string>()
        {
            { 1f, "1 SECOND" },
            { 2f, "2 SECONDS" },
            { 3f, "3 SECONDS" },
            { 4f, "4 SECONDS" },
            { 5f, "5 SECONDS" },
            { 6f, "6 SECONDS" },
            { 7f, "7 SECONDS" },
            { 8f, "8 SECONDS" },
            { 9f, "9 SECONDS" },
            { 10f, "10 SECONDS" },
            { 60f, "1 MINUTE" },
            { 180f, "3 MINUTES" },
            { 300f, "5 MINUTES" },
        };

        public static void Init()
        {
            var name = "Strange Root";
            var shortdesc = "Little Guy";
            var longdesc = "Grants you two more active item slots and occasionally finds an active item after room completion.\n\nA young fox squire, born in a far-away land ravaged by ruthless diplomacy and warfare. Although he is still too inexperienced for combat, he has gotten used to lugging around the heavy equipment of his former master, and has a great eye for finding loot in the battlefield...";
            var root = BuildStrangeRoot(name, shortdesc, longdesc, ItemQuality.A, 2);
            root.CompanionGuid = BuildGuy();
            BuildGal();
            rootid = root.PickupObjectId;
            rootsid = root.GetComponent<tk2dSprite>().spriteId;
            rootscoll = root.GetComponent<tk2dSprite>().Collection;
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnNewFloorLoaded += HandleNewFloorGal;
            CreateGal(player);
            if (!GameStatsManager.Instance.GetFlag(Plugin.completedSinisterCountdown))
            {
                cachedCanBeDropped = CanBeDropped;
                doSinisterCooldown = true;
                CanBeDropped = false;
            }
        }

        public void HandleNewFloorGal(PlayerController obj)
        {
            DestroyGal();
            if (!PreventRespawnOnFloorLoad)
            {
                CreateGal(obj);
            }
        }

        public override void DisableEffect(PlayerController player)
        {
            DestroyGal();
            if (player != null)
            {
                player.OnNewFloorLoaded -= HandleNewFloorGal;
            }
            if (doSinisterCooldown && GameManager.Instance.Dungeon != null)
            {
                RenderSettings.ambientIntensity = GameManager.Instance.Dungeon.TargetAmbientIntensity;
                Pixelator.Instance.pointLightMultiplier = 1;
            }
        }

        public override DebrisObject Drop(PlayerController player)
        {
            DestroyGal();
            player.OnNewFloorLoaded -= HandleNewFloorGal;
            return base.Drop(player);
        }

        public void DestroyGal()
        {
            if (littlegal)
            {
                Destroy(littlegal);
                littlegal = null;
            }
        }

        public override void Update()
        {
            base.Update();
            if (PickedUp && Owner != null && !hasgal && timer > 0f)
            {
                timer -= BraveTime.DeltaTime;
                if (doSinisterCooldown)
                {
                    foreach(var kvp in sinisterCountdown)
                    {
                        if(kvp.Key >= timer && kvp.Key < lastSinisterCountdownTime)
                        {
                            lastSinisterCountdownTime = kvp.Key;
                            GameUIRoot.Instance.notificationController.ForceHide();
                            GameUIRoot.Instance.notificationController.DoCustomNotification("YOU HAVE", kvp.Value, sprite.Collection, sprite.spriteId, UINotificationController.NotificationColor.PURPLE, true, false);
                            AkSoundEngine.PostEvent("Play_WPN_kthulu_blast_01", gameObject);
                            break;
                        }
                    }
                }
                if(timer <= 0f)
                {
                    Vector3? positionOverride = null;
                    if (Owner.CurrentRoom != null)
                    {
                        var cam = GameManager.Instance.MainCameraController;
                        var dungeondata = GameManager.Instance.Dungeon.data;
                        var roomdata = Owner.CurrentRoom.connectedRooms
                            .Concat(new RoomHandler[] { Owner.CurrentRoom })
                            .Select(r => r.Cells
                                .Where(x =>
                                    dungeondata.CheckInBoundsAndValid(x) &&
                                    !dungeondata.isTopWall(x.x, x.y) &&
                                    !dungeondata.isWall(x) &&
                                    (!cam.PointIsVisible(x.ToVector3()) || (dungeondata.GetRoomFromPosition(x) != null && dungeondata.GetRoomFromPosition(x).visibility is RoomHandler.VisibilityStatus.OBSCURED or RoomHandler.VisibilityStatus.REOBSCURED))))
                            .SelectMany(x => x);
                        if (roomdata.Any())
                        {
                            float closest = -1f;
                            roomdata.Do(x =>
                            {
                                var dist = Vector3.Distance(x.ToVector3(), Owner.CenterPosition);
                                if (closest < 0f || dist < closest)
                                {
                                    closest = dist;
                                    positionOverride = x.ToVector3();
                                }
                            });
                        }
                    }
                    hasgal = true;
                    DestroyGal();
                    CreateGal(Owner, positionOverride);
                    if (doSinisterCooldown)
                    {
                        CanBeDropped = cachedCanBeDropped;
                        RenderSettings.ambientIntensity = GameManager.Instance.Dungeon.TargetAmbientIntensity;
                        Pixelator.Instance.pointLightMultiplier = 1;
                        doSinisterCooldown = false;
                        GameStatsManager.Instance.SetFlag(Plugin.completedSinisterCountdown, true);
                    }
                }
                else if(timer <= 10f && doSinisterCooldown)
                {
                    var mul = (0.5f + Mathf.Ceil(timer) * 0.05f);
                    RenderSettings.ambientIntensity = Mathf.Min(mul * GameManager.Instance.Dungeon.TargetAmbientIntensity, RenderSettings.ambientIntensity);
                    Pixelator.Instance.pointLightMultiplier = mul;
                }
            }
        }

        public void CreateGal(PlayerController owner, Vector3? overridePosition = null)
        {
            if (!hasgal)
            {
                return;
            }
            littlegal = Instantiate(EnemyDatabase.GetOrLoadByGuid("lg_lilgal").gameObject, overridePosition ?? (owner.transform.position + Vector3.right), Quaternion.identity);
            var cc = littlegal.GetOrAddComponent<CompanionController>();
            cc.Initialize(owner);
            if (cc.specRigidbody != null)
            {
                PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(cc.specRigidbody);
            }
        }

        public static StrangeRoot BuildStrangeRoot(string name, string shortdesc, string longdesc, PickupObject.ItemQuality quality, int slotsToAdd)
        {
            var go = Plugin.bundle.LoadAsset<GameObject>("strangeroot");
            var comp = go.AddComponent<StrangeRoot>();
            ETGMod.Databases.Items.SetupItem(comp, name);
            var cdef = comp.sprite.GetCurrentSpriteDef();
            if(cdef.material != null)
            {
                cdef.material.shader = ShaderCache.Acquire("tk2d/CutoutVertexColorTintableTilted");
            }
            if(cdef.materialInst != null)
            {
                cdef.materialInst.shader = ShaderCache.Acquire("tk2d/CutoutVertexColorTintableTilted");
            }
            var ammonomicondef = CopyDefinitionFrom(comp.sprite.GetCurrentSpriteDef());
            if(ammonomicondef.material != null)
            {
                ammonomicondef.material.shader = ShaderCache.Acquire("tk2d/CutoutVertexColorTilted");
            }
            if(ammonomicondef.materialInst != null)
            {
                ammonomicondef.materialInst.shader = ShaderCache.Acquire("tk2d/CutoutVertexColorTilted");
            }
            var ammocoll = AmmonomiconController.ForceInstance.EncounterIconCollection;
            ammocoll.spriteDefinitions = ammocoll.spriteDefinitions.AddToArray(ammonomicondef);
            ammocoll.spriteNameLookupDict = null;
            ammocoll.InitDictionary();
            comp.encounterTrackable.journalData.AmmonomiconSprite = comp.sprite.GetCurrentSpriteDef().name;
            comp.SetShortDescription(shortdesc);
            comp.SetLongDescription(longdesc);
            Game.Items.Add($"lg:{name.ToID()}", comp);
            ETGMod.Databases.Items.AddSpecific(comp);
            comp.quality = quality;
            comp.passiveStatModifiers = new StatModifier[] { StatModifier.Create(PlayerStats.StatType.AdditionalItemCapacity, StatModifier.ModifyMethod.ADDITIVE, slotsToAdd) };
            return comp;
        }

        public static string BuildGal()
        {
            var galguid = "lg_lilgal";
            var go = Plugin.bundle.LoadAsset<GameObject>("littlegal");

            var rigidbody = go.AddComponent<SpeculativeRigidbody>();
            rigidbody.PixelColliders = new()
            {
                new()
                {
                    ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
                    CollisionLayer = CollisionLayer.PlayerCollider,
                    ManualWidth = 8,
                    ManualHeight = 14,
                    ManualOffsetX = 2,
                    ManualOffsetY = 0
                },
                new()
                {
                    ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
                    CollisionLayer = CollisionLayer.PlayerHitBox,
                    ManualWidth = 10,
                    ManualHeight = 16,
                    ManualOffsetX = 1,
                    ManualOffsetY = 0
                }
            };

            var ai = go.AddComponent<AIActor>();
            ai.EnemyGuid = galguid;
            ai.CanDropCurrency = false;
            ai.CollisionDamage = 0.5f;
            ai.IgnoreForRoomClear = true;
            ai.IsNormalEnemy = false;
            ai.MovementSpeed = 6f;
            ai.ActorName = "Little Gal";
            ai.DoDustUps = true;
            ai.DustUpInterval = 0.125f;
            ai.HasShadow = true;

            go.AddComponent<HitEffectHandler>();

            var hh = go.AddComponent<HealthHaver>();
            hh.SetHealthMaximum(15000, null, true);

            var kb = go.AddComponent<KnockbackDoer>();
            kb.shouldBounce = false;
            kb.weight = 35f;

            var aianim = go.AddComponent<AIAnimator>();
            aianim.facingType = AIAnimator.FacingType.Movement;
            aianim.IdleAnimation = new()
            {
                AnimNames = new string[] { "", "" },
                Prefix = "idle",
                Flipped = new DirectionalAnimation.FlipType[]
                {
                    DirectionalAnimation.FlipType.None,
                    DirectionalAnimation.FlipType.None
                },
                Type = DirectionalAnimation.DirectionType.TwoWayHorizontal
            };
            aianim.MoveAnimation = new()
            {
                AnimNames = new string[] { "", "" },
                Prefix = "move",
                Flipped = new DirectionalAnimation.FlipType[]
                {
                    DirectionalAnimation.FlipType.None,
                    DirectionalAnimation.FlipType.None
                },
                Type = DirectionalAnimation.DirectionType.TwoWayHorizontal
            };
            aianim.FlightAnimation = new()
            {
                AnimNames = new string[0],
                Prefix = "",
                Flipped = new DirectionalAnimation.FlipType[0],
                Type = DirectionalAnimation.DirectionType.None
            };
            aianim.HitAnimation = new()
            {
                AnimNames = new string[] { "", "" },
                Prefix = "hit",
                Flipped = new DirectionalAnimation.FlipType[]
                {
                    DirectionalAnimation.FlipType.None,
                    DirectionalAnimation.FlipType.None
                },
                Type = DirectionalAnimation.DirectionType.TwoWayHorizontal
            };
            aianim.OtherAnimations = new List<AIAnimator.NamedDirectionalAnimation>
            {
                new()
                {
                    name = "removehelmet",
                    anim = new()
                    {
                        AnimNames = new string[] { "", "" },
                        Prefix = "removehelmet",
                        Flipped = new DirectionalAnimation.FlipType[]
                        {
                            DirectionalAnimation.FlipType.None,
                            DirectionalAnimation.FlipType.None
                        },
                        Type = DirectionalAnimation.DirectionType.TwoWayHorizontal
                    }
                },
                new()
                {
                    name = "pet",
                    anim = new()
                    {
                        AnimNames = new string[] { "", "" },
                        Prefix = "pet",
                        Flipped = new DirectionalAnimation.FlipType[]
                        {
                            DirectionalAnimation.FlipType.None,
                            DirectionalAnimation.FlipType.None
                        },
                        Type = DirectionalAnimation.DirectionType.TwoWayHorizontal
                    }
                },
                new()
                {
                    name = "pet_end",
                    anim = new()
                    {
                        AnimNames = new string[] { "", "" },
                        Prefix = "pet_end",
                        Flipped = new DirectionalAnimation.FlipType[]
                        {
                            DirectionalAnimation.FlipType.None,
                            DirectionalAnimation.FlipType.None
                        },
                        Type = DirectionalAnimation.DirectionType.TwoWayHorizontal
                    }
                },
                new()
                {
                    name = "idle2",
                    anim = new()
                    {
                        AnimNames = new string[] { "", "" },
                        Prefix = "idle2",
                        Flipped = new DirectionalAnimation.FlipType[]
                        {
                            DirectionalAnimation.FlipType.None,
                            DirectionalAnimation.FlipType.None
                        },
                        Type = DirectionalAnimation.DirectionType.TwoWayHorizontal
                    }
                }
            };

            go.AddComponent<ObjectVisibilityManager>();

            var spec = go.AddComponent<BehaviorSpeculator>();
            spec.MovementBehaviors = new()
            {
                new CompanionFollowPlayerBehavior()
                {
                    CanRollOverPits = false,
                    CatchUpAccelTime = 3,
                    CatchUpAnimation = "",
                    CatchUpMaxSpeed = 10f,
                    CatchUpOutAnimation = "",
                    CatchUpRadius = 8f,
                    CatchUpSpeed = 6f,
                    DisableInCombat = true,
                    IdealRadius = 3f,
                    IdleAnimations = new string[0],
                    RollAnimation = "",
                    PathInterval = 0.25f,
                    TemporarilyDisabled = false
                },
                new SeekTargetBehavior()
                {
                    CustomRange = 2f,
                    ExternalCooldownSource = false,
                    LineOfSight = true,
                    PathInterval = 0.25f,
                    ReturnToSpawn = true,
                    SpawnTetherDistance = 0f,
                    StopWhenInRange = true
                }
            };
            spec.OtherBehaviors = new()
            {
                new LittleGalDoStuffBehavior()
            };
            spec.TargetBehaviors = new()
            {
                new TargetPlayerBehavior()
                {
                    LineOfSight = true,
                    ObjectPermanence = true,
                    PauseOnTargetSwitch = true,
                    PauseTime = 0.25f,
                    Radius = 35f,
                    SearchInterval = 0.25f
                }
            };
            spec.AttackBehaviors = new List<AttackBehaviorBase>();
            ((ISerializedObject)spec).SerializedObjectReferences = new List<UnityEngine.Object>(0);
            ((ISerializedObject)spec).SerializedStateKeys = new() { "OverrideBehaviors", "OtherBehaviors", "TargetBehaviors", "AttackBehaviors", "MovementBehaviors" };
            ((ISerializedObject)spec).SerializedStateValues = new() { "", "", "", "", "" };

            var comp = go.AddComponent<CompanionController>();
            comp.CanBePet = true;
            comp.CanInterceptBullets = true;

            var hold = go.AddComponent<PetOffsetHolder>();
            hold.petOffsetLeft = new Vector2(-0.6875f, -0.3125f);
            hold.petOffsetRight = new Vector2(0.5f, -0.3125f);

            go.AddComponent<HealOnTakingDamage>();

            EnemyDatabase.Instance.Entries.Add(new LittleGuyEntry()
            {
                difficulty = DungeonPlaceableBehaviour.PlaceableDifficulty.BASE,
                encounterGuid = galguid,
                ForcedPositionInAmmonomicon = 0,
                isInBossTab = false,
                isNormalEnemy = false,
                myGuid = galguid,
                path = go.name,
                placeableHeight = 1,
                placeableWidth = 1,
                unityGuid = galguid
            });

            Game.Enemies.Add($"lg:little_gal", ai);

            gal = go;
            return galguid;
        }

        public static string BuildGuy()
        {
            var guyguid = "lg_lilguy";
            var go = Plugin.bundle.LoadAsset<GameObject>("littleguy");

            var rigidbody = go.AddComponent<SpeculativeRigidbody>();
            rigidbody.PixelColliders = new List<PixelCollider>()
            {
                new()
                {
                    ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
                    CollisionLayer = CollisionLayer.EnemyCollider,
                    ManualWidth = 8,
                    ManualHeight = 14,
                    ManualOffsetX = 6,
                    ManualOffsetY = 0
                },
                new()
                {
                    ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
                    CollisionLayer = CollisionLayer.EnemyHitBox,
                    ManualWidth = 10,
                    ManualHeight = 16,
                    ManualOffsetX = 5,
                    ManualOffsetY = 0
                }
            };

            var ai = go.AddComponent<AIActor>();
            ai.EnemyGuid = guyguid;
            ai.CanDropCurrency = false;
            ai.CollisionDamage = 0.5f;
            ai.IgnoreForRoomClear = true;
            ai.IsNormalEnemy = false;
            ai.MovementSpeed = 6f;
            ai.ActorName = "Little Guy";
            ai.DoDustUps = true;
            ai.DustUpInterval = 0.125f;
            ai.HasShadow = true;

            go.AddComponent<HitEffectHandler>();

            var hh = go.AddComponent<HealthHaver>();
            hh.SetHealthMaximum(15000, null, true);

            var kb = go.AddComponent<KnockbackDoer>();
            kb.shouldBounce = false;
            kb.weight = 35f;

            var aianim = go.AddComponent<AIAnimator>();
            aianim.facingType = AIAnimator.FacingType.Movement;
            aianim.IdleAnimation = new()
            {
                AnimNames = new string[] { "", "" },
                Prefix = "idle",
                Flipped = new DirectionalAnimation.FlipType[]
                {
                    DirectionalAnimation.FlipType.None,
                    DirectionalAnimation.FlipType.None
                },
                Type = DirectionalAnimation.DirectionType.TwoWayHorizontal
            };
            aianim.MoveAnimation = new()
            {
                AnimNames = new string[] { "", "" },
                Prefix = "move",
                Flipped = new DirectionalAnimation.FlipType[]
                {
                    DirectionalAnimation.FlipType.None,
                    DirectionalAnimation.FlipType.None
                },
                Type = DirectionalAnimation.DirectionType.TwoWayHorizontal
            };
            aianim.FlightAnimation = new()
            {
                AnimNames = new string[0],
                Prefix = "",
                Flipped = new DirectionalAnimation.FlipType[0],
                Type = DirectionalAnimation.DirectionType.None
            };
            aianim.HitAnimation = new()
            {
                AnimNames = new string[0],
                Prefix = "",
                Flipped = new DirectionalAnimation.FlipType[0],
                Type = DirectionalAnimation.DirectionType.None
            };
            aianim.OtherAnimations = new()
            {
                new()
                {
                    name = "find",
                    anim = new()
                    {
                        AnimNames = new string[] { "", "" },
                        Prefix = "finditem",
                        Flipped = new DirectionalAnimation.FlipType[]
                        {
                            DirectionalAnimation.FlipType.None,
                            DirectionalAnimation.FlipType.None
                        },
                        Type = DirectionalAnimation.DirectionType.TwoWayHorizontal
                    }
                },
                new()
                {
                    name = "sleep",
                    anim = new()
                    {
                        AnimNames = new string[] { "", "" },
                        Prefix = "sleep",
                        Flipped = new DirectionalAnimation.FlipType[]
                        {
                            DirectionalAnimation.FlipType.None,
                            DirectionalAnimation.FlipType.None
                        },
                        Type = DirectionalAnimation.DirectionType.TwoWayHorizontal
                    }
                },
                new()
                {
                    name = "pet",
                    anim = new()
                    {
                        AnimNames = new string[] { "", "" },
                        Prefix = "pet",
                        Flipped = new DirectionalAnimation.FlipType[]
                        {
                            DirectionalAnimation.FlipType.None,
                            DirectionalAnimation.FlipType.None
                        },
                        Type = DirectionalAnimation.DirectionType.TwoWayHorizontal
                    }
                },
                new()
                {
                    name = "pet_end",
                    anim = new()
                    {
                        AnimNames = new string[] { "", "" },
                        Prefix = "pet_end",
                        Flipped = new DirectionalAnimation.FlipType[]
                        {
                            DirectionalAnimation.FlipType.None,
                            DirectionalAnimation.FlipType.None
                        },
                        Type = DirectionalAnimation.DirectionType.TwoWayHorizontal
                    }
                }
            };

            go.AddComponent<ObjectVisibilityManager>();

            var spec = go.AddComponent<BehaviorSpeculator>();
            spec.MovementBehaviors = new()
            {
                new CompanionFollowPlayerBehavior()
                {
                    CanRollOverPits = false,
                    CatchUpAccelTime = 3,
                    CatchUpAnimation = "",
                    CatchUpMaxSpeed = 10f,
                    CatchUpOutAnimation = "",
                    CatchUpRadius = 8f,
                    CatchUpSpeed = 6f,
                    DisableInCombat = false,
                    IdealRadius = 3f,
                    IdleAnimations = new string[0],
                    RollAnimation = "",
                    PathInterval = 0.25f,
                    TemporarilyDisabled = false
                }
            };
            spec.OtherBehaviors = new()
            {
                new LittleGuyFindBehavior(),
                new StayFartherAwayOutsideCombat()
                {
                    CombatDistance = 3f,
                    OutOfCombatDistance = 4f
                }
            };
            ((ISerializedObject)spec).SerializedObjectReferences = new List<UnityEngine.Object>(0);
            ((ISerializedObject)spec).SerializedStateKeys = new() { "OverrideBehaviors", "OtherBehaviors", "TargetBehaviors", "AttackBehaviors", "MovementBehaviors" };
            ((ISerializedObject)spec).SerializedStateValues = new() { "", "", "", "", "" };

            var comp = go.AddComponent<CompanionController>();
            comp.CanBePet = true;

            go.AddComponent<HeavyWeightSynergyForLittleGuy>();

            var hold = go.AddComponent<PetOffsetHolder>();
            hold.petOffsetLeft = new Vector2(-0.8125f, -0.4375f);
            hold.petOffsetRight = new Vector2(0.625f, -0.4375f);

            EnemyDatabase.Instance.Entries.Add(new LittleGuyEntry()
            {
                difficulty = DungeonPlaceableBehaviour.PlaceableDifficulty.BASE,
                encounterGuid = guyguid,
                ForcedPositionInAmmonomicon = 0,
                isInBossTab = false,
                isNormalEnemy = false,
                myGuid = guyguid,
                path = go.name,
                placeableHeight = 1,
                placeableWidth = 1,
                unityGuid = guyguid
            });

            Game.Enemies.Add($"lg:little_guy", ai);

            guy = go;
            return guyguid;
        }

        public class LittleGuyEntry : EnemyDatabaseEntry
        {
            public override AssetBundle assetBundle => Plugin.bundle;
        }

        public static tk2dSpriteDefinition CopyDefinitionFrom(tk2dSpriteDefinition other)
        {
            var result = new tk2dSpriteDefinition
            {
                boundsDataCenter = new Vector3
                {
                    x = other.boundsDataCenter.x,
                    y = other.boundsDataCenter.y,
                    z = other.boundsDataCenter.z
                },
                boundsDataExtents = new Vector3
                {
                    x = other.boundsDataExtents.x,
                    y = other.boundsDataExtents.y,
                    z = other.boundsDataExtents.z
                },
                colliderConvex = other.colliderConvex,
                colliderSmoothSphereCollisions = other.colliderSmoothSphereCollisions,
                colliderType = other.colliderType,
                colliderVertices = other.colliderVertices,
                collisionLayer = other.collisionLayer,
                complexGeometry = other.complexGeometry,
                extractRegion = other.extractRegion,
                flipped = other.flipped,
                indices = other.indices,
                material = new Material(other.material),
                materialId = other.materialId,
                materialInst = new Material(other.materialInst),
                metadata = other.metadata,
                name = other.name,
                normals = other.normals,
                physicsEngine = other.physicsEngine,
                position0 = new Vector3
                {
                    x = other.position0.x,
                    y = other.position0.y,
                    z = other.position0.z
                },
                position1 = new Vector3
                {
                    x = other.position1.x,
                    y = other.position1.y,
                    z = other.position1.z
                },
                position2 = new Vector3
                {
                    x = other.position2.x,
                    y = other.position2.y,
                    z = other.position2.z
                },
                position3 = new Vector3
                {
                    x = other.position3.x,
                    y = other.position3.y,
                    z = other.position3.z
                },
                regionH = other.regionH,
                regionW = other.regionW,
                regionX = other.regionX,
                regionY = other.regionY,
                tangents = other.tangents,
                texelSize = new Vector2
                {
                    x = other.texelSize.x,
                    y = other.texelSize.y
                },
                untrimmedBoundsDataCenter = new Vector3
                {
                    x = other.untrimmedBoundsDataCenter.x,
                    y = other.untrimmedBoundsDataCenter.y,
                    z = other.untrimmedBoundsDataCenter.z
                },
                untrimmedBoundsDataExtents = new Vector3
                {
                    x = other.untrimmedBoundsDataExtents.x,
                    y = other.untrimmedBoundsDataExtents.y,
                    z = other.untrimmedBoundsDataExtents.z
                }
            };
            if (other.uvs != null)
            {
                List<Vector2> uvs = new List<Vector2>();
                foreach (Vector2 vector in other.uvs)
                {
                    uvs.Add(new Vector2
                    {
                        x = vector.x,
                        y = vector.y
                    });
                }
                result.uvs = uvs.ToArray();
            }
            else
            {
                result.uvs = null;
            }
            if (other.colliderVertices != null)
            {
                List<Vector3> colliderVertices = new List<Vector3>();
                foreach (Vector3 vector in other.colliderVertices)
                {
                    colliderVertices.Add(new Vector3
                    {
                        x = vector.x,
                        y = vector.y,
                        z = vector.z
                    });
                }
                result.colliderVertices = colliderVertices.ToArray();
            }
            else
            {
                result.colliderVertices = null;
            }
            return result;
        }
    }
}
