using SharpEcs.Entity;

namespace SharpEcs.World;

/// <summary>
/// Main entry point for the ECS framework.
/// Manages entities, components, and archetypes with value type optimization.
/// </summary>
/// <remarks>
/// <para>
/// EcsWorld is the central coordinator for the Entity-Component-System framework.
/// It manages the lifecycle of entities, their component assignments, and provides
/// efficient querying capabilities for systems to process entities.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
/// <item><description>Value type entities (struct) for zero-allocation</description></item>
/// <item><description>Archetype-based storage for cache-friendly iteration</description></item>
/// <item><description>Entity generation tracking for safe entity references</description></item>
/// <item><description>Efficient component addition/removal with automatic archetype migration</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var world = new EcsWorld();
/// 
/// // Create entities with components
/// var entity = world.CreateEntity(
///     new Position { X = 0, Y = 0, Z = 0 },
///     new Velocity { X = 1, Y = 0.5f, Z = 0 }
/// );
/// 
/// // Query and process entities
/// foreach (var e in world.Query(typeof(Position), typeof(Velocity)))
/// {
///     var pos = world.GetComponent&lt;Position&gt;(e)!.Value;
///     var vel = world.GetComponent&lt;Velocity&gt;(e)!.Value;
///     world.SetComponent(e, new Position 
///     { 
///         X = pos.X + vel.X, 
///         Y = pos.Y + vel.Y, 
///         Z = pos.Z + vel.Z 
///     });
/// }
/// 
/// // Destroy entity when done
/// world.DestroyEntity(entity);
/// </code>
/// </example>
public sealed class EcsWorld
{
    /// <summary>
    /// Counter for generating unique entity IDs.
    /// </summary>
    private int _nextEntityId;

    /// <summary>
    /// Maps entity IDs to their current generation.
    /// </summary>
    private readonly Dictionary<int, int> _entityGenerations;

    /// <summary>
    /// Maps entities to their current archetype.
    /// </summary>
    private readonly Dictionary<Entity.Entity, Archetype> _entityToArchetype;

    /// <summary>
    /// Maps archetype keys to archetypes.
    /// </summary>
    private readonly Dictionary<ArchetypeKey, Archetype> _archetypes;

    /// <summary>
    /// Pool of recycled entity IDs.
    /// </summary>
    private readonly Queue<int> _recycledEntityIds;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcsWorld"/> class.
    /// </summary>
    public EcsWorld()
    {
        _nextEntityId = 0;
        _entityGenerations = new Dictionary<int, int>();
        _entityToArchetype = new Dictionary<Entity.Entity, Archetype>();
        _archetypes = new Dictionary<ArchetypeKey, Archetype>();
        _recycledEntityIds = new Queue<int>();
    }

    /// <summary>
    /// Gets the number of active entities in the world.
    /// </summary>
    public int EntityCount => _entityToArchetype.Count;

    /// <summary>
    /// Gets the number of archetypes in the world.
    /// </summary>
    public int ArchetypeCount => _archetypes.Count;

    /// <summary>
    /// Creates a new entity with the specified components.
    /// </summary>
    /// <param name="components">The components to attach to the entity.</param>
    /// <returns>The created entity.</returns>
    /// <exception cref="ArgumentException">Thrown when duplicate component types are provided.</exception>
    /// <example>
    /// <code>
    /// var entity = world.CreateEntity(
    ///     new Position { X = 10, Y = 20, Z = 0 },
    ///     new Velocity { X = 1, Y = 0, Z = 0 },
    ///     new Health { Current = 100, Maximum = 100 }
    /// );
    /// </code>
    /// </example>
    public Entity.Entity CreateEntity(params object[] components)
    {
        // Get or create entity ID
        int id;
        int generation;

        if (_recycledEntityIds.Count > 0)
        {
            id = _recycledEntityIds.Dequeue();
            generation = _entityGenerations[id];
        }
        else
        {
            id = _nextEntityId++;
            generation = 0;
            _entityGenerations[id] = generation;
        }

        var entity = new Entity.Entity(id, generation);

        // Build component type set and component dictionary
        var componentTypes = new HashSet<Type>();
        var componentDict = new Dictionary<Type, object>();

        foreach (var component in components)
        {
            var type = component.GetType();
            if (!componentTypes.Add(type))
            {
                throw new ArgumentException($"Duplicate component type: {type.Name}", nameof(components));
            }
            componentDict[type] = component;
        }

        // Find or create the archetype
        var key = new ArchetypeKey(componentTypes);
        if (!_archetypes.TryGetValue(key, out var archetype))
        {
            archetype = new Archetype(componentTypes);
            _archetypes[key] = archetype;
        }

        // Add entity to archetype
        archetype.AddEntity(entity, componentDict);
        _entityToArchetype[entity] = archetype;

        return entity;
    }

    /// <summary>
    /// Destroys an entity, removing it from the world.
    /// </summary>
    /// <param name="entity">The entity to destroy.</param>
    /// <exception cref="ArgumentException">Thrown when the entity doesn't exist or is stale.</exception>
    public void DestroyEntity(Entity.Entity entity)
    {
        if (!IsEntityValid(entity))
        {
            throw new ArgumentException($"Entity {entity} is not valid or has been destroyed.", nameof(entity));
        }

        // Remove from archetype
        if (_entityToArchetype.TryGetValue(entity, out var archetype))
        {
            archetype.RemoveEntity(entity);
            _entityToArchetype.Remove(entity);
        }

        // Increment generation and recycle ID
        _entityGenerations[entity.Id] = entity.Generation + 1;
        _recycledEntityIds.Enqueue(entity.Id);
    }

    /// <summary>
    /// Determines whether the specified entity is valid in this world.
    /// </summary>
    /// <param name="entity">The entity to validate.</param>
    /// <returns><c>true</c> if the entity is valid; otherwise, <c>false</c>.</returns>
    public bool IsEntityValid(Entity.Entity entity)
    {
        return _entityGenerations.TryGetValue(entity.Id, out int generation) 
            && generation == entity.Generation 
            && _entityToArchetype.ContainsKey(entity);
    }

    /// <summary>
    /// Gets a component for the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>The component value, or null if the entity doesn't have this component.</returns>
    /// <exception cref="ArgumentException">Thrown when the entity is not valid.</exception>
    public T? GetComponent<T>(Entity.Entity entity) where T : struct
    {
        if (!IsEntityValid(entity))
        {
            throw new ArgumentException($"Entity {entity} is not valid.", nameof(entity));
        }

        if (_entityToArchetype.TryGetValue(entity, out var archetype))
        {
            return archetype.GetComponent<T>(entity);
        }

        return null;
    }

    /// <summary>
    /// Determines whether the specified entity has a component of the given type.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns><c>true</c> if the entity has the component; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when the entity is not valid.</exception>
    public bool HasComponent<T>(Entity.Entity entity) where T : struct
    {
        if (!IsEntityValid(entity))
        {
            throw new ArgumentException($"Entity {entity} is not valid.", nameof(entity));
        }

        if (_entityToArchetype.TryGetValue(entity, out var archetype))
        {
            return archetype.HasComponentType<T>();
        }

        return false;
    }

    /// <summary>
    /// Adds a component to the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="component">The component to add.</param>
    /// <exception cref="ArgumentException">Thrown when the entity is not valid or already has this component.</exception>
    public void AddComponent<T>(Entity.Entity entity, T component) where T : struct
    {
        if (!IsEntityValid(entity))
        {
            throw new ArgumentException($"Entity {entity} is not valid.", nameof(entity));
        }

        if (!_entityToArchetype.TryGetValue(entity, out var currentArchetype))
        {
            throw new ArgumentException($"Entity {entity} has no archetype.", nameof(entity));
        }

        if (currentArchetype.HasComponentType<T>())
        {
            throw new ArgumentException($"Entity {entity} already has component type {typeof(T).Name}.", nameof(entity));
        }

        // Get current components
        var currentComponents = currentArchetype.RemoveEntity(entity);
        
        // Add the new component
        currentComponents[typeof(T)] = component;

        // Find or create the new archetype
        var newKey = currentArchetype.Key.With<T>();
        if (!_archetypes.TryGetValue(newKey, out var newArchetype))
        {
            var newTypes = new HashSet<Type>(currentArchetype.ComponentTypes) { typeof(T) };
            newArchetype = new Archetype(newTypes);
            _archetypes[newKey] = newArchetype;
        }

        // Add entity to new archetype
        newArchetype.AddEntity(entity, currentComponents);
        _entityToArchetype[entity] = newArchetype;
    }

    /// <summary>
    /// Removes a component from the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <exception cref="ArgumentException">Thrown when the entity is not valid or doesn't have this component.</exception>
    public void RemoveComponent<T>(Entity.Entity entity) where T : struct
    {
        if (!IsEntityValid(entity))
        {
            throw new ArgumentException($"Entity {entity} is not valid.", nameof(entity));
        }

        if (!_entityToArchetype.TryGetValue(entity, out var currentArchetype))
        {
            throw new ArgumentException($"Entity {entity} has no archetype.", nameof(entity));
        }

        if (!currentArchetype.HasComponentType<T>())
        {
            throw new ArgumentException($"Entity {entity} does not have component type {typeof(T).Name}.", nameof(entity));
        }

        // Get current components
        var currentComponents = currentArchetype.RemoveEntity(entity);
        
        // Remove the component
        currentComponents.Remove(typeof(T));

        // Find or create the new archetype
        var newKey = currentArchetype.Key.Without<T>();
        if (!_archetypes.TryGetValue(newKey, out var newArchetype))
        {
            var newTypes = new HashSet<Type>(currentArchetype.ComponentTypes);
            newTypes.Remove(typeof(T));
            newArchetype = new Archetype(newTypes);
            _archetypes[newKey] = newArchetype;
        }

        // Add entity to new archetype
        newArchetype.AddEntity(entity, currentComponents);
        _entityToArchetype[entity] = newArchetype;
    }

    /// <summary>
    /// Sets a component value for the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="component">The new component value.</param>
    /// <exception cref="ArgumentException">Thrown when the entity is not valid or doesn't have this component.</exception>
    public void SetComponent<T>(Entity.Entity entity, T component) where T : struct
    {
        if (!IsEntityValid(entity))
        {
            throw new ArgumentException($"Entity {entity} is not valid.", nameof(entity));
        }

        if (!_entityToArchetype.TryGetValue(entity, out var archetype))
        {
            throw new ArgumentException($"Entity {entity} has no archetype.", nameof(entity));
        }

        archetype.SetComponent(entity, component);
    }

    /// <summary>
    /// Queries for entities that have all the specified component types.
    /// </summary>
    /// <param name="types">The component types to query for.</param>
    /// <returns>A list of entities matching the query.</returns>
    /// <example>
    /// <code>
    /// var movableEntities = world.Query(typeof(Position), typeof(Velocity));
    /// foreach (var entity in movableEntities)
    /// {
    ///     // Process entity
    /// }
    /// </code>
    /// </example>
    public List<Entity.Entity> Query(params Type[] types)
    {
        var result = new List<Entity.Entity>();
        var requiredTypes = new HashSet<Type>(types);

        foreach (var archetype in _archetypes.Values)
        {
            if (archetype.Key.ContainsAll(requiredTypes))
            {
                result.AddRange(archetype.Entities);
            }
        }

        return result;
    }

    /// <summary>
    /// Executes an action for each entity that has all the specified component types.
    /// </summary>
    /// <param name="action">The action to execute for each matching entity.</param>
    /// <param name="types">The component types to query for.</param>
    /// <example>
    /// <code>
    /// world.ForEach(entity =>
    /// {
    ///     var pos = world.GetComponent&lt;Position&gt;(entity)!.Value;
    ///     Console.WriteLine($"Entity {entity.Id} at {pos}");
    /// }, typeof(Position));
    /// </code>
    /// </example>
    public void ForEach(Action<Entity.Entity> action, params Type[] types)
    {
        var requiredTypes = new HashSet<Type>(types);

        foreach (var archetype in _archetypes.Values)
        {
            if (archetype.Key.ContainsAll(requiredTypes))
            {
                foreach (var entity in archetype.Entities)
                {
                    action(entity);
                }
            }
        }
    }

    /// <summary>
    /// Gets the archetype for the specified entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The archetype, or null if the entity is not valid.</returns>
    public Archetype? GetArchetype(Entity.Entity entity)
    {
        return _entityToArchetype.TryGetValue(entity, out var archetype) ? archetype : null;
    }

    /// <summary>
    /// Gets all archetypes in the world.
    /// </summary>
    /// <returns>A collection of all archetypes.</returns>
    public IEnumerable<Archetype> GetArchetypes()
    {
        return _archetypes.Values;
    }
}
