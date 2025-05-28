namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Entity
{
    // Class for AMM imported props
    public class AmmEntity : Entity
    {
        public AmmEntity() : base()
        {
            DataType = "Entity Template (AMM)";
            SpawnData = "data/spawnables/entity/amm/";
            SpawnListType = "files";
            Node = "worldEntityNode";
            Description = "Same as entity, but allows for the AMM list of named props (Including custom ones) to be used";
            ModulePath = "entity/ammEntity";
        }
    }
}
