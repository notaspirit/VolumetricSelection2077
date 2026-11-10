
---@class StatusMessage
---@field message string
---@field endTime number
---@field duration number
---@field level string
---@function init: StatusMessage
---@function display: void
---@function setMessage: void
local StatusMessage = {}
StatusMessage.__index = StatusMessage

-- Create the single global instance
local instance = nil

local function init(self)
    self.message = ""
    self.endTime = 0
    self.duration = 0
    self.level = ""
end

function StatusMessage.getInstance()
    if instance == nil then
        instance = setmetatable({}, StatusMessage)
        init(instance)
    end
    return instance
end

-- sets a new status message overwriting the previous one even if it is still displayed
function StatusMessage:setMessage(message, level, duration)
    self.message = message
    self.level = level
    self.duration = duration or 10
    self.endTime = ImGui.GetTime() + self.duration
end

-- displays the status message if it is still active, this goes into the main imgui loop, at the place where the message is supposed to be displayed
function StatusMessage:display()
    if ImGui.GetTime() < self.endTime then
        if self.level == "error" then
            ImGui.PushStyleColor(ImGuiCol.Text, 1, 0, 0, 1)  -- Red
        elseif self.level == "success" then
            ImGui.PushStyleColor(ImGuiCol.Text, 0, 1, 0, 1)  -- Green
        else
            ImGui.PushStyleColor(ImGuiCol.Text, 1, 1, 1, 1)  -- White
        end
        ImGui.Text(self.message)
        ImGui.PopStyleColor()
    end
end

return StatusMessage