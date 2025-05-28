namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Collision
{
    // Class for worldCollisionNode
    public class Collider : Spawnable
    {
        public Collider() : base()
        {
            DataType = "Collider";
            ModulePath = "collision/collider";
            Node = "worldCollisionNode";
        }
    }
}
