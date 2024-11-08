VSGui = require("modules/VSGui")

-- initial variables
local isOverlayVisible = false
local RHTBool = false

-- mod info 
mod = {
    ready = false
}

-- onInit event
registerForEvent('onInit', function()
    local RHT = Game.GetWorldInspector()
    if RHT ~= nil then
        RHTBool = true
    else
        RHTBool = false
    end

    -- set as ready
    mod.ready = true
end)


-- tracks CET Overlay Visibility
-- onOverlayOpen
registerForEvent('onOverlayOpen', function()
    isOverlayVisible = true
end)

-- onOverlayClose
registerForEvent('onOverlayClose', function()
    isOverlayVisible = false
end)

-- onDraw (happens every frame)
registerForEvent('onDraw', function()
    -- if overlay is visible, draw ImGui
    if isOverlayVisible and VSGui then  -- Add nil check
        -- draws main gui only if RHT is installed
        if RHTBool then
            if type(VSGui.CETGui) == "function" then  -- Add type check
                VSGui.CETGui()
            else
                print("CETGui is not a function")
            end
        else
            if type(VSGui.noRHTGui) == "function" then  -- Add type check
                VSGui.noRHTGui()
            else
                print("noRHTGui is not a function")
            end
        end
    end
end)

-- onShutdown event, happens when game is closed or CET force reloads
registerForEvent("onShutdown", function ()
    VSGui.onShutdown()
end)

-- return mod info 
-- for communication between mods
return mod
