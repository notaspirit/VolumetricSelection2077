using SharpDX;

namespace VolumetricSelection2077.Models;

public class WheezeKitObject
{

}

public class WheezeKitProbabilitySettings : WheezeKitObject
{
    public float probability { get; set; }

    public WheezeKitProbabilitySettings()
    {
        probability = 0.5f;   
    }
}

public class WheezeKitHeaderStates : WheezeKitObject
{
    public bool transform { get; set; }
    public bool groupedProperties { get; set; }
    
   public WheezeKitHeaderStates()
    {
        transform = true;
        groupedProperties = false;
    }
}

public class WheezeKitGroup : WheezeKitObject
{
    public Vector4 pos { get; set; }
    public bool selected { get; set; }
    public string modulePath { get; set; }
    public WheezeKitProbabilitySettings probabilitySettings { get; set; }
    public bool rotationLocked { get; set; }
    public bool headerOpen { get; set; }
    public WheezeKitObject[] childs { get; set; }
    public bool transformExpanded { get; set; }
    public bool hiddenByParent { get; set; }
    public WheezeKitHeaderStates propertyHeaderStates { get; set; }
    public bool visible { get; set; }
    public bool rotationRelative { get; set; }
    public bool scaleLocked { get; set; }
    public string name { get; set; }
    public bool expandable { get; set; }

    public WheezeKitGroup(string groupName)
    {
        pos = Vector4.Zero;
        selected = false;
        modulePath = "modules/classes/editor/positionableGroup";
        probabilitySettings = new();
        rotationLocked = false;
        headerOpen = true;
        childs = new WheezeKitObject[0];
        transformExpanded = true;
        hiddenByParent = false;
        propertyHeaderStates = new();
        visible = true;
        rotationRelative = false;
        scaleLocked = true;
        name = groupName;
        expandable = true;
    }
}