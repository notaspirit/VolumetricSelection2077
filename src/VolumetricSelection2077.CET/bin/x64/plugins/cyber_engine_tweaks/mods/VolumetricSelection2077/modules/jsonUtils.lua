
-- Helper function to escape special characters in strings
local function escapeString(str)
    return str:gsub("\\", "\\\\")  -- Escape backslashes
              :gsub('"', '\\"')    -- Escape double quotes
              :gsub("\b", "\\b")   -- Escape backspace
              :gsub("\f", "\\f")   -- Escape form feed
              :gsub("\n", "\\n")   -- Escape newline
              :gsub("\r", "\\r")   -- Escape carriage return
              :gsub("\t", "\\t")   -- Escape tab
end

-- Converts a table to a JSON string
function TableToJSON(value, indentLevel)
    indentLevel = indentLevel or 0
    local indent = string.rep("  ", indentLevel)  -- Two spaces per indent level
    local nextIndent = string.rep("  ", indentLevel + 1)

    if type(value) == "table" then
        local jsonStr = {}
        local isArray = #value > 0  -- Check if it's an array (list)

        for key, val in pairs(value) do
            if isArray then
                jsonStr[#jsonStr + 1] = nextIndent .. TableToJSON(val, indentLevel + 1)  -- Append JSON string for array
            else
                jsonStr[#jsonStr + 1] = string.format('%s"%s": %s', nextIndent, tostring(key), TableToJSON(val, indentLevel + 1))
            end
        end

        if isArray then
            return "[\n" .. table.concat(jsonStr, ",\n") .. "\n" .. indent .. "]"  -- Array format
        else
            return "{\n" .. table.concat(jsonStr, ",\n") .. "\n" .. indent .. "}"  -- Object format
        end
    elseif type(value) == "string" then
        return string.format('"%s"', escapeString(value))  -- Use escapeString to handle special characters
    elseif type(value) == "number" or type(value) == "boolean" then
        return tostring(value)  -- Directly return numbers and booleans
    elseif value.__type == "visualizationBox" or value.__type == "box" then
        return TableToJSON(value:toTable(), indentLevel)
    else
        return "null"  -- Handle nil values
    end
end

-- Convert JSON string to Lua table
function JSONToTable(jsonStr)
    -- Remove whitespace
    jsonStr = jsonStr:gsub("^%s*(.-)%s*$", "%1")
    
    local pos = 1
    
    local function parseValue()
        local char = jsonStr:sub(pos, pos)
        
        -- Parse null
        if jsonStr:sub(pos, pos + 3) == "null" then
            pos = pos + 4
            return nil
        end
        
        -- Parse boolean
        if jsonStr:sub(pos, pos + 3) == "true" then
            pos = pos + 4
            return true
        end
        if jsonStr:sub(pos, pos + 4) == "false" then
            pos = pos + 5
            return false
        end
        
        -- Parse number
        local num = jsonStr:match("^-?%d+%.?%d*[eE]?[+-]?%d*", pos)
        if num then
            pos = pos + #num
            return tonumber(num)
        end
        
        -- Parse string
        if char == '"' then
            local value = ""
            pos = pos + 1
            while pos <= #jsonStr do
                char = jsonStr:sub(pos, pos)
                if char == '"' then
                    pos = pos + 1
                    return value
                end
                if char == '\\' then
                    pos = pos + 1
                    char = jsonStr:sub(pos, pos)
                    if char == 'n' then char = '\n'
                    elseif char == 'r' then char = '\r'
                    elseif char == 't' then char = '\t'
                    elseif char == 'b' then char = '\b'
                    elseif char == 'f' then char = '\f'
                    end
                end
                value = value .. char
                pos = pos + 1
            end
        end
        
        -- Parse array
        if char == '[' then
            pos = pos + 1
            local arr = {}
            while pos <= #jsonStr do
                char = jsonStr:sub(pos, pos)
                if char == ']' then
                    pos = pos + 1
                    return arr
                end
                if char ~= ',' and char ~= ' ' and char ~= '\n' and char ~= '\r' and char ~= '\t' then
                    table.insert(arr, parseValue())
                end
                pos = pos + 1
            end
        end
        
        -- Parse object
        if char == '{' then
            pos = pos + 1
            local obj = {}
            while pos <= #jsonStr do
                char = jsonStr:sub(pos, pos)
                if char == '}' then
                    pos = pos + 1
                    return obj
                end
                if char == '"' then
                    local key = parseValue()
                    -- Skip whitespace and colon
                    while jsonStr:sub(pos, pos):match("[ :\n\r\t]") do
                        pos = pos + 1
                    end
                    obj[key] = parseValue()
                end
                pos = pos + 1
            end
        end
    end
    
    return parseValue()
end


local jsonUtils = {
    TableToJSON = TableToJSON,
    JSONToTable = JSONToTable
}

return jsonUtils