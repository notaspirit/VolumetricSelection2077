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
    else
        print("Unhandled type:", type(value), "Value:", value)
        return "null"  -- Handle nil values
    end
end

local jsonUtils = {
    TableToJSON = TableToJSON
}

return jsonUtils