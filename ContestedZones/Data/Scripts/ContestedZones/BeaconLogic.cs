/*
 * 
 * This open-source project is available on GitHub and licensed under the GNU General Public License v3.0.
 * You are free to use, modify, and distribute this software under the terms of the GPL-3.0 license.
 * 
 * GitHub Repository: [https://github.com/MisterRee/Mods]
 * 
 */
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.ModAPI.Weapons;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using IMyControllableEntity = Sandbox.Game.Entities.IMyControllableEntity;

namespace ContestedZones
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "LargeBlockContestedZone", "SmallBlockContestedZone")]
    public class ContestedZone : MyGameLogicComponent
    {
        public IMyBeacon Beacon;
        internal IMyTerminalControlOnOffSwitch ShowInToolbarSwitch;
        public IMyPlayer Player = MyAPIGateway.Session.Player;
        public bool IsDedicated = MyAPIGateway.Utilities.IsDedicated;
        public bool IsServer = MyAPIGateway.Session.IsServer;
        internal readonly Settings Settings = new Settings();
        internal int BeaconEnabledTimer = 69;
        internal BoundingSphereD Sphere = new BoundingSphereD();
        internal readonly List<MyEntity> Entities = new List<MyEntity>();
        internal readonly List<IMySlimBlock> Blocks = new List<IMySlimBlock>();
        internal readonly List<IMyCubeGrid> Grids = new List<IMyCubeGrid>();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            try
            {
                Beacon = Entity as IMyBeacon;
                if (Beacon == null) throw new Exception("Unable to find Beacon during initialisation.");
                Settings.LoadCustomData(this);
                BeaconDoesStuff.instance.gridMap.Add(Beacon.CubeGrid, this);
                Beacon.Radius = 20;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                if (!IsDedicated)
                {
                    GetShowInToolbarSwitch();
                    Beacon.EnabledChanged += EnabledChanged;
                }
                Beacon.CustomDataChanged += CustomDataChanged;
            }
            catch (Exception e)
            {
                Logs.LogException(e);
            }
        }

        private void CustomDataChanged(IMyTerminalBlock block)
        {
            Settings.LoadCustomData(this);
        }

        private void EnabledChanged(IMyTerminalBlock obj)
        {
            BeaconEnabledTimer = 69;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (IsDedicated)
            {
                return;
            };
            if (!Beacon.Enabled) return;
        }

        public override void UpdateBeforeSimulation()
        {

            if (IsDedicated) return;
            if (!Beacon.Enabled) return;

            var character = Player.Character;
            var control = character as IMyControllableEntity;

            if (control == null) return;

            // Area of effect
            var distance = Vector3D.Distance(character.PositionComp.WorldAABB.Center, Beacon.PositionComp.WorldAABB.Center);

            // Warning Radius
            if (distance < Settings.EffectRadius + Settings.ApproachZoneMessageRange && distance > Settings.EffectRadius)
            {
                SendApproachNotification(Math.Round(distance - Settings.EffectRadius, 0));
                return;
            }
            if (distance > Settings.EffectRadius) return;

            if (control.EnabledLeadingGears) control.SwitchLandingGears();

            if (Settings.DisableJetpacks && control.EnabledThrusts)
            {
                control.SwitchThrusts();
                SendEnforcementNotification("JetPack");
            }
            if (Settings.ForcePlayerSignal && !control.EnabledBroadcasting)
            {
                control.SwitchBroadcasting();
                SendEnforcementNotification("Signal", "{tool}s must remain enabled!");
            }
            if (Settings.DisableHandGrinders && character.EquippedTool is IMyAngleGrinder)
            {
                control.SwitchToWeapon(null);
                SendEnforcementNotification("Grinder");
            }
            if (Settings.DisableHandWelders && character.EquippedTool is IMyWelder)
            {
                control.SwitchToWeapon(null);
                SendEnforcementNotification("Welder");
            }
            if (Settings.DisableHandDrills && character.EquippedTool is IMyHandDrill)
            {
                control.SwitchToWeapon(null);
                SendEnforcementNotification("Drill");
            }
            if (Settings.DisableHandWeapons && character.EquippedTool is IMyAutomaticRifleGun)
            {
                control.SwitchToWeapon(null);
                SendEnforcementNotification("Weapon");
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            if (IsDedicated) return;
            if (!Beacon.Enabled) return;
        }

        public override void UpdateBeforeSimulation100()
        {
            if (!IsServer) return;

            if (Beacon.Enabled == false) return;
            AreaEffects();
        }

        public override void UpdateAfterSimulation()
        {
            if (IsDedicated) return;
            if (!Beacon.Enabled)
            {
                if (BeaconEnabledTimer > 0)
                {
                    Beacon.SetEmissiveParts("Emissive", Color.Red, 1.0f);
                    BeaconEnabledTimer--;
                }
                return;
            }
            if (BeaconEnabledTimer > 0)
            {
                Beacon.SetEmissiveParts("Emissive", Color.Green, 1.0f);
                BeaconEnabledTimer--;
            }
            var value = Settings.ShowArea;
            if (value)
            {
                DisplayArea(Beacon.PositionComp.WorldAABB.Center, Settings.EffectRadius, Settings.ShowAreaColor);
            }
        }

        public override void Close()
        {
            BeaconDoesStuff.instance.gridMap.Remove(Beacon.CubeGrid);
            if (!IsDedicated)
            {
                Beacon.EnabledChanged -= EnabledChanged;
            }
            Beacon.CustomDataChanged -= CustomDataChanged;
        }

        private void AreaEffects()
        {
            Sphere.Center = Beacon.PositionComp.WorldAABB.Center;
            Sphere.Radius = Settings.EffectRadius;
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref Sphere, Entities);

            foreach (var entity in Entities)
            {
                var grid = entity as IMyCubeGrid;
                var character = entity as IMyCharacter;
                if (character != null && Settings.EnablePlayerDamageOverTime)
                {
                    character.DisplayName = "LEUT";
                    // if (player.PromoteLevel >= MyPromoteLevel.Admin) continue;
                    Logs.WriteLine($"Damaging {character.DisplayName} by {Settings.PlayerDamageOverTimeAmount}!");
                    var value = Settings.PlayerDamageOverTimeAmount;
                    character.DoDamage(value, MyDamageType.Unknown, true);
                }
                if (grid == null || grid.Physics == null) continue;
                if (!Settings.EnableGridDamageOverTime && !Settings.DisableBlocks) continue;
                var dmgValue = Settings.GridDamageOverTimeAmount;
                if (grid == Beacon.CubeGrid && Settings.EnableOwnGridProtection)
                {
                    if (Settings.OwnGridProtectionPercent == 100) continue;
                    dmgValue *= 1 - (Settings.OwnGridProtectionPercent / 100);
                }
                grid.GetBlocks(Blocks);
                Logs.WriteLine($"Damaging {grid.DisplayName} by {dmgValue}!");
                foreach (var slim in Blocks)
                {
                    if (!IsExposed(slim, slim.CubeGrid)) continue;
                    slim.DoDamage(dmgValue, MyDamageType.Unknown, true);
                    /*
                    var block = slim.FatBlock as IMyFunctionalBlock;
                    if (block == null) return;
                    if (block is IMyThrust && Settings.DisableBlockThrusters) block.Enabled = false; <= DISABLING BLOCKS
                    */
                }
                Blocks.Clear();
            }
            Entities.Clear();
        }

        private bool IsExposed(IMySlimBlock block, IMyCubeGrid grid)
        {

            Vector3I[] neighbourPositions = new Vector3I[]
            {
                block.Max + new Vector3I(1, 0, 0),
                block.Max + new Vector3I(0, 1, 0),
                block.Max + new Vector3I(0, 0, 1),
                block.Min - new Vector3I(1, 0, 0),
                block.Min - new Vector3I(0, 1, 0),
                block.Min - new Vector3I(0, 0, 1)
            };

            foreach (Vector3I position in neighbourPositions)
            {
                if (grid.GetCubeBlock(position) == null && !grid.IsRoomAtPositionAirtight(position))
                {
                    return true;
                }
            }
            return false;
        }

        internal static void DisplayArea(Vector3D pos, double radius, Color color, bool solid = true, int divideRatio = 20, float lineWidth = 0.02f)
        {

            var posMatCenterScaled = MatrixD.CreateTranslation(pos);
            var posMatScaler = MatrixD.Rescale(posMatCenterScaled, radius);
            var raster = solid ? MySimpleObjectRasterizer.Solid : MySimpleObjectRasterizer.Wireframe;

            MySimpleObjectDraw.DrawTransparentSphere(ref posMatScaler, 1f, ref color, raster, divideRatio, null, MyStringId.GetOrCompute("Square"), lineWidth);
        }

        private void SendEnforcementNotification(
            string tool,
            string signalMessage = null
            )
        {
            string message = "";
            var enabled = Settings.EnableEnforcementMessage;
            if (!enabled) return;

            if (string.IsNullOrEmpty(signalMessage))
            {
                message = Settings.EnforcementMessage;
                if (string.IsNullOrEmpty(message)) return;
            }
            else
            {
                message = signalMessage;
            }

            if (message.Contains("{tool}"))
            {
                message = message.Replace("{tool}", tool);
            }

            MyAPIGateway.Utilities.ShowNotification(message, 3000, "Red");
        }

        private void SendApproachNotification(double distance)
        {
            var enabled = Settings.EnableApproachZoneMessage;
            if (!enabled) return;

            var message = Settings.ApproachZoneMessage;
            if (string.IsNullOrEmpty(message)) return;

            if (message.Contains("{distance}"))
            {
                message = message.Replace("{distance}", $"{distance}m");
            }

            MyAPIGateway.Utilities.ShowNotification(message, 160, "Red");
        }

        private void GetShowInToolbarSwitch()
        {
            List<IMyTerminalControl> items;
            MyAPIGateway.TerminalControls.GetControls<IMyUpgradeModule>(out items);

            foreach (var item in items)
            {

                if (item.Id == "ShowInToolbarConfig")
                {
                    ShowInToolbarSwitch = (IMyTerminalControlOnOffSwitch)item;
                    break;
                }
            }
        }

        internal void RefreshTerminal()
        {
            Beacon.RefreshCustomInfo();

            if (ShowInToolbarSwitch != null)
            {
                var originalSetting = ShowInToolbarSwitch.Getter(Beacon);
                ShowInToolbarSwitch.Setter(Beacon, !originalSetting);
                ShowInToolbarSwitch.Setter(Beacon, originalSetting);
            }
        }
    }
}
