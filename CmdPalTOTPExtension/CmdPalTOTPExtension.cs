// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CommandPalette.Extensions;

namespace CmdPalTOTPExtension;

[Guid("4f7519ce-da75-4cdc-9950-86baf0817ca1")]
public sealed partial class CmdPalTOTPExtension(ManualResetEvent extensionDisposedEvent) : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent = extensionDisposedEvent;

    private readonly CmdPalTOTPExtensionCommandsProvider _provider = new();

    public object? GetProvider(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.Commands => _provider,
            _ => null,
        };
    }

    public void Dispose() => this._extensionDisposedEvent.Set();
}