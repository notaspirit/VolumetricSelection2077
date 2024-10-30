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
    local RHT = GetMod("RedHotTools")
    if not RHT then
        RHTBool = false
    else
        RHTBool = true
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

-- yesm an hem