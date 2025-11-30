namespace SharpEcs.Component;

/// <summary>
/// Velocity component for movement systems.
/// </summary>
/// <remarks>
/// <para>
/// The Velocity struct represents a 3D velocity vector using single-precision
/// floating-point values. It is typically used in conjunction with <see cref="Position"/>
/// to implement movement systems.
/// </para>
/// <para>
/// As a value type, Velocity is stored inline within component stores,
/// enabling efficient batch processing and SIMD optimization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var world = new EcsWorld();
/// var entity = world.CreateEntity(
///     new Position { X = 0, Y = 0, Z = 0 },
///     new Velocity { X = 1.0f, Y = 0.5f, Z = 0.0f }
/// );
/// 
/// // Movement system example
/// foreach (var e in world.Query(typeof(Position), typeof(Velocity)))
/// {
///     var pos = world.GetComponent&lt;Position&gt;(e)!.Value;
///     var vel = world.GetComponent&lt;Velocity&gt;(e)!.Value;
///     world.SetComponent(e, new Position { X = pos.X + vel.X, Y = pos.Y + vel.Y, Z = pos.Z + vel.Z });
/// }
/// </code>
/// </example>
public struct Velocity
{
    /// <summary>
    /// Gets or sets the X component of velocity.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Gets or sets the Y component of velocity.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Gets or sets the Z component of velocity.
    /// </summary>
    public float Z { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Velocity"/> struct.
    /// </summary>
    /// <param name="x">The X component of velocity.</param>
    /// <param name="y">The Y component of velocity.</param>
    /// <param name="z">The Z component of velocity.</param>
    public Velocity(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Returns a string representation of this velocity.
    /// </summary>
    /// <returns>A string containing the X, Y, and Z velocity components.</returns>
    public override readonly string ToString()
    {
        return $"Velocity({X}, {Y}, {Z})";
    }
}
