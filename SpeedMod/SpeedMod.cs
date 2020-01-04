using StardewModdingAPI.Events;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using System.Timers;
using StardewValley;
using System;
using ModSettingsTabApi.Events;
using ModSettingsTabApi.Framework.Interfaces;

namespace SpeedMod
{
    public class SpeedMod : Mod, ISettingsTabApi
    {
        private Timer _coolDownTimer;
        private double _countDown;
        private bool _cooldown;
#pragma warning disable CS0067
        public event EventHandler<OptionsChangedEventArgs> OptionsChanged;
#pragma warning restore CS0067
        public static ModConfig Config { get; private set; }

        public override void Entry(IModHelper modHelper)
        {
            const string ModSettingsTabUniqueID = "GilarF.ModSettingsTab";

            Config = Helper.ReadConfig<ModConfig>();

            modHelper.Events.GameLoop.SaveLoaded += SpeedMod_GameLoaded;
            modHelper.Events.GameLoop.UpdateTicked += SpeedMod_Speedup;
            modHelper.Events.GameLoop.DayEnding += GameLoop_DayEnding;
            modHelper.Events.Input.ButtonPressed += SpeedMod_KeyUp;

            if (Config != null)
            {
                Monitor.Log(Config.ToString(), LogLevel.Info);

                modHelper.Events.GameLoop.GameLaunched += (sender, args) => {
                    var modSettingsTabApi = modHelper.ModRegistry.GetApi<IModTabSettingsApi>(ModSettingsTabUniqueID);

                    if (modSettingsTabApi != null)
                    {
                        Monitor.Log("ModSettingsTab is present", LogLevel.Info);

                        var modTab = modSettingsTabApi.GetMod(Helper.ModRegistry.ModID);

                        if (modTab != null)
                        {
                            Monitor.Log("ModSettingsTab integration success", LogLevel.Info);

                            modTab.OptionsChanged += ModTabSettingsOptionsChanged;
                        }
                    }
                };
            }
        }

        private void ModTabSettingsOptionsChanged(object sender, OptionsChangedEventArgs e)
        {
            Config = Helper.ReadConfig<ModConfig>();

            if (Config != null)
            {
                Monitor.Log(Config.ToString(), LogLevel.Info);
                e.Reloaded = true;
            }
        }

        private void GameLoop_DayEnding(object sender, DayEndingEventArgs e)
        {
            _coolDownTimer?.Stop();
            _cooldown = false;
            _countDown = 0.0;
        }

        private void SpeedMod_GameLoaded(object sender, EventArgs e)
        {
            _cooldown = false;
        }

        private void SpeedMod_KeyUp(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsPlayerFree || !Context.IsWorldReady)
                return;

            if (e.Button == Config.TeleportHomeKey)
                TeleportHome();
        }

        private void SpeedMod_Speedup(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            Game1.player.addedSpeed = Math.Max(Config.SpeedModifier, Game1.player.addedSpeed);
        }

        private void TeleportHome()
        {
            if (!Context.IsWorldReady || Game1.player.currentLocation.IsFarm || (!Config.EnabledInMultiplayer && Game1.IsMultiplayer) || !Context.IsPlayerFree || !Context.CanPlayerMove)
                return;

            if (_cooldown)
            {
                var duration = TimeSpan.FromMilliseconds(Config.RecastCooldown.TotalMilliseconds - _countDown);
                var message = Helper.Translation.Get("TeleportOnCooldown", new { timeLeft = $"{duration:mm\\:ss}" });

                Game1.player.doEmote(15);                
                Game1.drawObjectDialogue(message);
            }
            else if (Game1.player.stamina < Config.StaminaCost + 1)
            {
                Game1.drawObjectDialogue(Helper.Translation.Get("NotEnoughStamina"));
            }
            else if (Game1.dialogueUp == false)
            {
                var location = Game1.player.currentLocation;
                var responses = location.createYesNoResponses();
                var teleportQuestion = Helper.Translation.Get("TeleportQuestion");

                location.createQuestionDialogue(teleportQuestion, responses, AnswerTeleportHome);
                location.lastQuestionKey = "Teleport";
            }
        }

        public void AnswerTeleportHome(Farmer who, string whichAnswer)
        {
            switch (whichAnswer)
            {
                case "Yes":
                    StartTeleport();
                break;
            }
        }

        public void StartTeleport()
        {
            if (!Context.IsWorldReady || _cooldown)
                return;

            var countdownMessage = Helper.Translation.Get("CountDownMessage", new { timeLeft = (int)Config.CastCooldown.TotalMilliseconds / 1000 });
            var teleportTimer = new Timer
            {
                Interval = 1000,
                AutoReset = true,
                Enabled = true
            };

            Game1.addHUDMessage(new HUDMessage(countdownMessage, Color.OrangeRed, 1000) {noIcon = true});
            Game1.player.faceDirection(2);
            Game1.freezeControls = true;
            Game1.player.completelyStopAnimatingOrDoingAction();
            teleportTimer.Elapsed += ExecuteTeleport;
            teleportTimer.Start();

            _cooldown = true;
        }

        private void ExecuteTeleport(object source, ElapsedEventArgs e)
        {
            if (!(source is Timer timer))
                return;

            _countDown += timer.Interval;

            var remainingTime = (int)(Config.CastCooldown.TotalMilliseconds - _countDown) / 1000;
            string message = (remainingTime <= 0)
                           ? Helper.Translation.Get("TeleportingNow") 
                           : Helper.Translation.Get("CountDownMessage", new { timeLeft = remainingTime});

            Game1.addHUDMessage(new HUDMessage(message, Color.OrangeRed, 1500) { noIcon = true });

            if (_countDown < Config.CastCooldown.TotalMilliseconds)
                return;

            timer.Stop();
            timer.Dispose();

            Game1.playSound("thunder");
            Game1.warpHome();
            Game1.freezeControls = false;
            Game1.player.stamina -= Config.StaminaCost;
            _countDown = 0.0;

            _coolDownTimer = new Timer
            {
                Interval = 1000,
                AutoReset = true,
                Enabled = true
            };

            _coolDownTimer.Elapsed += TickCooldown;
        }

        private void TickCooldown(object source, ElapsedEventArgs e)
        {
            if (!(source is Timer timer))
                return;

            _countDown += timer.Interval;

            if (_countDown < Config.RecastCooldown.TotalMilliseconds)
                return;

            timer.Stop();
            timer.Dispose();

            _cooldown = false;
            _countDown = 0.0;
        }
    }
}