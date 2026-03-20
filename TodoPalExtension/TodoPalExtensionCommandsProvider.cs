// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace TodoPalExtension;

public partial class TodoPalExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public TodoPalExtensionCommandsProvider()
    {
        DisplayName = "TodoPal";
        Icon = new IconInfo(new FontIconData("\uE73E", "Segoe Fluent Icons")); // checkmark
        _commands = [
            new CommandItem(new TodoPalExtensionPage()) { Title = DisplayName },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
