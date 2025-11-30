namespace SharpEcs.Component;

/// <summary>
/// Health component with current and maximum values.
/// </summary>
/// <remarks>
/// <para>
/// The Health struct represents an entity's health status, tracking both
/// the current health value and the maximum health capacity.
/// </para>
/// <para>
/// As a value type, Health is stored inline within component stores,
/// providing efficient memory access and zero-allocation updates.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var world = new EcsWorld();
/// var entity = world.CreateEntity(new Health { Current = 100, Maximum = 100 });
/// 
/// // Damage system example
/// if (world.GetComponent&lt;Health&gt;(entity) is Health health)
/// {
///     int damage = 25;
///     int newHealth = Math.Max(0, health.Current - damage);
///     world.SetComponent(entity, new Health { Current = newHealth, Maximum = health.Maximum });
/// }
/// </code>
/// </example>
public struct Health
{
    /// <summary>
    /// Gets or sets the current health value.
    /// </summary>
    public int Current { get; set; }

    /// <summary>
    /// Gets or sets the maximum health value.
    /// </summary>
    public int Maximum { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Health"/> struct.
    /// </summary>
    /// <param name="current">The current health value.</param>
    /// <param name="maximum">The maximum health value.</param>
    public Health(int current, int maximum)
    {
        Current = current;
        Maximum = maximum;
    }

    /// <summary>
    /// Gets the health as a percentage of maximum health.
    /// </summary>
    /// <returns>A value between 0 and 1 representing the health percentage.</returns>
    public readonly float GetPercentage()
    {
        return Maximum > 0 ? (float)Current / Maximum : 0f;
    }

    /// <summary>
    /// Determines whether the entity is alive (current health greater than zero).
    /// </summary>
    public readonly bool IsAlive => Current > 0;

    /// <summary>
    /// Determines whether the entity is at full health.
    /// </summary>
    public readonly bool IsFullHealth => Current >= Maximum;

    /// <summary>
    /// Returns a string representation of this health component.
    /// </summary>
    /// <returns>A string containing the current and maximum health values.</returns>
    public override readonly string ToString()
    {
        return $"Health({Current}/{Maximum})";
    }
}
