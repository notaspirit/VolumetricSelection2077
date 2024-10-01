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
local boxPos1 = {0.000, 0.000, 0.000}

local boxPos2 = {0.000, 0.000, 0.000}

-- For Testing

-- onOverlayOpen
registerForEvent('onOverlayOpen', function()
    isOverlayVisible = true
end)

-- onOverlayClose
registerForEvent('onOverlayClose', function()
    isOverlayVisible = false
end)

-- Sets either the Pos1 or the Pos2 Point to current Player Position
function BoxPointToPos(Pos1)
    local currentPos = Game.GetPlayer():GetWorldPosition()
    if Pos1 == true then
        boxPos1[1] = tonumber(string.format("%.3f", currentPos.x))
        boxPos1[2] = tonumber(string.format("%.3f", currentPos.y))
        boxPos1[3] = tonumber(string.format("%.3f", currentPos.z))
    else
        boxPos2[1] = tonumber(string.format("%.3f", currentPos.x))
        boxPos2[2]= tonumber(string.format("%.3f", currentPos.y))
        boxPos2[3] = tonumber(string.format("%.3f", currentPos.z))
    end
end

-- Sets default File name
local SaveFileName = ""
-- For Debug: Prints value of Box Points
function BoxPrintValue()
    print("Updated Box Pos1:")
    print("X:", boxPos1[1], "Y:", boxPos1[2], "Z:", boxPos1[3])
    print("Updated Box Pos2:")
    print("X:", boxPos2[1], "Y:", boxPos2[2], "Z:", boxPos2[3])
    print("SaveFileName:")
    print(SaveFileName)
end

-- onDraw
-- this event is triggered continuously
registerForEvent('onDraw', function()
    
    -- bail early if overlay is not open
    if not isOverlayVisible then
        return
    end
        -- draw ImGui window
    if ImGui.Begin('CtrlADel') then
        -- Defines Tabbar for main window
        if ImGui.BeginTabBar("TabList1") then
            -- Defines Main Tab Group 
            if ImGui.BeginTabItem("Selector Box") then
                ImGui.Text("Selector Box Position 1")
                -- Button For Setting BoxPos1 to PlayerPos 
                if ImGui.Button("Set Pos1 Point to Current Position") then
                    BoxPointToPos(true)
                end
                -- Change Coord of Pos1
                boxPos1, change = ImGui.DragFloat3("Selector Box Pos1", boxPos1)
                ImGui.Text("Selector Box Position 2")
                -- Button For Setting Pos2 to PlayerPos
                if ImGui.Button("Set Pos2 Point to Current Position") then
                    BoxPointToPos(false)
                end
                -- Change Cord of Pos2
                boxPos2, change = ImGui.DragFloat3("Selector Box Pos2", boxPos2)
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
                ImGui.EndTabItem()
            end
        end
        ImGui.EndTabBar()
    end
    ImGui.End()
    
end)
-- return mod info 
-- for communication between mods
return mod

