local vector3 = require("classes/vector3")
local Object_keys = require("modules/table_keys")
local jsonUtils = require("modules/jsonUtils")
local saveSelectionOutput = require("modules/saveSelectionOutput")
local settingsService = require("modules/settings")

-- gets the further point of the box from the point
local function getFurtherPoint(box, point)
    local vertices = box.vertices
    local furtherPoint = vector3:new(vertices[1].x, vertices[1].y, vertices[1].z)
    for i = 2, #vertices do
        if point:distance(vertices[i]) > point:distance(furtherPoint) then
            furtherPoint = vertices[i]
        end
    end
    return furtherPoint
end

local function getNodesInRange(TargetData, distanceWithBuffer, TargetKeys)
    local inRangeResults = {}

    for _, key in ipairs(TargetKeys) do
        if TargetData[key].distance < distanceWithBuffer then
            table.insert(inRangeResults, TargetData[key])
        end
    end
    return inRangeResults
end

local RHTResult = {text = "", type = "info"}    

function DeprecatedRHTScan(box)
    -- Move implementation to rely on RHT's .dll instead of the CET Mod
    -- Use this: Game.GetWorldInspector():GetStreamedNodesInFrustum()
    local RHT = GetMod("RedHotTools")
    if not RHT then
        return
    end
    local GamePlayPos = Game.GetPlayer():GetWorldPosition()
    local PlayerPos = vector3:new(GamePlayPos.x, GamePlayPos.y, GamePlayPos.z)
    local furtherPoint = getFurtherPoint(box, PlayerPos)
    local Distance = furtherPoint:distance(PlayerPos)
    local distanceWithBuffer = Distance*scaleBuffer
    -- TODO: Add check to make sure that the distance is less than what RHT can scan
    print("Distance:", tostring(Distance))

    local TargetData = RHT.GetWorldScannerResults()
    print("TargetData type:", type(TargetData))
    
    if type(TargetData) ~= "table" then
        RHTResult.text = "TargetData is not a table!"
        RHTResult.type = "error"
        return RHTResult
    end
    
    local TargetKeys = Object_keys(TargetData)
    print("Number of keys:", #TargetKeys)

    if #TargetKeys == 0 then
        RHTResult.text = "No keys found in TargetData, please initialize RHT Scan!"
        RHTResult.type = "error"
        return RHTResult
    end

    local inRangeResults = getNodesInRange(TargetData, distanceWithBuffer, TargetKeys)
    print("Number of in range results:", #inRangeResults)
end

local function tableContainsSector(tbl, sector)
    for _, info in ipairs(tbl) do
        if info == sector then
            return true
        end
    end
    return false
end

local function getSectorPathsAndDistancesInFrustum()
    local inspectionSystem = Game.GetWorldInspector()
    local nodes = inspectionSystem:GetStreamedNodesInFrustum()
    local sectorInfo = {}

    for _, node in ipairs(nodes) do
        local sectorData
        if IsDefined(node.nodeInstance) then
            sectorData = inspectionSystem:ResolveSectorDataFromNodeInstance(node.nodeInstance)
        elseif isNotEmpty(node.nodeID) then
            sectorData = inspectionSystem:ResolveSectorDataFromNodeID(node.nodeID)
        end

        if sectorData and sectorData.sectorHash ~= 0 then
            local sectorPath = RedHotTools.GetResourcePath(sectorData.sectorHash)
            local distance = node.distance 
            table.insert(sectorInfo, { sectorPath = sectorPath, distance = distance })
        end
    end
    return sectorInfo
end

function RHTScan(box)
    local sectorInfo = getSectorPathsAndDistancesInFrustum()
    local gamePlayPos = Game.GetPlayer():GetWorldPosition()
    local playerPos = vector3:new(gamePlayPos.x, gamePlayPos.y, gamePlayPos.z)
    local furtherPoint = getFurtherPoint(box, playerPos)
    local distance = furtherPoint:distance(playerPos)
    local settings = settingsService.getInstance()
    local maxDistance = settings.RHTRange
    if distance > maxDistance then
        RHTResult.text = string.format("Furthest point distance %.1f m should be less than %.1f m, please move closer to the selection", distance, maxDistance)
        RHTResult.type = "error"
        return RHTResult
    end
    local inRangeSector = {}

    for _, info in ipairs(sectorInfo) do
        if info.distance < distance and not tableContainsSector(inRangeSector, info.sectorPath) then
            table.insert(inRangeSector, info.sectorPath)
        end
    end
    local outputTable = {}
    outputTable["box"] = box:toTable()
    outputTable["sectors"] = inRangeSector
    if saveSelectionOutput(jsonUtils.TableToJSON(outputTable)) then
        RHTResult.text = "Saved "..#inRangeSector.." sectors"
        RHTResult.type = "success"
    else
        RHTResult.text = "Failed to save sectors"
        RHTResult.type = "error"
    end
    return RHTResult
end

return RHTScan
