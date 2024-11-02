local vector3 = require('classes/vector3')
local RHTScan = require('modules/RHTIntegration')
local visualizationBox = require('classes/visualizationBox')

-- Initialize variables
-- 3d Objects
local originPoint = vector3:new(0, 0, 0)
local rotationPoint = vector3:new(0, 0, 0)
local scalePoint = vector3:new(1, 1, 1)
local relativeOffset = vector3:new(0, 0, 0)
local selectionBox = visualizationBox:new(originPoint, scalePoint, rotationPoint)

-- Sorta Settings
local precisionBool = false
local unprecisePrecisionMove = 0.5
local precisePrecisionMove = 0.001
local unprecisePrecisionRotation = 1
local precisePrecisionRotation = 0.01
local precisionMove = unprecisePrecisionMove
local precisionRotation = unprecisePrecisionRotation
local isHighlighted = false

-- Ui Variables
local typeWidth = 40
local valueWidth = 100
local totalWidth = typeWidth + valueWidth

-- Status Text
local statusMessage = ""
local statusEndTime = 0
local statusDuration = 10

-- Entity
local entityState ={
    requestedEntity = false,
    addMesh = false,
    requestEndTime = 0
}


local function CenteredText(text, width)
    local textWidth = ImGui.CalcTextSize(text)
    local cellWidth = width
    local spacing = (cellWidth - textWidth) * 0.5
    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + spacing)
    ImGui.Text(text)
end

local function setPlayerPosition()
    local currentPos = Game.GetPlayer():GetWorldPosition()
    originPoint = vector3:new(currentPos.x, currentPos.y, currentPos.z)
    selectionBox:setOrigin(originPoint)
end

local function resetRotation()
    selectionBox:setRotation(vector3:new(0, 0, 0))
    rotationPoint = vector3:new(0, 0, 0)
end

local function resetScale()
    selectionBox:setScale(vector3:new(1, 1, 1))
    scalePoint = vector3:new(1, 1, 1)
end

local function wrapRotation(rotation)
    rotationPoint = vector3:new(rotation.x % 360, rotation.y % 360, rotation.z % 360)
    selectionBox:setRotation(rotationPoint)
end

-- Function to move a point by a distance considering rotation
local function movePoint(point, rotation, distance)
    local modifiedVector = point:move(rotation, distance)
    relativeOffset.x = 0
    relativeOffset.y = 0
    relativeOffset.z = 0
    return modifiedVector
end

local function showStatusText(text, type)
    statusMessage = text
    statusEndTime = ImGui.GetTime() + statusDuration
    statusType = type or "info"
end

local function handleRHTStatus(RHTResult)
    showStatusText(RHTResult.text, RHTResult.type)
end

local function requestEntity()
    entityState.requestedEntity = true
    entityState.requestEndTime = ImGui.GetTime() + 0.1
end

local function checkEntityRequest()
    if entityState.requestedEntity and ImGui.GetTime() > entityState.requestEndTime then
        entityState.requestedEntity = false
        return true
    end
    return false
end

-- Main gui function
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
            originPoint.x, changedOriginX = ImGui.DragFloat("##pos1x", originPoint.x, precisionMove)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            originPoint.y, changedOriginY = ImGui.DragFloat("##pos1y", originPoint.y, precisionMove)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            originPoint.z, changedOriginZ = ImGui.DragFloat("##pos1z", originPoint.z, precisionMove)
            
            if changedOriginX or changedOriginY or changedOriginZ then
                selectionBox:setOrigin(originPoint)
                selectionBox:updatePosition()
            end

            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            if ImGui.Button("Player Position##1") then
                setPlayerPosition()
                selectionBox:updatePosition()
            end

            -- Relative Position Row
            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth) 
            CenteredText("Relative", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            relativeOffset.x, changedRelativeRotationX = ImGui.DragFloat("##pos1xrel", relativeOffset.x, precisionMove)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            relativeOffset.y, changedRelativeRotationY = ImGui.DragFloat("##pos1yrel", relativeOffset.y, precisionMove)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            relativeOffset.z, changedRelativeRotationZ = ImGui.DragFloat("##pos1zrel", relativeOffset.z, precisionMove)

            if changedRelativeRotationX or changedRelativeRotationY or changedRelativeRotationZ then
                originPoint = movePoint(originPoint, rotationPoint, relativeOffset)
                selectionBox:setOrigin(originPoint)
                selectionBox:updatePosition()
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
            scalePoint.x, changedScaleX = ImGui.DragFloat("##scalex", scalePoint.x, precisionMove)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            scalePoint.y, changedScaleY = ImGui.DragFloat("##scaley", scalePoint.y, precisionMove)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            scalePoint.z, changedScaleZ = ImGui.DragFloat("##scalez", scalePoint.z, precisionMove)
            
            if changedScaleX or changedScaleY or changedScaleZ then
                selectionBox:setScale(scalePoint)
                selectionBox:updateScale()
            end

            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            if ImGui.Button("Reset Scale") then
                resetScale()
                selectionBox:updateScale()
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
            rotationPoint.x, changedRotationX = ImGui.DragFloat("##rotx", rotationPoint.x, precisionRotation)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            rotationPoint.y, changedRotationY = ImGui.DragFloat("##roty", rotationPoint.y, precisionRotation)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            rotationPoint.z, changedRotationZ = ImGui.DragFloat("##rotz", rotationPoint.z, precisionRotation)

            if changedRotationX or changedRotationY or changedRotationZ then
                wrapRotation(rotationPoint)
                selectionBox:updatePosition()
            end
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            if ImGui.Button("Reset Rotation") then
                resetRotation()
                selectionBox:updatePosition()
            end

            ImGui.TableNextRow()
            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            -- Potential point of confusion for unexperienced users, consider changing name
            -- Change button color to green
            ImGui.PushStyleColor(ImGuiCol.Button, 0, 180, 0, 0.8)  -- RGBA for green
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0, 180, 0, 0.6)  -- Slightly darker green when hovered
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0, 180, 0, 0.4)  -- Even darker green when active
            if ImGui.Button("Read RHT Scan") then
                handleRHTStatus(RHTScan(selectionBox))
            end
            ImGui.PopStyleColor(3)

            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            if ImGui.Button(string.format("Highlight [%s]", isHighlighted and "ON" or "OFF")) then
                isHighlighted = not isHighlighted
                -- Todo: Add Highlight Function
                if isHighlighted then
                    selectionBox:spawn()
                    requestEntity()
                else
                    selectionBox:resolveEntity()
                    selectionBox:despawn()
                end

            end

            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            if ImGui.Button(string.format("Precision [%s]", precisionBool and "ON" or "OFF")) then
                precisionBool = not precisionBool
                if precisionBool then
                    precisionMove = precisePrecisionMove
                    precisionRotation = precisePrecisionRotation
                else
                    precisionMove = unprecisePrecisionMove
                    precisionRotation = unprecisePrecisionRotation
                end
            end
            ImGui.EndTable()
        end

        if ImGui.GetTime() < statusEndTime then
            if statusType == "error" then
                ImGui.PushStyleColor(ImGuiCol.Text, 1, 0, 0, 1)  -- Red
            elseif statusType == "success" then
                ImGui.PushStyleColor(ImGuiCol.Text, 0, 1, 0, 1)  -- Green
            else
                ImGui.PushStyleColor(ImGuiCol.Text, 1, 1, 1, 1)  -- White
            end
            ImGui.Text(statusMessage)
            ImGui.PopStyleColor()
        end
        ImGui.Text("Make sure the entire selection is visible")
    end
    if checkEntityRequest() == true then
        selectionBox:resolveEntity()
        selectionBox:updateScale()
        selectionBox:updatePosition()
    end
    ImGui.End()
end

function NoRHTGui()
    if ImGui.Begin('VolumetricSelection2077') then
        ImGui.Text("RedHotTools is not installed.")
    end
    ImGui.End()
end

function onShutdown()
    selectionBox:despawn()
end
-- return function
return CETGui, NoRHTGui, onShutdown