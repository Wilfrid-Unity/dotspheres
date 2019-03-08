public class SphereRadiusDataProxy : UnityEngine.MonoBehaviour, IConvertGameObjectToEntity
{
    public float gameobjectRadius;

    // Lets you convert the editor data representation to the entity optimal runtime representation
    public void Convert(Unity.Entities.Entity entity, Unity.Entities.EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var radiusData = new SphereRadiusData
        {
            entityRadius = gameobjectRadius,
        };
        dstManager.AddComponentData(entity, radiusData);
    }
}
