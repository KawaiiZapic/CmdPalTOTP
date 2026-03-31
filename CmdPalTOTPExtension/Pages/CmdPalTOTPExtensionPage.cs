// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CmdPalTOTPExtension.DataManager;
using CmdPalTOTPExtension.DataModel;
using Genesis.QRCodeLib;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using OtpNet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace CmdPalTOTPExtension;

internal sealed partial class CmdPalTOTPExtensionPage: DynamicListPage {

    private readonly AuthenticatorsList config = Config.Instance.Authenticators;
    public CmdPalTOTPExtensionPage() {
        Icon = IconHelpers.FromRelativePaths("Assets\\icon-light.png", "Assets\\icon-dark.png");
        Title = "TOTP";
        Name = "Open";
    }

    private string currentQuery = "";

    static string DecryptKey(string encrypted) {
        return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(encrypted), null, DataProtectionScope.CurrentUser));
    }
    static string EncryptKey(string unencrypted) {
        return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(unencrypted), null, DataProtectionScope.CurrentUser));
    }

    static IListItem[] ToAuthenticatorResultList(IEnumerable<Authenticator> list) {
        return [..list.ToList().ConvertAll < ListItem > ((authenticator) => {
            var key = authenticator.Key;
            if (authenticator.IsEncrypted) {
                try {
                    key = DecryptKey(key);
                } catch (Exception) {
                    key = null;
                }
            }
            var AuthenticatorInst = new Totp(Base32Encoding.ToBytes(key));
            return new() {
                Title = authenticator.Name,
                    Subtitle = $"Expires in {AuthenticatorInst.RemainingSeconds()}s",
                    Icon = new IconInfo("\uE8C8"),
                    Command = new AnonymousCommand(() => {
                        new CopyTextCommand(AuthenticatorInst.ComputeTotp().ToString()).Invoke();
                    }) {
                        Name = "Copy"
                    },
                    Tags = [
                        new Tag {
                            Text = AuthenticatorInst.ComputeTotp().ToString()
                        }
                    ]
            };
        })];
    }

    static bool CheckKeyValid(string key) {
        try {
            Base32Encoding.ToBytes(key);
            return true;
        } catch (Exception) {
            return false;
        }
    }

    static Authenticator ParseOTPLink(string url) {
        var link = new Uri(url);
        var name = link.LocalPath.ToString()[1..];
        var queries = HttpUtility.ParseQueryString(link.Query);
        var secret = queries.Get("secret") ??
            throw new Exception();
        if (!CheckKeyValid(secret)) {
            throw new Exception();
        }
        return new() {
            Key = EncryptKey(secret),
            Name = name,
            IsEncrypted = true
        };
    }


    IListItem[] HandleGoogleAuthImport(string url) {
        try {
            var uri = new Uri(url);
            var queries = HttpUtility.ParseQueryString(uri.Query);
            var payload = queries.Get("data") ?? throw new Exception();
            var decoded = Payload.Parser.ParseFrom(Convert.FromBase64String(payload));

            return [
                    new ListItem() {
                        Title = string.Format("Import {0} authenticators from Google Authenticator", decoded.OtpParameters.Count),
                        Subtitle = string.Format("Batch {0}/{1}", decoded.BatchIndex + 1, decoded.BatchSize),
                        Command = new AnonymousCommand(() => {
                            foreach (var item in decoded.OtpParameters) {
                                var key = Base32Encoding.ToString(item.Secret.ToByteArray());
                                var name = item.Issuer;
                                if (item.Name.Length > 0) {
                                    if (name.Length > 0) {
                                        name += ": " + item.Name;
                                    } else {
                                        name = item.Name;
                                    }
                                } else {
                                    if (name.Length > 0) {
                                        name += ": <NO NAME>";
                                    } else {
                                        name = "<NO NAME>";
                                    }
                                }
                                config.Authenticators.Add(new () {
                                    Name = name,
                                    Key = EncryptKey(key),
                                    IsEncrypted = true
                                });
                                Config.Instance.Save();
                            }
                        })
                    }
                ];
        } catch (Exception) {
            return [
                    new ListItem() {
                        Title = "Invaild Google Authenticator export link",
                        Icon = new IconInfo("\uED14")
                    }
                ];
        }
    }

    IListItem[] HandleNormalOtpImport(string url) {
        try {
            var entry = ParseOTPLink(url);
            return [
                    new ListItem() {
                        Title = entry.Name,
                        Subtitle = "Import form otpauth:// link",
                        Command = new AnonymousCommand(() => {
                            config.Authenticators.Add(entry);
                            Config.Instance.Save();
                        })
                    }
                ];
        } catch (Exception) {
            return [
                    new ListItem() {
                        Title = "Resource.invalid_otpauth_link",
                        Subtitle = "Resource.invalid_otpauth_link_tip"
                    }
                ];
        }
    }

    IListItem[] ManageCommands => [
        new ListItem(new AnonymousCommand(() => {
            var result = GetSetupURLFromScreen();
            result.ForEach(item => {
                config.Authenticators.Add(ParseOTPLink(item));
            });
        })) {
            Title = "Scan QRCode from screen",
            Icon = new IconInfo("\uED14")
        },
        new ListItem(new AnonymousCommand(() => {
            try {
                var filePath = Path.Combine(Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%") + "\\Microsoft\\PowerToys\\PowerToys Run\\Settings\\Plugins\\Community.PowerToys.Run.Plugin.TOTP\\", "OTPList.json");
                Config.Instance.Load(filePath);
                Config.Instance.Save();
                new ToastStatusMessage(new StatusMessage {
                    Message = $"Imported {config.Authenticators.Count} authenticators from Powertoys Run TOTP.",
                        State = MessageState.Success
                }).Show();
                RaiseItemsChanged();
            } catch {
                new ToastStatusMessage(new StatusMessage {
                    Message = "Failed to load data from Powertoys Run TOTP",
                    State = MessageState.Error
                }).Show();
            }
        }) {
            Result = CommandResult.KeepOpen()
        }) {
            Title = "Import from PowerToys Run TOTP Plugin",
                Icon = new IconInfo("\uE8E5")
        }
    ];

    public override IListItem[] GetItems() {
        if (currentQuery.StartsWith("!")) {
            return ManageCommands;
        } else if (currentQuery.StartsWith("otpauth://totp/")) {
            return HandleNormalOtpImport(currentQuery);
        } else if (currentQuery.StartsWith("otpauth-migration://offline?")) {
            return HandleGoogleAuthImport(currentQuery);
        } else {
            if (string.IsNullOrEmpty(currentQuery)) {
                return ToAuthenticatorResultList(config.Authenticators);
            }
            return ToAuthenticatorResultList(
                config.Authenticators
                .Select(item => (FuzzyStringMatcher.ScoreFuzzy(currentQuery, item.Name), item))
                .Where(item => item.Item1 > 0)
                .OrderByDescending(item => item.Item1)
                .Select(item => item.item)
            );
        }
    }

    static List<string> GetSetupURLFromScreen() {
        var result = new List<string>();
        // TODO: Get display size by something else
        var size = new Size {
            Width = 1920,
            Height = 1080
        };
        var screenBitmap = new Bitmap(size.Width, size.Height);
        using (var g = Graphics.FromImage(screenBitmap)) {
            g.CopyFromScreen(Point.Empty, Point.Empty, size);
        }
        var decoder = new QRDecoder();
        var data = decoder.ImageDecoder(screenBitmap);
        if (data != null) {
            foreach (var item in data) {
                var link = QRCode.ByteArrayToStr(item);
                if (link != null && link.StartsWith("otpauth://totp/")) {
                    result.Add(link);
                }
            }
        }
        return result;
    }

    public override void UpdateSearchText(string oldSearch, string newSearch) {
        currentQuery = newSearch;
        RaiseItemsChanged();
    }
}