using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Script.DOTS
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SpawnEnemySystem))]
    public partial struct ProjectileSpawningSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerMovementParams>();
            state.RequireForUpdate<DataProperties>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var player = SystemAPI.GetSingletonEntity<PlayerMovementParams>();
            var playerTransform = SystemAPI.GetComponentRO<LocalTransform>(player).ValueRO;
            

            new SpawnProjectileJob()
            {
                DeltaTime = deltaTime,
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                PlayerTransform = playerTransform
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct SpawnProjectileJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            public LocalTransform PlayerTransform;

            [BurstCompile]
            private void Execute(DataAspect dataAspect, [EntityIndexInQuery] int sortKey)
            {
                dataAspect.ProjectileSpawnTimer -= DeltaTime;
                if (!dataAspect.TimeToSpawnBullet)
                {
                    return;
                }

                var newProjectile = ECB.Instantiate(sortKey, dataAspect.ProjectilePrefab);
                ECB.AddComponent(sortKey, newProjectile, new ProjectileTag());
                
                float3 projectileOffset = PlayerTransform.Up();
                float3 spawnPoint = PlayerTransform.Position + projectileOffset;
                ECB.SetComponent(sortKey, newProjectile, new LocalTransform
                {
                    Position = spawnPoint,
                    Rotation = PlayerTransform.Rotation,
                    Scale = 1f
                });
                
                dataAspect.ProjectileSpawnTimer = dataAspect.ProjectileSpawnRate;
            }
        }
        
    }
    public struct ProjectileTag : IComponentData
    {
            
    }
}