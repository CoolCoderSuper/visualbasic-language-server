return {
    setup = function()
        local make = require('make-runner')
        local terminal = require('terminal')
        vim.keymap.set('n', '<leader>b', function() make.run('build') end)
        vim.cmd("tabnew")
        terminal.create_terminal("default")
        vim.cmd("tabprevious")
    end
}
