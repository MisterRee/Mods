/*
 * 
 * This open-source project is available on GitHub and licensed under the GNU General Public License v3.0.
 * You are free to use, modify, and distribute this software under the terms of the GPL-3.0 license.
 * 
 * GitHub Repository: [https://github.com/MisterRee/Mods]
 * 
 */
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ContestedZones
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class BeaconDoesStuff : MySessionComponentBase
    {
        internal static BeaconDoesStuff instance;
        private readonly List<IMyTerminalControl> _terminalControls = new List<IMyTerminalControl>();
        private readonly List<IMyTerminalAction> _terminalActions = new List<IMyTerminalAction>();
        internal readonly Dictionary<IMyCubeGrid, ContestedZone> gridMap = new Dictionary<IMyCubeGrid, ContestedZone>();
        private IMyTerminalControlButton LoadFromCustomDataButton;

        public BeaconDoesStuff()
        {
            instance = this;
        }
        public void ProtectGrid(object target, ref MyDamageInformation damageInfo)
        {

            if (damageInfo.AttackerId == 0) return;
            IMySlimBlock damagedBlock = target as IMySlimBlock;
            if (damagedBlock == null) return;
            IMyCubeGrid damagedGrid = damagedBlock.CubeGrid;

            ContestedZone logic;
            if (!gridMap.TryGetValue(damagedGrid, out logic)) return;
            damageInfo.IsDeformation = false;
            if (!logic.Settings.EnableOwnGridProtection || !logic.Beacon.Enabled) return;
            damageInfo.Amount *= 1 - (logic.Settings.OwnGridProtectionPercent / 100f);
            MyAPIGateway.Utilities.ShowNotification(damageInfo.Amount.ToString(), 10000);
            MyAPIGateway.Utilities.ShowNotification(damageInfo.IsDeformation.ToString(), 10000);
        }

        public override void BeforeStart()
        {
            if (MyAPIGateway.Session.IsServer) MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, ProtectGrid);
        }
        public override void LoadData()
        {
            Logs.InitLogs();
            if (MyAPIGateway.Utilities.IsDedicated) return;

            #region LoadFromCustomData

            LoadFromCustomDataButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyBeacon>("CZ_LoadFromCustomDataButton");
            LoadFromCustomDataButton.Title = MyStringId.GetOrCompute("Load Custom Data");
            LoadFromCustomDataButton.Enabled = (IMyTerminalBlock block) => true;
            LoadFromCustomDataButton.Visible = (IMyTerminalBlock block) => true;
            LoadFromCustomDataButton.Action = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.LoadCustomData(logic);
                logic.RefreshTerminal();
            };

            #endregion

            #region EffectRadius

            var EffectRadiusSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>($"CZ_EffectRadiusSlider");
            EffectRadiusSlider.Title = MyStringId.GetOrCompute("Effect Radius");
            EffectRadiusSlider.SetLogLimits(1, 200000000);
            EffectRadiusSlider.Enabled = (IMyTerminalBlock block) => true;
            EffectRadiusSlider.Visible = (IMyTerminalBlock block) => true;
            EffectRadiusSlider.Setter = (IMyTerminalBlock block, float value) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.EffectRadius = MathHelper.RoundToInt(value);
                logic.Settings.SaveCustomData(block);
            };
            EffectRadiusSlider.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return 1000;
                return logic.Settings.EffectRadius;
            };
            EffectRadiusSlider.Writer = (IMyTerminalBlock block, StringBuilder text) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                var value = logic.Settings.EffectRadius;
                text.Append($"{value}m");
            };
            _terminalControls.Add(EffectRadiusSlider);

            #endregion

            #region ShowArea

            var ShowAreaSeperator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyBeacon>("CZ_ShowAreaSeperator");
            _terminalControls.Add(ShowAreaSeperator);

            var ShowAreaLabel = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyBeacon>("CZ_ShowAreaLabel");
            ShowAreaLabel.Label = MyStringId.GetOrCompute("Show Area");
            ShowAreaLabel.Enabled = (IMyTerminalBlock block) => true;
            ShowAreaLabel.Visible = (IMyTerminalBlock block) => true;
            _terminalControls.Add(ShowAreaLabel);

            var ShowArea = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyBeacon>("CZ_ShowArea");
            ShowArea.Enabled = (block) => true;
            ShowArea.Visible = (block) => true;
            ShowArea.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.ShowArea = state;
                logic.Settings.SaveCustomData(block);
                logic.RefreshTerminal();
            };
            ShowArea.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.ShowArea;
            };
            ShowArea.OnText = MyStringId.GetOrCompute("Enable");
            ShowArea.OffText = MyStringId.GetOrCompute("Disable");
            _terminalControls.Add(ShowArea);

            var ShowAreaColor = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlColor, IMyBeacon>("CZ_ShowAreaColor");
            ShowAreaColor.Title = MyStringId.GetOrCompute("Area Color");
            ShowAreaColor.Enabled = (IMyTerminalBlock block) => true;
            ShowAreaColor.Visible = (IMyTerminalBlock block) => true;
            ShowAreaColor.Setter = (IMyTerminalBlock block, Color color) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.ShowAreaColor.R = color.R;
                logic.Settings.ShowAreaColor.G = color.G;
                logic.Settings.ShowAreaColor.B = color.B;
                logic.Settings.SaveCustomData(block);
            };
            ShowAreaColor.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return Color.Red;
                return logic.Settings.ShowAreaColor;
            };
            _terminalControls.Add(ShowAreaColor);

            var ShowAreaColorAlpha = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>($"CZ_ShowAreaColorAlpha");
            ShowAreaColorAlpha.Title = MyStringId.GetOrCompute("Transparency");
            ShowAreaColorAlpha.SetLogLimits(1, 255);
            ShowAreaColorAlpha.Enabled = (IMyTerminalBlock block) => true;
            ShowAreaColorAlpha.Visible = (IMyTerminalBlock block) => true;
            ShowAreaColorAlpha.Setter = (IMyTerminalBlock block, float value) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.ShowAreaColor.A = (byte)MathHelper.RoundToInt(Math.Min(value, 255));
                logic.Settings.SaveCustomData(block);
            };
            ShowAreaColorAlpha.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return 175;
                return logic.Settings.ShowAreaColor.A;
            };
            ShowAreaColorAlpha.Writer = (IMyTerminalBlock block, StringBuilder text) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                var value = logic.Settings.ShowAreaColor.A;
                text.Append(value);
            };
            _terminalControls.Add(ShowAreaColorAlpha);

            #endregion

            #region EnforcementMessage

            var EnforcementMessageSeperator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyBeacon>("CZ_EnforcementMessageSeperator");
            _terminalControls.Add(EnforcementMessageSeperator);

            var EnforcementMessageLabel = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyBeacon>("CZ_EnforcementMessageLabel");
            EnforcementMessageLabel.Label = MyStringId.GetOrCompute("Enforcement Message");
            EnforcementMessageLabel.Enabled = (IMyTerminalBlock block) => true;
            EnforcementMessageLabel.Visible = (IMyTerminalBlock block) => true;
            _terminalControls.Add(EnforcementMessageLabel);

            var EnforcementMessageCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_EnforcementMessageCheckBox");
            EnforcementMessageCheckBox.Title = MyStringId.GetOrCompute("Enabled");
            EnforcementMessageCheckBox.OnText = MyStringId.GetOrCompute("Enable");
            EnforcementMessageCheckBox.OffText = MyStringId.GetOrCompute("Disable");
            EnforcementMessageCheckBox.Enabled = (IMyTerminalBlock block) => true;
            EnforcementMessageCheckBox.Visible = (IMyTerminalBlock block) => true;
            EnforcementMessageCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.EnableEnforcementMessage = state;
                logic.Settings.SaveCustomData(block);
                logic.RefreshTerminal();
            };
            EnforcementMessageCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.EnableEnforcementMessage;
            };
            _terminalControls.Add(EnforcementMessageCheckBox);

            var EnforcementMessageTextBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyBeacon>($"CZ_EnforcementMessageTextBox");
            EnforcementMessageTextBox.Title = MyStringId.GetOrCompute("Message");
            EnforcementMessageTextBox.Enabled = (block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.EnableEnforcementMessage;
            };
            EnforcementMessageTextBox.Visible = (block) => true;
            EnforcementMessageTextBox.Setter = (IMyTerminalBlock block, StringBuilder message) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.EnforcementMessage = message.ToString();
                logic.Settings.SaveCustomData(block);
            };
            EnforcementMessageTextBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return new StringBuilder("WARNING! {tool}s are disabled in the Contested Zone!");
                return new StringBuilder(logic.Settings.EnforcementMessage);
            };
            _terminalControls.Add(EnforcementMessageTextBox);

            #endregion

            #region InZoneMessage

            var InZoneMessageSeperator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyBeacon>("CZ_InZoneMessageSeperator");
            _terminalControls.Add(InZoneMessageSeperator);

            var InZoneMessageLabel = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyBeacon>("CZ_InZoneMessageLabel");
            InZoneMessageLabel.Label = MyStringId.GetOrCompute("In Zone Message");
            InZoneMessageLabel.Enabled = (IMyTerminalBlock block) => true;
            InZoneMessageLabel.Visible = (IMyTerminalBlock block) => true;
            _terminalControls.Add(InZoneMessageLabel);

            var InZoneMessageCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_InZoneMessageCheckBox");
            InZoneMessageCheckBox.Title = MyStringId.GetOrCompute("Enabled");
            InZoneMessageCheckBox.OnText = MyStringId.GetOrCompute("Enable");
            InZoneMessageCheckBox.OffText = MyStringId.GetOrCompute("Disable");
            InZoneMessageCheckBox.Enabled = (IMyTerminalBlock block) => true;
            InZoneMessageCheckBox.Visible = (IMyTerminalBlock block) => true;
            InZoneMessageCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.EnableInZoneMessage = state;
                logic.Settings.SaveCustomData(block);
                logic.RefreshTerminal();
            };
            InZoneMessageCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.EnableInZoneMessage;
            };
            _terminalControls.Add(InZoneMessageCheckBox);

            var InZoneMessageTextBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyBeacon>($"CZ_InZoneMessageTextBox");
            InZoneMessageTextBox.Title = MyStringId.GetOrCompute("Message");
            InZoneMessageTextBox.Enabled = (block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.EnableInZoneMessage;
            };
            InZoneMessageTextBox.Visible = (block) => true;
            InZoneMessageTextBox.Setter = (IMyTerminalBlock block, StringBuilder message) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.InZoneMessage = message.ToString();
                logic.Settings.SaveCustomData(block);
            };
            InZoneMessageTextBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return new StringBuilder("WARNING! You are in a Contested Zone!");
                return new StringBuilder(logic.Settings.InZoneMessage);
            };
            _terminalControls.Add(InZoneMessageTextBox);

            #endregion

            #region ApproachZoneMessage

            var ApproachZoneMessageSeperator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyBeacon>("CZ_ApproachZoneMessageSeperator");
            _terminalControls.Add(ApproachZoneMessageSeperator);

            var ApproachZoneMessageLabel = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyBeacon>("CZ_ApproachZoneMessageLabel");
            ApproachZoneMessageLabel.Label = MyStringId.GetOrCompute("Approach Zone Message");
            ApproachZoneMessageLabel.Enabled = (IMyTerminalBlock block) => true;
            ApproachZoneMessageLabel.Visible = (IMyTerminalBlock block) => true;
            _terminalControls.Add(ApproachZoneMessageLabel);

            var ApproachZoneMessageCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_ApproachZoneMessageCheckBox");
            ApproachZoneMessageCheckBox.Title = MyStringId.GetOrCompute("Enabled");
            ApproachZoneMessageCheckBox.OnText = MyStringId.GetOrCompute("Enable");
            ApproachZoneMessageCheckBox.OffText = MyStringId.GetOrCompute("Disable");
            ApproachZoneMessageCheckBox.Enabled = (IMyTerminalBlock block) => true;
            ApproachZoneMessageCheckBox.Visible = (IMyTerminalBlock block) => true;
            ApproachZoneMessageCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.EnableApproachZoneMessage = state;
                logic.Settings.SaveCustomData(block);
                logic.RefreshTerminal();
            };
            ApproachZoneMessageCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.EnableApproachZoneMessage;
            };
            _terminalControls.Add(ApproachZoneMessageCheckBox);

            var ApproachZoneMessageSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>($"CZ_ApproachZoneMessageSlider");
            ApproachZoneMessageSlider.Title = MyStringId.GetOrCompute("Range");
            ApproachZoneMessageSlider.SetLogLimits(1, 2000);
            ApproachZoneMessageSlider.Enabled = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.EnableApproachZoneMessage;
            };
            ApproachZoneMessageSlider.Visible = (IMyTerminalBlock block) => true;
            ApproachZoneMessageSlider.Setter = (IMyTerminalBlock block, float value) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.ApproachZoneMessageRange = MathHelper.RoundToInt(value);
                logic.Settings.SaveCustomData(block);
            };
            ApproachZoneMessageSlider.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return 1000;
                return logic.Settings.ApproachZoneMessageRange;
            };
            ApproachZoneMessageSlider.Writer = (IMyTerminalBlock block, StringBuilder text) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                var value = logic.Settings.ApproachZoneMessageRange;
                text.Append($"{value}m");
            };
            _terminalControls.Add(ApproachZoneMessageSlider);

            var ApproachZoneMessageTextBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyBeacon>($"CZ_ApproachZoneMessageTextBox");
            ApproachZoneMessageTextBox.Title = MyStringId.GetOrCompute("Message");
            ApproachZoneMessageTextBox.Enabled = (block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.EnableApproachZoneMessage;
            };
            ApproachZoneMessageTextBox.Visible = (block) => true;
            ApproachZoneMessageTextBox.Setter = (IMyTerminalBlock block, StringBuilder message) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.ApproachZoneMessage = message.ToString();
                logic.Settings.SaveCustomData(block);
            };
            ApproachZoneMessageTextBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return new StringBuilder("WARNING! You are {distance} from a Contested Zone!");
                return new StringBuilder(logic.Settings.ApproachZoneMessage);
            };
            _terminalControls.Add(ApproachZoneMessageTextBox);

            #endregion

            #region OwnGridProtection

            var OwnGridProtectionSeperator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyBeacon>("CZ_OwnGridProtectionSeperator");
            _terminalControls.Add(OwnGridProtectionSeperator);

            var OwnGridProtectionLabel = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyBeacon>("CZ_OwnGridProtectionLabel");
            OwnGridProtectionLabel.Label = MyStringId.GetOrCompute("Own Grid Protection");
            OwnGridProtectionLabel.Enabled = (IMyTerminalBlock block) => true;
            OwnGridProtectionLabel.Visible = (IMyTerminalBlock block) => true;
            _terminalControls.Add(OwnGridProtectionLabel);

            var OwnGridProtectionCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_OwnGridProtectionCheckBox");
            OwnGridProtectionCheckBox.Title = MyStringId.GetOrCompute("Enabled");
            OwnGridProtectionCheckBox.OnText = MyStringId.GetOrCompute("Enable");
            OwnGridProtectionCheckBox.OffText = MyStringId.GetOrCompute("Disable");
            OwnGridProtectionCheckBox.Enabled = (IMyTerminalBlock block) => true;
            OwnGridProtectionCheckBox.Visible = (IMyTerminalBlock block) => true;
            OwnGridProtectionCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.EnableOwnGridProtection = state;
                logic.Settings.SaveCustomData(block);
                logic.RefreshTerminal();
            };
            OwnGridProtectionCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.EnableOwnGridProtection;
            };
            _terminalControls.Add(OwnGridProtectionCheckBox);

            var OwnGridProtectionSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>($"CZ_OwnGridProtectionSlider");
            OwnGridProtectionSlider.Title = MyStringId.GetOrCompute("Protect %");
            OwnGridProtectionSlider.SetLogLimits(1, 100);
            OwnGridProtectionSlider.Enabled = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.EnableOwnGridProtection;
            };
            OwnGridProtectionSlider.Visible = (IMyTerminalBlock block) => true;
            OwnGridProtectionSlider.Setter = (IMyTerminalBlock block, float value) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.OwnGridProtectionPercent = MathHelper.RoundToInt(value);
                logic.Settings.SaveCustomData(block);
            };
            OwnGridProtectionSlider.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return 100;
                return logic.Settings.OwnGridProtectionPercent;
            };
            OwnGridProtectionSlider.Writer = (IMyTerminalBlock block, StringBuilder text) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                var value = logic.Settings.OwnGridProtectionPercent;
                text.Append($"{value}%");
            };
            _terminalControls.Add(OwnGridProtectionSlider);

            #endregion

            #region EnablePlayerDamageOverTime

            var PlayerDamageOverTimeSeperator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyBeacon>("CZ_PlayerDamageOverTimeSeperator");
            _terminalControls.Add(PlayerDamageOverTimeSeperator);

            var PlayerDamageOverTimeLabel = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyBeacon>("CZ_PlayerDamageOverTimeLabel");
            PlayerDamageOverTimeLabel.Label = MyStringId.GetOrCompute("Player Damage Over Time");
            PlayerDamageOverTimeLabel.Enabled = (IMyTerminalBlock block) => true;
            PlayerDamageOverTimeLabel.Visible = (IMyTerminalBlock block) => true;
            _terminalControls.Add(PlayerDamageOverTimeLabel);

            var PlayerDamageOverTimeCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_PlayerDamageOverTimeCheckBox");
            PlayerDamageOverTimeCheckBox.Title = MyStringId.GetOrCompute("Enabled");
            PlayerDamageOverTimeCheckBox.OnText = MyStringId.GetOrCompute("Enable");
            PlayerDamageOverTimeCheckBox.OffText = MyStringId.GetOrCompute("Disable");
            PlayerDamageOverTimeCheckBox.Enabled = (IMyTerminalBlock block) => true;
            PlayerDamageOverTimeCheckBox.Visible = (IMyTerminalBlock block) => true;
            PlayerDamageOverTimeCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.EnablePlayerDamageOverTime = state;
                logic.Settings.SaveCustomData(block);
                logic.RefreshTerminal();
            };
            PlayerDamageOverTimeCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.EnablePlayerDamageOverTime;
            };
            _terminalControls.Add(PlayerDamageOverTimeCheckBox);

            var PlayerDamageOverTimeSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>($"CZ_PlayerDamageOverTimeSlider");
            PlayerDamageOverTimeSlider.Title = MyStringId.GetOrCompute("Amount");
            PlayerDamageOverTimeSlider.SetLogLimits(1, 100);
            PlayerDamageOverTimeSlider.Enabled = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.EnablePlayerDamageOverTime;
            };
            PlayerDamageOverTimeSlider.Visible = (IMyTerminalBlock block) => true;
            PlayerDamageOverTimeSlider.Setter = (IMyTerminalBlock block, float value) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.PlayerDamageOverTimeAmount = MathHelper.RoundToInt(value);
                logic.Settings.SaveCustomData(block);
            };
            PlayerDamageOverTimeSlider.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return 100;
                return logic.Settings.PlayerDamageOverTimeAmount;
            };
            PlayerDamageOverTimeSlider.Writer = (IMyTerminalBlock block, StringBuilder text) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                var value = logic.Settings.PlayerDamageOverTimeAmount;
                text.Append(value);
            };
            _terminalControls.Add(PlayerDamageOverTimeSlider);

            var PlayerDamageOverTimeTextBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyBeacon>($"CZ_PlayerDamageOverTimeTextBox");
            PlayerDamageOverTimeTextBox.Title = MyStringId.GetOrCompute("Message");
            PlayerDamageOverTimeTextBox.Enabled = (block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.EnablePlayerDamageOverTime;
            };
            PlayerDamageOverTimeTextBox.Visible = (block) => true;
            PlayerDamageOverTimeTextBox.Setter = (IMyTerminalBlock block, StringBuilder message) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.PlayerDamageOverTimeMessage = message.ToString();
                logic.Settings.SaveCustomData(block);
            };
            PlayerDamageOverTimeTextBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return new StringBuilder("The Contested Zone is damaging you!");
                return new StringBuilder(logic.Settings.PlayerDamageOverTimeMessage);
            };
            _terminalControls.Add(PlayerDamageOverTimeTextBox);

            #endregion

            #region EnableGridDamageOverTime

            var GridDamageOverTimeSeperator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyBeacon>("CZ_GridDamageOverTimeSeperator");
            _terminalControls.Add(GridDamageOverTimeSeperator);

            var GridDamageOverTimeLabel = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyBeacon>("CZ_GridDamageOverTimeLabel");
            GridDamageOverTimeLabel.Label = MyStringId.GetOrCompute("Grid Damage Over Time");
            GridDamageOverTimeLabel.Enabled = (IMyTerminalBlock block) => true;
            GridDamageOverTimeLabel.Visible = (IMyTerminalBlock block) => true;
            _terminalControls.Add(GridDamageOverTimeLabel);

            var GridDamageOverTimeCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_GridDamageOverTimeCheckBox");
            GridDamageOverTimeCheckBox.Title = MyStringId.GetOrCompute("Enabled");
            GridDamageOverTimeCheckBox.OnText = MyStringId.GetOrCompute("Enable");
            GridDamageOverTimeCheckBox.OffText = MyStringId.GetOrCompute("Disable");
            GridDamageOverTimeCheckBox.Enabled = (IMyTerminalBlock block) => true;
            GridDamageOverTimeCheckBox.Visible = (IMyTerminalBlock block) => true;
            GridDamageOverTimeCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.EnableGridDamageOverTime = state;
                logic.Settings.SaveCustomData(block);
                logic.RefreshTerminal();
            };
            GridDamageOverTimeCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.EnableGridDamageOverTime;
            };
            _terminalControls.Add(GridDamageOverTimeCheckBox);

            var GridDamageOverTimeSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>($"CZ_GridDamageOverTimeSlider");
            GridDamageOverTimeSlider.Title = MyStringId.GetOrCompute("Amount");
            GridDamageOverTimeSlider.SetLogLimits(1, 100);
            GridDamageOverTimeSlider.Enabled = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.EnableGridDamageOverTime;
            };
            GridDamageOverTimeSlider.Visible = (IMyTerminalBlock block) => true;
            GridDamageOverTimeSlider.Setter = (IMyTerminalBlock block, float value) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.GridDamageOverTimeAmount = MathHelper.RoundToInt(value);
                logic.Settings.SaveCustomData(block);
            };
            GridDamageOverTimeSlider.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return 100;
                return logic.Settings.GridDamageOverTimeAmount;
            };
            GridDamageOverTimeSlider.Writer = (IMyTerminalBlock block, StringBuilder text) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                var value = logic.Settings.GridDamageOverTimeAmount;
                text.Append(value);
            };
            _terminalControls.Add(GridDamageOverTimeSlider);

            var GridDamageOverTimeTextBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyBeacon>($"CZ_GridDamageOverTimeTextBox");
            GridDamageOverTimeTextBox.Title = MyStringId.GetOrCompute("Message");
            GridDamageOverTimeTextBox.Enabled = (block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.EnableGridDamageOverTime;
            };
            GridDamageOverTimeTextBox.Visible = (block) => true;
            GridDamageOverTimeTextBox.Setter = (IMyTerminalBlock block, StringBuilder message) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.GridDamageOverTimeMessage = message.ToString();
                logic.Settings.SaveCustomData(block);
            };
            GridDamageOverTimeTextBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return new StringBuilder("The Contested Zone is damaging your grid!");
                return new StringBuilder(logic.Settings.GridDamageOverTimeMessage);
            };
            _terminalControls.Add(GridDamageOverTimeTextBox);

            #endregion

            #region ForcePlayerSignalsOn

            var CharacterRestrictionsSeperator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyBeacon>("CZ_CharacterRestrictionsSeperator");
            _terminalControls.Add(CharacterRestrictionsSeperator);

            var CharacterRestrictionsLabel = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyBeacon>("CZ_CharacterRestrictionsLabel");
            CharacterRestrictionsLabel.Label = MyStringId.GetOrCompute("Character Restrictions");
            CharacterRestrictionsLabel.Enabled = (IMyTerminalBlock block) => true;
            CharacterRestrictionsLabel.Visible = (IMyTerminalBlock block) => true;
            _terminalControls.Add(CharacterRestrictionsLabel);

            var ForcePlayerSignalOnCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_ForcePlayerSignalOn");
            ForcePlayerSignalOnCheckBox.Title = MyStringId.GetOrCompute("Force Player Signal On            ");
            ForcePlayerSignalOnCheckBox.Enabled = (IMyTerminalBlock block) => true;
            ForcePlayerSignalOnCheckBox.Visible = (IMyTerminalBlock block) => true;
            ForcePlayerSignalOnCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.ForcePlayerSignal = state;
                logic.Settings.SaveCustomData(block);
                logic.RefreshTerminal();
            };
            ForcePlayerSignalOnCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.ForcePlayerSignal;
            };
            _terminalControls.Add(ForcePlayerSignalOnCheckBox);

            #endregion

            #region DisableJetPacks

            var DisableJetPacksCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_DisableJetPacksCheckBox");
            DisableJetPacksCheckBox.Title = MyStringId.GetOrCompute("Disable JetPacks                     ");
            DisableJetPacksCheckBox.Enabled = (IMyTerminalBlock block) => true;
            DisableJetPacksCheckBox.Visible = (IMyTerminalBlock block) => true;
            DisableJetPacksCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.DisableJetpacks = state;
                logic.Settings.SaveCustomData(block);
                logic.RefreshTerminal();
            };
            DisableJetPacksCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.DisableJetpacks;
            };
            _terminalControls.Add(DisableJetPacksCheckBox);

            #endregion

            #region DisableHandGrinders

            var DisableHandGrindersCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_DisableHandGrindersCheckBox");
            DisableHandGrindersCheckBox.Title = MyStringId.GetOrCompute("Disable Grinders                      ");
            DisableHandGrindersCheckBox.Enabled = (IMyTerminalBlock block) => true;
            DisableHandGrindersCheckBox.Visible = (IMyTerminalBlock block) => true;
            DisableHandGrindersCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.DisableHandGrinders = state;
                logic.Settings.SaveCustomData(block);
                logic.RefreshTerminal();
            };
            DisableHandGrindersCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.DisableHandGrinders;
            };
            _terminalControls.Add(DisableHandGrindersCheckBox);

            #endregion

            #region DisableHandWelders

            var DisableHandWeldersCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_DisableHandWeldersCheckBox");
            DisableHandWeldersCheckBox.Title = MyStringId.GetOrCompute("Disable Welders                      ");
            DisableHandWeldersCheckBox.Enabled = (IMyTerminalBlock block) => true;
            DisableHandWeldersCheckBox.Visible = (IMyTerminalBlock block) => true;
            DisableHandWeldersCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.DisableHandWelders = state;
                logic.Settings.SaveCustomData(block);
                logic.RefreshTerminal();
            };
            DisableHandWeldersCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.DisableHandWelders;
            };
            _terminalControls.Add(DisableHandWeldersCheckBox);

            #endregion

            #region DisableHandDrills

            var DisableHandDrillsCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_DisableHandDrillsCheckBox");
            DisableHandDrillsCheckBox.Title = MyStringId.GetOrCompute("Disable Drills                           ");
            DisableHandDrillsCheckBox.Enabled = (IMyTerminalBlock block) => true;
            DisableHandDrillsCheckBox.Visible = (IMyTerminalBlock block) => true;
            DisableHandDrillsCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.DisableHandDrills = state;
                logic.Settings.SaveCustomData(block);
                logic.RefreshTerminal();
            };
            DisableHandDrillsCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.DisableHandDrills;
            };
            _terminalControls.Add(DisableHandDrillsCheckBox);

            #endregion

            #region DisableHandWeapons

            var DisableHandWeaponsCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_DisableHandWeaponsCheckBox");
            DisableHandWeaponsCheckBox.Title = MyStringId.GetOrCompute("Disable Weapons                    ");
            DisableHandWeaponsCheckBox.Enabled = (IMyTerminalBlock block) => true;
            DisableHandWeaponsCheckBox.Visible = (IMyTerminalBlock block) => true;
            DisableHandWeaponsCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.DisableHandWeapons = state;
                logic.Settings.SaveCustomData(block);
                logic.RefreshTerminal();
            };
            DisableHandWeaponsCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.DisableHandWeapons;
            };
            _terminalControls.Add(DisableHandWeaponsCheckBox);

            #endregion

            #region DisableBlockGrinders

            var GridRestrictionsSeperator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyBeacon>("CZ_GridRestrictionsSeperator");
            _terminalControls.Add(GridRestrictionsSeperator);

            var GridRestrictionsLabel = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyBeacon>("CZ_GridRestrictionsLabel");
            GridRestrictionsLabel.Label = MyStringId.GetOrCompute("Grid Restrictions");
            GridRestrictionsLabel.Enabled = (IMyTerminalBlock block) => true;
            GridRestrictionsLabel.Visible = (IMyTerminalBlock block) => true;
            _terminalControls.Add(GridRestrictionsLabel);

            var DisableBlockGrindersCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_DisableBlockGrindersCheckBox");
            DisableBlockGrindersCheckBox.Title = MyStringId.GetOrCompute("Disable Grinders                      ");
            DisableBlockGrindersCheckBox.Enabled = (IMyTerminalBlock block) => true;
            DisableBlockGrindersCheckBox.Visible = (IMyTerminalBlock block) => true;
            DisableBlockGrindersCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.DisableBlockGrinders = state;
                logic.Settings.SaveCustomData(block);
                logic.Settings.IsBlocksDisabled();
                logic.RefreshTerminal();
            };
            DisableBlockGrindersCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.DisableBlockGrinders;
            };
            _terminalControls.Add(DisableBlockGrindersCheckBox);

            #endregion

            #region DisableBlockWelders

            var DisableBlockWeldersCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_DisableBlockWeldersCheckBox");
            DisableBlockWeldersCheckBox.Title = MyStringId.GetOrCompute("Disable Welders                      ");
            DisableBlockWeldersCheckBox.Enabled = (IMyTerminalBlock block) => true;
            DisableBlockWeldersCheckBox.Visible = (IMyTerminalBlock block) => true;
            DisableBlockWeldersCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.DisableBlockWelders = state;
                logic.Settings.SaveCustomData(block);
                logic.Settings.IsBlocksDisabled();
                logic.RefreshTerminal();
            };
            DisableBlockWeldersCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.DisableBlockWelders;
            };
            _terminalControls.Add(DisableBlockWeldersCheckBox);

            #endregion

            #region DisableBlockDrills

            var DisableBlockDrillsCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_DisableBlockDrillsCheckBox");
            DisableBlockDrillsCheckBox.Title = MyStringId.GetOrCompute("Disable Drills                           ");
            DisableBlockDrillsCheckBox.Enabled = (IMyTerminalBlock block) => true;
            DisableBlockDrillsCheckBox.Visible = (IMyTerminalBlock block) => true;
            DisableBlockDrillsCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.DisableBlockDrills = state;
                logic.Settings.SaveCustomData(block);
                logic.Settings.IsBlocksDisabled();
                logic.RefreshTerminal();
            };
            DisableBlockDrillsCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.DisableBlockDrills;
            };
            _terminalControls.Add(DisableBlockDrillsCheckBox);

            #endregion

            #region DisableBlockWeapons

            var DisableBlockWeaponsCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_DisableBlockWeaponsCheckBox");
            DisableBlockWeaponsCheckBox.Title = MyStringId.GetOrCompute("Disable Weapons                    ");
            DisableBlockWeaponsCheckBox.Enabled = (IMyTerminalBlock block) => true;
            DisableBlockWeaponsCheckBox.Visible = (IMyTerminalBlock block) => true;
            DisableBlockWeaponsCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.DisableBlockWeapons = state;
                logic.Settings.SaveCustomData(block);
                logic.Settings.IsBlocksDisabled();
                logic.RefreshTerminal();
            };
            DisableBlockWeaponsCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.DisableBlockWeapons;
            };
            _terminalControls.Add(DisableBlockWeaponsCheckBox);

            #endregion

            #region DisableBlockThrusters

            var DisableBlockThrustersCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_DisableBlockThrustersCheckBox");
            DisableBlockThrustersCheckBox.Title = MyStringId.GetOrCompute("Disable Thrusters                    ");
            DisableBlockThrustersCheckBox.Enabled = (IMyTerminalBlock block) => true;
            DisableBlockThrustersCheckBox.Visible = (IMyTerminalBlock block) => true;
            DisableBlockThrustersCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.DisableBlockThrusters = state;
                logic.Settings.SaveCustomData(block);
                logic.Settings.IsBlocksDisabled();
                logic.RefreshTerminal();
            };
            DisableBlockThrustersCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.DisableBlockThrusters;
            };
            _terminalControls.Add(DisableBlockThrustersCheckBox);

            #endregion

            #region DisableProduction

            var DisableProductionCheckBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>($"CZ_DisableProductionCheckBox");
            DisableProductionCheckBox.Title = MyStringId.GetOrCompute("Disable Production                  ");
            DisableProductionCheckBox.Enabled = (IMyTerminalBlock block) => true;
            DisableProductionCheckBox.Visible = (IMyTerminalBlock block) => true;
            DisableProductionCheckBox.Setter = (IMyTerminalBlock block, bool state) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return;
                logic.Settings.DisableProduction = state;
                logic.Settings.SaveCustomData(block);
                logic.Settings.IsBlocksDisabled();
                logic.RefreshTerminal();
            };
            DisableProductionCheckBox.Getter = (IMyTerminalBlock block) =>
            {
                var logic = block?.GameLogic?.GetAs<ContestedZone>();
                if (logic == null) return false;
                return logic.Settings.DisableProduction;
            };
            _terminalControls.Add(DisableProductionCheckBox);

            #endregion

            MyAPIGateway.TerminalControls.CustomControlGetter += CustomControlGetter;
            MyAPIGateway.TerminalControls.CustomActionGetter += CustomActionGetter;
        }

        protected override void UnloadData()
        {
            Logs.Close();
            MyAPIGateway.TerminalControls.CustomControlGetter -= CustomControlGetter;
            MyAPIGateway.TerminalControls.CustomActionGetter -= CustomActionGetter;
        }

        private void CustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (!(block is IMyBeacon) || (block.BlockDefinition.SubtypeName != "LargeBlockContestedZone" && block.BlockDefinition.SubtypeName != "SmallBlockContestedZone")) return;

            for (int i = 0; i < controls.Count; i++)
            {
                var control = controls[i];
                if (control.Id == "Radius")
                {
                    var slider = control as IMyTerminalControlSlider;
                    slider.Title = MyStringId.GetOrCompute("Signal Radius");
                    slider.Tooltip = MyStringId.NullOrEmpty;
                    break;
                }
                if (control.Id == "CustomData")
                {
                    controls.Insert(i + 1, LoadFromCustomDataButton);
                }
            }

            foreach (var control in _terminalControls)
            {
                controls.Add(control);
            }
        }

        private void CustomActionGetter(IMyTerminalBlock block, List<IMyTerminalAction> actions)
        {
            if (!(block is IMyBeacon) || (block.BlockDefinition.SubtypeName != "LargeBlockContestedZone" && block.BlockDefinition.SubtypeName != "SmallBlockContestedZone")) return;

            foreach (var action in _terminalActions)
            {
                actions.Add(action);
            }
        }
    }
}