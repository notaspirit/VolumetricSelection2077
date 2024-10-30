local vector3 = require('classes/vector3')

-- Initialize variables
local pos1 = vector3:new(0, 0, 0)
local scale = vector3:new(1, 1, 1)
local rotation = vector3:new(0, 0, 0)
local relativeOffset = vector3:new(0, 0, 0)
local precisionBool = false
local precision = 1
local unprecisePrecision = 1
local precisePrecision = 0.001
local isHighlighted = false

local typeWidth = 40
local valueWidth = 100
local totalWidth = typeWidth + valueWidth

local function CenteredText(text, width)
    local textWidth = ImGui.CalcTextSize(text)
    local cellWidth = width
    local spacing = (cellWidth - textWidth) * 0.5
    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + spacing)
    ImGui.Text(text)
end

local function setPlayerPosition()
    local currentPos = Game.GetPlayer():GetWorldPosition()
    pos1.x = currentPos.x
    pos1.y = currentPos.y
    pos1.z = currentPos.z
end

local function resetRotation()
    rotation.x = 0
    rotation.y = 0
    rotation.z = 0
end

local function resetScale()
    scale.x = 1
    scale.y = 1
    scale.z = 1
end

local function wrapRotation(rotation)
    rotation.x = rotation.x % 360
    rotation.y = rotation.y % 360
    rotation.z = rotation.z % 360
end

-- Function to convert degrees to radians
local function degToRad(degrees)
    return degrees * math.pi / 180
end

-- Function to rotate a vector by given rotation angles
local function rotateVector3(vector, rotation)
    local radX = degToRad(rotation.x)
    local radY = degToRad(rotation.y)
    local radZ = degToRad(rotation.z)

    -- Rotation around X-axis
    local cosX = math.cos(radX)
    local sinX = math.sin(radX)
    local y1 = vector.y * cosX - vector.z * sinX
    local z1 = vector.y * sinX + vector.z * cosX

    -- Rotation around Y-axis
    local cosY = math.cos(radY)
    local sinY = math.sin(radY)
    local x2 = vector.x * cosY + z1 * sinY
    local z2 = -vector.x * sinY + z1 * cosY

    -- Rotation around Z-axis
    local cosZ = math.cos(radZ)
    local sinZ = math.sin(radZ)
    local x3 = x2 * cosZ - y1 * sinZ
    local y3 = x2 * sinZ + y1 * cosZ

    relativeOffset.x = 0
    relativeOffset.y = 0
    relativeOffset.z = 0

    return {x = x3, y = y3, z = z2}
end

-- Function to move a point by a distance considering rotation
local function movePoint(point, rotation, distance)
    local rotatedDistance = rotateVector3(distance, rotation)
    return {
        x = point.x + rotatedDistance.x,
        y = point.y + rotatedDistance.y,
        z = point.z + rotatedDistance.z
    }
end

function CETGui()
    -- draw ImGui window
    if ImGui.Begin('VolumetricSelection2077') then
        -- Position Headers
        if ImGui.BeginTable("PositionHeaders", 5, ImGuiTableFlags.SizingFixedFit) then  -- 5 columns: Label, X, Y, Z, Button
            -- Custom header row
            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)  -- Set width for Type column
            CenteredText("Type", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)  -- Set width for X column
            CenteredText("X", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)  -- Set width for Y column
            CenteredText("Y", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)  -- Set width for Z column
            CenteredText("Z", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)  -- Set width for Actions column
            CenteredText("Actions", valueWidth)

            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.TableNextColumn()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)  
            CenteredText("Box Point 1", valueWidth)
            -- Absolute Position Row
            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth) 
            CenteredText("Absolute", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            pos1.x = ImGui.DragFloat("##pos1x", pos1.x, precision)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            pos1.y = ImGui.DragFloat("##pos1y", pos1.y, precision)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            pos1.z = ImGui.DragFloat("##pos1z", pos1.z, precision)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            if ImGui.Button("Player Position##1") then
                setPlayerPosition()
            end

            -- Relative Position Row
            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth) 
            CenteredText("Relative", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            relativeOffset.x, changedRelativeRotationX = ImGui.DragFloat("##pos1xrel", relativeOffset.x, precision)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            relativeOffset.y, changedRelativeRotationY = ImGui.DragFloat("##pos1yrel", relativeOffset.y, precision)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            relativeOffset.z, changedRelativeRotationZ = ImGui.DragFloat("##pos1zrel", relativeOffset.z, precision)

            if changedRelativeRotationX or changedRelativeRotationY or changedRelativeRotationZ then
                pos1 = movePoint(pos1, rotation, relativeOffset)
            end
            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.TableNextColumn()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)  
            CenteredText("Scale", valueWidth)
            -- Absolute Position Row
            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth) 
            CenteredText("Absolute", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            scale.x = ImGui.DragFloat("##scalex", scale.x, precision)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            scale.y = ImGui.DragFloat("##scaley", scale.y, precision)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            scale.z = ImGui.DragFloat("##scalez", scale.z, precision)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            if ImGui.Button("Reset Scale") then
                resetScale()
            end

            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.TableNextColumn()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)  
            CenteredText("Rotation", valueWidth)
            -- Rotation Row
            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth) 
            CenteredText("Absolute", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            rotation.x, changedRotationX = ImGui.DragFloat("##rotx", rotation.x, precision)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            rotation.y, changedRotationY = ImGui.DragFloat("##roty", rotation.y, precision)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            rotation.z, changedRotationZ = ImGui.DragFloat("##rotz", rotation.z, precision)

            if changedRotationX or changedRotationY or changedRotationZ then
                wrapRotation(rotation)
            end
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            if ImGui.Button("Reset Rotation") then
                resetRotation()
            end

            ImGui.TableNextRow()
            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            if ImGui.Button("Scan with RHT") then
            end

            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            if ImGui.Button(string.format("Highlight [%s]", isHighlighted and "ON" or "OFF")) then
                isHighlighted = not isHighlighted
            end

            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            if ImGui.Button(string.format("Precision [%s]", precisionBool and "ON" or "OFF")) then
                precisionBool = not precisionBool
                if precisionBool then
                    precision = precisePrecision
                else
                    precision = unprecisePrecision
                end
            end
            ImGui.EndTable()
        end
        -- Todo: Add Scan RHT button
        -- Add Relative movement logic (Don't understand shit :kek:)
        -- Fix button spacing
    end
    ImGui.End()
end

function NoRHTGui()
    if ImGui.Begin('VolumetricSelection2077') then
        ImGui.Text("RedHotTools is not installed.")
    end
    ImGui.End()
end

-- return function
return CETGui, NoRHTGui