local vector3 = require("classes/vector3")
local Object_keys = require("modules/table_keys")

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

function RHTScan(box)
    local RHT = GetMod("RedHotTools")
    if not RHT then
        return
    end
    local GamePlayPos = Game.GetPlayer():GetWorldPosition()
    local PlayerPos = vector3:new(GamePlayPos.x, GamePlayPos.y, GamePlayPos.z)
    local furtherPoint = getFurtherPoint(box, PlayerPos)
    local Distance = furtherPoint:distance(PlayerPos)
    local distanceWithBuffer = Distance*1.2
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

return RHTScan
