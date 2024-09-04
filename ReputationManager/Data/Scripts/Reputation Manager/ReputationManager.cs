/*
 * 
 * This open-source project is available on GitHub and licensed under the GNU General Public License v3.0.
 * You are free to use, modify, and distribute this software under the terms of the GPL-3.0 license.
 * 
 * GitHub Repository: [https://github.com/MisterRee/Mods]
 * 
 */
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace ReputationManager
{
    /// <summary>
    /// Manages player reputation with friendly, hostile, and neutral factions based on predefined tags.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class ReputationManager : MySessionComponentBase
    {
        // Faction tag categories (configurable via settings)
        public string[] friendlyFactionTags = { "SDRC" };
        public string[] hostileFactionTags = { "SPRT" };
        public string[] neutralFactionTags = { "CORS" };

        // Faction and identity tracking
        private Dictionary<long, IMyFaction> FriendlyFactions = new Dictionary<long, IMyFaction>();
        private Dictionary<long, IMyFaction> HostileFactions = new Dictionary<long, IMyFaction>();
        private Dictionary<long, IMyFaction> NeutralFactions = new Dictionary<long, IMyFaction>();
        private List<IMyIdentity> Identities = new List<IMyIdentity>();

        // Stack for reusing reputation change data
        private Stack<ReputationChangeData> ReputationChangeStack = new Stack<ReputationChangeData>(4);
        private List<ReputationChangeData> ReputationChanges = new List<ReputationChangeData>();

        // Time interval for reputation checks (500 ticks)
        private int updateCounter = 0;
        private const int updateInterval = 500;

        /// <summary>
        /// Struct to hold reputation change data.
        /// </summary>
        public struct ReputationChangeData
        {
            public long IdentityId;
            public long FactionId;
            public int Reputation;

            public void Populate(long identityId, long factionId, int reputation)
            {
                IdentityId = identityId;
                FactionId = factionId;
                Reputation = reputation;
            }
        }

        /// <summary>
        /// Called when the mod is first loaded. Registers event handlers.
        /// </summary>
        public override void LoadData()
        {
            if (!MyAPIGateway.Multiplayer.IsServer) return;

            // Register reputation changed event
            MyAPIGateway.Session.Factions.ReputationChanged += ReputationChanged;
            // Register player connected event
            MyVisualScriptLogicProvider.PlayerConnected += PlayerConnected;
        }

        /// <summary>
        /// Handles player connection events. Logs when a player joins.
        /// </summary>
        private void PlayerConnected(long identityId)
        {
            MyLog.Default.WriteLineAndConsole($"[ReputationManager] Player {identityId} connected.");
        }

        /// <summary>
        /// Unloads data and unregisters event handlers.
        /// </summary>
        protected override void UnloadData()
        {
            if (!MyAPIGateway.Multiplayer.IsServer) return;

            // Unregister events
            MyAPIGateway.Session.Factions.ReputationChanged -= ReputationChanged;
            MyVisualScriptLogicProvider.PlayerConnected -= PlayerConnected;
        }

        /// <summary>
        /// Handles reputation changes between players and factions.
        /// Logs and stores the changes for further processing.
        /// </summary>
        /// <param name="identityId">Player's identity ID</param>
        /// <param name="factionId">Faction ID</param>
        /// <param name="newReputation">New reputation value</param>
        /// <param name="reason">The reason for the reputation change</param>
        private void ReputationChanged(long identityId, long factionId, int newReputation, ReputationChangeReason reason)
        {
            MyLog.Default.WriteLineAndConsole($"[ReputationManager] Reputation change for player {identityId} in faction {factionId}: new reputation = {newReputation}, Reason: {reason}");

            if (!IsTrackedFaction(factionId)) return; // Ignore non-tracked factions
            if (MyAPIGateway.Players.TryGetSteamId(identityId) <= 0) return; // Ignore if no valid Steam ID

            var data = ReputationChangeStack.Any() ? ReputationChangeStack.Pop() : new ReputationChangeData();
            data.Populate(identityId, factionId, newReputation);
            ReputationChanges.Add(data);

            MyLog.Default.WriteLineAndConsole($"[ReputationManager] Recorded reputation change: Player {identityId}, Faction {factionId}, New Reputation {newReputation}.");
        }

        /// <summary>
        /// Processes and sets player reputation for each faction based on their relationship (friendly, hostile, neutral).
        /// </summary>
        private void SetReputation()
        {
            // Clone the list to avoid modifying it while iterating
            var changesToProcess = new List<ReputationChangeData>(ReputationChanges);

            foreach (var data in changesToProcess)
            {
                var identityId = data.IdentityId;
                var factionId = data.FactionId;
                var reputation = data.Reputation;

                ReputationChangeStack.Push(data); // Recycle data for future use

                // Adjust reputation based on faction type
                AdjustReputation(identityId, factionId, reputation);
            }

            ReputationChanges.Clear(); // Clear the original list after processing
        }

        /// <summary>
        /// Adjusts the player's reputation with the faction based on predefined thresholds.
        /// </summary>
        private void AdjustReputation(long identityId, long factionId, int currentReputation)
        {
            IMyFaction faction;

            if (FriendlyFactions.TryGetValue(factionId, out faction) && currentReputation < 500)
            {
                MyAPIGateway.Session.Factions.SetReputationBetweenPlayerAndFaction(identityId, factionId, 1500);
                MyLog.Default.WriteLineAndConsole($"[ReputationManager] Set FRIENDLY reputation for player {identityId} with faction {factionId}.");
            }
            else if (HostileFactions.TryGetValue(factionId, out faction) && currentReputation > -500)
            {
                MyAPIGateway.Session.Factions.SetReputationBetweenPlayerAndFaction(identityId, factionId, -1500);
                MyLog.Default.WriteLineAndConsole($"[ReputationManager] Set HOSTILE reputation for player {identityId} with faction {factionId}.");
            }
            else if (NeutralFactions.TryGetValue(factionId, out faction) && (currentReputation > 500 || currentReputation < -500))
            {
                MyAPIGateway.Session.Factions.SetReputationBetweenPlayerAndFaction(identityId, factionId, 0);
                MyLog.Default.WriteLineAndConsole($"[ReputationManager] Set NEUTRAL reputation for player {identityId} with faction {factionId}.");
            }
        }

        /// <summary>
        /// Initializes faction tracking based on predefined faction tags.
        /// Called before the game starts.
        /// </summary>
        public override void BeforeStart()
        {
            MyLog.Default.WriteLineAndConsole("[ReputationManager] Initializing factions.");

            PopulateFactions(friendlyFactionTags, FriendlyFactions, "FRIENDLY");
            PopulateFactions(hostileFactionTags, HostileFactions, "HOSTILE");
            PopulateFactions(neutralFactionTags, NeutralFactions, "NEUTRAL");
        }

        /// <summary>
        /// Populates faction lists based on tags.
        /// </summary>
        private void PopulateFactions(string[] factionTags, Dictionary<long, IMyFaction> factionDictionary, string factionType)
        {
            if (factionTags == null || factionTags.Length == 0) return;

            foreach (string tag in factionTags)
            {
                var faction = MyAPIGateway.Session.Factions.TryGetFactionByTag(tag);
                if (faction == null) continue;

                factionDictionary[faction.FactionId] = faction;
                MyLog.Default.WriteLineAndConsole($"[ReputationManager] Found {factionType} faction \"{faction.Name}\" with tag \"{tag}\".");
            }
        }

        /// <summary>
        /// Periodic reputation updates every `updateInterval` ticks.
        /// </summary>
        public override void UpdateBeforeSimulation()
        {
            updateCounter++;
            if (updateCounter % updateInterval != 0) return; // Only run every `updateInterval` ticks

            if (!MyAPIGateway.Multiplayer.IsServer) return;

            if (ReputationChanges.Any()) SetReputation(); // Apply pending reputation changes

            UpdatePlayerFactions(); // Ensure players' reputations are in sync
        }

        /// <summary>
        /// Updates players' reputations with all tracked factions.
        /// </summary>
        private void UpdatePlayerFactions()
        {
            Identities.Clear();
            MyAPIGateway.Players.GetAllIdentites(Identities);

            foreach (var identity in Identities)
            {
                if (MyAPIGateway.Players.TryGetSteamId(identity.IdentityId) <= 0) continue;

                UpdateFactionReputation(identity.IdentityId, FriendlyFactions, 1500, "FRIENDLY");
                UpdateFactionReputation(identity.IdentityId, HostileFactions, -1500, "HOSTILE");
                UpdateFactionReputation(identity.IdentityId, NeutralFactions, 0, "NEUTRAL");
            }
        }

        /// <summary>
        /// Updates the player's reputation with a specific faction type.
        /// </summary>
        private void UpdateFactionReputation(long identityId, Dictionary<long, IMyFaction> factionDict, int targetReputation, string factionType)
        {
            foreach (var pair in factionDict)
            {
                var factionId = pair.Key;
                var faction = pair.Value;

                int currentReputation = MyAPIGateway.Session.Factions.GetReputationBetweenPlayerAndFaction(identityId, factionId);

                if ((factionType == "FRIENDLY" && currentReputation >= 500) ||
                    (factionType == "HOSTILE" && currentReputation <= -500) ||
                    (factionType == "NEUTRAL" && currentReputation == 0)) continue;

                MyAPIGateway.Session.Factions.SetReputationBetweenPlayerAndFaction(identityId, factionId, targetReputation);
                MyLog.Default.WriteLineAndConsole($"[ReputationManager] Setting {factionType} reputation for player {identityId} with faction {factionId}.");
            }
        }

        /// <summary>
        /// Checks if the faction is tracked (in friendly, hostile, or neutral factions).
        /// </summary>
        private bool IsTrackedFaction(long factionId)
        {
            return FriendlyFactions.ContainsKey(factionId) || HostileFactions.ContainsKey(factionId) || NeutralFactions.ContainsKey(factionId);
        }
    }
}
