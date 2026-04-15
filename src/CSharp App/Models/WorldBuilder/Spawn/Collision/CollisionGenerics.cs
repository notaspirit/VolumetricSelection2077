using System;
using System.Linq;

namespace VolumetricSelection2077.Models.WorldBuilder.Spawn.Collision;

public class CollisionGenerics
{
    public string[] OriginalMaterials { get; set; }
    public string[] Materials { get; set; }
    public string[] Presets { get; set; }

    public CollisionGenerics()
    {
        OriginalMaterials =
        [
            "meatbag.physmat", "linoleum.physmat", "trash.physmat", "plastic.physmat", "character_armor.physmat",
            "furniture_upholstery.physmat", "metal_transparent.physmat", "tire_car.physmat", "meat.physmat",
            "metal_car_pipe_steam.physmat", "character_flesh.physmat", "brick.physmat", "character_flesh_head.physmat",
            "leaves.physmat", "flesh.physmat", "water.physmat", "plastic_road.physmat", "metal_hollow.physmat",
            "cyberware_flesh.physmat", "plaster.physmat", "plexiglass.physmat", "character_vr.physmat",
            "vehicle_chassis.physmat", "sand.physmat", "glass_electronics.physmat", "leaves_stealth.physmat",
            "tarmac.physmat", "metal_car.physmat", "tiles.physmat", "glass_car.physmat", "grass.physmat",
            "concrete.physmat", "carpet_techpiercable.physmat", "wood_hedge.physmat", "stone.physmat",
            "leaves_semitransparent.physmat", "metal_catwalk.physmat", "upholstery_car.physmat",
            "cyberware_metal.physmat", "paper.physmat", "leather.physmat", "metal_pipe_steam.physmat",
            "metal_pipe_water.physmat", "metal_semitransparent.physmat", "neon.physmat", "glass_dst.physmat",
            "plastic_car.physmat", "mud.physmat", "dirt.physmat", "metal_car_pipe_water.physmat",
            "furniture_leather.physmat", "asphalt.physmat", "wood_bamboo_poles.physmat", "glass_opaque.physmat",
            "carpet.physmat", "food.physmat", "cyberware_metal_head.physmat", "metal_road.physmat", "wood_tree.physmat",
            "wood_player_npc_semitransparent.physmat", "wood.physmat", "metal_car_ricochet.physmat",
            "cardboard.physmat", "wood_crown.physmat", "metal_ricochet.physmat", "plastic_electronics.physmat",
            "glass_semitransparent.physmat", "metal_painted.physmat", "rubber.physmat", "ceramic.physmat",
            "glass_bulletproof.physmat", "metal_car_electronics.physmat", "trash_bag.physmat",
            "character_cyberflesh.physmat", "metal_heavypiercable.physmat", "metal.physmat",
            "plastic_car_electronics.physmat", "oil_spill.physmat", "fabrics.physmat", "glass.physmat",
            "metal_techpiercable.physmat", "concrete_water_puddles.physmat", "character_metal.physmat"
        ];
        
        Materials = OriginalMaterials
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Presets =
        [
            "World Dynamic", "Player Collision", "Player Hitbox", "NPC Collision", "NPC Trace Obstacle", "NPC Hitbox",
            "Big NPC Collision", "Player Blocker", "Block Player and Vehicles", "Vehicle Blocker",
            "Block PhotoMode Camera", "Ragdoll", "Ragdoll Inner", "RagdollVehicle", "Terrain", "Sight Blocker",
            "Moving Kinematic", "Interaction Object", "Particle", "Destructible", "Debris", "Debris Cluster",
            "Foliage Debris", "ItemDrop", "Shooting", "Moving Platform", "Water", "Window", "Device transparent",
            "Device solid visible", "Vehicle Device", "Environment transparent", "Bullet logic", "World Static",
            "Simple Environment Collision", "Complex Environment Collision", "Foliage Trunk",
            "Foliage Trunk Destructible", "Foliage Low Trunk", "Foliage Crown", "Vehicle Part", "Vehicle Proxy",
            "Vehicle Part Query Only Exception", "Vehicle Chassis", "Chassis Bottom", "Chassis Bottom Traffic",
            "Vehicle Chassis Traffic", "AV Chassis", "Tank Chassis", "Vehicle Chassis LOD3",
            "Vehicle Chassis Traffic LOD3", "Tank Chassis LOD3", "Drone", "Prop Interaction", "Nameplate",
            "Road Barrier Simple Collision", "Road Barrier Complex Collision", "Lootable Corpse", "Spider Tank"
        ];
    }
}