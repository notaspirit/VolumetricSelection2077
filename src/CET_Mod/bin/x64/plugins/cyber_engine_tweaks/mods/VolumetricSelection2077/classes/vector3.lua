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
    return math.sqrt((self.x - other.x)^2 + (self.y - other.y)^2 + (self.z - other.z)^2)
end

-- Function to convert degrees to radians
local function degToRad(degrees)
    return degrees * math.pi / 180
end

-- Function to rotate a vector by given rotation angles
local function rotateVector3(vector, rotation)
    local radX = degToRad(rotation.x)
    local radY = degToRad(rotation.y)
    local radZ = degToRad(rotation.z)

    -- Rotation around X-axis
    local cosX = math.cos(radX)
    local sinX = math.sin(radX)
    local y1 = vector.y * cosX - vector.z * sinX
    local z1 = vector.y * sinX + vector.z * cosX

    -- Rotation around Y-axis
    local cosY = math.cos(radY)
    local sinY = math.sin(radY)
    local x2 = vector.x * cosY + z1 * sinY
    local z2 = -vector.x * sinY + z1 * cosY

    -- Rotation around Z-axis
    local cosZ = math.cos(radZ)
    local sinZ = math.sin(radZ)
    local x3 = x2 * cosZ - y1 * sinZ
    local y3 = x2 * sinZ + y1 * cosZ

    return vector3:new(x3, y3, z2)
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