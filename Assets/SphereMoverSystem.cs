[Unity.Entities.UpdateInGroup(typeof(Unity.Transforms.TransformSystemGroup))]
public class SphereMoverSystem : Unity.Entities.JobComponentSystem
{
    struct PositionUpdateJob : Unity.Entities.IJobProcessComponentData<Unity.Transforms.Translation, SphereSpeedData>
    {
        public void Execute(ref Unity.Transforms.Translation spherePosition, [Unity.Collections.ReadOnly] ref SphereSpeedData sphereSpeedData)
        {
            spherePosition.Value += sphereSpeedData.entitySpeed;
        }
    }

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

    // this jobs does nothing except to allow EntityDebugger to display the entities found in the scene that have a Translation component
    struct DebugVisualizeSphereJob : Unity.Entities.IJobProcessComponentData<Unity.Transforms.Translation>
    {
        public void Execute([Unity.Collections.ReadOnly] ref Unity.Transforms.Translation spherePosition)
        {
        }
    }

    // inputDeps: other writers and readers of the components declared as consumed by the system
    protected override Unity.Jobs.JobHandle OnUpdate(Unity.Jobs.JobHandle inputDeps)
    {
        // "position update jobs" -> "visualize jobs" update
        PositionUpdateJob positionUpdateJob = new PositionUpdateJob();
        Unity.Jobs.JobHandle positionUpdateJobHandle = Unity.Entities.JobProcessComponentDataExtensions.Schedule(positionUpdateJob, this, inputDeps);

        //DebugVisualizeSphereJob visualizeSphereJob = new DebugVisualizeSphereJob();
        //Unity.Jobs.JobHandle visualizeSphereJobHandle = Unity.Entities.JobProcessComponentDataExtensions.Schedule(visualizeSphereJob, this, positionUpdateJobHandle);

        BounceOnWallJob bounceOnWallJob = new BounceOnWallJob();
        Unity.Jobs.JobHandle bounceOnWallJobHandle = Unity.Entities.JobProcessComponentDataExtensions.Schedule(bounceOnWallJob, this, positionUpdateJobHandle);

        return bounceOnWallJobHandle;
    }
}
