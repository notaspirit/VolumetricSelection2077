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
local boxOrigin = {
    x = 0,       -- X-coordinate
    y = 0,       -- Y-coordinate
    z = 0,       -- Z-coordinate
    yaw = 0,     -- Rotation around the vertical axis
    tilt = 0,    -- Tilt angle 
    roll = 0     -- Roll angle
}

local boxSupport = {
    x = 0,       -- X-coordinate
    y = 0,       -- Y-coordinate
    z = 0       -- Z-coordinate
}

-- For Testing
local InputTextTest = tostring(boxOrigin.x)

-- onOverlayOpen
registerForEvent('onOverlayOpen', function()
    isOverlayVisible = true
end)

-- onOverlayClose
registerForEvent('onOverlayClose', function()
    isOverlayVisible = false
end)

-- Sets either the Origin or the Support Point to current Player Position
function BoxPointToPos(Origin)
    local currentPos = Game.GetPlayer():GetWorldPosition()
    if Origin == true then
        boxOrigin.x = currentPos.x
        boxOrigin.y = currentPos.y
        boxOrigin.z = currentPos.z
    else
        boxSupport.x = currentPos.x
        boxSupport.y = currentPos.y
        boxSupport.z = currentPos.z
    end
end
-- For Debug: Prints value of Box Points
function BoxPrintValue()
    print("Updated Box Origin:")
    print("X:", boxOrigin.x, "Y:", boxOrigin.y, "Z:", boxOrigin.z, "Yaw:", boxOrigin.yaw, "Tilt:", boxOrigin.tilt, "Roll:", boxOrigin.roll)
    print("Updated Box Support:")
    print("X:", boxSupport.x, "Y:", boxSupport.y, "Z:", boxSupport.z)
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
                -- Button For Setting Origin Point X Y Z to Current Player X Y Z 
                if ImGui.Button("Set Origin Point to Current Position", 250, 50) then
                    BoxPointToPos(true)
                end
                -- Doesn't work as expected
                if ImGui.InputText("Pos X", InputTextTest, 100) then
                    if ImGui.Button("Confirm") then
                        boxOrigin.x = tonumber(InputTextTest)
                    end
                end
                -- Button For Setting Support Point X Y Z to Current Player X Y Z 
                if ImGui.Button("Set Support Point to Current Position", 250, 50) then
                    BoxPointToPos(false)
                end
                ImGui.EndTabItem()
            end
            -- Defines Debug Tab
            if ImGui.BeginTabItem("Debug") then
                -- Button that Prints all Box Values
                if ImGui.Button("Print Box Values", 175, 50) then
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

