VSGui = require("modules/VSGui")
local GameSession = require("libs/GameSession")

local isOverlayVisible = false

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

registerForEvent("onInit", function ()
    GameSession.Listen(function(state)
        if tostring(state.event) == "Start" then
            VSGui.onSaveLoaded()
        end
    end)
end)