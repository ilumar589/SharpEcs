using SharpEcs.Entity;

namespace SharpEcs.World;

/// <summary>
/// Stores entities with identical component signatures.
/// Uses struct-based storage for cache-friendly layouts.
/// </summary>
/// <remarks>
/// <para>
/// An Archetype groups together all entities that share the same set of component types.
/// This allows for efficient iteration over entities with specific component combinations,
/// as all data is stored contiguously in memory.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
/// <item><description>Cache-friendly storage with components in contiguous arrays</description></item>
/// <item><description>O(1) entity addition and O(1) entity removal via swap-and-pop</description></item>
/// <item><description>Type-safe component access through generic methods</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var archetype = new Archetype(new[] { typeof(Position), typeof(Velocity) });
/// 
/// var entity = new Entity(1, 0);
/// var components = new Dictionary&lt;Type, object&gt;
/// {
///     { typeof(Position), new Position { X = 0, Y = 0, Z = 0 } },
///     { typeof(Velocity), new Velocity { X = 1, Y = 0, Z = 0 } }
/// };
/// 
/// archetype.AddEntity(entity, components);
/// 
/// var position = archetype.GetComponent&lt;Position&gt;(entity);
/// </code>
/// </example>
public sealed class Archetype
{
    /// <summary>
    /// The default initial capacity for entity storage.
    /// </summary>
    private const int DefaultInitialCapacity = 16;

    /// <summary>
    /// The set of component types this archetype manages.
    /// </summary>
    private readonly HashSet<Type> _componentTypes;

    /// <summary>
    /// The list of entities in this archetype.
    /// </summary>
    private readonly List<Entity.Entity> _entities;

    /// <summary>
    /// Maps component types to their respective stores.
    /// </summary>
    private readonly Dictionary<Type, object> _componentStores;

    /// <summary>
    /// Maps entities to their indices in the entity list and component stores.
    /// </summary>
    private readonly Dictionary<Entity.Entity, int> _entityToIndex;

    /// <summary>
    /// The archetype key representing this archetype's component signature.
    /// </summary>
    private readonly ArchetypeKey _key;

    /// <summary>
    /// Initializes a new instance of the <see cref="Archetype"/> class.
    /// </summary>
    /// <param name="types">The component types for this archetype.</param>
    public Archetype(IEnumerable<Type> types)
    {
        _componentTypes = new HashSet<Type>(types);
        _entities = new List<Entity.Entity>(DefaultInitialCapacity);
        _componentStores = new Dictionary<Type, object>();
        _entityToIndex = new Dictionary<Entity.Entity, int>();
        _key = new ArchetypeKey(_componentTypes);

        // Create component stores for each type
        foreach (var type in _componentTypes)
        {
            var storeType = typeof(ComponentStore<>).MakeGenericType(type);
            var store = Activator.CreateInstance(storeType, DefaultInitialCapacity)!;
            _componentStores[type] = store;
        }
    }

    /// <summary>
    /// Gets the component types managed by this archetype.
    /// </summary>
    public IReadOnlySet<Type> ComponentTypes => _componentTypes;

    /// <summary>
    /// Gets the entities in this archetype.
    /// </summary>
    public IReadOnlyList<Entity.Entity> Entities => _entities;

    /// <summary>
    /// Gets the number of entities in this archetype.
    /// </summary>
    public int EntityCount => _entities.Count;

    /// <summary>
    /// Gets the archetype key for this archetype.
    /// </summary>
    public ArchetypeKey Key => _key;

    /// <summary>
    /// Determines whether this archetype contains the specified entity.
    /// </summary>
    /// <param name="entity">The entity to check for.</param>
    /// <returns><c>true</c> if the archetype contains the entity; otherwise, <c>false</c>.</returns>
    public bool ContainsEntity(Entity.Entity entity)
    {
        return _entityToIndex.ContainsKey(entity);
    }

    /// <summary>
    /// Determines whether this archetype has the specified component type.
    /// </summary>
    /// <typeparam name="T">The component type to check for.</typeparam>
    /// <returns><c>true</c> if the archetype has the component type; otherwise, <c>false</c>.</returns>
    public bool HasComponentType<T>() where T : struct
    {
        return _componentTypes.Contains(typeof(T));
    }

    /// <summary>
    /// Determines whether this archetype has the specified component type.
    /// </summary>
    /// <param name="type">The component type to check for.</param>
    /// <returns><c>true</c> if the archetype has the component type; otherwise, <c>false</c>.</returns>
    public bool HasComponentType(Type type)
    {
        return _componentTypes.Contains(type);
    }

    /// <summary>
    /// Adds an entity with its components to this archetype.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="components">A dictionary mapping component types to component values.</param>
    /// <exception cref="ArgumentException">Thrown when the entity already exists or components don't match.</exception>
    public void AddEntity(Entity.Entity entity, Dictionary<Type, object> components)
    {
        if (_entityToIndex.ContainsKey(entity))
        {
            throw new ArgumentException($"Entity {entity} already exists in this archetype.", nameof(entity));
        }

        int index = _entities.Count;
        _entities.Add(entity);
        _entityToIndex[entity] = index;

        foreach (var type in _componentTypes)
        {
            if (!components.TryGetValue(type, out var component))
            {
                throw new ArgumentException($"Missing component of type {type.Name} for entity {entity}.", nameof(components));
            }

            AddComponentToStore(type, component);
        }
    }

    /// <summary>
    /// Removes an entity from this archetype using swap-and-pop.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    /// <returns>A dictionary containing the removed entity's components.</returns>
    /// <exception cref="ArgumentException">Thrown when the entity doesn't exist in this archetype.</exception>
    public Dictionary<Type, object> RemoveEntity(Entity.Entity entity)
    {
        if (!_entityToIndex.TryGetValue(entity, out int index))
        {
            throw new ArgumentException($"Entity {entity} does not exist in this archetype.", nameof(entity));
        }

        var removedComponents = new Dictionary<Type, object>();
        int lastIndex = _entities.Count - 1;

        // Get the components being removed
        foreach (var type in _componentTypes)
        {
            removedComponents[type] = GetComponentFromStore(type, index);
        }

        // If not the last element, swap with the last
        if (index != lastIndex)
        {
            var lastEntity = _entities[lastIndex];
            _entities[index] = lastEntity;
            _entityToIndex[lastEntity] = index;

            // Swap components in all stores
            foreach (var type in _componentTypes)
            {
                SwapPopComponentInStore(type, index);
            }
        }
        else
        {
            // Just pop from all stores
            foreach (var type in _componentTypes)
            {
                PopComponentFromStore(type);
            }
        }

        _entities.RemoveAt(lastIndex);
        _entityToIndex.Remove(entity);

        return removedComponents;
    }

    /// <summary>
    /// Gets a component for the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>The component value, or null if the entity doesn't have this component type.</returns>
    public T? GetComponent<T>(Entity.Entity entity) where T : struct
    {
        if (!_componentTypes.Contains(typeof(T)))
        {
            return null;
        }

        if (!_entityToIndex.TryGetValue(entity, out int index))
        {
            return null;
        }

        var store = GetComponentStore<T>();
        return store?.Get(index);
    }

    /// <summary>
    /// Sets a component for the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="component">The component value.</param>
    /// <exception cref="InvalidOperationException">Thrown when the archetype doesn't have this component type.</exception>
    /// <exception cref="ArgumentException">Thrown when the entity doesn't exist in this archetype.</exception>
    public void SetComponent<T>(Entity.Entity entity, T component) where T : struct
    {
        if (!_componentTypes.Contains(typeof(T)))
        {
            throw new InvalidOperationException($"Archetype does not have component type {typeof(T).Name}.");
        }

        if (!_entityToIndex.TryGetValue(entity, out int index))
        {
            throw new ArgumentException($"Entity {entity} does not exist in this archetype.", nameof(entity));
        }

        var store = GetComponentStore<T>()!;
        store.Set(index, component);
    }

    /// <summary>
    /// Gets the component store for the specified component type.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>The component store, or null if the type is not in this archetype.</returns>
    public ComponentStore<T>? GetComponentStore<T>() where T : struct
    {
        if (_componentStores.TryGetValue(typeof(T), out var store))
        {
            return (ComponentStore<T>)store;
        }
        return null;
    }

    /// <summary>
    /// Gets the component store for the specified component type as an untyped object.
    /// </summary>
    /// <param name="type">The component type.</param>
    /// <returns>The component store as an object, or null if the type is not in this archetype.</returns>
    public object? GetComponentStoreUntyped(Type type)
    {
        return _componentStores.TryGetValue(type, out var store) ? store : null;
    }

    /// <summary>
    /// Adds a component to its store using reflection.
    /// </summary>
    private void AddComponentToStore(Type type, object component)
    {
        var store = _componentStores[type];
        var addMethod = store.GetType().GetMethod("Add")!;
        addMethod.Invoke(store, new[] { component });
    }

    /// <summary>
    /// Gets a component from its store using reflection.
    /// </summary>
    private object GetComponentFromStore(Type type, int index)
    {
        var store = _componentStores[type];
        var getMethod = store.GetType().GetMethod("Get")!;
        return getMethod.Invoke(store, new object[] { index })!;
    }

    /// <summary>
    /// Performs swap-and-pop removal in a component store using reflection.
    /// </summary>
    private void SwapPopComponentInStore(Type type, int index)
    {
        var store = _componentStores[type];
        var removeMethod = store.GetType().GetMethod("RemoveSwapPop")!;
        removeMethod.Invoke(store, new object[] { index });
    }

    /// <summary>
    /// Pops the last component from a store using reflection.
    /// </summary>
    private void PopComponentFromStore(Type type)
    {
        var store = _componentStores[type];
        var sizeProperty = store.GetType().GetProperty("Size")!;
        int size = (int)sizeProperty.GetValue(store)!;
        if (size > 0)
        {
            var removeMethod = store.GetType().GetMethod("RemoveSwapPop")!;
            removeMethod.Invoke(store, new object[] { size - 1 });
        }
    }
}
