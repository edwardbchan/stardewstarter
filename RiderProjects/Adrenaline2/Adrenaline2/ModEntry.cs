using System;
using HarmonyLib;
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
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            var player = Game1.player;

            if (player.health < _lastHealth)
            {
                _adrenalineBar = 0f;
                if (_adrenalineActive)
                {
                    _adrenalineActive = false;
                    _adrenalineTimer = 0;
                }
                Monitor.Log("Took damage! Adrenaline reset.", LogLevel.Debug);
            }

            _lastHealth = player.health;

            if (_adrenalineActive)
            {
                _adrenalineTimer--;
                if (_adrenalineTimer <= 0)
                {
                    _adrenalineActive = false;
                    Monitor.Log("Adrenaline wore off.", LogLevel.Debug);
                }
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            Farmer character = Game1.player;
            Vector2 pos = character.Tile;
            foreach (Vector2 adjacentTilesOffset in Character.AdjacentTilesOffsets)
            {
                Vector2 tileLocation2 = pos + adjacentTilesOffset;
                NPC npc = character.currentLocation.isCharacterAtTile(tileLocation2);
                this.Monitor.Log("tile position " + tileLocation2, LogLevel.Debug);
                if (npc != null)
                {
                    this.Monitor.Log("this is an npc " + npc.Name, LogLevel.Debug);
                }

                if (npc != null && npc.IsMonster && !npc.Name.Equals("Cat"))
                {
                   
                    if (_adrenalineActive) return;
                    _adrenalineBar = Math.Min(_adrenalineBar + 1f, MaxBar);
                    Monitor.Log($"Hit registered! Bar: {_adrenalineBar}/{MaxBar}", LogLevel.Debug);
                }
            }
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

    internal class MonsterPatch
    {
        public static void DamageMonster_Postfix(Farmer? who, bool __result)
        {
            if (who == null || !who.IsLocalPlayer) return;
            if (!__result) return; // no monster was actually hit

            ModEntry.Instance.RegisterHit();
        }
    }
}