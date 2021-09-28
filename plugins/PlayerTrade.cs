

using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("PlayerTrade", "lolman300", "0.0.1")]
    [Description("Player trading")]
    class PlayerTrade : CovalencePlugin
    {
        #region Fields
        private Dictionary<IPlayer, Tuple<IPlayer, Timer>> _tradePendingInviterCache = new Dictionary<IPlayer, Tuple<IPlayer, Timer>>();
        private Dictionary<IPlayer, Tuple<IPlayer, Timer>> _tradePendingInviteeCache = new Dictionary<IPlayer, Tuple<IPlayer, Timer>>();
        private Dictionary<Tuple<IPlayer, IPlayer>, Tuple<ShopFrontLootPanel, Timer>> _tradeSessionCache = new Dictionary<Tuple<IPlayer, IPlayer>, Tuple<ShopFrontLootPanel, Timer>>();
        private const string PERMISSION_USE = "playertrade.use";
        private const string HELP_TEXT = "<color=orange>PlayerTrade</color>" +
            "\n<color=yellow>/trade</color> OR <color=yellow>/tr \"</color><color=#55aaff>player</color><color=yellow>\"</color> - Send a trade request" +
            "\n<color=yellow>/trade accept</color> OR <color=yellow>/tra</color> - Accept a trade" +
            "\n<color=yellow>/trade cancel</color> OR <color=yellow>/trc</color> - Cancel a trade" +
            "\n<color=yellow>/trade decline</color> OR <color=yellow>/trd</color> - Decline a trade" +
            "\n<color=yellow>/trade help</color> - Display this help text";
        private const float _tradePendingDuration = 30.0f;
        private const float _tradeSessionDuration = 120.0f;
        #endregion
        #region Commands
        [Command("trade"), Permission(PERMISSION_USE)]
        private void Trade(IPlayer player, string command, string[] args)
        {
            if (player == null || !player.IsConnected) return;
            if (args.Length == 0)
            {
                SendPlayerMessage(player, string.Format(HELP_TEXT));
                return;
            }
            switch (args[0].ToLower())
            {
                case ("accept"):
                    {
                        TradeAccept(player);
                        return;
                    }
                case ("decline"):
                    {
                        TradeDecline(player);
                        return;
                    }
                case ("cancel"):
                    {
                        TradeCancel(player);
                        return;
                    }
                case ("help"):
                    {
                        SendPlayerMessage(player, string.Format(HELP_TEXT));
                        return;
                    }
                default:
                    {
                        TradeRequest(player, command, args);
                        return;
                    }

            }
        }
        [Command("tr"), Permission(PERMISSION_USE)]
        private void TradeRequest(IPlayer inviter, string command, string[] args)
        {
            if (args.Length == 0)
            {
                SendPlayerMessage(inviter, string.Format(HELP_TEXT));
                return;
            }
            if(_tradePendingInviterCache.ContainsKey(inviter))
            {
                SendPlayerMessage(inviter, "You already have a pending outgoing trade request\nType <color=yellow>/trc</color> to cancel");
                return;
            }
            IPlayer invitee = players.FindPlayer(args[0]);
            if(invitee == null || !invitee.IsConnected)
            {
                SendPlayerMessage(inviter, "Could not find any players with the name '<color=#55aaff>" + args[0] + "</color>'");
                return;
            }
            if(!permission.UserHasPermission(invitee.Id, PERMISSION_USE))
            {
                SendPlayerMessage(inviter, "'<color=#55aaff>" + args[0] + "</color>' does not have permission to use trade");
                return;
            }
            if (_tradePendingInviterCache.ContainsKey(invitee))
            {
                SendPlayerMessage(inviter, "'<color=#55aaff>" + invitee.Name + "</color>' already has an outgoing trade request");
                SendPlayerMessage(invitee, "'<color=#55aaff>" + inviter.Name + "</color>' tried trading you, but you already have a pending outgoing trade request \nType <color=yellow>/trc</color> to cancel");
                return;
            }
            if (_tradePendingInviteeCache.ContainsKey(invitee))
            {
                SendPlayerMessage(inviter, "'<color=#55aaff>" + invitee.Name + "</color>' already has an incoming trade request");
                SendPlayerMessage(invitee, "'<color=#55aaff>" + inviter.Name + "</color>' tried trading you, but you already have a pending incoming trade request \nType <color=yellow>/trd</color> to decline");
                return;
            }
            SendTrade(inviter, invitee);
        }
        [Command("tra"), Permission(PERMISSION_USE)]
        private void TradeAccept(IPlayer invitee)
        {
            if (!_tradePendingInviteeCache.ContainsKey(invitee))
            {
                SendPlayerMessage(invitee, "You do not have an incoming trade request");
                return;
            }
            IPlayer inviter = _tradePendingInviteeCache[invitee].Item1;
            RemoveInviterFromPendingCache(inviter);
            RemoveInviteeFromPendingCache(invitee);
            Tuple<IPlayer, IPlayer> playersTuple = new Tuple<IPlayer, IPlayer>(inviter, invitee);
            _tradeSessionCache.Add(playersTuple, new Tuple<ShopFront, timer.In(_tradeSessionDuration, () =>
            {
                _tradeSessionCache.Remove(playersTuple);
                SendPlayerMessage(inviter, "Trade session expired");
                SendPlayerMessage(invitee, "Trade session expired");
            }));
            SendPlayerMessage(inviter, "Accepted trade");
            SendPlayerMessage(invitee, "Accepted trade");
            return;
        }
        [Command("trc"), Permission(PERMISSION_USE)]
        private void TradeCancel(IPlayer inviter)
        {
            if(!_tradePendingInviterCache.ContainsKey(inviter))
            {
                SendPlayerMessage(inviter, "You do not have an outgoing trade request");
                return;
            }
            IPlayer invitee = _tradePendingInviterCache[inviter].Item1;
            RemoveInviterFromPendingCache(inviter);
            RemoveInviteeFromPendingCache(invitee);
            SendPlayerMessage(inviter, "Outgoing trade request has been cancelled");
            SendPlayerMessage(invitee, "Incoming trade request has been cancelled");
            return;
        }
        [Command("trd"), Permission(PERMISSION_USE)]
        private void TradeDecline(IPlayer invitee)
        {
            if (!_tradePendingInviteeCache.ContainsKey(invitee))
            {
                SendPlayerMessage(invitee, "You do not have an incoming trade request");
                return;
            }
            IPlayer inviter = _tradePendingInviteeCache[invitee].Item1;
            RemoveInviterFromPendingCache(inviter);
            RemoveInviteeFromPendingCache(invitee);
            SendPlayerMessage(inviter, "Outgoing trade request has been declined");
            SendPlayerMessage(invitee, "Incoming trade request has been declined");
            return;
        }
        #endregion
        #region Helper Functions
        private void NewTradeInterface()
        {
            // 1. Create shot front

            //  a. Shop front slots are a row + 2
            //  b. Shop front should show player name
            //  c. Right click to put in shop front window
            //  d. Accept button
            // 2. Open shop front for both players
            // 3. Cancel if players move, sleep, die, log off, etc...
        }
        private void RemoveInviterFromPendingCache(IPlayer player)
        {
            _tradePendingInviterCache[player].Item2.Destroy();
            _tradePendingInviterCache.Remove(player);
        }
        private void RemoveInviteeFromPendingCache(IPlayer player)
        {
            _tradePendingInviteeCache[player].Item2.Destroy();
            _tradePendingInviteeCache.Remove(player);
        }
        private void SendTrade(IPlayer inviter, IPlayer invitee)
        {
            _tradePendingInviterCache.Add(inviter, new Tuple<IPlayer, Timer>(invitee, timer.In(_tradePendingDuration, () => {
                _tradePendingInviterCache.Remove(inviter);
                SendPlayerMessage(inviter, "Outgoing trade request has expired");
            })));
            SendPlayerMessage(inviter, "Sent trade request to '<color=#55aaff>" + invitee.Name + "</color>'");
            _tradePendingInviteeCache.Add(invitee, new Tuple<IPlayer, Timer>(inviter, timer.In(_tradePendingDuration, () => {
                _tradePendingInviteeCache.Remove(invitee);
                SendPlayerMessage(invitee, "Incoming trade request has expired");
            })));
            SendPlayerMessage(invitee, "'<color=#55aaff>" + inviter.Name + "</color>' has sent you a trade request!");
        }
        private void SendPlayerMessage(BasePlayer player, string message)
        {
            if (player == null) return;
            SendPlayerMessage(player.IPlayer, message);
        }
        private void SendPlayerMessage(IPlayer player, string message)
        {
            if (player == null || !player.IsConnected) return;
            player.Reply(message);
        }
        #endregion
        #region Hooks
        private void Init() => permission.RegisterPermission(PERMISSION_USE, this);
        void OnUserDisconnected(IPlayer player)
        {
            if (_tradePendingInviterCache.ContainsKey(player))
            {
#if DEBUG
                Puts(player.Name + " logged off.  Cancelling trade");
#endif
                TradeCancel(player);
                return;
            }
            if (_tradePendingInviteeCache.ContainsKey(player))
            {
#if DEBUG
                Puts(player.Name + " logged off.  Declining trade");
#endif
                TradeDecline(player);
                return;
            }
        }
        #endregion
    }
}
