using System.Collections.Generic;

namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Area
{
    // Class for worldTriggerAreaNode
    public class TriggerArea : Area
    {
        public object Trigger;
        public string TriggerType;
        public List<string> Channels;
        public object PreventionActivationTable;
        public object PreventionNotifierTable;
        public TriggerArea() : base()
        {
            DataType = "TriggerArea";
            ModulePath = "area/triggerArea";
            Node = "worldTriggerAreaNode";
            Trigger = null;
            TriggerType = string.Empty;
            Channels = new List<string>();
            PreventionActivationTable = null;
            PreventionNotifierTable = null;
        }
    }
}
