-- mod info
mod = {
    ready = false
}

-- print on load
print('My Mod is loaded!')

-- onInit event
registerForEvent('onInit', function() 
    
    -- set as ready
    mod.ready = true
    
    -- print on initialize
    print('My Mod is initialized!')
    
end)

-- return mod info 
-- for communication between mods
return mod