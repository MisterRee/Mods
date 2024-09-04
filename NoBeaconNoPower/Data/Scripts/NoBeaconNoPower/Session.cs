/*
 * 
 * This open-source project is available on GitHub and licensed under the GNU General Public License v3.0.
 * You are free to use, modify, and distribute this software under the terms of the GPL-3.0 license.
 * 
 * GitHub Repository: [https://github.com/MisterRee/Mods]
 * 
 */
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.Entity;

namespace NoBeaconNoPower
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class NoBeaconNoPower : MySessionComponentBase
    {
        private readonly HashSet<long> InitializedGrids = new HashSet<long>();

        public override void LoadData()
        {
            Logs.InitLogs();
            Logs.WriteLine("LoadData called. Initializing NoBeaconNoPower session component.");

            if (!MyAPIGateway.Session.IsServer)
            {
                Logs.WriteLine("Not running on the server. Exiting LoadData.");
                return;
            }

            Logs.WriteLine("Running on server. Subscribing to OnEntityCreate event.");
            MyEntities.OnEntityCreate += OnEntityCreate;
        }

        protected override void UnloadData()
        {
            Logs.WriteLine("UnloadData called. Unsubscribing from OnEntityCreate event.");
            MyEntities.OnEntityCreate -= OnEntityCreate;

            Logs.WriteLine("Closing logs.");
            Logs.Close();
        }

        private void OnEntityCreate(MyEntity entity)
        {
            Logs.WriteLine($"OnEntityCreate called for entity: {entity.EntityId}, Type: {entity.GetType().Name}");

            var grid = entity as MyCubeGrid;
            if (grid != null)
            {
                Logs.WriteLine($"Entity {entity.EntityId} is a MyCubeGrid. Checking if already initialized.");

                // Prevent initializing the same grid multiple times.
                if (InitializedGrids.Contains(grid.EntityId))
                {
                    Logs.WriteLine($"Grid {grid.EntityId} is already initialized. Skipping.");
                    return;
                }

                Logs.WriteLine($"Grid {grid.EntityId} is new. Attaching AddedToScene event.");
                grid.AddedToScene += AddedToScene;
            }
            else
            {
                Logs.WriteLine($"Entity {entity.EntityId} is not a MyCubeGrid. Ignoring.");
            }
        }

        private void AddedToScene(MyEntity entity)
        {
            Logs.WriteLine($"AddedToScene called for entity: {entity.EntityId}, Type: {entity.GetType().Name}");

            var grid = entity as MyCubeGrid;
            if (grid != null)
            {
                if (InitializedGrids.Contains(grid.EntityId))
                {
                    Logs.WriteLine($"Grid {grid.EntityId} is already processed. Skipping.");
                    return;
                }

                if (grid.Physics == null)
                {
                    Logs.WriteLine($"Grid {grid.EntityId} has no physics. Ignoring.");
                    return;
                }

                if (grid.IsPreview)
                {
                    Logs.WriteLine($"Grid {grid.EntityId} is in preview mode. Ignoring.");
                    return;
                }

                Logs.WriteLine($"Grid {grid.EntityId} is valid. Initializing GridData.");
                InitializedGrids.Add(grid.EntityId);
                var data = new GridData(grid);
                data.Init();
            }
            else
            {
                Logs.WriteLine($"Entity {entity.EntityId} is not a MyCubeGrid. Ignoring.");
            }
        }
    }
}
