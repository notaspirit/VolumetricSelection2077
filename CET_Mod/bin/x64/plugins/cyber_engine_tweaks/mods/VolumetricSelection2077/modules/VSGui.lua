local vector3 = require('classes/vector3')
local RHTScan = require('modules/RHTIntegration')
local box = require('classes/box')
local selectionUtils = require('modules/selectionUtils')
local jsonUtils = require('modules/jsonUtils')

-- Initialize variables
-- 3d Objects
local SelectionBox = box:new(vector3:new(0, 0, 0), vector3:new(1, 1, 1), vector3:new(0, 0, 0))
local originPoint = vector3:new(0, 0, 0)
local rotationPoint = vector3:new(0, 0, 0)
local scalePoint = vector3:new(1, 1, 1)
local relativeOffset = vector3:new(0, 0, 0)

-- Sorta Settings
local precisionBool = false
local precision = 1
local unprecisePrecision = 1
local precisePrecision = 0.001
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
local entity = nil
local entityId = nil

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
    SelectionBox:setOrigin(originPoint)
end

local function resetRotation()
    SelectionBox:setRotation(vector3:new(0, 0, 0))
    rotationPoint = vector3:new(0, 0, 0)
end

local function resetScale()
    SelectionBox:setScale(vector3:new(1, 1, 1))
    scalePoint = vector3:new(1, 1, 1)
end

local function wrapRotation(rotation)
    rotationPoint = vector3:new(rotation.x % 360, rotation.y % 360, rotation.z % 360)
    SelectionBox:setRotation(rotationPoint)
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
            originPoint.x, changedOriginX = ImGui.DragFloat("##pos1x", originPoint.x, precision)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            originPoint.y, changedOriginY = ImGui.DragFloat("##pos1y", originPoint.y, precision)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            originPoint.z, changedOriginZ = ImGui.DragFloat("##pos1z", originPoint.z, precision)
            
            if changedOriginX or changedOriginY or changedOriginZ then
                SelectionBox:setOrigin(originPoint)
            end

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
                originPoint = movePoint(originPoint, rotationPoint, relativeOffset)
                SelectionBox:setOrigin(originPoint)
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
            scalePoint.x, changedScaleX = ImGui.DragFloat("##scalex", scalePoint.x, precision)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            scalePoint.y, changedScaleY = ImGui.DragFloat("##scaley", scalePoint.y, precision)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            scalePoint.z, changedScaleZ = ImGui.DragFloat("##scalez", scalePoint.z, precision)
            
            if changedScaleX or changedScaleY or changedScaleZ then
                SelectionBox:setScale(scalePoint)
            end

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
            rotationPoint.x, changedRotationX = ImGui.DragFloat("##rotx", rotationPoint.x, precision)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            rotationPoint.y, changedRotationY = ImGui.DragFloat("##roty", rotationPoint.y, precision)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            rotationPoint.z, changedRotationZ = ImGui.DragFloat("##rotz", rotationPoint.z, precision)

            if changedRotationX or changedRotationY or changedRotationZ then
                wrapRotation(rotationPoint)
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
            -- Potential point of confusion for unexperienced users, consider changing name
            -- Change button color to green
            ImGui.PushStyleColor(ImGuiCol.Button, 0, 180, 0, 0.8)  -- RGBA for green
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0, 180, 0, 0.6)  -- Slightly darker green when hovered
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0, 180, 0, 0.4)  -- Even darker green when active
            if ImGui.Button("Read RHT Scan") then
                handleRHTStatus(RHTScan(SelectionBox))
            end
            ImGui.PopStyleColor(3)

            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            if ImGui.Button(string.format("Highlight [%s]", isHighlighted and "ON" or "OFF")) then
                isHighlighted = not isHighlighted
                -- Todo: Add Highlight Function
                if isHighlighted then
                    local entityString = "base\\items\\interactive\\industrial\\int_industrial_002__robotic_arm_delamain.ent"
                    entityId = selectionUtils.spawnEntity(entityString, originPoint, rotationPoint)
                    -- Why does this not work here??? it literally works a couple of lines below??????
                    selectionUtils.addMesh(Game.FindEntityByID(entityId), "Mesh", "base\\spawner\\cube.mesh", scalePoint, "red", true)
                else
                    -- Game.FindEntityByID(entityId) **HAS** to be used here, otherwise it returns nil?????????????????
                    -- So just don't touch it and hope it continues to work
                    exEntitySpawner.Despawn(Game.FindEntityByID(entityId))
                end

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
        -- Todo: Fix Button Spacing
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