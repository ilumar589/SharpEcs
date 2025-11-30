namespace SharpEcs.Entity;

/// <summary>
/// Represents an entity in the ECS world as a value type.
/// Uses struct for zero-allocation and inline storage.
/// </summary>
/// <remarks>
/// <para>
/// The Entity struct is the fundamental identifier in the ECS framework.
/// It consists of an Id and a Generation, where the Generation helps detect
/// stale entity references after an entity has been destroyed and its Id reused.
/// </para>
/// <para>
/// As a value type (struct), Entity instances are stored inline and copied by value,
/// eliminating heap allocations and providing cache-friendly memory access patterns.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var world = new EcsWorld();
/// Entity entity = world.CreateEntity(new Position { X = 0, Y = 0, Z = 0 });
/// 
/// // Entities can be compared for equality
/// if (entity == anotherEntity)
/// {
///     Console.WriteLine("Same entity!");
/// }
/// </code>
/// </example>
public readonly struct Entity : IEquatable<Entity>
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    /// <remarks>
    /// The Id is unique within a generation but may be reused after the entity is destroyed.
    /// Always check the Generation when validating entity references.
    /// </remarks>
    public int Id { get; }

    /// <summary>
    /// Gets the generation of this entity.
    /// </summary>
    /// <remarks>
    /// The generation is incremented each time an entity Id is reused.
    /// This allows detection of stale entity references that point to
    /// a destroyed entity whose Id has been recycled.
    /// </remarks>
    public int Generation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity"/> struct.
    /// </summary>
    /// <param name="id">The unique identifier for the entity.</param>
    /// <param name="generation">The generation of the entity.</param>
    public Entity(int id, int generation)
    {
        Id = id;
        Generation = generation;
    }

    /// <summary>
    /// Determines whether this entity is equal to another entity.
    /// </summary>
    /// <param name="other">The other entity to compare with.</param>
    /// <returns><c>true</c> if the entities are equal; otherwise, <c>false</c>.</returns>
    public bool Equals(Entity other)
    {
        return Id == other.Id && Generation == other.Generation;
    }

    /// <summary>
    /// Determines whether this entity is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><c>true</c> if the object is an entity and is equal to this entity; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Entity other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for this entity.
    /// </summary>
    /// <returns>A hash code that combines the Id and Generation.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Generation);
    }

    /// <summary>
    /// Determines whether two entities are equal.
    /// </summary>
    /// <param name="left">The first entity.</param>
    /// <param name="right">The second entity.</param>
    /// <returns><c>true</c> if the entities are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Entity left, Entity right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two entities are not equal.
    /// </summary>
    /// <param name="left">The first entity.</param>
    /// <param name="right">The second entity.</param>
    /// <returns><c>true</c> if the entities are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Entity left, Entity right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Returns a string representation of this entity.
    /// </summary>
    /// <returns>A string containing the Id and Generation of the entity.</returns>
    public override string ToString()
    {
        return $"Entity(Id: {Id}, Generation: {Generation})";
    }
}
