[Unity.Entities.UpdateInGroup(typeof(Unity.Entities.InitializationSystemGroup))]
public class SphereSpawnerSystem : Unity.Entities.JobComponentSystem
{
    public UnityEngine.GameObject prefab;

    // EndSimulationEntityCommandBufferSystem is used to create a command buffer which will then be played back when the system group finished execution
    Unity.Entities.EndInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    int spheresSpawnedCount = 0;
    int framesSkipped = 0;

    protected override void OnCreateManager()
    {
        // Cache the EndSimulationBarrier in a field, so we don't have to create it every frame
        m_EntityCommandBufferSystem = World.GetOrCreateManager<Unity.Entities.EndInitializationEntityCommandBufferSystem>();
    }

    static readonly public Unity.Mathematics.float3 InitialSphereSpeed = new Unity.Mathematics.float3(0, 0, .1F);

    struct SpawnSphereJob : Unity.Entities.IJobProcessComponentData<SphereSpawnerData>
    {
        public Unity.Entities.EntityCommandBuffer commandBuffer;
        public int sphereSpawnedIndex;

        public void Execute([Unity.Collections.ReadOnly] ref SphereSpawnerData spawnerData)
        {
            var sphereInstance = commandBuffer.Instantiate(spawnerData.spherePrefabEntity);

            // Place the instance in a grid
            float x = sphereSpawnedIndex / 10;
            float z = sphereSpawnedIndex % 10;
            const float spaceBetweenSpheres = 1.2F;
            var position = new Unity.Mathematics.float3(spaceBetweenSpheres * x, 0, spaceBetweenSpheres * z);
            commandBuffer.SetComponent(sphereInstance, new Unity.Transforms.Translation { Value = position });

            // set initial speed
            commandBuffer.SetComponent(sphereInstance, new SphereSpeedData { entitySpeed = InitialSphereSpeed });

        }
    }

    // inputDeps: other writers and readers of the components declared as consumed by the system
    protected override Unity.Jobs.JobHandle OnUpdate(Unity.Jobs.JobHandle inputDeps)
    {
        Unity.Jobs.JobHandle updatedDependencies = inputDeps;

        // "spawn jobs" update
        ++framesSkipped;
        // every 2 frames, schedule a SpawnSphereJob (stop after 100 spheres)
        if (framesSkipped >= 2 && spheresSpawnedCount < 100)
        {
            var spawnSphereJob = new SpawnSphereJob
            {
                commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer(),
                sphereSpawnedIndex = spheresSpawnedCount,
            };

            updatedDependencies = Unity.Entities.JobProcessComponentDataExtensions.ScheduleSingle(spawnSphereJob, this, inputDeps);
            //same as extension method Unity.Jobs.JobHandle jobHandle = job.ScheduleSingle(this, inputDeps);

            // SpawnSphereJob runs in parallel with no sync point until the system group execution finishes.
            // The commands in commandBuffer are then played back (creating the entities and placing them).
            // We need to tell the system group which job it needs to complete before it can play back the commands.
            m_EntityCommandBufferSystem.AddJobHandleForProducer(updatedDependencies);

            ++spheresSpawnedCount;
            framesSkipped = 0;
        }

        return updatedDependencies;
    }
}
