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
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace AdminMaxStats
{
    /// <summary>
    /// A session component that automatically maxes out the stats (energy, health, hydrogen, oxygen)
    /// for players with the promotion level of SpaceMaster or higher.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Session : MySessionComponentBase
    {
        /// <summary>
        /// List of players currently in the session. This is populated every update interval.
        /// </summary>
        private List<IMyPlayer> _players = new List<IMyPlayer>();

        /// <summary>
        /// Constants for maximum player stats.
        /// MaxLevel is used for energy, hydrogen, and oxygen.
        /// MaxHealth is the full health value.
        /// </summary>
        private const float MaxLevel = 1.0f;  // Max level for energy, hydrogen, and oxygen
        private const int MaxHealth = 100;    // Max health for the player

        /// <summary>
        /// The interval, in ticks, at which player stats are updated.
        /// 1 tick is roughly 1/60th of a second, so 300 ticks is around 5 seconds.
        /// </summary>
        private int _updateInterval = 300;

        /// <summary>
        /// Counter to track the number of ticks since the last update.
        /// When this reaches _updateInterval, player stats are updated.
        /// </summary>
        private int _ticksSinceLastUpdate = 0;

        /// <summary>
        /// This method is called every simulation tick (after the game processes its logic).
        /// It checks if the update interval has passed, and if so, it updates the stats
        /// of all players in the session with promotion level of SpaceMaster or higher.
        /// </summary>
        public override void UpdateAfterSimulation()
        {
            // Check if it's time to perform an update based on the tick counter.
            if (_ticksSinceLastUpdate++ < _updateInterval) return;

            // Reset the tick counter after performing the update.
            _ticksSinceLastUpdate = 0;

            // Clear the player list and repopulate it with the current players in the session.
            _players.Clear();
            MyAPIGateway.Players.GetPlayers(_players);

            // Iterate over each player in the session.
            foreach (var player in _players)
            {
                // Skip bots and players without a character (e.g., those not fully spawned).
                if (player.IsBot || player.Character == null) continue;

                // If the player's promotion level is SpaceMaster or higher, apply max stats.
                if (player.PromoteLevel >= MyPromoteLevel.SpaceMaster)
                {
                    ApplyMaxStatsToPlayer(player);
                }
            }
        }

        /// <summary>
        /// Applies maximum energy, health, hydrogen, and oxygen levels to the given player.
        /// This method only updates stats that are not already at the maximum value.
        /// </summary>
        /// <param name="player">The player to apply max stats to.</param>
        private void ApplyMaxStatsToPlayer(IMyPlayer player)
        {
            var identityId = player.IdentityId;

            // Only apply max energy if the current energy is below the maximum level.
            if (MyVisualScriptLogicProvider.GetPlayersEnergyLevel(identityId) < MaxLevel)
            {
                MyVisualScriptLogicProvider.SetPlayersEnergyLevel(identityId, MaxLevel);
            }

            // Only apply max health if the current health is below the maximum value.
            if (MyVisualScriptLogicProvider.GetPlayersHealth(identityId) < MaxHealth)
            {
                MyVisualScriptLogicProvider.SetPlayersHealth(identityId, MaxHealth);
            }

            // Only apply max hydrogen if the current hydrogen level is below the maximum level.
            if (MyVisualScriptLogicProvider.GetPlayersHydrogenLevel(identityId) < MaxLevel)
            {
                MyVisualScriptLogicProvider.SetPlayersHydrogenLevel(identityId, MaxLevel);
            }

            // Only apply max oxygen if the current oxygen level is below the maximum level.
            if (MyVisualScriptLogicProvider.GetPlayersOxygenLevel(identityId) < MaxLevel)
            {
                MyVisualScriptLogicProvider.SetPlayersOxygenLevel(identityId, MaxLevel);
            }
        }
    }
}
