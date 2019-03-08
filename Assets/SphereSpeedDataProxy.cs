public class SphereSpeedDataProxy : UnityEngine.MonoBehaviour, IConvertGameObjectToEntity
{
    public Unity.Mathematics.float3 gameobjectSpeed;

    // Lets you convert the editor data representation to the entity optimal runtime representation
    public void Convert(Unity.Entities.Entity entity, Unity.Entities.EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var speedData = new SphereSpeedData
        {
            entitySpeed = gameobjectSpeed,
        };
        dstManager.AddComponentData(entity, speedData);
    }
}
