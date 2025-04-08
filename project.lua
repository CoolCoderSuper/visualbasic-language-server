return {
    setup = function(terminal, make)
        vim.keymap.set('n', '<leader>b', function() make.run('build') end)
        vim.cmd("tabnew")
        terminal.create_terminal("default")
        vim.cmd("tabprevious")
    end
}
