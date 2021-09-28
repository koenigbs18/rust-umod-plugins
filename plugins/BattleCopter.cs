using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Rust;
using System;
using System.Collections.Generic;
using UnityEngine;
using VLB;

namespace Oxide.Plugins
{
    [Info("BattleCopter", "lolman300", "1.0.0")]
    [Description("Your own personal battlecopter!")]
    class BattleCopter : CovalencePlugin
    {
        #region Fields
        private static Configuration _config;
        private Dictionary<BasePlayer, MiniCopter> _battlecopterCache = new Dictionary<BasePlayer, MiniCopter>();
        private const string PERMISSION_USE = "battlecopter.use";
        // OTHER PREFABS
        //"assets/prefabs/ammo/rocket/rocket_hv.prefab",//"assets/prefabs/npc/patrol helicopter/rocket_heli.prefab"
        private const string _minicopterPrefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab";
        private const string _rocketPrefab = "assets/prefabs/npc/patrol helicopter/rocket_heli.prefab";
        private const string _rocketEffectPrefab = "assets/prefabs/npc/patrol helicopter/effects/rocket_fire.prefab";
        private const string _machinegunEffectPrefab = "assets/prefabs/npc/patrol helicopter/effects/gun_fire.prefab";
        #endregion

        #region Commands
        [Command("battlecopter"), Permission(PERMISSION_USE)]
        private void SpawnBattleCopter(IPlayer player)
        {
            BasePlayer p = player.Object as BasePlayer;
            if (p == null) return;
            if (!_battlecopterCache.ContainsKey(p)) _battlecopterCache.Add(p, null);
            RaycastHit hitInfo;
            if (CastEyeRay(p, out hitInfo, _config.DistanceCheck, LayerMask.GetMask("World", "Terrain", "Construction")))
            {
                if(_config.DrawRayCast == true)
                {
                    player.Command("ddraw.line", new object[] { 10f, Color.red, p.eyes.position, hitInfo.point });
                }
                KillBattlecopterFromCache(p);
                MiniCopter e = GameManager.server.CreateEntity(_minicopterPrefab, hitInfo.point, Quaternion.identity) as MiniCopter;
                if (e == null) throw new Exception();
                e.gameObject.AddComponent<BattleCopterEntity>();
                _battlecopterCache[p] = e;
                e.Spawn();
            }
        }
        #endregion

        #region Helper Functions
        private void Error(string text)
        {
            LogError(text);
        }
        private bool CastEyeRay(BasePlayer player, out RaycastHit hitInfo, float distance, int layerMask)
        {
            return GamePhysics.Trace(player.eyes.HeadRay(), 0f, out hitInfo, distance, layerMask, QueryTriggerInteraction.UseGlobal);
        }
        private void RemoveFromBattlecopterCache(BasePlayer player)
        {
            if (KillBattlecopterFromCache(player)) _battlecopterCache.Remove(player);
        }
        private bool KillBattlecopterFromCache(BasePlayer player)
        {
            MiniCopter m;
            if (_battlecopterCache.TryGetValue(player, out m))
            {
                if (m.IsValid() && !m.IsDestroyed) m.Kill();
                return true;
            }
            return false;
        }
        private bool BasePlayerFromName(string name, out BasePlayer player)
        {
            IPlayer iPlayer = players.FindPlayer(name);
            player = null;
            if (iPlayer == null) return false;
            player = iPlayer as BasePlayer;
            return true;
        }
        private bool BasePlayerFromID(string id, out BasePlayer player)
        {
            IPlayer iPlayer = players.FindPlayerById(id);
            player = null;
            if (iPlayer == null) return false;
            player = iPlayer as BasePlayer;
            return true;
        }
        private bool BooleanWithReply(IPlayer player, string message, bool ret)
        {
            player.Reply(message);
            return ret;
        }
        private void ClearBattlecopterCache()
        {
            foreach(BasePlayer player in _battlecopterCache.Keys) KillBattlecopterFromCache(player);
            _battlecopterCache.Clear();
        }
        private void CleanUp()
        {
            ClearBattlecopterCache();
            _config = null;
        }
        #endregion

        #region Hooks
        private void Init() => permission.RegisterPermission(PERMISSION_USE, this);
        private void Unload() => CleanUp();
        private void OnPlayerDisconnected(BasePlayer player, string reason) => RemoveFromBattlecopterCache(player);
/*        void OnUserPermissionRevoked(string id, string permName)
        {
            if (permName != PERMISSION_USE) return;
            BasePlayer player;
            if(BasePlayerFromID(id, out player)) RemoveFromBattlecopterCache(player);
        }*/
        object CanMountEntity(BasePlayer player, BaseMountable entity)
        {
            MiniCopter m = entity.GetParentEntity() as MiniCopter;
            if (m == null) return null;
            if (!m.HasComponent<BattleCopterEntity>()) return null;
            if (!_battlecopterCache.ContainsKey(player)) return BooleanWithReply(player.IPlayer, "This is not your battlecopter!", false);
            if (_battlecopterCache[player] != m) return BooleanWithReply(player.IPlayer, "This is not your battlecopter!", false);
            return null;

        }
        #endregion

        #region Configuration

        #region Classes
        [JsonObject(MemberSerialization.OptIn)]
        private class Configuration
        {
            [JsonProperty(PropertyName = "Gun Settings")]
            private GunSettings _gun = new GunSettings();
            public GunSettings Gun => _gun;
            //public GunSettings Gun { get; set; } = new GunSettings();
            [JsonProperty(PropertyName = "Distance Check")]
            private float _distanceCheck = 50.0f;
            public float DistanceCheck => _distanceCheck;
            //public float DistanceCheck { get; set; } = 50.0f;

            [JsonProperty(PropertyName = "Draw Ray Cast")]
            private bool _drawRayCast = true;
            public bool DrawRayCast => _drawRayCast;
            //public bool DrawRayCast { get; set; } = true;

            [JsonObject(MemberSerialization.OptIn)]
            public class GunSettings
            {
                [JsonProperty(PropertyName = "Rockets Enabled")]
                private bool _rocketsEnabled = true;
                public bool RocketsEnabled => _rocketsEnabled;
                //public bool RocketsEnabled { get; set; } = true;

                [JsonProperty(PropertyName = "Machinegun Enabled")]
                private bool _machinegunEnabled = true;
                public bool MachinegunEnabled => _machinegunEnabled;
                //public bool MachinegunEnabled { get; set; } = true;

                [JsonProperty(PropertyName = "Rockets Velocity")]
                private float _rocketVelocity = 88.0f;
                public float RocketVelocity => _rocketVelocity;
                //public float RocketVelocity { get; set; } = 88.0f;
                [JsonProperty(PropertyName = "Machinegun Damage")]
                private float _machinegunDamage = 40.0f;
                public float MachinegunDamage => _machinegunDamage;
            }
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
                Error("Could not load config file.  Loading defaults...");
                LoadDefaultConfig();
                SaveConfig();
            }
        }

        protected override void LoadDefaultConfig() => _config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion Configuration

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["DistanceCheck"] = "Distance check failed",
                ["NoPermission"] = "You do not have permission to run this command"
            }, this);
        }

        #endregion

        #region BattleCopter Class
        private class BattleCopterEntity : MonoBehaviour
        {
            private MiniCopter _minicopter;
            private BasePlayer _driver;
            private InputState _lastInput;
            private void Update()
            {
                if (_minicopter == null) return;
                _driver = _minicopter.GetDriver();
                if (_driver != null) _lastInput = _driver.serverInput;
                else _lastInput = null;
            }
            private void Awake()
            {
                _minicopter = this.gameObject.GetComponent<MiniCopter>();
                if(_config.Gun.RocketsEnabled) InvokeRepeating(nameof(CheckRockets), 0f, 0.175f);
                if (_config.Gun.MachinegunEnabled) InvokeRepeating(nameof(CheckMachinegun), 0f, 0.1f);
            }

            private void OnDestroy()
            {
                CancelInvoke();
            }
            private void CheckRockets()
            {
                if (_minicopter == null || _driver == null || _lastInput == null) return;
                float additionalVelocity = (_minicopter.GetLocalVelocity().z > 0) ? _minicopter.GetLocalVelocity().z : 0;
                if (_lastInput.IsDown(BUTTON.FIRE_PRIMARY)) FireRockets(_driver,
                     _minicopter.transform.position,
                     _minicopter.transform.forward,
                     5.0f,
                     _minicopter.transform.rotation,
                     _config.Gun.RocketVelocity + additionalVelocity);
            }
            private void CheckMachinegun()
            {
                if (_minicopter == null || _driver == null || _lastInput == null) return;
                if (_lastInput.IsDown(BUTTON.FIRE_SECONDARY)) FireMachinegun(_driver, 
                    _minicopter.transform.position,
                    _minicopter.transform.forward,
                    5.0f);
            }
            private void FireRockets(BasePlayer player, Vector3 positionWorld, Vector3 normalWorld, float positionNormalOffset, Quaternion rotation, float initVelocity)
            {
                Vector3 offsetWorldPosition = positionWorld + (normalWorld * positionNormalOffset);
                BaseEntity rocketEntity = GameManager.server.CreateEntity(_rocketPrefab, offsetWorldPosition, rotation, true);
                if (rocketEntity == null) return;
                rocketEntity.creatorEntity = player;
                ServerProjectile projectile = rocketEntity.GetComponent<ServerProjectile>();
                if (projectile == null) return;
                projectile.InitializeVelocity(normalWorld * initVelocity);
                Effect.server.Run(_rocketEffectPrefab, rocketEntity.transform.position, rocketEntity.transform.forward, null, true);
                rocketEntity.Spawn();
            }
            private void FireMachinegun(BasePlayer player, Vector3 positionWorld, Vector3 normalWorld, float positionNormalOffset)
            {
                Vector3 offsetWorldPosition = positionWorld + (normalWorld * positionNormalOffset);
                RaycastHit hitInfo;
                Effect.server.Run(_machinegunEffectPrefab, offsetWorldPosition, normalWorld, null, true);
                // Layer mask value is from PatrolHelicopterAI -> DoMachineGuns
                if (!GamePhysics.Trace(new Ray(positionWorld, normalWorld), 0f, out hitInfo, 300f, 1219701521, QueryTriggerInteraction.UseGlobal)) return;
                if (!hitInfo.collider) return;
                string hitMaterialString = (hitInfo.collider.sharedMaterial ? hitInfo.collider.sharedMaterial.GetName() : "generic");
                BaseEntity e = hitInfo.GetEntity();
                if (e)
                {
                    HitInfo i = new HitInfo(player, e, Rust.DamageType.Bullet, _config.Gun.MachinegunDamage, hitInfo.point);
                    BaseCombatEntity ce = e as BaseCombatEntity;
                    if (ce)
                    {
                        if (hitInfo.collider && hitInfo.collider.name.Length > 0) i.HitBone = StringPool.Get(hitInfo.collider.name);
                        if (ce is BasePlayer) hitMaterialString = "Flesh";
                        ce.OnAttacked(i);
                    }
                    else
                    {
                        e.OnAttacked(i);
                    }
                }
                Effect.server.ImpactEffect(new HitInfo
                {
                    HitPositionWorld = hitInfo.point,
                    HitNormalWorld = normalWorld.Inverse(),
                    HitMaterial = StringPool.Get(hitMaterialString)
                });
            }
        }

        #endregion
    }
}