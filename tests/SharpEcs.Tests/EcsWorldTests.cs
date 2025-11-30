using SharpEcs.Component;
using SharpEcs.World;
using Xunit;

namespace SharpEcs.Tests;

/// <summary>
/// Unit tests for <see cref="EcsWorld"/>.
/// </summary>
public class EcsWorldTests
{
    #region Entity Creation Tests

    [Fact]
    public void CreateEntity_NoComponents_CreatesEntity()
    {
        var world = new EcsWorld();
        
        var entity = world.CreateEntity();
        
        Assert.Equal(0, entity.Id);
        Assert.Equal(0, entity.Generation);
        Assert.Equal(1, world.EntityCount);
    }

    [Fact]
    public void CreateEntity_WithComponents_CreatesEntityWithComponents()
    {
        var world = new EcsWorld();
        
        var entity = world.CreateEntity(
            new Position { X = 10, Y = 20, Z = 30 },
            new Velocity { X = 1, Y = 2, Z = 3 }
        );
        
        Assert.Equal(1, world.EntityCount);
        Assert.True(world.HasComponent<Position>(entity));
        Assert.True(world.HasComponent<Velocity>(entity));
        Assert.False(world.HasComponent<Health>(entity));
    }

    [Fact]
    public void CreateEntity_MultipleEntities_AssignsSequentialIds()
    {
        var world = new EcsWorld();
        
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();
        
        Assert.Equal(0, entity1.Id);
        Assert.Equal(1, entity2.Id);
        Assert.Equal(2, entity3.Id);
        Assert.Equal(3, world.EntityCount);
    }

    [Fact]
    public void CreateEntity_DuplicateComponentTypes_ThrowsArgumentException()
    {
        var world = new EcsWorld();
        
        Assert.Throws<ArgumentException>(() => world.CreateEntity(
            new Position { X = 1, Y = 2, Z = 3 },
            new Position { X = 4, Y = 5, Z = 6 }
        ));
    }

    #endregion

    #region Entity Destruction Tests

    [Fact]
    public void DestroyEntity_ValidEntity_RemovesEntity()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity(new Position());
        
        world.DestroyEntity(entity);
        
        Assert.Equal(0, world.EntityCount);
    }

    [Fact]
    public void DestroyEntity_InvalidEntity_ThrowsArgumentException()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity();
        world.DestroyEntity(entity);
        
        // Entity is now stale
        Assert.Throws<ArgumentException>(() => world.DestroyEntity(entity));
    }

    [Fact]
    public void DestroyEntity_RecyclesId_IncrementsGeneration()
    {
        var world = new EcsWorld();
        var entity1 = world.CreateEntity();
        
        Assert.Equal(0, entity1.Id);
        Assert.Equal(0, entity1.Generation);
        
        world.DestroyEntity(entity1);
        var entity2 = world.CreateEntity();
        
        Assert.Equal(0, entity2.Id); // Recycled ID
        Assert.Equal(1, entity2.Generation); // Incremented generation
    }

    [Fact]
    public void IsEntityValid_ValidEntity_ReturnsTrue()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity();
        
        Assert.True(world.IsEntityValid(entity));
    }

    [Fact]
    public void IsEntityValid_DestroyedEntity_ReturnsFalse()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity();
        world.DestroyEntity(entity);
        
        Assert.False(world.IsEntityValid(entity));
    }

    [Fact]
    public void IsEntityValid_StaleEntity_ReturnsFalse()
    {
        var world = new EcsWorld();
        var entity1 = world.CreateEntity();
        world.DestroyEntity(entity1);
        var entity2 = world.CreateEntity(); // Reuses ID with new generation
        
        Assert.False(world.IsEntityValid(entity1)); // Stale reference
        Assert.True(world.IsEntityValid(entity2)); // New entity is valid
    }

    #endregion

    #region Component Access Tests

    [Fact]
    public void GetComponent_ExistingComponent_ReturnsComponent()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity(new Position { X = 100, Y = 200, Z = 300 });
        
        var pos = world.GetComponent<Position>(entity);
        
        Assert.NotNull(pos);
        Assert.Equal(100, pos.Value.X);
        Assert.Equal(200, pos.Value.Y);
        Assert.Equal(300, pos.Value.Z);
    }

    [Fact]
    public void GetComponent_NonexistentComponent_ReturnsNull()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity(new Position());
        
        var health = world.GetComponent<Health>(entity);
        
        Assert.Null(health);
    }

    [Fact]
    public void GetComponent_InvalidEntity_ThrowsArgumentException()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity();
        world.DestroyEntity(entity);
        
        Assert.Throws<ArgumentException>(() => world.GetComponent<Position>(entity));
    }

    [Fact]
    public void HasComponent_ExistingComponent_ReturnsTrue()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity(new Position());
        
        Assert.True(world.HasComponent<Position>(entity));
    }

    [Fact]
    public void HasComponent_NonexistentComponent_ReturnsFalse()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity(new Position());
        
        Assert.False(world.HasComponent<Health>(entity));
    }

    [Fact]
    public void SetComponent_ExistingComponent_UpdatesComponent()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity(new Position { X = 0, Y = 0, Z = 0 });
        
        world.SetComponent(entity, new Position { X = 999, Y = 888, Z = 777 });
        
        var pos = world.GetComponent<Position>(entity);
        Assert.NotNull(pos);
        Assert.Equal(999, pos.Value.X);
    }

    [Fact]
    public void SetComponent_NonexistentComponent_ThrowsInvalidOperationException()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity(new Position());
        
        Assert.Throws<InvalidOperationException>(() => 
            world.SetComponent(entity, new Health { Current = 100, Maximum = 100 }));
    }

    #endregion

    #region Component Add/Remove Tests

    [Fact]
    public void AddComponent_NewComponent_AddsToEntity()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity(new Position());
        
        Assert.False(world.HasComponent<Velocity>(entity));
        
        world.AddComponent(entity, new Velocity { X = 1, Y = 2, Z = 3 });
        
        Assert.True(world.HasComponent<Velocity>(entity));
        var vel = world.GetComponent<Velocity>(entity);
        Assert.NotNull(vel);
        Assert.Equal(1, vel.Value.X);
    }

    [Fact]
    public void AddComponent_ExistingComponent_ThrowsArgumentException()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity(new Position());
        
        Assert.Throws<ArgumentException>(() => 
            world.AddComponent(entity, new Position { X = 100 }));
    }

    [Fact]
    public void AddComponent_MigratesEntityToNewArchetype()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity(new Position { X = 10, Y = 20, Z = 30 });
        
        int initialArchetypeCount = world.ArchetypeCount;
        
        world.AddComponent(entity, new Velocity { X = 1, Y = 2, Z = 3 });
        
        Assert.Equal(initialArchetypeCount + 1, world.ArchetypeCount);
        
        // Original component should still be accessible
        var pos = world.GetComponent<Position>(entity);
        Assert.NotNull(pos);
        Assert.Equal(10, pos.Value.X);
    }

    [Fact]
    public void RemoveComponent_ExistingComponent_RemovesFromEntity()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity(new Position(), new Velocity());
        
        Assert.True(world.HasComponent<Velocity>(entity));
        
        world.RemoveComponent<Velocity>(entity);
        
        Assert.False(world.HasComponent<Velocity>(entity));
        Assert.True(world.HasComponent<Position>(entity)); // Other components preserved
    }

    [Fact]
    public void RemoveComponent_NonexistentComponent_ThrowsArgumentException()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity(new Position());
        
        Assert.Throws<ArgumentException>(() => world.RemoveComponent<Health>(entity));
    }

    [Fact]
    public void RemoveComponent_MigratesEntityToNewArchetype()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity(
            new Position { X = 10 },
            new Velocity { X = 20 },
            new Health { Current = 100, Maximum = 100 }
        );
        
        world.RemoveComponent<Health>(entity);
        
        Assert.True(world.HasComponent<Position>(entity));
        Assert.True(world.HasComponent<Velocity>(entity));
        Assert.False(world.HasComponent<Health>(entity));
        
        // Check data is preserved
        var pos = world.GetComponent<Position>(entity);
        Assert.NotNull(pos);
        Assert.Equal(10, pos.Value.X);
    }

    #endregion

    #region Query Tests

    [Fact]
    public void Query_SingleType_ReturnsMatchingEntities()
    {
        var world = new EcsWorld();
        var entity1 = world.CreateEntity(new Position());
        var entity2 = world.CreateEntity(new Position(), new Velocity());
        var entity3 = world.CreateEntity(new Velocity());
        
        var result = world.Query(typeof(Position));
        
        Assert.Equal(2, result.Count);
        Assert.Contains(entity1, result);
        Assert.Contains(entity2, result);
        Assert.DoesNotContain(entity3, result);
    }

    [Fact]
    public void Query_MultipleTypes_ReturnsMatchingEntities()
    {
        var world = new EcsWorld();
        var entity1 = world.CreateEntity(new Position());
        var entity2 = world.CreateEntity(new Position(), new Velocity());
        var entity3 = world.CreateEntity(new Position(), new Velocity(), new Health());
        
        var result = world.Query(typeof(Position), typeof(Velocity));
        
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(entity1, result);
        Assert.Contains(entity2, result);
        Assert.Contains(entity3, result);
    }

    [Fact]
    public void Query_NoMatches_ReturnsEmptyList()
    {
        var world = new EcsWorld();
        world.CreateEntity(new Position());
        world.CreateEntity(new Velocity());
        
        var result = world.Query(typeof(Health));
        
        Assert.Empty(result);
    }

    [Fact]
    public void ForEach_ExecutesActionForMatchingEntities()
    {
        var world = new EcsWorld();
        var entity1 = world.CreateEntity(new Position { X = 1 });
        var entity2 = world.CreateEntity(new Position { X = 2 });
        var entity3 = world.CreateEntity(new Velocity()); // No position
        
        var processedEntities = new List<Entity.Entity>();
        
        world.ForEach(entity => processedEntities.Add(entity), typeof(Position));
        
        Assert.Equal(2, processedEntities.Count);
        Assert.Contains(entity1, processedEntities);
        Assert.Contains(entity2, processedEntities);
        Assert.DoesNotContain(entity3, processedEntities);
    }

    #endregion

    #region Archetype Tests

    [Fact]
    public void ArchetypeCount_ReflectsUniqueComponentCombinations()
    {
        var world = new EcsWorld();
        
        Assert.Equal(0, world.ArchetypeCount);
        
        world.CreateEntity(new Position());
        Assert.Equal(1, world.ArchetypeCount);
        
        world.CreateEntity(new Position()); // Same archetype
        Assert.Equal(1, world.ArchetypeCount);
        
        world.CreateEntity(new Velocity());
        Assert.Equal(2, world.ArchetypeCount);
        
        world.CreateEntity(new Position(), new Velocity());
        Assert.Equal(3, world.ArchetypeCount);
    }

    [Fact]
    public void GetArchetype_ValidEntity_ReturnsArchetype()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity(new Position(), new Velocity());
        
        var archetype = world.GetArchetype(entity);
        
        Assert.NotNull(archetype);
        Assert.True(archetype.HasComponentType<Position>());
        Assert.True(archetype.HasComponentType<Velocity>());
    }

    [Fact]
    public void GetArchetype_InvalidEntity_ReturnsNull()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity();
        world.DestroyEntity(entity);
        
        var archetype = world.GetArchetype(entity);
        
        Assert.Null(archetype);
    }

    [Fact]
    public void GetArchetypes_ReturnsAllArchetypes()
    {
        var world = new EcsWorld();
        world.CreateEntity(new Position());
        world.CreateEntity(new Velocity());
        world.CreateEntity(new Position(), new Velocity());
        
        var archetypes = world.GetArchetypes().ToList();
        
        Assert.Equal(3, archetypes.Count);
    }

    #endregion

    #region Entity Struct Tests

    [Fact]
    public void Entity_Equality_WorksCorrectly()
    {
        var entity1 = new Entity.Entity(1, 0);
        var entity2 = new Entity.Entity(1, 0);
        var entity3 = new Entity.Entity(1, 1);
        var entity4 = new Entity.Entity(2, 0);
        
        Assert.Equal(entity1, entity2);
        Assert.NotEqual(entity1, entity3);
        Assert.NotEqual(entity1, entity4);
        Assert.True(entity1 == entity2);
        Assert.True(entity1 != entity3);
    }

    [Fact]
    public void Entity_HashCode_ConsistentWithEquality()
    {
        var entity1 = new Entity.Entity(1, 0);
        var entity2 = new Entity.Entity(1, 0);
        var entity3 = new Entity.Entity(1, 1);
        
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
        Assert.NotEqual(entity1.GetHashCode(), entity3.GetHashCode());
    }

    [Fact]
    public void Entity_ToString_ReturnsReadableFormat()
    {
        var entity = new Entity.Entity(42, 3);
        var str = entity.ToString();
        
        Assert.Contains("42", str);
        Assert.Contains("3", str);
    }

    [Fact]
    public void Entity_WorksAsDictionaryKey()
    {
        var dict = new Dictionary<Entity.Entity, string>();
        var entity1 = new Entity.Entity(1, 0);
        var entity2 = new Entity.Entity(1, 0);
        
        dict[entity1] = "test";
        
        Assert.True(dict.ContainsKey(entity2));
        Assert.Equal("test", dict[entity2]);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void MovementSystem_UpdatesPositionsCorrectly()
    {
        var world = new EcsWorld();
        var entity1 = world.CreateEntity(
            new Position { X = 0, Y = 0, Z = 0 },
            new Velocity { X = 1, Y = 2, Z = 3 }
        );
        var entity2 = world.CreateEntity(
            new Position { X = 10, Y = 10, Z = 10 },
            new Velocity { X = -1, Y = -1, Z = -1 }
        );
        
        // Simulate movement system
        foreach (var entity in world.Query(typeof(Position), typeof(Velocity)))
        {
            var pos = world.GetComponent<Position>(entity)!.Value;
            var vel = world.GetComponent<Velocity>(entity)!.Value;
            world.SetComponent(entity, new Position 
            { 
                X = pos.X + vel.X, 
                Y = pos.Y + vel.Y, 
                Z = pos.Z + vel.Z 
            });
        }
        
        var pos1 = world.GetComponent<Position>(entity1)!.Value;
        Assert.Equal(1, pos1.X);
        Assert.Equal(2, pos1.Y);
        Assert.Equal(3, pos1.Z);
        
        var pos2 = world.GetComponent<Position>(entity2)!.Value;
        Assert.Equal(9, pos2.X);
        Assert.Equal(9, pos2.Y);
        Assert.Equal(9, pos2.Z);
    }

    [Fact]
    public void DamageSystem_UpdatesHealthCorrectly()
    {
        var world = new EcsWorld();
        var entity = world.CreateEntity(
            new Health { Current = 100, Maximum = 100 }
        );
        
        // Simulate damage
        var health = world.GetComponent<Health>(entity)!.Value;
        int damage = 25;
        world.SetComponent(entity, new Health 
        { 
            Current = health.Current - damage, 
            Maximum = health.Maximum 
        });
        
        var newHealth = world.GetComponent<Health>(entity)!.Value;
        Assert.Equal(75, newHealth.Current);
        Assert.Equal(100, newHealth.Maximum);
        Assert.True(newHealth.IsAlive);
        Assert.False(newHealth.IsFullHealth);
    }

    [Fact]
    public void ComplexScenario_CreateQueryDestroyRecycle()
    {
        var world = new EcsWorld();
        
        // Create many entities
        var entities = new List<Entity.Entity>();
        for (int i = 0; i < 100; i++)
        {
            entities.Add(world.CreateEntity(
                new Position { X = i, Y = i, Z = i },
                new Velocity { X = 1, Y = 1, Z = 1 }
            ));
        }
        
        Assert.Equal(100, world.EntityCount);
        
        // Query and process
        var movable = world.Query(typeof(Position), typeof(Velocity));
        Assert.Equal(100, movable.Count);
        
        // Destroy half
        for (int i = 0; i < 50; i++)
        {
            world.DestroyEntity(entities[i]);
        }
        
        Assert.Equal(50, world.EntityCount);
        
        // Create new entities (should recycle IDs)
        var newEntities = new List<Entity.Entity>();
        for (int i = 0; i < 25; i++)
        {
            newEntities.Add(world.CreateEntity(new Position()));
        }
        
        Assert.Equal(75, world.EntityCount);
        
        // Verify new entities have recycled IDs with incremented generations
        foreach (var entity in newEntities)
        {
            Assert.Equal(1, entity.Generation); // Recycled, so generation is 1
        }
    }

    #endregion
}
