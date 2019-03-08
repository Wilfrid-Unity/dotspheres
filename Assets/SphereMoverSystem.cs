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

    // this jobs does nothing except to allow EntityDebugger to display the entities found in the scene that have a Translation component
    struct VisualizeSphereJob : Unity.Entities.IJobProcessComponentData<Unity.Transforms.Translation>
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

        VisualizeSphereJob visualizeSphereJob = new VisualizeSphereJob();
        Unity.Jobs.JobHandle visualizeSphereJobHandle = Unity.Entities.JobProcessComponentDataExtensions.Schedule(visualizeSphereJob, this, positionUpdateJobHandle);

        return visualizeSphereJobHandle;
    }
}
