using StardewModdingAPI;
using StardewValley.Menus;

namespace FishingMod.Framework;

/// <summary>Handles access to a bobber bar's private fields.</summary>
internal class SBobberBar
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying field for <see cref="DistanceFromCatching"/>.</summary>
    private readonly IReflectedField<float> DistanceFromCatchingField;

    /// <summary>The underlying field for <see cref="Difficulty"/>.</summary>
    private readonly IReflectedField<float> DifficultyField;

    /// <summary>The underlying field for <see cref="MotionType"/>.</summary>
    private readonly IReflectedField<int> MotionTypeField;

    /// <summary>The underlying field for <see cref="BobberInBar"/>.</summary>
    private readonly IReflectedField<bool> BobberInBarField;

    /// <summary>The underlying field for <see cref="Treasure"/>.</summary>
    private readonly IReflectedField<bool> TreasureField;

    /// <summary>The underlying field for <see cref="TreasureCaught"/>.</summary>
    private readonly IReflectedField<bool> TreasureCaughtField;

    /// <summary>The underlying field for <see cref="Perfect"/>.</summary>
    private readonly IReflectedField<bool> PerfectField;


    /*********
    ** Accessors
    *********/
    /// <summary>The underlying bobber bar.</summary>
    public BobberBar Instance { get; set; }

    /// <summary>
    ///     The green bar on the right. How close to catching the fish you are
    ///     Range: 0 - 1 | 1 = catch, 0 = fail
    /// </summary>
    public float DistanceFromCatching
    {
        get => DistanceFromCatchingField.GetValue();
        set => DistanceFromCatchingField.SetValue(value);
    }

    public float Difficulty
    {
        get => DifficultyField.GetValue();
        set => DifficultyField.SetValue(value);
    }

    public int MotionType
    {
        get => MotionTypeField.GetValue();
        set => MotionTypeField.SetValue(value);
    }

    public bool BobberInBar
    {
        get => BobberInBarField.GetValue();
    }

    /// <summary>
    ///     Whether or not a treasure chest appears
    /// </summary>
    public bool Treasure
    {
        get => TreasureField.GetValue();
        set => TreasureField.SetValue(value);
    }

    public bool TreasureCaught
    {
        get => TreasureCaughtField.GetValue();
        set => TreasureCaughtField.SetValue(value);
    }

    public bool Perfect
    {
        get => PerfectField.GetValue();
        set => PerfectField.SetValue(value);
    }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="instance">The underlying bobber bar.</param>
    /// <param name="reflection">Simplifies access to private code.</param>
    public SBobberBar(BobberBar instance, IReflectionHelper reflection)
    {
        Instance = instance;

        DistanceFromCatchingField = reflection.GetField<float>(instance, "distanceFromCatching");
        DifficultyField = reflection.GetField<float>(instance, "difficulty");
        MotionTypeField = reflection.GetField<int>(instance, "motionType");
        BobberInBarField = reflection.GetField<bool>(instance, "bobberInBar");
        TreasureField = reflection.GetField<bool>(instance, "treasure");
        TreasureCaughtField = reflection.GetField<bool>(instance, "treasureCaught");
        PerfectField = reflection.GetField<bool>(instance, "perfect");
    }
}
