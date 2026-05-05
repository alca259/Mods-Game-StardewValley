using Alca259.Common;
using NoMoreStuckMonsters.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;

namespace NoMoreStuckMonsters;

public partial class ModEntry : Mod
{
    #region Fields
    private ModConfig _config = null!;
    #endregion

    #region Override entry point
    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        CommonHelper.RemoveObsoleteFiles(this, "NoMoreStuckMonsters.pdb");
        _config = helper.ReadConfig<ModConfig>();
        _config.EnsureArguments();

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }
    #endregion

    #region Event Handlers
    /// <summary>Inicializa integraciones del mod tras cargar el juego.</summary>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        SetupGenericModMenu();
    }
    #endregion
}
