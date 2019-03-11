public class SphereSpawnedIndexDataProxy : UnityEngine.MonoBehaviour, IConvertGameObjectToEntity
{
    public int gameobjectSpawnIndex;

    // Lets you convert the editor data representation to the entity optimal runtime representation
    public void Convert(Unity.Entities.Entity entity, Unity.Entities.EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var indexData = new SphereSpawnedIndexData
        {
            entitySpawnedIndex = gameobjectSpawnIndex,
        };
        dstManager.AddComponentData(entity, indexData);
    }
}
