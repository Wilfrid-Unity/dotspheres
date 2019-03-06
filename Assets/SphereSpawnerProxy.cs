public class SphereSpawnerProxy : UnityEngine.MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public UnityEngine.GameObject spherePrefabGameObject;

    // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
    public void DeclareReferencedPrefabs(System.Collections.Generic.List<UnityEngine.GameObject> gameObjects)
    {
        gameObjects.Add(spherePrefabGameObject);
    }

    // Lets you convert the editor data representation to the entity optimal runtime representation

    public void Convert(Unity.Entities.Entity entity, Unity.Entities.EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var spawnerData = new SphereSpawnerData
        {
            // The referenced prefab will be converted due to DeclareReferencedPrefabs.
            // So here we simply map the game object to an entity reference to that prefab.
            spherePrefabEntity = conversionSystem.GetPrimaryEntity(spherePrefabGameObject),
        };
        dstManager.AddComponentData(entity, spawnerData);
    }
}
