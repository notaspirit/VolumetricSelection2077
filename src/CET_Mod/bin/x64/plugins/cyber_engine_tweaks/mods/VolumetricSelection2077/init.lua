VSGui = require("modules/VSGui")

local isOverlayVisible = false

registerForEvent('onInit', function()
    mod.ready = true
end)

registerForEvent('onOverlayOpen', function()
    isOverlayVisible = true
end)

registerForEvent('onOverlayClose', function()
    isOverlayVisible = false
end)

registerForEvent('onDraw', function()
    if isOverlayVisible then
       VSGui.CETGui()
    end
end)

registerForEvent("onShutdown", function ()
    VSGui.onShutdown()
end)