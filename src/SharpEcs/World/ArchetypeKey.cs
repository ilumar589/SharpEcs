namespace SharpEcs.World;

/// <summary>
/// Represents a hashable key for an archetype based on its component types.
/// </summary>
/// <remarks>
/// <para>
/// ArchetypeKey provides an efficient way to identify and lookup archetypes
/// based on their component composition. It implements proper equality semantics
/// to work with dictionaries and hash sets.
/// </para>
/// <para>
/// The key is based on a sorted, immutable set of component types, ensuring
/// that two archetypes with the same components will have the same key
/// regardless of the order in which components were added.
/// </para>
/// </remarks>
public sealed class ArchetypeKey : IEquatable<ArchetypeKey>
{
    /// <summary>
    /// The set of component types that define this archetype key.
    /// </summary>
    private readonly HashSet<Type> _types;

    /// <summary>
    /// The cached hash code for this key.
    /// </summary>
    private readonly int _hashCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchetypeKey"/> class.
    /// </summary>
    /// <param name="types">The component types that define this archetype.</param>
    public ArchetypeKey(IEnumerable<Type> types)
    {
        _types = new HashSet<Type>(types);
        _hashCode = ComputeHashCode();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchetypeKey"/> class.
    /// </summary>
    /// <param name="types">The component types that define this archetype.</param>
    public ArchetypeKey(params Type[] types)
        : this((IEnumerable<Type>)types)
    {
    }

    /// <summary>
    /// Gets the component types in this archetype key.
    /// </summary>
    public IReadOnlySet<Type> Types => _types;

    /// <summary>
    /// Gets the number of component types in this archetype key.
    /// </summary>
    public int Count => _types.Count;

    /// <summary>
    /// Determines whether this archetype key contains the specified component type.
    /// </summary>
    /// <typeparam name="T">The component type to check for.</typeparam>
    /// <returns><c>true</c> if the key contains the type; otherwise, <c>false</c>.</returns>
    public bool Contains<T>() where T : struct
    {
        return _types.Contains(typeof(T));
    }

    /// <summary>
    /// Determines whether this archetype key contains the specified component type.
    /// </summary>
    /// <param name="type">The component type to check for.</param>
    /// <returns><c>true</c> if the key contains the type; otherwise, <c>false</c>.</returns>
    public bool Contains(Type type)
    {
        return _types.Contains(type);
    }

    /// <summary>
    /// Determines whether this archetype key contains all the specified component types.
    /// </summary>
    /// <param name="types">The component types to check for.</param>
    /// <returns><c>true</c> if the key contains all types; otherwise, <c>false</c>.</returns>
    public bool ContainsAll(IEnumerable<Type> types)
    {
        return types.All(_types.Contains);
    }

    /// <summary>
    /// Creates a new archetype key with an additional component type.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <returns>A new archetype key including the additional type.</returns>
    public ArchetypeKey With<T>() where T : struct
    {
        var newTypes = new HashSet<Type>(_types) { typeof(T) };
        return new ArchetypeKey(newTypes);
    }

    /// <summary>
    /// Creates a new archetype key with an additional component type.
    /// </summary>
    /// <param name="type">The component type to add.</param>
    /// <returns>A new archetype key including the additional type.</returns>
    public ArchetypeKey With(Type type)
    {
        var newTypes = new HashSet<Type>(_types) { type };
        return new ArchetypeKey(newTypes);
    }

    /// <summary>
    /// Creates a new archetype key without the specified component type.
    /// </summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    /// <returns>A new archetype key excluding the specified type.</returns>
    public ArchetypeKey Without<T>() where T : struct
    {
        var newTypes = new HashSet<Type>(_types);
        newTypes.Remove(typeof(T));
        return new ArchetypeKey(newTypes);
    }

    /// <summary>
    /// Creates a new archetype key without the specified component type.
    /// </summary>
    /// <param name="type">The component type to remove.</param>
    /// <returns>A new archetype key excluding the specified type.</returns>
    public ArchetypeKey Without(Type type)
    {
        var newTypes = new HashSet<Type>(_types);
        newTypes.Remove(type);
        return new ArchetypeKey(newTypes);
    }

    /// <summary>
    /// Computes a hash code based on the component types.
    /// </summary>
    /// <returns>The computed hash code.</returns>
    private int ComputeHashCode()
    {
        // Use XOR for order-independent hash combination
        int hash = 0;
        foreach (var type in _types)
        {
            hash ^= type.GetHashCode();
        }
        return hash;
    }

    /// <summary>
    /// Determines whether this archetype key is equal to another archetype key.
    /// </summary>
    /// <param name="other">The other archetype key to compare with.</param>
    /// <returns><c>true</c> if the keys are equal; otherwise, <c>false</c>.</returns>
    public bool Equals(ArchetypeKey? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_hashCode != other._hashCode) return false;
        if (_types.Count != other._types.Count) return false;
        return _types.SetEquals(other._types);
    }

    /// <summary>
    /// Determines whether this archetype key is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><c>true</c> if the object is an archetype key and is equal to this key; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is ArchetypeKey other && Equals(other);
    }

    /// <summary>
    /// Returns the cached hash code for this archetype key.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return _hashCode;
    }

    /// <summary>
    /// Determines whether two archetype keys are equal.
    /// </summary>
    /// <param name="left">The first archetype key.</param>
    /// <param name="right">The second archetype key.</param>
    /// <returns><c>true</c> if the keys are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(ArchetypeKey? left, ArchetypeKey? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two archetype keys are not equal.
    /// </summary>
    /// <param name="left">The first archetype key.</param>
    /// <param name="right">The second archetype key.</param>
    /// <returns><c>true</c> if the keys are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(ArchetypeKey? left, ArchetypeKey? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Returns a string representation of this archetype key.
    /// </summary>
    /// <returns>A string containing the component type names.</returns>
    public override string ToString()
    {
        var typeNames = _types.Select(t => t.Name).OrderBy(n => n);
        return $"ArchetypeKey[{string.Join(", ", typeNames)}]";
    }
}
