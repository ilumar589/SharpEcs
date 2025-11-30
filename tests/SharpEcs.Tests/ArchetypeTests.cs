using SharpEcs.Component;
using SharpEcs.World;
using Xunit;

namespace SharpEcs.Tests;

/// <summary>
/// Unit tests for <see cref="Archetype"/> and <see cref="ArchetypeKey"/>.
/// </summary>
public class ArchetypeTests
{
    #region ArchetypeKey Tests

    [Fact]
    public void ArchetypeKey_SameTypes_AreEqual()
    {
        var key1 = new ArchetypeKey(typeof(Position), typeof(Velocity));
        var key2 = new ArchetypeKey(typeof(Velocity), typeof(Position)); // Different order
        
        Assert.Equal(key1, key2);
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    [Fact]
    public void ArchetypeKey_DifferentTypes_AreNotEqual()
    {
        var key1 = new ArchetypeKey(typeof(Position));
        var key2 = new ArchetypeKey(typeof(Velocity));
        
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void ArchetypeKey_Contains_ReturnsCorrectResult()
    {
        var key = new ArchetypeKey(typeof(Position), typeof(Velocity));
        
        Assert.True(key.Contains<Position>());
        Assert.True(key.Contains<Velocity>());
        Assert.False(key.Contains<Health>());
    }

    [Fact]
    public void ArchetypeKey_ContainsAll_ReturnsCorrectResult()
    {
        var key = new ArchetypeKey(typeof(Position), typeof(Velocity), typeof(Health));
        
        Assert.True(key.ContainsAll(new[] { typeof(Position) }));
        Assert.True(key.ContainsAll(new[] { typeof(Position), typeof(Velocity) }));
        Assert.True(key.ContainsAll(new[] { typeof(Position), typeof(Velocity), typeof(Health) }));
        Assert.False(key.ContainsAll(new[] { typeof(Position), typeof(int) }));
    }

    [Fact]
    public void ArchetypeKey_With_AddsType()
    {
        var key1 = new ArchetypeKey(typeof(Position));
        var key2 = key1.With<Velocity>();
        
        Assert.False(key1.Contains<Velocity>());
        Assert.True(key2.Contains<Position>());
        Assert.True(key2.Contains<Velocity>());
        Assert.Equal(2, key2.Count);
    }

    [Fact]
    public void ArchetypeKey_Without_RemovesType()
    {
        var key1 = new ArchetypeKey(typeof(Position), typeof(Velocity));
        var key2 = key1.Without<Velocity>();
        
        Assert.True(key1.Contains<Velocity>());
        Assert.True(key2.Contains<Position>());
        Assert.False(key2.Contains<Velocity>());
        Assert.Equal(1, key2.Count);
    }

    [Fact]
    public void ArchetypeKey_WorksAsDictionaryKey()
    {
        var dict = new Dictionary<ArchetypeKey, string>();
        var key1 = new ArchetypeKey(typeof(Position), typeof(Velocity));
        var key2 = new ArchetypeKey(typeof(Velocity), typeof(Position));
        
        dict[key1] = "test";
        
        Assert.True(dict.ContainsKey(key2));
        Assert.Equal("test", dict[key2]);
    }

    [Fact]
    public void ArchetypeKey_Operators_WorkCorrectly()
    {
        var key1 = new ArchetypeKey(typeof(Position));
        var key2 = new ArchetypeKey(typeof(Position));
        var key3 = new ArchetypeKey(typeof(Velocity));
        
        Assert.True(key1 == key2);
        Assert.False(key1 != key2);
        Assert.False(key1 == key3);
        Assert.True(key1 != key3);
    }

    [Fact]
    public void ArchetypeKey_Null_HandledCorrectly()
    {
        var key = new ArchetypeKey(typeof(Position));
        ArchetypeKey? nullKey = null;
        
        Assert.False(key.Equals(null));
        Assert.False(key == nullKey);
        Assert.True(key != nullKey);
        Assert.True(nullKey == null);
    }

    #endregion

    #region Archetype Tests

    [Fact]
    public void Archetype_AddEntity_AddsSuccessfully()
    {
        var archetype = new Archetype(new[] { typeof(Position), typeof(Velocity) });
        var entity = new Entity.Entity(1, 0);
        var components = new Dictionary<Type, object>
        {
            { typeof(Position), new Position { X = 10, Y = 20, Z = 30 } },
            { typeof(Velocity), new Velocity { X = 1, Y = 2, Z = 3 } }
        };
        
        archetype.AddEntity(entity, components);
        
        Assert.Equal(1, archetype.EntityCount);
        Assert.True(archetype.ContainsEntity(entity));
    }

    [Fact]
    public void Archetype_AddEntity_DuplicateEntity_ThrowsArgumentException()
    {
        var archetype = new Archetype(new[] { typeof(Position) });
        var entity = new Entity.Entity(1, 0);
        var components = new Dictionary<Type, object> { { typeof(Position), new Position() } };
        
        archetype.AddEntity(entity, components);
        
        Assert.Throws<ArgumentException>(() => archetype.AddEntity(entity, components));
    }

    [Fact]
    public void Archetype_AddEntity_MissingComponent_ThrowsArgumentException()
    {
        var archetype = new Archetype(new[] { typeof(Position), typeof(Velocity) });
        var entity = new Entity.Entity(1, 0);
        var components = new Dictionary<Type, object>
        {
            { typeof(Position), new Position() }
            // Missing Velocity
        };
        
        Assert.Throws<ArgumentException>(() => archetype.AddEntity(entity, components));
    }

    [Fact]
    public void Archetype_RemoveEntity_RemovesSuccessfully()
    {
        var archetype = new Archetype(new[] { typeof(Position) });
        var entity = new Entity.Entity(1, 0);
        var components = new Dictionary<Type, object>
        {
            { typeof(Position), new Position { X = 100, Y = 200, Z = 300 } }
        };
        
        archetype.AddEntity(entity, components);
        var removed = archetype.RemoveEntity(entity);
        
        Assert.Equal(0, archetype.EntityCount);
        Assert.False(archetype.ContainsEntity(entity));
        Assert.True(removed.ContainsKey(typeof(Position)));
        var pos = (Position)removed[typeof(Position)];
        Assert.Equal(100, pos.X);
    }

    [Fact]
    public void Archetype_RemoveEntity_NonexistentEntity_ThrowsArgumentException()
    {
        var archetype = new Archetype(new[] { typeof(Position) });
        var entity = new Entity.Entity(1, 0);
        
        Assert.Throws<ArgumentException>(() => archetype.RemoveEntity(entity));
    }

    [Fact]
    public void Archetype_RemoveEntity_SwapPop_MaintainsCorrectIndices()
    {
        var archetype = new Archetype(new[] { typeof(Position) });
        var entity1 = new Entity.Entity(1, 0);
        var entity2 = new Entity.Entity(2, 0);
        var entity3 = new Entity.Entity(3, 0);
        
        archetype.AddEntity(entity1, new Dictionary<Type, object> { { typeof(Position), new Position { X = 1 } } });
        archetype.AddEntity(entity2, new Dictionary<Type, object> { { typeof(Position), new Position { X = 2 } } });
        archetype.AddEntity(entity3, new Dictionary<Type, object> { { typeof(Position), new Position { X = 3 } } });
        
        // Remove the first entity - should swap with last
        archetype.RemoveEntity(entity1);
        
        Assert.Equal(2, archetype.EntityCount);
        Assert.False(archetype.ContainsEntity(entity1));
        Assert.True(archetype.ContainsEntity(entity2));
        Assert.True(archetype.ContainsEntity(entity3));
        
        // Entity3 should still have its correct component
        var pos3 = archetype.GetComponent<Position>(entity3);
        Assert.NotNull(pos3);
        Assert.Equal(3, pos3.Value.X);
    }

    [Fact]
    public void Archetype_GetComponent_ReturnsCorrectComponent()
    {
        var archetype = new Archetype(new[] { typeof(Position), typeof(Velocity) });
        var entity = new Entity.Entity(1, 0);
        var components = new Dictionary<Type, object>
        {
            { typeof(Position), new Position { X = 10, Y = 20, Z = 30 } },
            { typeof(Velocity), new Velocity { X = 1, Y = 2, Z = 3 } }
        };
        
        archetype.AddEntity(entity, components);
        
        var pos = archetype.GetComponent<Position>(entity);
        var vel = archetype.GetComponent<Velocity>(entity);
        
        Assert.NotNull(pos);
        Assert.Equal(10, pos.Value.X);
        Assert.NotNull(vel);
        Assert.Equal(1, vel.Value.X);
    }

    [Fact]
    public void Archetype_GetComponent_NonexistentType_ReturnsNull()
    {
        var archetype = new Archetype(new[] { typeof(Position) });
        var entity = new Entity.Entity(1, 0);
        var components = new Dictionary<Type, object> { { typeof(Position), new Position() } };
        
        archetype.AddEntity(entity, components);
        
        var health = archetype.GetComponent<Health>(entity);
        
        Assert.Null(health);
    }

    [Fact]
    public void Archetype_SetComponent_UpdatesComponent()
    {
        var archetype = new Archetype(new[] { typeof(Position) });
        var entity = new Entity.Entity(1, 0);
        var components = new Dictionary<Type, object> { { typeof(Position), new Position { X = 0 } } };
        
        archetype.AddEntity(entity, components);
        archetype.SetComponent(entity, new Position { X = 999, Y = 888, Z = 777 });
        
        var pos = archetype.GetComponent<Position>(entity);
        Assert.NotNull(pos);
        Assert.Equal(999, pos.Value.X);
        Assert.Equal(888, pos.Value.Y);
        Assert.Equal(777, pos.Value.Z);
    }

    [Fact]
    public void Archetype_SetComponent_NonexistentType_ThrowsInvalidOperationException()
    {
        var archetype = new Archetype(new[] { typeof(Position) });
        var entity = new Entity.Entity(1, 0);
        var components = new Dictionary<Type, object> { { typeof(Position), new Position() } };
        
        archetype.AddEntity(entity, components);
        
        Assert.Throws<InvalidOperationException>(() => archetype.SetComponent(entity, new Health()));
    }

    [Fact]
    public void Archetype_GetComponentStore_ReturnsCorrectStore()
    {
        var archetype = new Archetype(new[] { typeof(Position), typeof(Velocity) });
        
        var posStore = archetype.GetComponentStore<Position>();
        var velStore = archetype.GetComponentStore<Velocity>();
        var healthStore = archetype.GetComponentStore<Health>();
        
        Assert.NotNull(posStore);
        Assert.NotNull(velStore);
        Assert.Null(healthStore);
    }

    [Fact]
    public void Archetype_ComponentTypes_ReturnsCorrectTypes()
    {
        var archetype = new Archetype(new[] { typeof(Position), typeof(Velocity) });
        
        Assert.Contains(typeof(Position), archetype.ComponentTypes);
        Assert.Contains(typeof(Velocity), archetype.ComponentTypes);
        Assert.DoesNotContain(typeof(Health), archetype.ComponentTypes);
    }

    [Fact]
    public void Archetype_HasComponentType_ReturnsCorrectResult()
    {
        var archetype = new Archetype(new[] { typeof(Position) });
        
        Assert.True(archetype.HasComponentType<Position>());
        Assert.False(archetype.HasComponentType<Velocity>());
    }

    [Fact]
    public void Archetype_Entities_ReturnsReadOnlyList()
    {
        var archetype = new Archetype(new[] { typeof(Position) });
        var entity1 = new Entity.Entity(1, 0);
        var entity2 = new Entity.Entity(2, 0);
        
        archetype.AddEntity(entity1, new Dictionary<Type, object> { { typeof(Position), new Position() } });
        archetype.AddEntity(entity2, new Dictionary<Type, object> { { typeof(Position), new Position() } });
        
        var entities = archetype.Entities;
        
        Assert.Equal(2, entities.Count);
        Assert.Contains(entity1, entities);
        Assert.Contains(entity2, entities);
    }

    #endregion
}
