#define DEBUG
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections.Generic;
using UnityEngine;
using VLB;
namespace Oxide.Plugins
{
    [Info("PlayerBlocking", "lolman300", "1.0.0")]
    [Description("Combat and raid blocking")]
    class PlayerBlocking : CovalencePlugin
    {
        #region Fields
        [PluginReference] private Plugin Clans;
        [PluginReference] private static Plugin ImageLibrary;
        private static Configuration _config;
        private string PERMISSION_USE = "playerblocking.use";
        private string PERMISSION_ADMIN = "playerblocking.admin";
        private struct PluginImageReference
        {
            public PluginImageReference(string imageName, string imageUrl, string imageCacheIdString = "")
            {
                ImageName = imageName;
                ImageUrl = imageUrl;
                ImageCacheIdString = imageCacheIdString;
            }
            public readonly string ImageName;
            public readonly string ImageUrl;
            public string ImageCacheIdString;
        }
        private static PluginImageReference _combatBlockImage = new PluginImageReference("combatblock_icon", "https://static.thenounproject.com/png/138-200.png");
        private static PluginImageReference _raidBlockImage = new PluginImageReference("raidblock_icon", "https://www.freeiconspng.com/uploads/explosion-icon-12.png");
        #endregion
        #region Configuration
        #region Classes
        [JsonObject(MemberSerialization.OptIn)]
        private class Configuration
        {
            [JsonProperty(PropertyName = "Minimum Combat Block Damage")]
            private float _minCombatDamage = 1.0f;
            public float MinCombatDamage => _minCombatDamage;
            [JsonProperty(PropertyName = "Combat Block Duration (in seconds)")]
            private uint _combatBlockDuration = 180;
            public uint CombatBlockDuration => _combatBlockDuration;
            [JsonProperty(PropertyName = "Combat Damage Types")]
            private List<string> _combatDamageTypes = new List<string> { "Bullet", "Arrow", "Blunt", "Stab", "Slash", "Explosion", "Heat", "ElectricShock" };
            public List<string> CombatDamageTypes => _combatDamageTypes;
            [JsonProperty(PropertyName = "Minimum Raid Block Damage")]
            private float _minRaidDamage = 0.2f;
            public float MinRaidDamage => _minRaidDamage;
            [JsonProperty(PropertyName = "Raid Block Duration (in seconds)")]
            private uint _raidBlockDuration = 300;
            public uint RaidBlockDuration => _raidBlockDuration;
            [JsonProperty(PropertyName = "Raid Block Distance")]
            private float _raidBlockDistance = 75.0f;
            public float RaidBlockDistance => _raidBlockDistance;
            [JsonProperty(PropertyName = "Raid Damage Types")]
            private List<string> _raidDamageTypes = new List<string> { "Bullet", "Blunt", "Stab", "Slash", "Explosion", "Heat" };
            public List<string> RaidDamageTypes => _raidDamageTypes;

        }
        #endregion Classes
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
            }
            catch
            {
                LogError("Could not load config file.  Loading defaults...");
                LoadDefaultConfig();
                SaveConfig();
            }
        }
        protected override void LoadDefaultConfig() => _config = new Configuration();
        protected override void SaveConfig() => Config.WriteObject(_config);
        #endregion Configuration
        #region Helper Functions
        private static void SendPlayerMessage(BasePlayer player, string message)
        {
            if (player == null || player.IPlayer == null || !player.IPlayer.IsConnected) return;
            player.IPlayer.Reply(message);
        }
        private void CleanUp()
        {
            ImageLibrary = null;
            _config = null;
            CombatBlock c;
            RaidBlock r;
            foreach (var entity in BaseNetworkable.serverEntities)
            {
                BasePlayer player = entity as BasePlayer;
                if (player == null) continue;
                if (player.TryGetComponent(out c))
                {
                    UnityEngine.Object.Destroy(c);
                }
                if (player.TryGetComponent(out r))
                {
                    UnityEngine.Object.Destroy(r);
                }
            }
        }
        private bool IsCombatDamage(HitInfo info)
        {
            for (int i = 0; i < info.damageTypes.types.Length; i++)
            {
                if (info.damageTypes.types[i] < _config.MinCombatDamage) continue;
                DamageType dt = (DamageType)i;
                if (_config.CombatDamageTypes.Contains(dt.ToString())) return true;
            }
            return false;
        }
        private bool IsRaidDamage(HitInfo info)
        {
            for (int i = 0; i < info.damageTypes.types.Length; i++)
            {
                if (info.damageTypes.types[i] < _config.MinRaidDamage) continue;
                DamageType dt = (DamageType)i;
                if (_config.RaidDamageTypes.Contains(dt.ToString())) return true;
            }
            return false;
        }
        private void DoCombatBlock(BasePlayer player)
        {
            if (player == null) return;
            CombatBlock c;
            if (player.userID.IsSteamId() && permission.UserHasPermission(player.UserIDString, PERMISSION_USE) && !permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN)) // NPC check
            {
                if (player.TryGetComponent(out c))
                {
                    c.Refresh();
                } else
                {
#if DEBUG
                    Puts("DoCombatBlock");
#endif
                    player.gameObject.AddComponent<CombatBlock>();
                    SendPlayerMessage(player, "You have been combat blocked for " + _config.CombatBlockDuration + " seconds");
                }
            }
        }
        private void CheckCombatBlock(BasePlayer target, BasePlayer attacker)
        {
            if (target != null && attacker != null)
            {
                if (target == attacker) return;
                if (Clans?.Call<bool>("IsClanMember", target.UserIDString, attacker.UserIDString) == true) return;
            }
            DoCombatBlock(target);
            DoCombatBlock(attacker);
        }
        private bool DoRaidBlock(BasePlayer player)
        {
            if (player == null) return false;
            RaidBlock r;
            if (player.userID.IsSteamId() && permission.UserHasPermission(player.UserIDString, PERMISSION_USE) && !permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN)) // NPC check
            {
                if (player.TryGetComponent(out r))
                {
                    r.Refresh();
                }
                else
                {
#if DEBUG
                    Puts("DoRaidBlock");
#endif
                    player.gameObject.AddComponent<RaidBlock>();
                    SendPlayerMessage(player, "You have been raid blocked for " + _config.RaidBlockDuration + " seconds");
                }
            }
            return true;
        }
        private void CheckRaidBlock(BaseCombatEntity entity, HitInfo info)
        {
            BuildingPrivlidge buildingPrivlidge = entity.GetBuildingPrivilege();
            if (buildingPrivlidge == null) return;
            BasePlayer entityOwner = players.FindPlayerById(entity.OwnerID.ToString()).Object as BasePlayer;
            BasePlayer attacker = info.Initiator as BasePlayer;
            if (entityOwner == attacker) return;
            if (Clans?.Call<bool>("IsClanMember", entity.OwnerID.ToString(), attacker.UserIDString) == true) return;
            if (attacker != null && !buildingPrivlidge.IsAuthed(attacker))
            {
                if (DoRaidBlock(attacker))
                {
                    List<BasePlayer> playersInRange = new List<BasePlayer>();
                    Vis.Entities(buildingPrivlidge.CenterPoint(), _config.RaidBlockDistance, playersInRange);
#if DEBUG
                    Debug.Log("Found " + playersInRange.Count + " players in range");
#endif
                    foreach (BasePlayer basePlayer in playersInRange)
                    {
                        if (attacker == basePlayer) continue;
                        DoRaidBlock(basePlayer);
                    }
                }
            }
        }
        private bool AddImageToLibrary(ref PluginImageReference image, ulong skin = 0)
        {
            if (ImageLibrary == null) return false;
            if (ImageLibrary.Call<bool>("AddImage", image.ImageUrl, image.ImageName, skin) == false) return false;
            return !String.IsNullOrEmpty(image.ImageCacheIdString = ImageLibrary.Call<string>("GetImage", image.ImageName));
        }
        private static CuiElement NewImageElement(string parent, string name, PluginImageReference image, string anchorMin, string anchorMax) 
        {
            return new CuiElement
            {
                Parent = parent,
                Name = name,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = String.IsNullOrEmpty(image.ImageCacheIdString) ? null : image.ImageCacheIdString,
                        Url = String.IsNullOrEmpty(image.ImageCacheIdString) ? image.ImageUrl : null
                    },
                    new CuiRectTransformComponent { AnchorMin = anchorMin, AnchorMax = anchorMax }

                }
            };
        }
        #endregion
        #region Hooks
        object CanTrade(BasePlayer player)
        {
            if (player.HasComponent<CombatBlock>() || player.HasComponent<RaidBlock>()) return "You cannot use trade while combat/raid blocked";
            return null;
        }
        object canRemove(BasePlayer player)
        {
            if (player.HasComponent<RaidBlock>()) return "You cannot use remover tool while raid blocked";
            return null;
        }
        object CanTeleport(BasePlayer player)
        {
            if (player.HasComponent<CombatBlock>() || player.HasComponent<RaidBlock>()) return "You cannot use TP while combat/raid blocked";
            return null;
        }
        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            CombatBlock c;
            RaidBlock r;
            if (player.TryGetComponent(out c)) UnityEngine.Object.Destroy(c);
            if (player.TryGetComponent(out r)) UnityEngine.Object.Destroy(r);
            return null;
        }
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null || info.Initiator == null) return null;
            if (entity as BaseAnimalNPC != null || info.Initiator as BaseAnimalNPC != null || entity as BaseCorpse != null) return null;
            BasePlayer playerTarget = entity as BasePlayer;
            BasePlayer playerAttacker = info.Initiator as BasePlayer;
            if (playerTarget == null && playerAttacker == null) return null;
            if (playerAttacker != null && playerAttacker.userID.IsSteamId() && entity.OwnerID.IsSteamId() && IsRaidDamage(info)) CheckRaidBlock(entity, info);
            else if (IsCombatDamage(info)) CheckCombatBlock(playerTarget, playerAttacker);
            return null;
        }
        private void Init()
        {
            permission.RegisterPermission(PERMISSION_USE, this);
            permission.RegisterPermission(PERMISSION_ADMIN, this);
            if ((ImageLibrary = plugins.Find("ImageLibrary")) == null) LogError("ImageLibrary is not loaded, get it at https://umod.org/plugins/image-library");
            if (AddImageToLibrary(ref _combatBlockImage) == false) LogError("Failed to load combat block image!");
            if (AddImageToLibrary(ref _raidBlockImage) == false) LogError("Failed to load raid block image!");
        }
        private void Loaded()
        {
            if (Clans == null) LogError("Clans is not loaded, get it at https://umod.org/plugins/clans");
        }
        private void Unload() => CleanUp();
        private void OnPluginLoaded(Plugin plugin)
        {
            if (plugin.Name == "ImageLibrary")
            {
                ImageLibrary = plugin;
                if (AddImageToLibrary(ref _combatBlockImage) == false) LogError("Failed to load combat block image!");
                if (AddImageToLibrary(ref _raidBlockImage) == false) LogError("Failed to load raid block image!");
            }
        }
        private void OnPluginUnloaded(Plugin plugin)
        {
            if (plugin.Name == "ImageLibrary")
            {
                ImageLibrary = null;
                _combatBlockImage.ImageCacheIdString = "";
                _raidBlockImage.ImageCacheIdString = "";
            }
        }
        #endregion
        #region Classes
        private class BlockBehaviorCui 
        {
            protected CuiElementContainer _cuiElements = new CuiElementContainer();
            protected BlockBehaviorCui() { }
            protected void ClearCuiElements()
            {
                _cuiElements.Clear();
            }
            public void DestroyCui(BasePlayer player)
            {
                if (player == null || player.IPlayer == null || !player.IPlayer.IsConnected) return;
                foreach (CuiElement cuiElement in _cuiElements)
                {
                    CuiHelper.DestroyUi(player, cuiElement.Name);
                }
            }
            protected virtual void RenderCui(BasePlayer player)
            {
                if (player == null || player.IPlayer == null || !player.IPlayer.IsConnected) return;
                DestroyCui(player);
                CuiHelper.AddUi(player, _cuiElements);
            }
        }
        private class CombatBlockCui : BlockBehaviorCui
        {
            private CuiPanel _combatBlockPanel = new CuiPanel
            {
                Image =
                {
                    Color = "0.8 0.28 0.2 0.67"
                },
                RectTransform =
                {
                    AnchorMin = "0.87 0.39",
                    AnchorMax = "0.99 0.42"
                }
            };
            private CuiLabel _combatBlockLabel = new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 11
                },
                RectTransform =
                {
                    AnchorMin = "0.36 0",
                    AnchorMax = "0.96 1.0"
                }
            };
            public CombatBlockCui() { }
            public void RenderCui(BasePlayer player, float duration)
            {
                if (player == null || player.IPlayer == null || !player.IPlayer.IsConnected) return;
                ClearCuiElements();
                string panelElement = _cuiElements.Add(_combatBlockPanel, "Hud", "CombatBlockCuiPanel");
                _combatBlockLabel.Text.Text = "Combat Block (" + TimeSpan.FromSeconds(duration).ToString(@"m\:ss") + ")";
                _cuiElements.Add(_combatBlockLabel, panelElement);
                _cuiElements.Add(NewImageElement(panelElement, "CombatBlockCuiIcon", _combatBlockImage, "0.03 0.05", "0.22 0.95"));
                base.RenderCui(player);
            }
        }
        private class RaidBlockCui : BlockBehaviorCui
        {
            private CuiPanel _raidBlockPanel = new CuiPanel
            {
                Image =
                {
                    Color = "0.8 0.28 0.2 0.67"
                },
                RectTransform =
                {
                    AnchorMin = "0.87 0.35",
                    AnchorMax = "0.99 0.38"
                }
            };
            private CuiLabel _raidBlockLabel = new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleRight,
                    FontSize = 11
                },
                RectTransform =
                {
                    AnchorMin = "0.3 0",
                    AnchorMax = "0.95 1.0"
                }
            };
            public RaidBlockCui() { }
            public void RenderCui(BasePlayer player, float duration)
            {
                if (player == null) return;
                ClearCuiElements();
                string panelElement = _cuiElements.Add(_raidBlockPanel, "Hud", "RaidBlockCuiPanel");
                _raidBlockLabel.Text.Text = "Raid Block (" + TimeSpan.FromSeconds(duration).ToString(@"m\:ss") + ")";
                _cuiElements.Add(_raidBlockLabel, panelElement);
                _cuiElements.Add(NewImageElement(panelElement, "RaidBlockCuiIcon", _raidBlockImage, "0 0.05", "0.25 0.95"));
                base.RenderCui(player);
            }
        }
        private class BlockBehavior : MonoBehaviour
        {
            protected BasePlayer _player;
            public uint _initialDuration = 0;
            public uint _duration = 0;
            protected virtual void Awake()
            {
                _player = gameObject.GetComponent<BasePlayer>();
                InvokeRepeating(nameof(Tick), 0f, 1.0f);
            }
            protected virtual void OnDestroy()
            {
                CancelInvoke();
#if DEBUG
                Debug.Log("OnDestroy BlockBehavior");
#endif
            }
            protected virtual void Tick()
            {
                if (_player == null || _duration <= 0) Destroy(this);
                else _duration--;
            }
            public void Refresh()
            {
                CancelInvoke();
                _duration = _initialDuration;
                InvokeRepeating(nameof(Tick), 0f, 1.0f);
            }
        }
        private class CombatBlock : BlockBehavior
        {
            private CombatBlockCui _combatBlockCui = new CombatBlockCui();
            protected override void Awake()
            {
                _duration = _initialDuration = _config.CombatBlockDuration;
                base.Awake();
            }
            protected override void OnDestroy()
            {
                _combatBlockCui.DestroyCui(_player);
                SendPlayerMessage(_player, "You are no longer combat blocked");
#if DEBUG
                Debug.Log("OnDestroy CombatBlock");
#endif
                base.OnDestroy();
            }
            protected override void Tick()
            {
                _combatBlockCui.RenderCui(_player, _duration);
                base.Tick();
            }
        }
        private class RaidBlock : BlockBehavior
        {
            private RaidBlockCui _raidBlockCui = new RaidBlockCui();
            protected override void Awake()
            {
                _duration = _initialDuration = _config.RaidBlockDuration;
                base.Awake();
            }
            protected override void OnDestroy()
            {
                _raidBlockCui.DestroyCui(_player);
                SendPlayerMessage(_player, "You are no longer raid blocked");
#if DEBUG
                Debug.Log("OnDestroy RaidBlock");
#endif
                base.OnDestroy();
            }
            protected override void Tick()
            {
                _raidBlockCui.RenderCui(_player, _duration);
                base.Tick();
            }
        }
        #endregion
    }
}