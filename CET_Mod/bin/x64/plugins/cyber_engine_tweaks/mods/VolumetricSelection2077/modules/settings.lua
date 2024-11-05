---@class settings
---@field selectionBox box
---@field unpreciseMove number
---@field preciseMove number
---@field unpreciseRotation number
---@field preciseRotation number
---@field precisionBool boolean

local jsonUtils = require('modules/jsonUtils')
local statusMessage = require('modules/StatusMessage')
local visualizationBox = require('classes/visualizationBox')
local vector3 = require('classes/vector3')

local settings = {}
settings.__index = settings

-- Create the single global instance
local instance = nil

-- Loads the settings from the settings.json file
local function loadSettings()
    -- Check if file exists
    local settingsFile = io.open("data/settings.json", "r")
    if not settingsFile then
        return nil
    end
    -- Read the file content
    local settingsString = settingsFile:read("*a")
    settingsFile:close()
    -- Parse JSON into table
    local success, settingsTable = pcall(function()
        return jsonUtils.JSONToTable(settingsString)
    end)
    if not success or not settingsTable then
        print("Failed to parse settings file")
        statusMessage:setMessage("Failed to parse settings file", "error")
        return nil
    end
    print("Settings file parsed successfully")
    -- Convert any class data back into instances
    if settingsTable.selectionBox then
        local origin = vector3:new(settingsTable.selectionBox.origin.x, settingsTable.selectionBox.origin.y, settingsTable.selectionBox.origin.z)
        local scale = vector3:new(settingsTable.selectionBox.scale.x, settingsTable.selectionBox.scale.y, settingsTable.selectionBox.scale.z)
        local rotation = vector3:new(settingsTable.selectionBox.rotation.x, settingsTable.selectionBox.rotation.y, settingsTable.selectionBox.rotation.z)
        local success, result = pcall(function()
            return visualizationBox:new(origin, scale, rotation)
        end)
        if success then
            settingsTable.selectionBox = result
        else
            print("Failed to create visualizationBox when loading settings")
            statusMessage:setMessage("Failed to create visualizationBox when loading settings", "error")
        end
    end
    return settingsTable
end

local function init(self)
    local savedSettings = loadSettings()
    if savedSettings then
        -- Apply saved settings
        self.selectionBox = savedSettings.selectionBox
        self.unpreciseMove = savedSettings.unpreciseMove or 0.5
        self.preciseMove = savedSettings.preciseMove or 0.001
        self.unpreciseRotation = savedSettings.unpreciseRotation or 1
        self.preciseRotation = savedSettings.preciseRotation or 0.01
        self.precisionBool = savedSettings.precisionBool or false
        self.RHTRange = savedSettings.RHTRange or 120
        if self.precisionBool == true then
            self.currentMove = self.preciseMove
            self.currentRotation = self.preciseRotation
        else
            self.currentMove = self.unpreciseMove
            self.currentRotation = self.unpreciseRotation
        end
    else
        -- Use defaults if no saved settings
        self.selectionBox = nil
        self.unpreciseMove = 0.5
        self.currentMove = self.unpreciseMove
        self.preciseMove = 0.001
        self.unpreciseRotation = 1.0
        self.currentRotation = self.unpreciseRotation
        self.preciseRotation = 0.01
        self.precisionBool = false
        self.RHTRange = 120
    end
end

-- returns the single global instance
function settings.getInstance()
    if instance == nil then
        instance = setmetatable({}, settings)
        init(instance)
    end
    return instance
end

local function saveSettings()
    local settingsInst = settings.getInstance()
    
    local settingsTable = {
        precisionBool = settingsInst.precisionBool,
        unpreciseMove = settingsInst.unpreciseMove,
        preciseMove = settingsInst.preciseMove,
        unpreciseRotation = settingsInst.unpreciseRotation,
        preciseRotation = settingsInst.preciseRotation,
        selectionBox = settingsInst.selectionBox,
        RHTRange = settingsInst.RHTRange
    }
    
    local settingsString = jsonUtils.TableToJSON(settingsTable)
    local settingsFile = io.open("data/settings.json", "w")
    if not settingsFile then
        statusMessage:setMessage("Failed to save settings", "error")
        return
    end
    
    local success, errorMsg = pcall(function()
        settingsFile:write(settingsString)
        settingsFile:close()
    end)
    
    if not success then
        print("Failed to write settings: " .. (errorMsg or ""))
        statusMessage:setMessage("Failed to write settings: " .. (errorMsg or ""), "error")
    end
end

function settings:update(settingType, value)  -- Changed parameter name from 'type' to 'settingType'
    -- Validate input types
    if settingType == nil then return end
    
    -- Use a table for cleaner type checking
    local validTypes = {
        precisionBool = function(v) return v == true or v == false end,
        unpreciseMove = function(v) return type(v) == "number" end,
        preciseMove = function(v) return type(v) == "number" end,
        unpreciseRotation = function(v) return type(v) == "number" end,
        preciseRotation = function(v) return type(v) == "number" end,
        selectionBox = function(v) return v == nil or v.__type == "visualizationBox" or v.__type == "box" end,
        RHTRange = function(v) return type(v) == "number" end
    }

    -- Check if valid type and value
    if not validTypes[settingType] or not validTypes[settingType](value) then
        print("Invalid settings update: " .. settingType .. " " .. tostring(value))
        statusMessage:setMessage("Invalid settings update", "error")
        return
    end

    -- Update the value
    self[settingType] = value

    -- Handle precision mode changes
    if settingType == "precisionBool" then
        self.currentMove = value and self.preciseMove or self.unpreciseMove
        self.currentRotation = value and self.preciseRotation or self.unpreciseRotation
    end

    -- Save after any change
    saveSettings()
end

function settings:save()
    saveSettings()
end

return settings
