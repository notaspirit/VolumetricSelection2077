require("modules/VSGui")

-- initial variables
local isOverlayVisible = false

-- mod info 
mod = {
    ready = false
}

-- onInit event
registerForEvent('onInit', function()
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
        CETGui()

    end
end)

-- return mod info 
-- for communication between mods
return mod

-- yesm an hem