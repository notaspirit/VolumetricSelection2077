-- mod info
mod = {
    ready = false
}

-- print on load
print('CtrlADel is loaded!')

-- onInit event
registerForEvent('onInit', function() 
    
    -- set as ready
    mod.ready = true
    
    -- print on initialize
    print('CtrlADel is initialized!')
    
end)

-- set initial var
local isOverlayVisible = false

-- define selection boxes point 
local boxPos1 = {x=0.00,y=0.00,z=0.00,w=1.00}

local boxPos2 = {x=0.00,y=0.00,z=0.00,w=1.00}

-- Sets default File name
local SaveFileName = ""

-- For Testing
local everything = false
local roundThis = ""

-- onOverlayOpen
registerForEvent('onOverlayOpen', function()
    isOverlayVisible = true
end)

-- onOverlayClose
registerForEvent('onOverlayClose', function()
    isOverlayVisible = false
end)

-- To round to a specific decimal place:
function round(num, decimalPlaces)
    local mult = 10^(decimalPlaces or 0)
    return math.floor(num * mult + 0.5) / mult
end

-- Sets either the Pos1 or the Pos2 Point to current Player Position
function BoxPointToPos(Pos1)
    -- gets current player position
    local currentPos = Game.GetPlayer():GetWorldPosition()
    -- if pos1 is specified sets boxPos1 values to match currentPosition
    if Pos1 == true then
        boxPos1[1] = round(currentPos.x, 3)
        boxPos1[2] = round(currentPos.y, 3)
        boxPos1[3] = round(currentPos.z, 3)
        boxPos1[4] = currentPos.w
    -- if pos1 is false = pos2 is meant to be set, sets boxPos2 values to match currentPosition
    else
        boxPos2[1] = round(currentPos.x, 3)
        boxPos2[2] = round(currentPos.y, 3)
        boxPos2[3] = round(currentPos.z, 3)
        boxPos2[4] = currentPos.w
    end
end

-- For Debug: Prints value of Box Points
function BoxPrintValue()
    print("Updated Box Pos1:")
    print("X:", boxPos1.x, "Y:", boxPos1.y, "Z:", boxPos1.z)
    print("Updated Box Pos2:")
    print("X:", boxPos2.x, "Y:", boxPos2.y, "Z:", boxPos2.z)
    print("SaveFileName:")
    print(SaveFileName)
    print(boxPos1)
end

-- Supposed to spawn cube based on boxPos1 but crashes game instead
function SpawnCube() 
    print("db1")
    spdlog.error("Debug1")
    local worldTransform1 = WorldTransform.new()
    print("db2")
    spdlog.error("debug2")
    worldTransform1:SetPosition(Vector4.new(-1325.501,1225.301,135.335,1.000))
    print("db3")
    spdlog.error("debug3")
    worldTransform1:SetOrientation(Quaternion.new(1.00,1.00,1.00,1.00))
    spdlog.error(tostring(WorldTransform.GetWorldPosition(worldTransform1)))
    print("db4")
    spdlog.error("Debug4")
    -- Do NOT try to spawn a mesh directly, this causes a game crash 
    local EntityId = exEntitySpawner.Spawn("base\\fx\\meshes\\cube_debug.mesh", worldTransform1) -- old path: base\\fx\\meshes\\cube_debug.mesh base\\gameplay\\devices\\advertising\\billboard_devices\\billboard_16x9.ent base\\gameplay\\devices\\advertising\\digital\\billboards\\digital_billboard_3_4.ent base\\items\\interactive\\industrial\\int_industrial_002__robotic_arm_delamain.ent
    spdlog.error("debug5")
    print("debug5")
    spdlog.error(tostring(EntityId))
end

-- Draws and deals with the Ui
function CETUi()
    -- bail early if overlay is not open
    if not isOverlayVisible then
        return
    end
    -- draw ImGui window
    if ImGui.Begin('CtrlADel') then
        -- draw Tabbar for main window
        if ImGui.BeginTabBar("TabList1") then
            -- draw Main Tab Group 
            if ImGui.BeginTabItem("Selector Box") then
                ImGui.Text("Selector Box Position 1")
                -- Button For Setting BoxPos1 to PlayerPos 
                if ImGui.Button("Set Pos1 Point to Current Position") then
                    BoxPointToPos(true)
                end
                -- Change values of Pos1
                boxPos1, change = ImGui.DragFloat3("Selector Box Pos1", boxPos1, 0.01)
                ImGui.Text("Selector Box Position 2")
                -- Button For Setting Pos2 to PlayerPos
                if ImGui.Button("Set Pos2 Point to Current Position") then
                    BoxPointToPos(false)
                end
                -- Change values of Pos2
                boxPos2, change = ImGui.DragFloat3("Selector Box Pos2", boxPos2, 0.01)
                ImGui.EndTabItem()
            end
            -- Defines tab that controlls what gets removed
            if ImGui.BeginTabItem("Removal") then
                everything, change = ImGui.Checkbox("Remove Everything?", everything)
                ImGui.EndTabItem()
            end
            -- Defines File Tab
            if ImGui.BeginTabItem("Output") then
                SaveFileName, change = ImGui.InputText("Project File Name", SaveFileName, 50)
                if ImGui.Button("Save to File") then
                    -- Add File Saving Mechanism Here
                end
                ImGui.EndTabItem()
            end
            -- Defines Debug Tab
            if ImGui.BeginTabItem("Debug") then
                -- Button that Prints all Box Values
                if ImGui.Button("Print Box Values") then
                    BoxPrintValue()
                end
                -- Rounding Function Testing
                roundThis, change = ImGui.InputText("Round This", roundThis, 100)
                if ImGui.Button("Round") then
                    print(round(tonumber(roundThis), 3))
                end
                -- for spawning the visualizing cube
                if ImGui.Button("Spawn Cube") then
                    SpawnCube()
                end
                ImGui.EndTabItem()
            end
        end
        ImGui.EndTabBar()
    end
    ImGui.End()
end
-- onDraw
-- this event is triggered continuously
registerForEvent('onDraw', function()
    CETUi()
end)
    
    
-- return mod info 
-- for communication between mods
return mod

