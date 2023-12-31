﻿using Unity.Burst;
using Unity.Entities;

namespace Script.DOTS
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SpawnEnemySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<DataProperties>();
            state.RequireForUpdate<EnemySpawnTimer>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new SpawnEnemyJob()
            {
                DeltaTime = deltaTime,
                ECB = ecbSingleton,
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct SpawnEnemyJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;

        [BurstCompile]
        private void Execute(DataAspect dataAspect, [EntityIndexInQuery] int sortKey)
        {
            dataAspect.EnemySpawnTimer -= DeltaTime;
            if (!dataAspect.TimeToSpawnWave) return;
            
            for (int i = 0; i < dataAspect.NumberToSpawn; i++)
            {
                var newEnemy = ECB.Instantiate(sortKey, dataAspect.EnemyPrefab);
                ECB.SetComponent(sortKey, newEnemy, dataAspect.GetRandomEnemyTransform());
            }

            dataAspect.EnemySpawnTimer = dataAspect.SpawnRate;
        }
    }
}