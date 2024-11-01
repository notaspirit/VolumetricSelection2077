-- Converts a rotation vector to a quaternion
local function ToQuaternion(rotation)
    local cy = math.cos(rotation.z * 0.5)
    local sy = math.sin(rotation.z * 0.5)
    local cp = math.cos(rotation.y * 0.5)
    local sp = math.sin(rotation.y * 0.5)
    local cr = math.cos(rotation.x * 0.5)
    local sr = math.sin(rotation.x * 0.5)

    local w = cr * cp * cy + sr * sp * sy
    local x = sr * cp * cy - cr * sp * sy
    local y = cr * sp * cy + sr * cp * sy
    local z = cr * cp * sy - sr * sp * cy

    return Quaternion.new(x, y, z, w)
end


-- Spawns an entity at a given position and rotation
local function spawnEntity(entityPath, position, rotation)
    local worldTransform = WorldTransform.new()
    worldTransform:SetPosition(ToVector4({x = position.x, y = position.y, z = position.z, w = 1}))
    worldTransform:SetOrientation(ToQuaternion(rotation))
    local entityId = exEntitySpawner.Spawn(entityPath, worldTransform)
    return entityId
end

local function despawnEntity(entity)
    exEntitySpawner.Despawn(entity)
end

local function addMesh(entity, name, mesh, scale, app, enabled)
    local parent = nil
    for _, component in pairs(entity:GetComponents()) do
        if component:IsA("entIPlacedComponent") then
            parent = component
            break
        end
    end
    if not parent then parent = entity:GetComponents()[1] end

    local component = entMeshComponent.new()
    component.name = name
    component.mesh = ResRef.FromString(mesh)
    component.visualScale = ToVector3({x = scale.x, y = scale.y, z = scale.z})
    component.meshAppearance = app
    component.isEnabled = enabled

    -- Bind to something, to avoid weird bug where other components would lose their localTransform
    if parent then
        local parentTransform = entHardTransformBinding.new()
        parentTransform.bindName = parent.name.value
        component.parentTransform = parentTransform
    end

    entity:AddComponent(component)
end

local selectionUtils = {
    spawnEntity = spawnEntity,
    despawnEntity = despawnEntity,
    addMesh = addMesh
}

return selectionUtils

