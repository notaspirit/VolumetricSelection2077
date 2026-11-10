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

local function projectWorldToScreen(worldPos, camTransform, fovDeg, aspect, screenW, screenH, near, otherWorldPos)
    local function dot(a,b) return a.x*b.x + a.y*b.y + a.z*b.z end

    near = near or 0.1

    local camPos = camTransform:GetTranslation()
    local right   = camTransform:GetAxisX()
    local forward = camTransform:GetAxisY()
    local up      = camTransform:GetAxisZ()

    local vec = { x = worldPos.x - camPos.x, y = worldPos.y - camPos.y, z = worldPos.z - camPos.z }
    local x_cam = dot(vec, right)
    local y_cam = dot(vec, up)
    local z_cam = dot(vec, forward)

    if z_cam > near then
        local f = 1 / math.tan(math.rad(fovDeg) * 0.5) -- vertical f
        local ndc_x = (x_cam / z_cam) * (f / aspect)
        local ndc_y = (y_cam / z_cam) * f
        local sx = (ndc_x * 0.5 + 0.5) * screenW
        local sy = (0.5 - ndc_y * 0.5) * screenH -- origin top-left
        return math.floor(sx + 0.5), math.floor(sy + 0.5), z_cam, false
    end

    -- If an opposite endpoint is provided and it is in front of the near plane,
    -- compute the world-space intersection of the segment with the near plane,
    -- then project that intersection. This avoids mirrored results when vertices are behind the camera.
    if otherWorldPos then
        local vec_o = { x = otherWorldPos.x - camPos.x, y = otherWorldPos.y - camPos.y, z = otherWorldPos.z - camPos.z }
        local x_cam_o = dot(vec_o, right)
        local y_cam_o = dot(vec_o, up)
        local z_cam_o = dot(vec_o, forward)

        if z_cam_o > near and z_cam ~= z_cam_o then
            -- t along world segment worldPos -> otherWorldPos where cam-space z == near
            local t = (near - z_cam) / (z_cam_o - z_cam)
            -- clamp t just in case
            if t < 0 then t = 0 elseif t > 1 then t = 1 end
            local ix = worldPos.x + t * (otherWorldPos.x - worldPos.x)
            local iy = worldPos.y + t * (otherWorldPos.y - worldPos.y)
            local iz = worldPos.z + t * (otherWorldPos.z - worldPos.z)
            -- project intersection (should now be at z ~= <=near but we treat it as near)
            local ivec = { x = ix - camPos.x, y = iy - camPos.y, z = iz - camPos.z }
            local ix_cam = dot(ivec, right)
            local iy_cam = dot(ivec, up)
            local iz_cam = dot(ivec, forward)
            if iz_cam > 0 then
                local f = 1 / math.tan(math.rad(fovDeg) * 0.5)
                local ndc_x = (ix_cam / iz_cam) * (f / aspect)
                local ndc_y = (iy_cam / iz_cam) * f
                local sx = (ndc_x * 0.5 + 0.5) * screenW
                local sy = (0.5 - ndc_y * 0.5) * screenH
                return math.floor(sx + 0.5), math.floor(sy + 0.5), iz_cam, true
            end
        end
    end

    return nil
end

-- 1 Back Bottom Left, 2 Back Bottom Right, 3 Back Top Left, 4 Back Top Right
-- 5 Front Bottom Left, 6 Front Bottom Right, 7 Front Top Left, 8 Front Top Right
local edges = {
    {1,2}, {2,4}, {4,3}, {3,1}, -- back face
    {5,6}, {6,8}, {8,7}, {7,5}, -- front face
    {1,5}, {2,6}, {3,7}, {4,8}  -- connections between faces
}

local screenW, screenH = 1, 1
local aspectRatio = 1
local near = 0.1

function visualizationBox:drawEdgeVisualizer()
    local camMatrix = GetPlayer():GetFPPCameraComponent():GetLocalToWorld()
    local fov = GetPlayer():GetFPPCameraComponent():GetFOV()
    local drawList = ImGui.GetBackgroundDrawList()

    for _, e in ipairs(edges) do
        local a, b = e[1], e[2]
        local wa, wb = self.vertices[a], self.vertices[b]

        local ax, ay = projectWorldToScreen(wa, camMatrix, fov, aspectRatio, screenW, screenH, near, wb)
        local bx, by = projectWorldToScreen(wb, camMatrix, fov, aspectRatio, screenW, screenH, near, wa)

        if ax and bx then
            ImGui.ImDrawListAddLine(drawList, ax, ay, bx, by, 0xFF000080, 2.0)
        end
    end

    local sx, sy, sz = projectWorldToScreen(
        self.origin,
        camMatrix,
        fov,
        screenW / screenH,
        screenW,
        screenH
    )
    if not sx or not sy then return end

    ImGui.ImDrawListAddCircleFilled(drawList, sx, sy, 5, 0xFF000080, 24)
end

function visualizationBox:onResume()
    local resSetting = Game.GetSettingsSystem():GetVar("/video/display", "Resolution"):GetValue()
    local index = 1
    for settingsPart in string.gmatch(resSetting, "[^x]+") do
        if index == 1 then
            screenW = tonumber(settingsPart)
        elseif index == 2 then
            screenH = tonumber(settingsPart)
        end
        index = index + 1
    end
    aspectRatio = screenW / screenH
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
