require("modules/VSGui")

-- initial variables
local isOverlayVisible = false
local RHT = false

-- mod info 
mod = {
    ready = false
}

-- onInit event
registerForEvent('onInit', function()
    RHT = Game.GetWorldInspector() ~= nil

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
    if isOverlayVisible then
        if RHT then
            CETGui()
        else
            NoRHTGui()
        end
    end
end)

-- return mod info 
-- for communication between mods
return mod

-- yesm an hem