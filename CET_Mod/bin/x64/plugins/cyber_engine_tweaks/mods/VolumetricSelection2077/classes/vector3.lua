
--- vector3 class
---@class vector3
---@field x number
---@field y number
---@field z number
local vector3 = setmetatable({}, {})

function vector3:new(x, y, z)
    local self = setmetatable({}, vector3)
    self.x = x
    self.y = y
    self.z = z
    return self
end

function vector3:add(other)
    self.x = self.x + other.x
    self.y = self.y + other.y
    self.z = self.z + other.z
    return self
end

function vector3:subtract(other)
    self.x = self.x - other.x
    self.y = self.y - other.y
    self.z = self.z - other.z
    return self
end

function vector3:scale(scalar)
    self.x = self.x * scalar
    self.y = self.y * scalar
    self.z = self.z * scalar
    return self
end

return vector3

