-- Function to save a string to a file
local function saveSelectionOutput(content)
    local path = "data/selection.json"
    local file, err = io.open(path, "w")  -- Open the file in write mode
    if not file then
        print("Error opening file:", err)
        return false
    end

    file:write(content)  -- Write the content to the file
    file:close()  -- Close the file
    return true
end

return saveSelectionOutput