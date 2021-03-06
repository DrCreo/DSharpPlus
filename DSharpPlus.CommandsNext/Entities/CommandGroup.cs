﻿using DSharpPlus.CommandsNext.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext.Attributes;

namespace DSharpPlus.CommandsNext
{
    /// <summary>
    /// Represents a command group.
    /// </summary>
    public class CommandGroup : Command
    {
        /// <summary>
        /// Gets all the commands that belong to this module.
        /// </summary>
        public IReadOnlyList<Command> Children { get; internal set; }

        internal CommandGroup() : base() { }

        /// <summary>
        /// Executes this command or its subcommand with specified context.
        /// </summary>
        /// <param name="ctx">Context to execute the command in.</param>
        /// <returns>Command's execution results.</returns>
        public override async Task<CommandResult> ExecuteAsync(CommandContext ctx)
        {
            //var cn = ctx.RawArguments.FirstOrDefault();
            var cn = CommandsNextUtilities.ExtractNextArgument(ctx.RawArgumentString, out var x);

            if (x != null)
            {
                var xi = 0;
                for (; xi < x.Length; xi++)
                    if (!char.IsWhiteSpace(x[xi]))
                        break;
                if (xi > 0)
                    x = x.Substring(xi);
            }

            if (cn != null)
            {
                Command cmd = null;
                if (ctx.Config.CaseSensitive)
                    cmd = this.Children.FirstOrDefault(xc => xc.Name == cn || (xc.Aliases != null && xc.Aliases.Contains(cn)));
                else
                    cmd = this.Children.FirstOrDefault(xc => xc.Name.ToLower() == cn.ToLower() || (xc.Aliases != null && xc.Aliases.Select(xs => xs.ToLower()).Contains(cn.ToLower())));

                if (cmd != null)
                {
                    // pass the execution on
                    var xctx = new CommandContext
                    {
                        Client = ctx.Client,
                        Message = ctx.Message,
                        Command = cmd,
                        Config = ctx.Config,
                        RawArgumentString = x,
                        CommandsNext = ctx.CommandsNext,
                        Dependencies = ctx.Dependencies
                    };
                    
                    var fchecks = new List<CheckBaseAttribute>();
                    if (cmd.ExecutionChecks != null && cmd.ExecutionChecks.Any())
                        foreach (var ec in cmd.ExecutionChecks)
                            if (!(await ec.CanExecute(xctx)))
                                fchecks.Add(ec);
                    if (fchecks.Any())
                        return new CommandResult
                        {
                            IsSuccessful = false,
                            Exception = new ChecksFailedException("One or more pre-execution checks failed.", cmd, xctx, fchecks),
                            Context = xctx
                        };
                    
                    return await cmd.ExecuteAsync(xctx);
                }
            }

            if (this.Callable == null)
                return new CommandResult
                {
                    IsSuccessful = false,
                    Exception = new NotSupportedException("No matching subcommands were found, and this group is not executable."),
                    Context = ctx
                };

            return await base.ExecuteAsync(ctx);
        }
    }
}
