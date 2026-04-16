using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace VintageTweaks.src.server;

internal sealed class WaypointSharing
{
    private readonly List<PendingWaypointShare> _pendingRequests = [];
    private readonly ICoreServerAPI _sapi;
    private readonly VintageTweaksSystem.WaypointConfig _config;

    public WaypointSharing(ICoreServerAPI sapi, VintageTweaksSystem.WaypointConfig config)
    {
        _sapi = sapi;
        _config = config;

        _sapi.ChatCommands
            .Create("wpshare")
            .WithArgs(_sapi.ChatCommands.Parsers.OnlinePlayer("player"), _sapi.ChatCommands.Parsers.Int("waypointid"))
            .WithDescription("Shares a waypoint with a player")
            .RequiresPrivilege(Privilege.chat)
            .RequiresPlayer()
            .HandleWith(HandleShareWaypointCommand);

        IChatCommand acceptCommand = _sapi.ChatCommands
            .Create("wpaccept")
            .WithArgs(_sapi.ChatCommands.Parsers.OnlinePlayer("player"), _sapi.ChatCommands.Parsers.Int("waypointid"))
            .WithDescription("Accepts a waypoint from a player")
            .RequiresPrivilege(Privilege.chat)
            .RequiresPlayer()
            .HandleWith(HandleAcceptWaypointCommand);

        acceptCommand
            .BeginSubCommand("all")
            .WithDescription("Accepts all pending waypoint shares")
            .RequiresPrivilege(Privilege.chat)
            .RequiresPlayer()
            .HandleWith(HandleAcceptAllWaypointsCommand);

        IChatCommand denyCommand = _sapi.ChatCommands
            .Create("wpdeny")
            .WithArgs(_sapi.ChatCommands.Parsers.OnlinePlayer("player"), _sapi.ChatCommands.Parsers.Int("waypointid"))
            .WithDescription("Denies a waypoint from a player")
            .RequiresPrivilege(Privilege.chat)
            .RequiresPlayer()
            .HandleWith(HandleDenyWaypointCommand);

        denyCommand
            .BeginSubCommand("all")
            .WithDescription("Denies all pending waypoint shares")
            .RequiresPrivilege(Privilege.chat)
            .RequiresPlayer()
            .HandleWith(HandleDenyAllWaypointsCommand);
    }

    private bool IsSharingDisabled()
    {
        return !_config.AllowSharingWaypoints;
    }

    private TextCommandResult HandleShareWaypointCommand(TextCommandCallingArgs args)
    {
        if (IsSharingDisabled())
        {
            return TextCommandResult.Error("Waypoint sharing is disabled on this server.");
        }

        if (args.Caller.Player is not IServerPlayer sender)
        {
            return TextCommandResult.Error("Only players can share waypoints.");
        }

        if (args.Parsers[0].GetValue() is not IServerPlayer recipient)
        {
            return TextCommandResult.Error("The recipient must be an online player.");
        }

        if (sender.PlayerUID == recipient.PlayerUID)
        {
            return TextCommandResult.Error("You cannot share a waypoint with yourself.");
        }

        int waypointId = (int)args.Parsers[1].GetValue();

        WaypointMapLayer waypointLayer = GetWaypointLayer();
        if (waypointLayer == null)
        {
            return TextCommandResult.Error("Waypoint map layer is not available.");
        }

        if (!TryGetPlayerWaypoint(waypointLayer, sender, waypointId, out Waypoint waypoint, out string errorMessage))
        {
            return TextCommandResult.Error(errorMessage);
        }

        string waypointTitle = GetWaypointTitle(waypoint);
        PendingWaypointShare share = new(
            sender.PlayerUID,
            recipient.PlayerUID,
            waypointId,
            CreateWaypointSnapshot(waypoint)
        );

        _pendingRequests.RemoveAll(request =>
            request.SenderUid == share.SenderUid &&
            request.RecipientUid == share.RecipientUid &&
            request.WaypointId == share.WaypointId);

        _pendingRequests.Add(share);

        recipient.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"{sender.PlayerName} wants to share waypoint \"{waypointTitle}\" (#{waypointId}) with you. Use /wpaccept {sender.PlayerName} {waypointId} to accept or /wpdeny {sender.PlayerName} {waypointId} to deny.",
            EnumChatType.Notification
        );

        _sapi.Logger.Notification(
            "[WaypointShare] {0} ({1}) shared waypoint '{2}' (ID: {3}) with {4} ({5})",
            sender.PlayerName,
            sender.PlayerUID,
            waypointTitle,
            waypointId,
            recipient.PlayerName,
            recipient.PlayerUID
        );

        return TextCommandResult.Success("Waypoint share request sent.");
    }

    private TextCommandResult HandleAcceptWaypointCommand(TextCommandCallingArgs args)
    {
        if (IsSharingDisabled())
        {
            return TextCommandResult.Error("Waypoint sharing is disabled on this server.");
        }

        if (args.Caller.Player is not IServerPlayer recipient)
        {
            return TextCommandResult.Error("Only players can accept waypoint shares.");
        }

        if (args.Parsers[0].GetValue() is not IServerPlayer sender)
        {
            return TextCommandResult.Error("The sender must be an online player.");
        }

        int waypointId = (int)args.Parsers[1].GetValue();

        PendingWaypointShare pendingRequest = FindPendingRequest(sender.PlayerUID, recipient.PlayerUID, waypointId);

        if (pendingRequest == null)
        {
            return TextCommandResult.Error("No pending waypoint share request found.");
        }

        WaypointMapLayer waypointLayer = GetWaypointLayer();
        if (waypointLayer == null)
        {
            return TextCommandResult.Error("Waypoint map layer is not available.");
        }

        int newWaypointId = waypointLayer.AddWaypoint(CreateWaypointForRecipient(pendingRequest.Waypoint, recipient), recipient);
        _pendingRequests.Remove(pendingRequest);

        sender.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"{recipient.PlayerName} has accepted your waypoint share request for \"{GetWaypointTitle(pendingRequest.Waypoint)}\".",
            EnumChatType.Notification
        );

        return TextCommandResult.Success($"Waypoint accepted and added as #{newWaypointId}.");
    }

    private TextCommandResult HandleAcceptAllWaypointsCommand(TextCommandCallingArgs args)
    {
        if (IsSharingDisabled())
        {
            return TextCommandResult.Error("Waypoint sharing is disabled on this server.");
        }

        if (args.Caller.Player is not IServerPlayer recipient)
        {
            return TextCommandResult.Error("Only players can accept waypoint shares.");
        }

        List<PendingWaypointShare> requestsToAccept = _pendingRequests
            .Where(request => request.RecipientUid == recipient.PlayerUID)
            .ToList();

        if (requestsToAccept.Count == 0)
        {
            return TextCommandResult.Error("No pending waypoint share requests found.");
        }

        WaypointMapLayer waypointLayer = GetWaypointLayer();
        if (waypointLayer == null)
        {
            return TextCommandResult.Error("Waypoint map layer is not available.");
        }

        foreach (PendingWaypointShare request in requestsToAccept)
        {
            waypointLayer.AddWaypoint(CreateWaypointForRecipient(request.Waypoint, recipient), recipient);
            _pendingRequests.Remove(request);

            IServerPlayer sender = GetOnlinePlayer(request.SenderUid);
            sender?.SendMessage(
                GlobalConstants.GeneralChatGroup,
                $"{recipient.PlayerName} has accepted your waypoint share request for \"{GetWaypointTitle(request.Waypoint)}\".",
                EnumChatType.Notification
            );
        }

        return TextCommandResult.Success($"{requestsToAccept.Count} waypoint share request(s) accepted.");
    }

    private TextCommandResult HandleDenyWaypointCommand(TextCommandCallingArgs args)
    {
        if (IsSharingDisabled())
        {
            return TextCommandResult.Error("Waypoint sharing is disabled on this server.");
        }

        if (args.Caller.Player is not IServerPlayer recipient)
        {
            return TextCommandResult.Error("Only players can deny waypoint shares.");
        }

        if (args.Parsers[0].GetValue() is not IServerPlayer sender)
        {
            return TextCommandResult.Error("The sender must be an online player.");
        }

        int waypointId = (int)args.Parsers[1].GetValue();

        PendingWaypointShare pendingRequest = FindPendingRequest(sender.PlayerUID, recipient.PlayerUID, waypointId);

        if (pendingRequest == null)
        {
            return TextCommandResult.Error("No pending waypoint share request found.");
        }

        _pendingRequests.Remove(pendingRequest);

        sender.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"{recipient.PlayerName} has denied your waypoint share request for \"{GetWaypointTitle(pendingRequest.Waypoint)}\".",
            EnumChatType.Notification
        );

        return TextCommandResult.Success("Waypoint denied.");
    }

    private TextCommandResult HandleDenyAllWaypointsCommand(TextCommandCallingArgs args)
    {
        if (IsSharingDisabled())
        {
            return TextCommandResult.Error("Waypoint sharing is disabled on this server.");
        }

        if (args.Caller.Player is not IServerPlayer recipient)
        {
            return TextCommandResult.Error("Only players can deny waypoint shares.");
        }

        List<PendingWaypointShare> requestsToDeny = _pendingRequests
            .Where(request => request.RecipientUid == recipient.PlayerUID)
            .ToList();

        if (requestsToDeny.Count == 0)
        {
            return TextCommandResult.Error("No pending waypoint share requests found.");
        }

        foreach (PendingWaypointShare request in requestsToDeny)
        {
            _pendingRequests.Remove(request);

            IServerPlayer sender = GetOnlinePlayer(request.SenderUid);
            sender?.SendMessage(
                GlobalConstants.GeneralChatGroup,
                $"{recipient.PlayerName} has denied your waypoint share request for \"{GetWaypointTitle(request.Waypoint)}\".",
                EnumChatType.Notification
            );
        }

        return TextCommandResult.Success($"{requestsToDeny.Count} waypoint share request(s) denied.");
    }

    private WaypointMapLayer GetWaypointLayer()
    {
        WorldMapManager mapSystem = _sapi.ModLoader.GetModSystem<WorldMapManager>();
        return mapSystem?.MapLayers?.OfType<WaypointMapLayer>().FirstOrDefault();
    }

    private static bool TryGetPlayerWaypoint(WaypointMapLayer waypointLayer, IServerPlayer player, int waypointId, out Waypoint waypoint, out string errorMessage)
    {
        waypoint = null;
        errorMessage = string.Empty;

        List<Waypoint> ownWaypoints = waypointLayer.Waypoints
            .Where(wp => wp != null && wp.OwningPlayerUid == player.PlayerUID)
            .ToList();

        if (ownWaypoints.Count == 0)
        {
            errorMessage = "You have no waypoints to share.";
            return false;
        }

        if (waypointId < 0 || waypointId >= ownWaypoints.Count)
        {
            errorMessage = $"Invalid waypoint id. Valid ids are 0..{ownWaypoints.Count - 1}.";
            return false;
        }

        waypoint = ownWaypoints[waypointId];
        if (waypoint.Position == null)
        {
            errorMessage = "That waypoint has no position and cannot be shared.";
            return false;
        }

        return true;
    }

    private PendingWaypointShare FindPendingRequest(string senderUid, string recipientUid, int waypointId)
    {
        return _pendingRequests.FirstOrDefault(request =>
            request.SenderUid == senderUid &&
            request.RecipientUid == recipientUid &&
            request.WaypointId == waypointId);
    }

    private IServerPlayer GetOnlinePlayer(string playerUid)
    {
        return _sapi.World.AllOnlinePlayers
            .OfType<IServerPlayer>()
            .FirstOrDefault(player => player.PlayerUID == playerUid);
    }

    private static Waypoint CreateWaypointSnapshot(Waypoint waypoint)
    {
        return new Waypoint
        {
            Position = waypoint.Position.Clone(),
            Title = waypoint.Title,
            Text = waypoint.Text,
            Color = waypoint.Color,
            Icon = waypoint.Icon,
            ShowInWorld = waypoint.ShowInWorld,
            Pinned = waypoint.Pinned,
            OwningPlayerUid = waypoint.OwningPlayerUid,
            OwningPlayerGroupId = waypoint.OwningPlayerGroupId,
            Temporary = false,
            Guid = waypoint.Guid
        };
    }

    private static Waypoint CreateWaypointForRecipient(Waypoint waypoint, IServerPlayer recipient)
    {
        return new Waypoint
        {
            Position = waypoint.Position.Clone(),
            Title = GetWaypointTitle(waypoint),
            Text = waypoint.Text,
            Color = waypoint.Color,
            Icon = waypoint.Icon,
            ShowInWorld = waypoint.ShowInWorld,
            Pinned = waypoint.Pinned,
            OwningPlayerUid = recipient.PlayerUID,
            OwningPlayerGroupId = 0,
            Temporary = false,
            Guid = Guid.NewGuid().ToString()
        };
    }

    private static string GetWaypointTitle(Waypoint waypoint)
    {
        if (!string.IsNullOrWhiteSpace(waypoint.Title))
        {
            return waypoint.Title;
        }

        if (!string.IsNullOrWhiteSpace(waypoint.Text))
        {
            return waypoint.Text;
        }

        return "Unnamed waypoint";
    }

    private sealed record PendingWaypointShare(
        string SenderUid,
        string RecipientUid,
        int WaypointId,
        Waypoint Waypoint);
}
