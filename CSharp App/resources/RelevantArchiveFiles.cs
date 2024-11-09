using System.Collections.Generic;
namespace VolumetricSelection2077.Resources
{
    public static class RelevantArchiveFiles
    {
        public static readonly List<string> Files = new List<string>
        {
            "basegame_1_engine.archive",
            "basegame_2_mainmenu.archive",  
            "basegame_3_nightcity.archive", 
            "basegame_3_nightcity_terrain.archive", 
            "basegame_4_appearance.archive",    
            "basegame_4_gamedata.archive",  
            "ep1_1_nightcity.archive",
            "ep1_1_nightcity_terrain.archive",
            "ep1_2_gamedata.archive",
        };
        public static bool IsRelevant(string fileName)
        {
            return Files.Contains(fileName);
        }
    }
}