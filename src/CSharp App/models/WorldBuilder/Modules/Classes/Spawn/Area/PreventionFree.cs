namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Area
{
    // Class for worldPreventionFreeAreaNode
    public class PreventionFree : Area
    {
        public PreventionFree() : base()
        {
            DataType = "Prevention Free Area";
            ModulePath = "area/preventionFree";
            Node = "worldPreventionFreeAreaNode";
            Description = "Prevents police from entering the area. Does not clear wanted level.";
            PreviewNote = "Does not work in the editor.";
            Icon = "PoliceBadgeOutline";
        }
    }
}
