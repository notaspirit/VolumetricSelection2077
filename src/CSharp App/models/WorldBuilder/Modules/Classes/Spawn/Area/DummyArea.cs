namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Area
{
    // Dummy area, useful for getting outline
    public class DummyArea : Area
    {
        public DummyArea() : base()
        {
            DataType = "DummyArea";
            ModulePath = "area/dummyArea";
            Node = "---";
            Description = "Spawns a dummy area, which can be used for getting an outline for a gameStaticAreaShapeComponent.";
            PreviewNote = "Does not do anything or get exported.";
            Icon = "SelectionOff";
            NoExport = true;
        }
    }
}
