﻿using Alca259.Common;
using HealthBars.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace HealthBars;

/// <summary>The main entry point.</summary>
public class ModEntry : Mod
{
    /*********
    ** Properties
    *********/
    /// <summary>The mod configuration.</summary>
    private ModConfig Config;

    /// <summary>The cached health bar texture.</summary>
    private Texture2D BarTexture;


    /*********
    ** Public methods
    *********/
    /// <summary>The mod entry point, called after the mod is first loaded.</summary>
    /// <param name="helper">Provides simplified APIs for writing mods.</param>
    public override void Entry(IModHelper helper)
    {
        CommonHelper.RemoveObsoleteFiles(this, "HealthBars.pdb");

        // read config
        Config = helper.ReadConfig<ModConfig>();

        // build bar texture
        BarTexture = GetBarTexture();

        // hook events
        helper.Events.Display.RenderedWorld += OnRenderedWorld;
        helper.Events.Input.ButtonsChanged += OnButtonsChanged;
    }


    /*********
    ** Private methods
    *********/
    /// <inheritdoc cref="IDisplayEvents.RenderedWorld"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        SpriteFont font = Game1.smallFont;
        SpriteBatch batch = Game1.spriteBatch;

        foreach (NPC npc in Game1.currentLocation.characters)
        {
            if (npc is Monster monster)
                DrawHealthBar(batch, monster, font);
        }
    }

    /// <inheritdoc cref="IInputEvents.ButtonsChanged"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
    {
        if (Config.ReloadKey.JustPressed())
        {
            Config = Helper.ReadConfig<ModConfig>();
            Monitor.Log("Config reloaded", LogLevel.Info);
        }
    }

    /// <summary>Draw a health bar for the given monster, if needed.</summary>
    /// <param name="batch">The sprite batch being drawn.</param>
    /// <param name="monster">The monster whose health to display.</param>
    /// <param name="font">The font to use for the health numbers.</param>
    private void DrawHealthBar(SpriteBatch batch, Monster monster, SpriteFont font)
    {
        if (monster.MaxHealth < monster.Health)
            monster.MaxHealth = monster.Health;

        if (monster.MaxHealth == monster.Health && !Config.DisplayHealthWhenNotDamaged)
            return;

        Vector2 size = new Vector2(monster.Sprite.SpriteWidth, monster.Sprite.SpriteHeight) * Game1.pixelZoom;

        Vector2 screenLoc = Game1.GlobalToLocal(monster.position.Value);
        screenLoc.X += size.X / 2 - Config.BarWidth / 2.0f;
        screenLoc.Y -= Config.BarHeight;

        float fill = monster.Health / (float)monster.MaxHealth;

        batch.Draw(BarTexture, screenLoc + new Vector2(Config.BarBorderWidth, Config.BarBorderHeight), BarTexture.Bounds, Color.Lerp(Config.LowHealthColor, Config.HighHealthColor, fill), 0.0f, Vector2.Zero, new Vector2(fill, 1.0f), SpriteEffects.None, 0);

        if (Config.DisplayCurrentHealthNumber)
        {
            string textLeft = monster.Health.ToString();
            Vector2 textSizeL = font.MeasureString(textLeft);
            if (Config.DisplayTextBorder)
                batch.DrawString(Game1.smallFont, textLeft, screenLoc - new Vector2(-1.0f, textSizeL.Y + 1.65f), Config.TextBorderColor, 0.0f, Vector2.Zero, 0.66f, SpriteEffects.None, 0);
            batch.DrawString(font, textLeft, screenLoc - new Vector2(0.0f, textSizeL.Y + 1.0f), Config.TextColor, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0);
        }

        if (Config.DisplayMaxHealthNumber)
        {
            string textRight = monster.MaxHealth.ToString();
            Vector2 textSizeR = font.MeasureString(textRight);
            if (Config.DisplayTextBorder)
                batch.DrawString(Game1.smallFont, textRight, screenLoc + new Vector2(Config.BarWidth, 0.0f) - new Vector2(textSizeR.X - 1f, textSizeR.Y + 1.65f), Config.TextBorderColor, 0.0f, Vector2.Zero, 0.66f, SpriteEffects.None, 0);
            batch.DrawString(font, textRight, screenLoc + new Vector2(Config.BarWidth, 0.0f) - new Vector2(textSizeR.X, textSizeR.Y + 1.0f), Config.TextColor, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0);
        }
    }

    /// <summary>Get a health bar texture.</summary>
    private Texture2D GetBarTexture()
    {
        // calculate size
        int innerBarWidth = Config.BarWidth - Config.BarBorderWidth * 2;
        int innerBarHeight = Config.BarHeight - Config.BarBorderHeight * 2;

        // get pixels
        var data = new uint[innerBarWidth * innerBarHeight];
        for (int i = 0; i < data.Length; i++)
            data[i] = 0xffffffff;

        // build texture
        var texture = new Texture2D(Game1.graphics.GraphicsDevice, innerBarWidth, innerBarHeight);
        texture.SetData(data);
        return texture;
    }
}
