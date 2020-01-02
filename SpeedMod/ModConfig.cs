using StardewModdingAPI;
using System;

namespace SpeedMod
{
    public class ModConfig
    {
        public bool EnabledInMultiplayer { get; set; }

        public int SpeedModifier { get; set; }

        public int StaminaCost { get; set; }

        public TimeSpan RecastCooldown { get; set; }

        public TimeSpan CastCooldown { get; set; }

        public SButton TeleportHomeKey { get; set; }
        
        public ModConfig()
        {
            EnabledInMultiplayer = false;
            SpeedModifier = 2;
            StaminaCost = 50;
            RecastCooldown = new TimeSpan(0, 7, 0);
            CastCooldown = new TimeSpan(0, 0, 0, 5);
            TeleportHomeKey = SButton.H;
        }

        private string GetOptionState(bool option)
        {
            return option ? "X" : " ";
        }

        public override string ToString()
        {
            return "Configuration:\n"
                + $" - Enabled in multiplier:\t\t\t[{GetOptionState(EnabledInMultiplayer)}]\n"
                + $" - Speed modifier:\t\t\t\t{SpeedModifier}\n"
                + $" - Teleport home key:\t\t\t\t{TeleportHomeKey}\n"
                + $" - Teleport cast length:\t\t\t{CastCooldown:mm\\:ss}\n"
                + $" - Teleport cooldown:\t\t\t\t{RecastCooldown:mm\\:ss}\n"
                + $" - Teleport stamina cost:\t\t\t{StaminaCost}";
        }
    }
}
