using Oxide.Core.Libraries.Covalence;
using UnityEngine;
using Rust;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;
using uMod.Libraries.Universal;
using System.Text;

namespace Oxide.Plugins
{
    [Info("ClansFF", "lolman300", "1.0.0")]
    [Description("Adds friendly fire checks to clans")]
    class ClansFF : CovalencePlugin
    {
        #region Fields
        [PluginReference] Plugin Clans;
        private Dictionary<string, bool> _clanFriendlyFireEnabledCache = new Dictionary<string, bool>();
        /*private Dictionary<BasePlayer, DateTime> _lastFFMessageCache = new Dictionary<BasePlayer, DateTime>();*/
        private const string PERMISSION_USE = "clansff.use";
        private const string HELP_TEXT = "<color=orange>ClansFF</color>" +
            "\nCommand usage: <color=yellow>/cff on | off</color> OR <color=yellow>/cff help</color>" +
            "\nClans friendly fire status: {0}";
        private DateTime _lastFF = DateTime.Now;
        #endregion
        #region Chat Commands
        [Command("cff"), Permission(PERMISSION_USE)]
        private void ClanFriendlyFire(IPlayer player, string command, string[] args)
        {
            BasePlayer basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;
            if (args.Length == 0)
            {
                player.Reply(string.Format(HELP_TEXT, GetClanStateString(basePlayer)));
                return;
            }
            switch (args[0].ToLower())
            {
                case ("on"):
                    {
                        if (SetClanFF(basePlayer, true)) player.Reply("Clan friendly fire enabled");
                        return;
                    }
                case ("off"):
                    {
                        if (SetClanFF(basePlayer, false)) player.Reply("Clan friendly fire disabled");
                        return;
                    }
                case ("help"):
                default:
                    player.Reply(string.Format(HELP_TEXT, GetClanStateString(basePlayer)));
                    return;

            }
        }
        #endregion
        #region Helper Functions
        private string GetClanStateString(BasePlayer player)
        {
            string clanTag = GetClanTag(player);
            if (string.IsNullOrEmpty(clanTag))
            {
                return "<color=#808080>N/A</color>";
            }
            return GetClanFF(clanTag) ? "<color=red>ON</color>" : "<color=green>OFF</color>";
        }
        private JArray GetAllClans()
        {
            return Clans?.Call<JArray>("GetAllClans");
        }
        private string GetClanTag(BasePlayer player)
        {
            return Clans?.Call<string>("GetClanOf", player);
        }
        private JObject GetClanObject(string clanName)
        {
            return Clans?.Call<JObject>("GetClan", clanName);
        }
        private bool IsClanOwner(BasePlayer player, JObject clan)
        {
            return (player.UserIDString == (clan?["owner"].ToString()));
        }
        private void SetClanFF(string clanTag, bool state)
        {
            _clanFriendlyFireEnabledCache[clanTag] = state;
        }
        private bool SetClanFF(BasePlayer player, bool state)
        {
            string clanTag = GetClanTag(player);
            if (string.IsNullOrEmpty(clanTag)) {
                player.IPlayer.Reply("You must be in a clan to issue this command");
                return false;
            }
            JObject clan = GetClanObject(clanTag);
            if(clan == null)
            {
                player.IPlayer.Reply("Clan was invalid! (1)");
                return false;
            }
            if (!IsClanOwner(player, clan))
            {
                player.IPlayer.Reply("You must be the clan owner to issue this command");
                return false;
            }
            SetClanFF(clanTag, state);
            return true;
        }
        private bool GetClanFF(string clanTag)
        {
            return _clanFriendlyFireEnabledCache[clanTag];
        }
        private bool GetClanFF(BasePlayer player)
        {
            return GetClanFF(GetClanTag(player));
        }
        private void AddClansFFCache(string clanTag, bool ff)
        {
            _clanFriendlyFireEnabledCache.Add(clanTag, ff);
        }
        private void RemoveClansFFCache(string clanTag)
        {
            _clanFriendlyFireEnabledCache.Remove(clanTag);
        }
        private void LoadClansFFCache()
        {
            JArray clansList = GetAllClans();
            if (clansList == null) return;
            foreach (string clanTag in clansList.Children())
            {
                AddClansFFCache(clanTag, false);
            }
        }
        private void UnloadClansFFCache()
        {
            _clanFriendlyFireEnabledCache.Clear();
        }
        private bool IsValidPlayer(BasePlayer player)
        {
            return (player != null) && (player.userID.IsSteamId());
        }

        #endregion
        #region Hooks
        private void Init()
        {
            permission.RegisterPermission(PERMISSION_USE, this);
            if (Clans == null) Clans = plugins.Find("Clans");
            if(Clans != null) LoadClansFFCache();
        }
        private void OnPluginLoaded(Plugin plugin)
        {
            if (plugin.Name == "Clans")
            {
                Clans = plugin;
                LoadClansFFCache();
            }
        }
        private void OnPluginUnloaded(Plugin plugin)
        {
            if (plugin.Name == "Clans")
            {
                UnloadClansFFCache();
                Clans = null;
            }
        }
        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            BasePlayer attacker = info.Initiator as BasePlayer;
            BasePlayer receiver = entity as BasePlayer;
            if (!IsValidPlayer(attacker) || !IsValidPlayer(receiver)) return null;
            if (attacker == receiver) return null;
            if(ShouldDamageClanFF(attacker, receiver) == false) return false;
            return null;
        }
        private void OnClanCreate(string tag)
        {
            AddClansFFCache(tag, false);
        }
        private void OnClanDestroy(string tag)
        {
            RemoveClansFFCache(tag);
        }
        private bool ShouldDamageClanFF(BasePlayer player1, BasePlayer player2)
        {
            if (player1 == null || player2 == null) return true;
            string clanTag1 = GetClanTag(player1);
            string clanTag2 = GetClanTag(player2);
            if (string.IsNullOrEmpty(clanTag1) || string.IsNullOrEmpty(clanTag2))
            {
                return true;
            }
            if (clanTag1 != clanTag2) return true;
            return GetClanFF(clanTag1);
        }
        #endregion
    }
}