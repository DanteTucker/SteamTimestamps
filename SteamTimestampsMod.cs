using ResoniteModLoader;
using FrooxEngine;
using Steamworks;
using System;
using System.Collections.Generic;

namespace SteamTimestamps;

public class SteamTimestampsMod : ResoniteMod
{
    public override string Name    => "SteamTimestamps";
    public override string Author  => "Dante";
    public override string Version => "1.0.0";
    public override string Link    => "https://github.com/DanteTucker/SteamTimestamps";

    private static readonly HashSet<World> _readyWorlds = new();

    public override void OnEngineInit()
    {
        Msg("SteamTimestamps initialised — hooking WorldManager events.");

        Engine.Current.RunPostInit(() =>
        {
            Engine.Current.WorldManager.WorldAdded   += OnWorldAdded;
            Engine.Current.WorldManager.WorldFocused += OnWorldFocused;
        });
    }

    private static void OnWorldAdded(World world)
    {
        if (world.IsUserspace())
            return;

        world.WorldRunning += OnWorldRunning;
        world.WorldDestroyed += OnWorldDestroyed;
    }

    private static void OnWorldRunning(World world)
    {
        world.UserJoined += (user) => OnUserJoined(world, user);

        world.UserJoined += (user) => OnUserJoined(world, user);

        world.RunSynchronously(() =>
        {
            lock (_readyWorlds)
                _readyWorlds.Add(world);

            AddTimelineEvent(
                title:       "Joined Session",
                description: world.RawName,
                icon:        "steam_avatar_user",
                priority:    100u,
                clipPriority: ETimelineEventClipPriority.k_ETimelineEventClipPriority_Standard);

            Msg($"Session joined timestamp — world: {world.RawName}");
        });
    }

    private static void OnWorldDestroyed(World world)
    {
        lock (_readyWorlds)
            _readyWorlds.Remove(world);
    }

    private static void OnWorldFocused(World world)
    {
        if (world.IsUserspace())
            return;
        lock (_readyWorlds)
            if (!_readyWorlds.Contains(world))
                return;

        AddTimelineEvent(
            title:       "Changed World",
            description: world.RawName,
            icon:        "steam_location",
            priority:    75u,
            clipPriority: ETimelineEventClipPriority.k_ETimelineEventClipPriority_Standard);

        Msg($"Changed world timestamp — world: {world.RawName}");
    }

    private static void OnUserJoined(World world, User user)
    {
        lock (_readyWorlds)
            if (!_readyWorlds.Contains(world))
                return;

        if (user.IsLocalUser)
            return;

        if (world.Focus != World.WorldFocus.Focused)
            return;

        string userName = user.UserName ?? "Unknown";

        AddTimelineEvent(
            title:        "User Joined",
            description:  $"{userName} joined {world.RawName}",
            icon:         "steam_avatar_friend_joined",
            priority:     50u,
            clipPriority: ETimelineEventClipPriority.k_ETimelineEventClipPriority_Standard);

        Msg($"User joined timestamp — user: {userName}, world: {world.RawName}");
    }

    private static void AddTimelineEvent(
        string title,
        string description,
        string icon,
        uint   priority,
        ETimelineEventClipPriority clipPriority)
    {
        try
        {
            if (!SteamAPI.IsSteamRunning())
            {
                Warn("Steam is not running — skipping timeline event.");
                return;
            }

            SteamTimeline.AddInstantaneousTimelineEvent(
                pchTitle:             title,
                pchDescription:       description,
                pchIcon:              icon,
                unIconPriority:       priority,
                flStartOffsetSeconds: 0.0f,
                ePossibleClip:        clipPriority);
        }
        catch (Exception ex)
        {
            Error($"Failed to add Steam timeline event: {ex}");
        }
    }
}
