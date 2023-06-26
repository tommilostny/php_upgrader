﻿// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "MA0052:Replace constant Enum.ToString with nameof", Justification = "ColoredConsole", Scope = "member", Target = "~M:FtpSync.FtpSynchronizer.SynchronizeAsync~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "MA0052:Replace constant Enum.ToString with nameof", Justification = "ColoredConsole", Scope = "member", Target = "~M:FtpSync.FtpBase.ConnectClientAsync(FluentFTP.AsyncFtpClient)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "MA0052:Replace constant Enum.ToString with nameof", Justification = "ColoredConsole", Scope = "member", Target = "~M:FtpSync.FtpSynchronizer.DownloadAndUploadAsync(FluentFTP.AsyncFtpClient,FluentFTP.AsyncFtpClient,System.String,System.String,System.Int32)~System.Threading.Tasks.Task")]