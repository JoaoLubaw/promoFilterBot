/*
 * File: IUnitOfWork.cs
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

namespace PromoFilterBot.Infrastructure.Interfaces;

/// <summary>
/// Groups the writes of an operation in one database transaction, so they are saved only if every step succeeds.
/// The repositories keep calling SaveChanges: inside the transaction their writes are only visible to the operation
/// until it is committed.
/// </summary>
public interface IUnitOfWork {
    bool HasActiveTransaction { get; }

    /// <summary>
    /// Opens a transaction, or keeps using the one already open.
    /// </summary>
    Task BeginTransactionAsync();

    /// <summary>
    /// Runs the actions registered for before the commit, saves the writes of the open transaction and runs the actions
    /// registered for its end. If an action before the commit fails, the transaction is rolled back and the failure is
    /// thrown.
    /// </summary>
    Task CommitAsync();

    /// <summary>
    /// Undoes the writes of the open transaction and runs the actions registered for its end.
    /// </summary>
    Task RollbackAsync();

    /// <summary>
    /// Runs an operation in a transaction: committed when the result is successful, rolled back otherwise (or when it
    /// throws). Inside a transaction already open, the operation joins it, and its owner decides how it ends.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, Func<T, bool> isSuccessful);

    /// <summary>
    /// Registers an action to run right before the open transaction is committed, and only if it is.
    /// </summary>
    void BeforeCommit(Func<Task> action);

    /// <summary>
    /// Registers an action to run when the open transaction ends, with whether it was committed. It is how side effects
    /// outside the database (the notification queue) wait for the outcome of the writes.
    /// </summary>
    void AfterCompletion(Func<bool, Task> action);
}