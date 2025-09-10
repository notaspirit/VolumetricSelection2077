--- vector3 class
---@class vector3
---@field x number
---@field y number
---@field z number
local vector3 = {}
vector3.__index = vector3  -- Set the __index of the metatable to vector3

function vector3:new(x, y, z)
    local instance = setmetatable({}, vector3)  -- Use vector3 as the metatable
    instance.x = x
    instance.y = y
    instance.z = z
    return instance
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

function vector3:distance(other)
    return Vector4.Distance(
        Vector4.new(self.x, self.y, self.z, 0),
        Vector4.new(other.x, other.y, other.z, 0)
    )
end

-- Function to rotate a vector by given rotation angles
local function rotateVector3(vector, rotation)
    local v4 = Vector4.new(vector.x, vector.y, vector.z, 0)
    local ea = EulerAngles.new(rotation.x, rotation.y, rotation.z)
    local m = Matrix.BuiltRotation(ea)
    local rotatedV4 = Vector4.Transform(m, v4)
    return vector3:new(rotatedV4.x, rotatedV4.y, rotatedV4.z)
end


function vector3:move(rotation, distance)
    local rotatedDistance = rotateVector3(distance, rotation)
    return vector3:new(
        self.x + rotatedDistance.x,
        self.y + rotatedDistance.y,
        self.z + rotatedDistance.z
    )
end

return vector3