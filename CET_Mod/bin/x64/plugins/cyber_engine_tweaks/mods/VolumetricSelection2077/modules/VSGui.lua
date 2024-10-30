local vector3 = require('classes/vector3')

-- Initialize variables
local pos1 = vector3:new(0, 0, 0)
local pos2 = vector3:new(0, 0, 0)
local rotation = vector3:new(0, 0, 0)

local typeWidth = 40
local valueWidth = 100
local totalWidth = typeWidth + valueWidth

local function CenteredText(text, width)
    local textWidth = ImGui.CalcTextSize(text)
    local cellWidth = width
    local spacing = (cellWidth - textWidth) * 0.5
    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + spacing)
    ImGui.Text(text)
end


function CETGui()
    -- draw ImGui window
    if ImGui.Begin('VolumetricSelection2077') then
        -- Position Headers
        if ImGui.BeginTable("PositionHeaders", 5, ImGuiTableFlags.SizingFixedFit) then  -- 5 columns: Label, X, Y, Z, Button
            -- Custom header row
            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)  -- Set width for Type column
            CenteredText("Type", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)  -- Set width for X column
            CenteredText("X", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)  -- Set width for Y column
            CenteredText("Y", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)  -- Set width for Z column
            CenteredText("Z", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)  -- Set width for Actions column
            CenteredText("Actions", valueWidth)

            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)  
            CenteredText("Box Point 1", valueWidth)
            -- Absolute Position Row
            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth) 
            CenteredText("Absolute", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            pos1.x = ImGui.DragFloat("##pos1x", pos1.x, 0.004)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            pos1.y = ImGui.DragFloat("##pos1y", pos1.y, 0.004)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            pos1.z = ImGui.DragFloat("##pos1z", pos1.z, 0.004)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            if ImGui.Button("Player Position##1") then
                local currentPos = Game.GetPlayer():GetWorldPosition()
                pos1.x = currentPos.x
                pos1.y = currentPos.y
                pos1.z = currentPos.z
            end

            -- Relative Position Row
            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth) 
            CenteredText("Relative", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            pos1.x = ImGui.DragFloat("##pos1xrel", pos1.x, 0.004)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            pos1.y = ImGui.DragFloat("##pos1yrel", pos1.y, 0.004)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            pos1.z = ImGui.DragFloat("##pos1zrel", pos1.z, 0.004)

            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)  
            CenteredText("Box Point 2", valueWidth)
            -- Absolute Position Row
            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth) 
            CenteredText("Absolute", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            pos2.x = ImGui.DragFloat("##pos2x", pos2.x, 0.004)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            pos2.y = ImGui.DragFloat("##pos2y", pos2.y, 0.004)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            pos2.z = ImGui.DragFloat("##pos2z", pos2.z, 0.004)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            if ImGui.Button("Player Position##2") then
                local currentPos = Game.GetPlayer():GetWorldPosition()
                pos2.x = currentPos.x
                pos2.y = currentPos.y
                pos2.z = currentPos.z
            end

            -- Relative Position Row
            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth) 
            CenteredText("Relative", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            pos2.x = ImGui.DragFloat("##pos2xrel", pos2.x, 0.004)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            pos2.y = ImGui.DragFloat("##pos2yrel", pos2.y, 0.004)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            pos2.z = ImGui.DragFloat("##pos2zrel", pos2.z, 0.004)

            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)  
            CenteredText("Rotation", valueWidth)
            -- Rotation Row
            ImGui.TableNextRow()
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth) 
            CenteredText("Absolute", valueWidth)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            rotation.x = ImGui.DragFloat("##rotx", rotation.x, 0.004)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            rotation.y = ImGui.DragFloat("##roty", rotation.y, 0.004)
            
            ImGui.TableNextColumn()
            ImGui.SetNextItemWidth(valueWidth)
            rotation.z = ImGui.DragFloat("##rotz", rotation.z, 0.004)

            ImGui.EndTable()
        end
        -- Todo: Add Scan RHT button
        -- Add Overflow back into rotation
        -- Add Relative movement logic
        -- Add More visual distinction between rows
        ImGui.End()
    end
end

-- return function
return CETGui