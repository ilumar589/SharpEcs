namespace SharpEcs.Component;

/// <summary>
/// Position component stored as a value type with SIMD-ready layout.
/// </summary>
/// <remarks>
/// <para>
/// The Position struct represents a 3D position in space using single-precision
/// floating-point values. As a value type, it is stored inline within component
/// stores, enabling cache-friendly memory access patterns.
/// </para>
/// <para>
/// The layout is compatible with SIMD operations via <see cref="System.Numerics.Vector3"/>,
/// allowing for high-performance batch processing of position data.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var world = new EcsWorld();
/// var entity = world.CreateEntity(new Position { X = 10.0f, Y = 20.0f, Z = 0.0f });
/// 
/// // Retrieve and modify position
/// if (world.GetComponent&lt;Position&gt;(entity) is Position pos)
/// {
///     Console.WriteLine($"Entity at ({pos.X}, {pos.Y}, {pos.Z})");
/// }
/// </code>
/// </example>
public struct Position
{
    /// <summary>
    /// Gets or sets the X coordinate.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Gets or sets the Z coordinate.
    /// </summary>
    public float Z { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Position"/> struct.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    public Position(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Returns a string representation of this position.
    /// </summary>
    /// <returns>A string containing the X, Y, and Z coordinates.</returns>
    public override readonly string ToString()
    {
        return $"Position({X}, {Y}, {Z})";
    }
}
