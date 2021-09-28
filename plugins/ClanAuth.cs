// https://bit.ly/3ko6ysD
using Newtonsoft.Json.Linq;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using ProtoBuf;
using UnityEngine;

// Abandoned - just going to use AutomaticAuthorization
namespace Oxide.Plugins
{
    [Info("ClanAuth", "lolman300", "0.0.1")]
    [Description("Adds automatic authorization to clans")]
    class ClanAuth : CovalencePlugin
    {
        #region
        [PluginReference] private Plugin Clans;
        private const string PERMISSION_USE = "clanauth.use";
        #endregion
        #region Hooks
        private void Init() => permission.RegisterPermission(PERMISSION_USE, this);
        private void Loaded()
        {
            if (Clans == null) LogError("Clans is not loaded, get it at https://umod.org/plugins/clans");
        }
        private object CanUseLockedEntity(BasePlayer player, BaseLock baseLock)
        {
            if (Clans == null || player == null || player.IPlayer == null || !player.IPlayer.HasPermission(PERMISSION_USE) || baseLock == null || !baseLock.IsLocked() || !baseLock.OwnerID.IsSteamId()) return null;
            BaseEntity parentEntity = baseLock.GetParentEntity();
            ulong ownerID = baseLock.OwnerID.IsSteamId() ? baseLock.OwnerID : parentEntity != null ? parentEntity.OwnerID : 0;
            if (!ownerID.IsSteamId() || ownerID == player.userID) return null;
            if (Clans?.Call<bool>("IsClanMember", player.UserIDString, baseLock.OwnerID.ToString()) == true) return true;
            return null;
        }
        object OnConstructionPlace(BaseEntity entity, Construction component, Construction.Target constructionTarget, BasePlayer player)
        {
            if (Clans == null || entity == null || player == null || !player.UserIDString.IsSteamId()) return null;
            BuildingPrivlidge b = entity as BuildingPrivlidge;
            if (b == null) return null;
            string clanTag = Clans.Call<string>("GetClanOf", player);
            if (string.IsNullOrEmpty(clanTag)) return null;
            JObject clanJson = Clans.Call<JObject>("GetClan", clanTag);
            foreach(string s in clanJson["members"]) {
                IPlayer p = players.FindPlayerById(s);
                if (p == null) continue;
                PlayerNameID playerNameID = new PlayerNameID { userid = ulong.Parse(s), username = p.Name };
                b.authorizedPlayers.Add(playerNameID);
            }
            return null;
        }
        // Do auto turrets
        #endregion
    }
}