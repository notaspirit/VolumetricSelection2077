-- class containing the basic more abstract properties of a box
local vector3 = require("classes/vector3")

---@class box
---@field origin vector3
---@field min vector3
---@field max vector3
---@field vertices vector3[]
---@field scale vector3
---@field rotation vector3
local box = {}
box.__index = box
box.__type = "box"

local function buildVertices(origin, scale, rotation)
    local vertices = {}
    -- Calculate the starting point (back bottom left vertex)
    local point = vector3:new(
        origin.x - scale.x/2,
        origin.y - scale.y/2,
        origin.z - scale.z/2
    )
    -- Back Bottom Left 1/8
    vertices[1] = point
    -- Back Bottom Right 2/8
    vertices[2] = point:move(rotation, {x = scale.x, y = 0, z = 0})
    -- Back Top Left 3/8
    vertices[3] = point:move(rotation, {x = 0, y = scale.y, z = 0})
    -- Back Top Right 4/8
    vertices[4] = point:move(rotation, {x = scale.x, y = scale.y, z = 0})
    -- Front Bottom Left 5/8
    vertices[5] = point:move(rotation, {x = 0, y = 0, z = scale.z})
    -- Front Bottom Right 6/8
    vertices[6] = point:move(rotation, {x = scale.x, y = 0, z = scale.z})
    -- Front Top Left 7/8
    vertices[7] = point:move(rotation, {x = 0, y = scale.y, z = scale.z})
    -- Front Top Right 8/8
    vertices[8] = point:move(rotation, {x = scale.x, y = scale.y, z = scale.z})
    return vertices
end

local function getMin(vertices)
    local min = vector3:new(vertices[1].x, vertices[1].y, vertices[1].z)
    for i = 2, #vertices do
        if vertices[i].x < min.x then min.x = vertices[i].x end
        if vertices[i].y < min.y then min.y = vertices[i].y end
        if vertices[i].z < min.z then min.z = vertices[i].z end
    end
    return min
end

local function getMax(vertices)
    local max = vector3:new(vertices[1].x, vertices[1].y, vertices[1].z)
    for i = 2, #vertices do
        if vertices[i].x > max.x then max.x = vertices[i].x end
        if vertices[i].y > max.y then max.y = vertices[i].y end
        if vertices[i].z > max.z then max.z = vertices[i].z end
    end
    return max
end

function box:new(origin, scale, rotation)
    local instance = setmetatable({}, box)
    instance.origin = vector3:new(origin.x, origin.y, origin.z)
    instance.scale = vector3:new(scale.x, scale.y, scale.z)
    instance.rotation = vector3:new(rotation.x, rotation.y, rotation.z)
    instance:updateVertices()
    return instance
end

function box:updateVertices()
    self.vertices = buildVertices(self.origin, self.scale, self.rotation)
    self.min = getMin(self.vertices)
    self.max = getMax(self.vertices)
end

function box:setScale(newScale)
    self.scale = vector3:new(newScale.x, newScale.y, newScale.z)
    self:updateVertices()
end

function box:setRotation(newRotation)
    self.rotation = vector3:new(newRotation.x, newRotation.y, newRotation.z)
    self:updateVertices()
end

function box:setOrigin(newOrigin)
    self.origin = vector3:new(newOrigin.x, newOrigin.y, newOrigin.z)
    self:updateVertices()
end

function box:toTable()
	local quat = EulerAngles.new(self.rotation.x, self.rotation.y, self.rotation.z):ToQuat()
    return {
        origin = {x = self.origin.x, y = self.origin.y, z = self.origin.z},
        min = {x = self.min.x, y = self.min.y, z = self.min.z},
        max = {x = self.max.x, y = self.max.y, z = self.max.z},
        vertices = (function()
            local verts = {}
            for i, v in ipairs(self.vertices) do
                verts[i] = {x = v.x, y = v.y, z = v.z}
            end
            return verts
        end)(),
        scale = {x = self.scale.x, y = self.scale.y, z = self.scale.z},
        rotation = {x = self.rotation.x, y = self.rotation.y, z = self.rotation.z},
        rotationQuat = {i = quat.i, j = quat.j, k = quat.k, r = quat.r}
    }
end

return box