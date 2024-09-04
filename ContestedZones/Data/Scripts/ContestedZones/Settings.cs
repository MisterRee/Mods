/*
 * 
 * This open-source project is available on GitHub and licensed under the GNU General Public License v3.0.
 * You are free to use, modify, and distribute this software under the terms of the GPL-3.0 license.
 * 
 * GitHub Repository: [https://github.com/MisterRee/Mods]
 * 
 */
using Sandbox.ModAPI;
using System;
using System.Text;
using VRageMath;

namespace ContestedZones
{
    internal class Settings
    {
        internal int EffectRadius = 20;
        internal bool ShowArea = false;
        internal Color ShowAreaColor = new Color(0, 255, 255, 175);
        internal bool EnableEnforcementMessage = true;
        internal string EnforcementMessage = "WARNING! {tool}s are disabled in the Contested Zone!";
        internal bool EnableApproachZoneMessage = true;
        internal string ApproachZoneMessage = "WARNING! You are {distance} from a Contested Zone!";
        internal int ApproachZoneMessageRange = 1000;
        internal bool EnableInZoneMessage = true;
        internal string InZoneMessage = "WARNING! You are in a Contested Zone!";
        internal bool EnableOwnGridProtection = false;
        internal int OwnGridProtectionPercent = 100;
        internal bool EnablePlayerDamageOverTime = false;
        internal int PlayerDamageOverTimeAmount = 1;
        internal string PlayerDamageOverTimeMessage = "The Contested Zone is damaging you!";
        internal bool EnableGridDamageOverTime = false;
        internal int GridDamageOverTimeAmount = 1;
        internal string GridDamageOverTimeMessage = "The Contested Zone is damaging your grid!";
        internal bool ForcePlayerSignal = false;
        internal bool DisableJetpacks = false;
        internal bool DisableHandGrinders = false;
        internal bool DisableHandWelders = false;
        internal bool DisableHandDrills = false;
        internal bool DisableHandWeapons = false;
        internal bool DisableBlocks = false;
        internal bool DisableBlockGrinders = false;
        internal bool DisableBlockWelders = false;
        internal bool DisableBlockDrills = false;
        internal bool DisableBlockWeapons = false;
        internal bool DisableBlockThrusters = false;
        internal bool DisableProduction = false;

        internal void LoadCustomData(ContestedZone logic)
        {
            var customData = logic.Beacon.CustomData;
            if (string.IsNullOrEmpty(customData))
            {
                SaveCustomData(logic.Beacon);
                return;
            }
            string[] lines = customData.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    var key = parts[0];
                    var value = parts[1];
                    switch (key)
                    {
                        case "EffectRadius":
                            int.TryParse(value, out EffectRadius);
                            EffectRadius = MathHelper.Clamp(EffectRadius, 1, 200000000);
                            break;
                        case "ShowArea":
                            bool.TryParse(value, out ShowArea);
                            break;
                        case "ShowAreaColor":
                            uint packedvalue;
                            uint.TryParse(value, out packedvalue);
                            ShowAreaColor = new Color(packedvalue);
                            break;
                        case "EnableEnforcementMessage":
                            bool.TryParse(value, out EnableEnforcementMessage);
                            break;
                        case "EnforcementMessage":
                            EnforcementMessage = (value.Length <= 80) ? value : value.Substring(0, value.LastIndexOf(' ', 80)).TrimEnd();
                            break;
                        case "EnableApproachZoneMessage":
                            bool.TryParse(value, out EnableApproachZoneMessage);
                            break;
                        case "ApproachZoneMessage":
                            ApproachZoneMessage = (value.Length <= 80) ? value : value.Substring(0, value.LastIndexOf(' ', 80)).TrimEnd();
                            break;
                        case "ApproachZoneMessageRange":
                            int.TryParse(value, out ApproachZoneMessageRange);
                            ApproachZoneMessageRange = MathHelper.Clamp(ApproachZoneMessageRange, 1, 2000);
                            break;
                        case "EnableInZoneMessage":
                            bool.TryParse(value, out EnableInZoneMessage);
                            break;
                        case "InZoneMessage":
                            InZoneMessage = (value.Length <= 80) ? value : value.Substring(0, value.LastIndexOf(' ', 80)).TrimEnd();
                            break;
                        case "EnableOwnGridProtection":
                            bool.TryParse(value, out EnableOwnGridProtection);
                            break;
                        case "OwnGridProtectionPercent":
                            int.TryParse(value, out OwnGridProtectionPercent);
                            OwnGridProtectionPercent = MathHelper.Clamp(OwnGridProtectionPercent, 1, 100);
                            break;
                        case "EnablePlayerDamageOverTime":
                            bool.TryParse(value, out EnablePlayerDamageOverTime);
                            break;
                        case "PlayerDamageOverTimeAmount":
                            int.TryParse(value, out PlayerDamageOverTimeAmount);
                            PlayerDamageOverTimeAmount = MathHelper.Clamp(PlayerDamageOverTimeAmount, 1, 100);
                            break;
                        case "PlayerDamageOverTimeMessage":
                            PlayerDamageOverTimeMessage = (value.Length <= 80) ? value : value.Substring(0, value.LastIndexOf(' ', 80)).TrimEnd();
                            break;
                        case "EnableGridDamageOverTime":
                            bool.TryParse(value, out EnableGridDamageOverTime);
                            break;
                        case "GridDamageOverTimeAmount":
                            int.TryParse(value, out GridDamageOverTimeAmount);
                            GridDamageOverTimeAmount = MathHelper.Clamp(GridDamageOverTimeAmount, 1, 100);
                            break;
                        case "GridDamageOverTimeMessage":
                            GridDamageOverTimeMessage = (value.Length <= 80) ? value : value.Substring(0, value.LastIndexOf(' ', 80)).TrimEnd();
                            break;
                        case "ForcePlayerSignal":
                            bool.TryParse(value, out ForcePlayerSignal);
                            break;
                        case "DisableJetpacks":
                            bool.TryParse(value, out DisableJetpacks);
                            break;
                        case "DisableHandGrinders":
                            bool.TryParse(value, out DisableHandGrinders);
                            break;
                        case "DisableHandWelders":
                            bool.TryParse(value, out DisableHandWelders);
                            break;
                        case "DisableHandDrills":
                            bool.TryParse(value, out DisableHandDrills);
                            break;
                        case "DisableHandWeapons":
                            bool.TryParse(value, out DisableHandWeapons);
                            break;
                        case "DisableBlockGrinders":
                            bool.TryParse(value, out DisableBlockGrinders);
                            break;
                        case "DisableBlockWelders":
                            bool.TryParse(value, out DisableBlockWelders);
                            break;
                        case "DisableBlockDrills":
                            bool.TryParse(value, out DisableBlockDrills);
                            break;
                        case "DisableBlockWeapons":
                            bool.TryParse(value, out DisableBlockWeapons);
                            break;
                        case "DisableBlockThrusters":
                            bool.TryParse(value, out DisableBlockThrusters);
                            break;
                        case "DisableProduction":
                            bool.TryParse(value, out DisableProduction);
                            break;
                        default:
                            break;
                    }
                }
            }
            IsBlocksDisabled();
        }

        internal void IsBlocksDisabled()
        {
            DisableBlocks = DisableBlockGrinders || DisableBlockWelders || DisableBlockDrills || DisableBlockWeapons || DisableBlockThrusters || DisableProduction;
        }

        internal void SaveCustomData(IMyTerminalBlock beacon)
        {
            var data = new StringBuilder();
            data.AppendLine($"EffectRadius:{EffectRadius}");
            data.AppendLine($"ShowArea:{ShowArea}");
            data.AppendLine($"ShowAreaColor:{ShowAreaColor.PackedValue}");
            data.AppendLine($"EnableEnforcementMessage:{EnableEnforcementMessage}");
            data.AppendLine($"EnforcementMessage:{EnforcementMessage}");
            data.AppendLine($"EnableApproachZoneMessage:{EnableApproachZoneMessage}");
            data.AppendLine($"ApproachZoneMessage:{ApproachZoneMessage}");
            data.AppendLine($"ApproachZoneMessageRange:{ApproachZoneMessageRange}");
            data.AppendLine($"EnableInZoneMessage:{EnableInZoneMessage}");
            data.AppendLine($"InZoneMessage:{InZoneMessage}");
            data.AppendLine($"EnableOwnGridProtection:{EnableOwnGridProtection}");
            data.AppendLine($"OwnGridProtectionPercent:{OwnGridProtectionPercent}");
            data.AppendLine($"EnablePlayerDamageOverTime:{EnablePlayerDamageOverTime}");
            data.AppendLine($"PlayerDamageOverTimeAmount:{PlayerDamageOverTimeAmount}");
            data.AppendLine($"PlayerDamageOverTimeMessage:{PlayerDamageOverTimeMessage}");
            data.AppendLine($"EnableGridDamageOverTime:{EnableGridDamageOverTime}");
            data.AppendLine($"GridDamageOverTimeAmount:{GridDamageOverTimeAmount}");
            data.AppendLine($"GridDamageOverTimeMessage:{GridDamageOverTimeMessage}");
            data.AppendLine($"ForcePlayerSignal:{ForcePlayerSignal}");
            data.AppendLine($"DisableJetpacks:{DisableJetpacks}");
            data.AppendLine($"DisableHandGrinders:{DisableHandGrinders}");
            data.AppendLine($"DisableHandWelders:{DisableHandWelders}");
            data.AppendLine($"DisableHandDrills:{DisableHandDrills}");
            data.AppendLine($"DisableHandWeapons:{DisableHandWeapons}");
            data.AppendLine($"DisableBlockGrinders:{DisableBlockGrinders}");
            data.AppendLine($"DisableBlockWelders:{DisableBlockWelders}");
            data.AppendLine($"DisableBlockDrills:{DisableBlockDrills}");
            data.AppendLine($"DisableBlockWeapons:{DisableBlockWeapons}");
            data.AppendLine($"DisableBlockThrusters:{DisableBlockThrusters}");
            data.AppendLine($"DisableProduction:{DisableProduction}");
            beacon.CustomData = data.ToString();
        }
    }
}
