return {
    setup = function(terminal)
        vim.keymap.set('n', '<leader>b', function() vim.cmd('Make build') end)
        vim.cmd("tabnew")
        terminal.create_terminal("default")
        vim.cmd("tabprevious")
    end
}
