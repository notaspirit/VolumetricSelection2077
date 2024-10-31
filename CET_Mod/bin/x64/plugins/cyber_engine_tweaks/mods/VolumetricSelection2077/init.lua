require("modules/VSGui")

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
    if isOverlayVisible then
        -- draws main gui only if RHT is installed
        if RHTBool then
            CETGui()
        else
            NoRHTGui()
        end
    end
end)

-- return mod info 
-- for communication between mods
return mod
