// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPalTOTPExtension;

public partial class CmdPalTOTPExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public CmdPalTOTPExtensionCommandsProvider()
    {
        DisplayName = "TOTP";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        _commands = [
            new CommandItem(new CmdPalTOTPExtensionPage()) { Title = DisplayName },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
