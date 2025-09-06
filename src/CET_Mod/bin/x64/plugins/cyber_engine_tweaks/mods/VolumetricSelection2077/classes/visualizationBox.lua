local StatusMessage = require("modules/StatusMessage")

-- class inheriting from the box class, but with the addition of the visualizer entity
---@class visualizationBox : box
---@field entityID entityID
---@field entity entEntity

local visualizationBox = {}
visualizationBox.__index = visualizationBox

local box = require("classes/box")

local visualizationBox = setmetatable({}, { __index = box })
visualizationBox.__index = visualizationBox

function visualizationBox:new(origin, scale, rotation)
    ---@type visualizationBox
    local instance = box:new(origin, scale, rotation)  -- Create an instance using the box constructor
    setmetatable(instance, visualizationBox)  -- Set the metatable to visualizationBox
    instance.entityID = nil
    instance.entity = nil
    return instance
end

function visualizationBox:spawn()
    local statusMessage = StatusMessage.getInstance()
    if self.entity then return end
    local entityPath = "vs2077\\customcube.ent"
    local worldTransform = WorldTransform.new()
    worldTransform:SetPosition(ToVector4({x = self.origin.x, y = self.origin.y, z = self.origin.z, w = 1}))
    worldTransform:SetOrientation(EulerAngles.new(self.rotation.x, self.rotation.y, self.rotation.z):ToQuat())
    self.entityID = exEntitySpawner.Spawn(entityPath, worldTransform)
    if not self.entityID then
        statusMessage:setMessage("Failed to spawn visualization box", "error")
        return false
    end
    return true
end

function visualizationBox:resolveEntity()
    local statusMessage = StatusMessage.getInstance()
    self.entity = Game.FindEntityByID(self.entityID)
    if not self.entity then
        statusMessage:setMessage("Failed to resolve visualization box", "error")
        return false
    end
    return true
end

function visualizationBox:despawn()
    if not self.entity then return end
    exEntitySpawner.Despawn(self.entity)
    self.entity = nil
    self.entityID = nil
end

function visualizationBox:updatePosition()
    if not self.entity then return end
    local EARotation = EulerAngles.new(self.rotation.x, self.rotation.y, self.rotation.z)
    local vector4Position = ToVector4({x = self.origin.x, y = self.origin.y, z = self.origin.z, w = 1})
    Game.GetTeleportationFacility():Teleport(self.entity, vector4Position, EARotation)
    local component = self.entity:FindComponentByName("Component")
    component:Toggle(false)
    component:Toggle(true)
end

function visualizationBox:updateScale()
    if not self.entity then return end
    local component = self.entity:FindComponentByName("Component")
    component.visualScale = ToVector3({x=self.scale.x*0.5, y=self.scale.y*0.5, z=self.scale.z*0.5})
    component:Toggle(false)
    component:Toggle(true)
end

function visualizationBox:LogCurrentStats()
    if not self.entity then return end
    local rotation = self.entity:GetWorldOrientation()
    local position = self.entity:GetWorldPosition()
    print("Current Box Position as read from the game:")
    print(string.format("Rotation Quat: i: [%f] j: [%f] k: [%f] r: [%f]", rotation.i, rotation.j, rotation.k, rotation.r))
    print(string.format("Rotation Euler: X: [%f] Y: [%f] Z: [%f]", self.rotation.x, self.rotation.y, self.rotation.z))
    --print(string.format("Position: X: [%f] Y: [%f] Z: [%f] W: [%f]", position.X, position.Y, position.Z, position.W))
end

return visualizationBox
