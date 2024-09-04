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
using VRage.Collections;
using VRage.Game.Entity;

namespace NoBeaconNoPower
{
    public class GridData
    {
        public MyCubeGrid Grid;
        public int Beacons;
        public readonly HashSet<IMyPowerProducer> Blocks = new HashSet<IMyPowerProducer>();

        public GridData(MyCubeGrid grid)
        {
            Grid = grid;
            Logs.WriteLine($"GridData initialized with grid: {grid.EntityId}");
        }

        public void Init()
        {
            Logs.WriteLine($"Init called for grid: {Grid.EntityId}");
            var blocks = Grid.GetFatBlocks();
            Logs.WriteLine($"Grid has {blocks.Count} blocks to process.");

            // Processing each block on grid initialization.
            foreach (var block in blocks)
            {
                Logs.WriteLine($"Processing block: {block.EntityId} ({block.GetType().Name})");
                OnFatBlockAdded(block);
            }

            Grid.OnFatBlockAdded += OnFatBlockAdded;
            Grid.OnFatBlockRemoved += OnFatBlockRemoved;
            Grid.OnClose += OnClose;
        }

        public void OnClose(MyEntity entity)
        {
            Logs.WriteLine($"OnClose called for grid: {Grid?.EntityId}");
            Grid = null;
            Blocks.Clear();
            Logs.WriteLine("Grid and Blocks cleared.");
        }

        private void OnFatBlockRemoved(MyCubeBlock block)
        {
            Logs.WriteLine($"OnFatBlockRemoved called for block: {block.EntityId} ({block.GetType().Name})");

            if (block is IMyBeacon)
            {
                Logs.WriteLine($"Block {block.EntityId} is a beacon.");
                block.IsWorkingChanged -= IsWorkingChanged;
                if (block.IsWorking)
                {
                    Beacons--;
                    Logs.WriteLine($"Beacon removed, current beacon count: {Beacons}");
                }
                if (Beacons == 0)
                {
                    Logs.WriteLine("No beacons left, disabling power blocks.");
                    DisableBlocks(Grid.GetFatBlocks());
                }
                return;
            }

            if (Beacons == 0 && block is IMyPowerProducer)
            {
                var powerBlock = block as IMyPowerProducer;
                Logs.WriteLine($"Re-enabling power block {block.EntityId} ({block.GetType().Name}).");
                ReenableBlock(powerBlock);
            }
        }

        private void OnFatBlockAdded(MyCubeBlock block)
        {
            Logs.WriteLine($"OnFatBlockAdded called for block: {block.EntityId} ({block.GetType().Name})");

            if (block is IMyBeacon)
            {
                Logs.WriteLine($"Block {block.EntityId} is a beacon.");
                block.IsWorkingChanged += IsWorkingChanged;
                if (block.IsWorking)
                {
                    Beacons++;
                    Logs.WriteLine($"Beacon added, current beacon count: {Beacons}");
                }
                if (Beacons == 1)
                {
                    Logs.WriteLine("First beacon added, re-enabling power blocks.");
                    ReenableBlocks(Grid.GetFatBlocks());
                }
                return;
            }

            if (Beacons > 0)
            {
                Logs.WriteLine("Beacons present, skipping power block disabling.");
                return;
            }

            if (block is IMyPowerProducer)
            {
                Logs.WriteLine($"Disabling power block {block.EntityId} ({block.GetType().Name}).");
                DisableBlock(block as IMyPowerProducer);
            }
        }

        private void IsWorkingChanged(MyCubeBlock block)
        {
            Logs.WriteLine($"IsWorkingChanged called for beacon: {block.EntityId}, working: {block.IsWorking}");

            if (block.IsWorking)
            {
                Beacons++;
                Logs.WriteLine($"Beacon is now working, current beacon count: {Beacons}");
                if (Beacons == 1)
                {
                    Logs.WriteLine("First beacon working, re-enabling power blocks.");
                    ReenableBlocks(Grid.GetFatBlocks());
                }
                return;
            }

            Beacons--;
            Logs.WriteLine($"Beacon stopped working, current beacon count: {Beacons}");
            if (Beacons == 0)
            {
                Logs.WriteLine("No beacons working, disabling power blocks.");
                DisableBlocks(Grid.GetFatBlocks());
            }
        }

        private void DisableBlocks(ListReader<MyCubeBlock> blocks)
        {
            Logs.WriteLine("Disabling all power producer blocks.");
            foreach (var block in blocks)
            {
                if (block is IMyPowerProducer)
                {
                    Logs.WriteLine($"Disabling power producer block: {block.EntityId}");
                    DisableBlock(block as IMyPowerProducer);
                }
            }
        }

        private void DisableBlock(IMyPowerProducer powerBlock)
        {
            Logs.WriteLine($"DisableBlock called for power block: {powerBlock.EntityId}");

            powerBlock.EnabledChanged += EnabledChanged;

            if (powerBlock.Enabled)
            {
                Logs.WriteLine($"Power block {powerBlock.EntityId} is enabled, disabling now.");
                powerBlock.Enabled = false;
                Blocks.Add(powerBlock);
            }
        }

        private void EnabledChanged(IMyTerminalBlock block)
        {
            var powerBlock = block as IMyPowerProducer;
            Logs.WriteLine($"EnabledChanged called for power block: {block.EntityId}, enabled: {powerBlock?.Enabled}");

            if (powerBlock != null && powerBlock.Enabled)
            {
                Logs.WriteLine($"Disabling power block {powerBlock.EntityId}");
                powerBlock.Enabled = false;
            }
        }

        private void ReenableBlocks(ListReader<MyCubeBlock> blocks)
        {
            Logs.WriteLine("Re-enabling all power producer blocks.");
            foreach (var block in blocks)
            {
                if (block is IMyPowerProducer)
                {
                    Logs.WriteLine($"Re-enabling power block: {block.EntityId}");
                    ReenableBlock(block as IMyPowerProducer);
                }
            }
        }

        private void ReenableBlock(IMyPowerProducer powerBlock)
        {
            Logs.WriteLine($"ReenableBlock called for power block: {powerBlock.EntityId}");

            powerBlock.EnabledChanged -= EnabledChanged;

            if (Blocks.Remove(powerBlock))
            {
                Logs.WriteLine($"Power block {powerBlock.EntityId} re-enabled.");
                powerBlock.Enabled = true;
            }
        }
    }
}
