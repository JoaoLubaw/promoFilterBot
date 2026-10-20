/*
 * File: IReaderSession.cs
 * Project: Interfaces
 * Create Date: Tuesday, 20th October 2026
 * Author: João Lubawaski (joaolubawaski@gmail.com)
 * -----
 * Last Modified: Tuesday, 20th October 2026 4:30:00 pm
 * Modified by: João Lubawaski (joaolubawaski@gmail.com)
 * -----
 * Copyright (c) 2026 João Lubawaski
 * ----
 * HISTORY:
 * Date      	By	Comments
 * ----------	---	---------------------------------------------------------
 */

using PromoFilterBot.Infrastructure.Dto;

using TL;

namespace PromoFilterBot.Reader.Interfaces;

/// <summary>
/// The connection of the reader account. There is one client per session file, shared by the reader and the join
/// workers.
/// </summary>
public interface IReaderSession {
    bool IsConnected { get; }

    // Connects and logs in with the saved session, without asking anything (a missing session throws
    // ReaderLoginRequiredException), and starts sending the updates to the handler. Only the monitored channels are
    // loaded: the chats of the account are never listed.
    Task<User> ConnectAsync(Func<Update, Task> onUpdate, IReadOnlyCollection<ChannelPeerDto> channels, CancellationToken cancellationToken);

    // Completes when the connection is lost for good.
    Task WaitForDisconnectionAsync(CancellationToken cancellationToken);

    Task<WTelegram.Client> WaitUntilReadyAsync(CancellationToken cancellationToken);

    // Saves the update state, so a restart fetches what was posted meanwhile.
    void SaveState();

    Task DisconnectAsync();
}