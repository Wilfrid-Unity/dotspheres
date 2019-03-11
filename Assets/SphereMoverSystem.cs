[Unity.Entities.UpdateInGroup(typeof(Unity.Entities.LateSimulationSystemGroup))]
public class SphereMoverSystem : Unity.Entities.JobComponentSystem
{
    #region inter-sphere collision
    Unity.Collections.NativeList<Collision> collisions;
    Unity.Entities.ComponentGroup SphereDataComponentGroup; // used to gather all sphere entities in a NativeArray
    protected override void OnCreateManager()
    {
        SphereDataComponentGroup = GetComponentGroup(Unity.Entities.ComponentType.ReadWrite<SphereSpeedData>());
        collisions = new Unity.Collections.NativeList<Collision>(1, Unity.Collections.Allocator.TempJob);
    }

    public struct Collision
    {
        public int sphereSpawnedIndex_i;
        public int sphereSpawnedIndex_j;
        public Unity.Mathematics.float3 impactNormal;
    };

    [Unity.Burst.BurstCompile]
    struct DetectCollisionJob : Unity.Jobs.IJob
    {
        public Unity.Collections.NativeArray<Unity.Entities.Entity> AllSpheres;

        // You have to declare these members to be able to access componentData from entity later
        [Unity.Collections.ReadOnly] public Unity.Entities.ComponentDataFromEntity<Unity.Transforms.Translation> SpherePositionData;
        [Unity.Collections.ReadOnly] public Unity.Entities.ComponentDataFromEntity<SphereRadiusData> SphereRadiusComponentData;
        [Unity.Collections.ReadOnly] public Unity.Entities.ComponentDataFromEntity<SphereSpawnedIndexData> SphereSpawnedIndexData;

        public Unity.Collections.NativeList<Collision> Collisions;

        public void Execute()
        {
            // register spheres entering into each other
            for (int i = 0; i < AllSpheres.Length; ++i)
            {
                for (int j = 0; j < i; ++j)
                {
                    Unity.Entities.Entity sphere_i = AllSpheres[i];
                    Unity.Entities.Entity sphere_j = AllSpheres[j];
                    Unity.Transforms.Translation spherePosition_i = SpherePositionData[sphere_i];
                    Unity.Transforms.Translation spherePosition_j = SpherePositionData[sphere_j];
                    SphereRadiusData sphereRadius_i = SphereRadiusComponentData[sphere_i];
                    SphereRadiusData sphereRadius_j = SphereRadiusComponentData[sphere_j];
                    SphereSpawnedIndexData sphereSpawnedIndex_i = SphereSpawnedIndexData[sphere_i];
                    SphereSpawnedIndexData sphereSpawnedIndex_j = SphereSpawnedIndexData[sphere_j];
                    float radiusSum = sphereRadius_i.entityRadius + sphereRadius_j.entityRadius;

                    if (Unity.Mathematics.math.distancesq(spherePosition_i.Value, spherePosition_j.Value) < radiusSum * radiusSum)
                    {
                        Collisions.Add(
                            new Collision
                            {
                                sphereSpawnedIndex_i = sphereSpawnedIndex_i.entitySpawnedIndex,
                                sphereSpawnedIndex_j = sphereSpawnedIndex_j.entitySpawnedIndex,
                                impactNormal = Unity.Mathematics.math.normalize(spherePosition_j.Value - spherePosition_i.Value),
                            });
                    }
                }
            }

#if false
            // TODO find how to efficiently implement this (while expressing the data dependency as the API expects)
            for (int k = 0; k < Collisions.Length; ++k)
            {
                Collision collision = Collisions[k];

                SphereSpeedData sphereSpeed_i = SphereSpeedComponentData[AllSpheres[collision.sphereSpawnedIndex_i]];
                SphereSpeedData sphereSpeed_j = SphereSpeedComponentData[AllSpheres[collision.sphereSpawnedIndex_j]];

                sphereSpeed_i.entitySpeed = Unity.Mathematics.math.reflect(sphereSpeed_i.entitySpeed, collision.impactNormal);
                sphereSpeed_j.entitySpeed = Unity.Mathematics.math.reflect(sphereSpeed_j.entitySpeed, -collision.impactNormal);
            }
#endif
        }
    }

    // TODO find how to efficiently implement this (i.e avoid doing a full loop for every entity, but still express the data dependency as the API expects)
    [Unity.Burst.BurstCompile]
    struct ApplyCollisionJob : Unity.Entities.IJobProcessComponentData<SphereSpawnedIndexData, SphereSpeedData, Unity.Transforms.Translation>
    {
        [Unity.Collections.ReadOnly] /*[Unity.Collections.DeallocateOnJobCompletion]*/ public Unity.Collections.NativeList<Collision> CollisionsDetected;

        public void Execute([Unity.Collections.ReadOnly] ref SphereSpawnedIndexData sphereSpawnedIndex, ref SphereSpeedData sphereSpeedData, ref Unity.Transforms.Translation spherePosition)
        {
            for(int i = 0; i<CollisionsDetected.Length; ++i)
            {
                if(CollisionsDetected[i].sphereSpawnedIndex_i == sphereSpawnedIndex.entitySpawnedIndex)
                {
                    sphereSpeedData.entitySpeed = Unity.Mathematics.math.reflect(sphereSpeedData.entitySpeed, CollisionsDetected[i].impactNormal);
                    spherePosition.Value += 1.5F*sphereSpeedData.entitySpeed;
                }

                if (CollisionsDetected[i].sphereSpawnedIndex_j == sphereSpawnedIndex.entitySpawnedIndex)
                {
                    sphereSpeedData.entitySpeed = Unity.Mathematics.math.reflect(sphereSpeedData.entitySpeed, CollisionsDetected[i].impactNormal);
                    spherePosition.Value += 1.5F*sphereSpeedData.entitySpeed;
                }
            }
        }
    }

#endregion


    [Unity.Burst.BurstCompile]
    struct PositionUpdateJob : Unity.Entities.IJobProcessComponentData<Unity.Transforms.Translation, SphereSpeedData>
    {
        public void Execute(ref Unity.Transforms.Translation spherePosition, [Unity.Collections.ReadOnly] ref SphereSpeedData sphereSpeedData)
        {
            spherePosition.Value += sphereSpeedData.entitySpeed;
        }
    }



#region wall-sphere collision

    const float X_plus = 10F;
    const float X_minus = -10F;
    const float Y_plus = 5F;
    const float Y_minus = -5F;
    const float Z_plus = 10F;
    const float Z_minus = -10F;
    static readonly Unity.Mathematics.float3 X_plus_normal = new Unity.Mathematics.float3(-1F, 0, 0);
    static readonly Unity.Mathematics.float3 X_minus_normal = new Unity.Mathematics.float3(1F, 0, 0);
    static readonly Unity.Mathematics.float3 Y_plus_normal = new Unity.Mathematics.float3(0, -1F, 0);
    static readonly Unity.Mathematics.float3 Y_minus_normal = new Unity.Mathematics.float3(0, 1F, 0);
    static readonly Unity.Mathematics.float3 Z_plus_normal = new Unity.Mathematics.float3(0, 0F, -1F);
    static readonly Unity.Mathematics.float3 Z_minus_normal = new Unity.Mathematics.float3(0, 0, 1F);

    [Unity.Burst.BurstCompile]
    struct BounceOnWallJob : Unity.Entities.IJobProcessComponentData<SphereSpeedData, Unity.Transforms.Translation, SphereRadiusData>
    {
        public void Execute(
            ref SphereSpeedData sphereSpeedData,
            [Unity.Collections.ReadOnly] ref Unity.Transforms.Translation spherePosition, 
            [Unity.Collections.ReadOnly] ref SphereRadiusData sphereRadius)
        {
            // reflect speed when collision detected onto one of the walls
            if (spherePosition.Value.x - sphereRadius.entityRadius <= X_minus) sphereSpeedData.entitySpeed = Unity.Mathematics.math.reflect(sphereSpeedData.entitySpeed, X_minus_normal);
            if (spherePosition.Value.x + sphereRadius.entityRadius >= X_plus) sphereSpeedData.entitySpeed = Unity.Mathematics.math.reflect(sphereSpeedData.entitySpeed, X_plus_normal);
            if (spherePosition.Value.y - sphereRadius.entityRadius <= Y_minus) sphereSpeedData.entitySpeed = Unity.Mathematics.math.reflect(sphereSpeedData.entitySpeed, Y_minus_normal);
            if (spherePosition.Value.y + sphereRadius.entityRadius >= Y_plus) sphereSpeedData.entitySpeed = Unity.Mathematics.math.reflect(sphereSpeedData.entitySpeed, Y_plus_normal);
            if (spherePosition.Value.z - sphereRadius.entityRadius <= Z_minus) sphereSpeedData.entitySpeed = Unity.Mathematics.math.reflect(sphereSpeedData.entitySpeed, Z_minus_normal);
            if (spherePosition.Value.z + sphereRadius.entityRadius >= Z_plus) sphereSpeedData.entitySpeed = Unity.Mathematics.math.reflect(sphereSpeedData.entitySpeed, Z_plus_normal);
        }
    }

#endregion

    // inputDeps: other writers and readers of the components declared as consumed by the system
    protected override Unity.Jobs.JobHandle OnUpdate(Unity.Jobs.JobHandle inputDeps)
    {
        PositionUpdateJob positionUpdateJob = new PositionUpdateJob();
        Unity.Jobs.JobHandle positionUpdateJobHandle = Unity.Entities.JobProcessComponentDataExtensions.Schedule(positionUpdateJob, this, inputDeps);

        collisions.Dispose(); // previous frame simulation results

        collisions = new Unity.Collections.NativeList<Collision>(1, Unity.Collections.Allocator.TempJob);
        DetectCollisionJob detectCollisionJob = new DetectCollisionJob
        {
            AllSpheres = SphereDataComponentGroup.ToEntityArray(Unity.Collections.Allocator.TempJob),
            SpherePositionData = GetComponentDataFromEntity<Unity.Transforms.Translation>(),
            SphereRadiusComponentData = GetComponentDataFromEntity<SphereRadiusData>(),
            SphereSpawnedIndexData = GetComponentDataFromEntity<SphereSpawnedIndexData>(),
            Collisions = collisions,
        };
        Unity.Jobs.JobHandle detectCollisionJobHandle = Unity.Jobs.IJobExtensions.Schedule(detectCollisionJob, positionUpdateJobHandle);

        ApplyCollisionJob applyCollisionJob = new ApplyCollisionJob()
        {
            CollisionsDetected = collisions,
        };
        Unity.Jobs.JobHandle applyCollisionJobHandle = Unity.Entities.JobProcessComponentDataExtensions.Schedule(applyCollisionJob, this, detectCollisionJobHandle);


        BounceOnWallJob bounceOnWallJob = new BounceOnWallJob();
        Unity.Jobs.JobHandle bounceOnWallJobHandle = Unity.Entities.JobProcessComponentDataExtensions.Schedule(bounceOnWallJob, this, applyCollisionJobHandle);

        return bounceOnWallJobHandle;
    }
}
