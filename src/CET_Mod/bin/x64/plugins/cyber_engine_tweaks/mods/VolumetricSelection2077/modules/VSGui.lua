local vector3 = require('classes/vector3')
local saveSelectionOutput = require("modules/saveSelectionOutput")
local visualizationBox = require('classes/visualizationBox')
local StatusMessage = require('modules/StatusMessage')
local settings = require('modules/settings')
local jsonUtils = require('modules/jsonUtils')

-- Initialize variables
-- 3d Objects
local originPoint = vector3:new(0, 0, 0)
local rotationPoint = vector3:new(0, 0, 0)
local scalePoint = vector3:new(1, 1, 1)
local relativeOffset = vector3:new(0, 0, 0)
local selectionBox = nil

-- Settings
local versionString = "1000.0.0-beta12"

local settingsInstance
local isHighlighted = false
local isInitialized = false

-- local variables for dragloats in settings tab
local preciseMoveLocal = 0.001
local unpreciseMoveLocal = 0.5
local preciseRotationLocal = 0.01
local unpreciseRotationLocal = 1.0
local RHTRangeLocal = 120
-- Ui Variables
local typeWidth = 40
local valueWidth = 100
local totalWidth = typeWidth + valueWidth

-- Status Text
local statusMessage = StatusMessage.getInstance()

-- Entity
local entityState = {
    requestedEntity = false,
    addMesh = false,
    requestEndTime = 0
}

-- Helper Functions
-- I think this function gets called every frame? not sure and it works so idk rn
local function initSelectionBox()
    if isInitialized then return end

    -- First initialize the selection box
    if not settingsInstance.selectionBox then
        local newBox = visualizationBox:new(originPoint, scalePoint, rotationPoint)
        settingsInstance:update("selectionBox", newBox)
        selectionBox = newBox
        originPoint = newBox.origin
        scalePoint = newBox.scale
        rotationPoint = newBox.rotation
    else
        selectionBox = settingsInstance.selectionBox
        originPoint = selectionBox.origin
        scalePoint = selectionBox.scale
        rotationPoint = selectionBox.rotation
    end

    -- Then load the saved precision values
    preciseMoveLocal = settingsInstance.preciseMove
    unpreciseMoveLocal = settingsInstance.unpreciseMove
    preciseRotationLocal = settingsInstance.preciseRotation
    unpreciseRotationLocal = settingsInstance.unpreciseRotation
    RHTRangeLocal = settingsInstance.RHTRange
    isInitialized = true
end

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

local function retogglePrecision()
    if settingsInstance.precisionBool then
        settingsInstance:update("precisionBool", false)
        settingsInstance:update("precisionBool", true)
    else
        settingsInstance:update("precisionBool", true)
        settingsInstance:update("precisionBool", false)
    end
end

local function syncVisualizierPosition()
    local selectionFile = io.open("data/selection.json", "r")
    if not selectionFile then
        return
    end

    local settingsString = selectionFile:read("*a")
    selectionFile:close()

    local success, selectionTable = pcall(function()
        return jsonUtils.JSONToTable(settingsString)
    end)
    if not success or not selectionTable or not selectionTable.box then
        print("Failed to parse selection file")
        statusMessage:setMessage("Failed to parse selection file", "error")
        return
    end

    originPoint = vector3:new(selectionTable.box.origin.x, selectionTable.box.origin.y, selectionTable.box.origin.z)
    scalePoint = vector3:new(selectionTable.box.scale.x, selectionTable.box.scale.y, selectionTable.box.scale.z)
    rotationPoint = vector3:new(selectionTable.box.rotation.x, selectionTable.box.rotation.y, selectionTable.box.rotation.z)


    selectionBox:setOrigin(originPoint)
    selectionBox:setScale(scalePoint)
    selectionBox:setRotation(rotationPoint)

    if isHighlighted then
        selectionBox:updatePosition()
        selectionBox:updateScale()
    end
    
    settingsInstance:update("selectionBox", selectionBox)
end

-- Controls Tab
local function controlsTab()
    -- Position Headers
    if ImGui.BeginTable("PositionHeaders", 5, ImGuiTableFlags.SizingFixedFit) then -- 5 columns: Label, X, Y, Z, Button
        if (ImGui.IsKeyDown(ImGuiKey.LeftShift)) then
            if (settingsInstance.precisionBool == false) then
                settingsInstance:update("precisionBool", true)
            end
        else
            if (settingsInstance.precisionBool == true) then
                settingsInstance:update("precisionBool", false)
            end
        end
    
        -- Custom header row
        ImGui.TableNextRow()
        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth) -- Set width for Type column
        CenteredText("Type", valueWidth)

        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth) -- Set width for X column
        CenteredText("X", valueWidth)

        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth) -- Set width for Y column
        CenteredText("Y", valueWidth)

        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth) -- Set width for Z column
        CenteredText("Z", valueWidth)

        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth) -- Set width for Actions column
        CenteredText("Actions", valueWidth)

        ImGui.TableNextRow()
        ImGui.TableNextColumn()
        ImGui.TableNextColumn()
        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        CenteredText("Origin (Center)", valueWidth)
        -- Absolute Position Row
        ImGui.TableNextRow()
        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        CenteredText("Absolute", valueWidth)

        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        originPoint.x, changedOriginX = ImGui.DragFloat("##pos1x", originPoint.x, settingsInstance.currentMove)

        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        originPoint.y, changedOriginY = ImGui.DragFloat("##pos1y", originPoint.y, settingsInstance.currentMove)

        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        originPoint.z, changedOriginZ = ImGui.DragFloat("##pos1z", originPoint.z, settingsInstance.currentMove)

        if changedOriginX or changedOriginY or changedOriginZ then
            selectionBox:setOrigin(originPoint)
            selectionBox:updatePosition()
            settingsInstance:update("selectionBox", selectionBox)
        end

        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        if ImGui.Button("Player Position##1") then
            setPlayerPosition()
            selectionBox:updatePosition()
            settingsInstance:update("selectionBox", selectionBox)
        end

        -- Relative Position Row
        ImGui.TableNextRow()
        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        CenteredText("Relative", valueWidth)

        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        relativeOffset.x, changedRelativeRotationX = ImGui.DragFloat("##pos1xrel", relativeOffset.x,
            settingsInstance.currentMove)

        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        relativeOffset.y, changedRelativeRotationY = ImGui.DragFloat("##pos1yrel", relativeOffset.y,
            settingsInstance.currentMove)

        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        relativeOffset.z, changedRelativeRotationZ = ImGui.DragFloat("##pos1zrel", relativeOffset.z,
            settingsInstance.currentMove)

        if changedRelativeRotationX or changedRelativeRotationY or changedRelativeRotationZ then
            originPoint = movePoint(originPoint, rotationPoint, relativeOffset)
            selectionBox:setOrigin(originPoint)
            selectionBox:updatePosition()
            settingsInstance:update("selectionBox", selectionBox)
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
        scalePoint.x, changedScaleX = ImGui.DragFloat("##scalex", scalePoint.x, settingsInstance.currentMove)

        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        scalePoint.y, changedScaleY = ImGui.DragFloat("##scaley", scalePoint.y, settingsInstance.currentMove)

        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        scalePoint.z, changedScaleZ = ImGui.DragFloat("##scalez", scalePoint.z, settingsInstance.currentMove)

        if changedScaleX or changedScaleY or changedScaleZ then
            selectionBox:setScale(scalePoint)
            selectionBox:updateScale()
            settingsInstance:update("selectionBox", selectionBox)
        end

        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        if ImGui.Button("Reset##scale") then
            resetScale()
            selectionBox:updateScale()
            settingsInstance:update("selectionBox", selectionBox)
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
        rotationPoint.x, changedRotationX = ImGui.DragFloat("##rotx", rotationPoint.x, settingsInstance.currentRotation)

        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        rotationPoint.y, changedRotationY = ImGui.DragFloat("##roty", rotationPoint.y, settingsInstance.currentRotation)

        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        rotationPoint.z, changedRotationZ = ImGui.DragFloat("##rotz", rotationPoint.z, settingsInstance.currentRotation)

        if changedRotationX or changedRotationY or changedRotationZ then
            wrapRotation(rotationPoint)
            selectionBox:updatePosition()
            settingsInstance:update("selectionBox", selectionBox)
        end
        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        if ImGui.Button("Reset##rotation") then
            resetRotation()
            selectionBox:updatePosition()
            settingsInstance:update("selectionBox", selectionBox)
        end

        ImGui.TableNextRow()
        ImGui.TableNextRow()
        ImGui.TableNextColumn()
        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        -- Change button color to green
        ImGui.PushStyleColor(ImGuiCol.Button, 0, 180, 0, 0.8)        -- RGBA for green
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0, 180, 0, 0.6) -- Slightly darker green when hovered
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0, 180, 0, 0.4)  -- Even darker green when active
        if ImGui.Button("Save Selection") then
            local outputTable = {
                box = selectionBox:toTable(),
            };
            if saveSelectionOutput(jsonUtils.TableToJSON(outputTable)) then
                statusMessage:setMessage("Saved selection to file.", "success", 10);
            else
                statusMessage:setMessage("Failed to save selection to file!", "error", 10);
            end
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
        ImGui.Text(string.format("Precision [%s]", settingsInstance.precisionBool and "ON" or "OFF"))

        ImGui.TableNextColumn()
        if ImGui.Button("Sync Visualizer##visualizerPosition") then
            syncVisualizierPosition()
        end
        ImGui.EndTable()
    end
    statusMessage:display()
end

-- Settings Tab
local function settingsTab()
    if ImGui.BeginTable("SettingsTable", 3, ImGuiTableFlags.SizingFixedFit) then
        -- Move Precision Section
        ImGui.TableNextRow()
        ImGui.TableNextColumn()
        ImGui.Text("Move Precision")
        ImGui.TableNextColumn()

        -- Imprecise Move
        ImGui.TableNextRow()
        ImGui.TableNextColumn()
        ImGui.Text("Inprecise")
        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        local newUnpreciseMove
        newUnpreciseMove, changedUnpreciseMove = ImGui.DragFloat("##inpreciseMove", unpreciseMoveLocal, 0.01)
        if changedUnpreciseMove then
            unpreciseMoveLocal = newUnpreciseMove
            settingsInstance:update("unpreciseMove", unpreciseMoveLocal)
            retogglePrecision()
        end
        ImGui.TableNextColumn()
        if ImGui.Button("Reset##inpreciseMove") then
            unpreciseMoveLocal = 0.5
            settingsInstance:update("unpreciseMove", unpreciseMoveLocal)
            retogglePrecision()
        end

        -- Precise Move
        ImGui.TableNextRow()
        ImGui.TableNextColumn()
        ImGui.Text("Precise")
        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        local newPreciseMove
        newPreciseMove, changedPreciseMove = ImGui.DragFloat("##preciseMove", preciseMoveLocal, 0.001, 0.0001,
            unpreciseMoveLocal)
        if changedPreciseMove then
            preciseMoveLocal = newPreciseMove
            settingsInstance:update("preciseMove", preciseMoveLocal)
            retogglePrecision()
        end

        ImGui.TableNextColumn()
        if ImGui.Button("Reset##preciseMove") then
            preciseMoveLocal = 0.001
            settingsInstance:update("preciseMove", preciseMoveLocal)
            retogglePrecision()
        end

        -- Rotation Precision Section
        ImGui.TableNextRow()
        ImGui.TableNextColumn()
        ImGui.Text("Rotation Precision")
        ImGui.TableNextColumn()

        -- Imprecise Rotation
        ImGui.TableNextRow()
        ImGui.TableNextColumn()
        ImGui.Text("Inprecise")
        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        local newUnpreciseRotation
        newUnpreciseRotation, changedUnpreciseRotation = ImGui.DragFloat("##inpreciseRotation", unpreciseRotationLocal,
            0.01)
        if changedUnpreciseRotation then
            unpreciseRotationLocal = newUnpreciseRotation
            settingsInstance:update("unpreciseRotation", unpreciseRotationLocal)
            retogglePrecision()
        end

        ImGui.TableNextColumn()
        if ImGui.Button("Reset##inpreciseRotation") then
            unpreciseRotationLocal = 1.0
            settingsInstance:update("unpreciseRotation", unpreciseRotationLocal)
            retogglePrecision()
        end

        -- Precise Rotation
        ImGui.TableNextRow()
        ImGui.TableNextColumn()
        ImGui.Text("Precise")
        ImGui.TableNextColumn()
        ImGui.SetNextItemWidth(valueWidth)
        local newPreciseRotation
        newPreciseRotation, changedPreciseRotation = ImGui.DragFloat("##preciseRotation", preciseRotationLocal, 0.001,
            0.0001, unpreciseRotationLocal)
        if changedPreciseRotation then
            preciseRotationLocal = newPreciseRotation
            settingsInstance:update("preciseRotation", preciseRotationLocal)
            retogglePrecision()
        end

        ImGui.TableNextColumn()
        if ImGui.Button("Reset##preciseRotation") then
            preciseRotationLocal = 0.01
            settingsInstance:update("preciseRotation", preciseRotationLocal)
            retogglePrecision()
        end
        ImGui.EndTable()
    end
end

function CETGui()
	if not settingsInstance then
		settingsInstance = settings.getInstance()
	end
    if not isInitialized then
        initSelectionBox()
    end
    if ImGui.Begin('VolumetricSelection2077', true, ImGuiWindowFlags.AlwaysAutoResize) then
        if ImGui.BeginTabBar("TabList1") then
            if ImGui.BeginTabItem("Controls") then
                controlsTab()
                ImGui.EndTabItem()
            end
            if ImGui.BeginTabItem("Settings") then
                settingsTab()
                ImGui.EndTabItem()
            end
        end
    end
    if checkEntityRequest() == true then
        selectionBox:resolveEntity()
        selectionBox:updateScale()
        selectionBox:updatePosition()
    end
    ImGui.Text("Version: " .. versionString)
    ImGui.End()
end

function onShutdown()
    selectionBox:despawn()
end

function onSaveLoaded()
    isHighlighted = false
    if (not selectionBox == nil) then
		selectionBox.entity = nil
		selectionBox.entityID = nil
	end
end

return {
    CETGui = CETGui,
    onShutdown = onShutdown,
    onSaveLoaded = onSaveLoaded
}
