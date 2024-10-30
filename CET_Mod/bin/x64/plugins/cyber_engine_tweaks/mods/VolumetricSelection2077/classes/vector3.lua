
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

return vector3

