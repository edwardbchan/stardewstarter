using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace Adrenaline2
{
    internal sealed class ModEntry : Mod
    {
        private Dictionary<NPC, int> _monsterHealthLastTick = new();
        private int _hitCooldown = 0;
        private const int HitCooldownDuration = 40;
        private float _adrenalineBar = 0f;
        private const float MaxBar = 10f;
        private bool _adrenalineActive = false;
        private int _adrenalineTimer = 0;
        private const int AdrenalineDuration = 600;
        private int _lastHealth;

        internal static ModEntry Instance = null!;

        public override void Entry(IModHelper helper)
        {
            Monitor.Log("Adrenaline mod loaded!", LogLevel.Info);
            Instance = this;

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Display.RenderedHud += OnRenderedHud;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            _lastHealth = Game1.player.health;
            _monsterHealthLastTick.Clear();
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            var player = Game1.player;

            // Check damage BEFORE updating _lastHealth
            bool tookDamage = player.health < _lastHealth;

            if (tookDamage && !_adrenalineActive)
            {
                _adrenalineBar = 0f;
                _hitCooldown = HitCooldownDuration;
                Monitor.Log("Took damage! Adrenaline reset.", LogLevel.Debug);
            }

            // Now update last health
            _lastHealth = player.health;

            // Tick adrenaline timer
            if (_adrenalineActive)
            {
                _adrenalineTimer--;
                if (_adrenalineTimer <= 0)
                {
                    _adrenalineActive = false;
                    Monitor.Log("Adrenaline wore off.", LogLevel.Debug);
                }
            }

            // Tick hit cooldown
            if (_hitCooldown > 0)
            {
                _hitCooldown--;
            }

            // Track monster health and detect actual damage dealt
            if (player.currentLocation != null)
            {
                var currentMonsters = new Dictionary<NPC, int>();
                bool hitLanded = false;

                foreach (NPC npc in player.currentLocation.characters)
                {
                    if (!npc.IsMonster) continue;
                    var monster = (Monster)npc;
                    currentMonsters[npc] = monster.Health;

                    if (_monsterHealthLastTick.TryGetValue(npc, out int lastHealth) &&
                        monster.Health < lastHealth)
                    {
                        hitLanded = true;
                    }
                }

                // Check for monsters that were killed this tick
                foreach (var kvp in _monsterHealthLastTick)
                {
                    if (!currentMonsters.ContainsKey(kvp.Key) && kvp.Value > 0)
                    {
                        hitLanded = true;
                    }
                }

                if (hitLanded && !tookDamage && _hitCooldown == 0)
                {
                    _hitCooldown = HitCooldownDuration;
                    RegisterHit();
                }

                _monsterHealthLastTick = currentMonsters;
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            if (e.Button != SButton.Q) return;
            if (_adrenalineBar < MaxBar) return;
            if (_adrenalineActive) return;

            _adrenalineActive = true;
            _adrenalineTimer = AdrenalineDuration;
            _adrenalineBar = 0f;
            Monitor.Log("ADRENALINE ACTIVATED! 2x damage for 10 seconds!", LogLevel.Info);
        }

        private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            var sb = e.SpriteBatch;
            int barWidth = 200;
            int barHeight = 20;
            int x = 20;
            int y = Game1.uiViewport.Height - 100;

            sb.Draw(Game1.staminaRect, new Rectangle(x, y, barWidth, barHeight), Color.DarkRed * 0.8f);

            float fill = _adrenalineBar / MaxBar;
            sb.Draw(Game1.staminaRect, new Rectangle(x, y, (int)(barWidth * fill), barHeight),
                _adrenalineActive ? Color.Gold : Color.OrangeRed);

            string label = _adrenalineActive
                ? $"ADRENALINE! ({_adrenalineTimer / 60 + 1}s)"
                : $"Adrenaline: {_adrenalineBar}/{MaxBar}";
            Game1.drawWithBorder(label, Color.Black, _adrenalineActive ? Color.Gold : Color.White,
                new Vector2(x, y - 30));
        }

        public void RegisterHit()
        {
            if (_adrenalineActive) return;
            _adrenalineBar = Math.Min(_adrenalineBar + 1f, MaxBar);
            Monitor.Log($"Hit registered! Bar: {_adrenalineBar}/{MaxBar}", LogLevel.Debug);
        }

        public bool IsAdrenalineActive() => _adrenalineActive;
    }
}