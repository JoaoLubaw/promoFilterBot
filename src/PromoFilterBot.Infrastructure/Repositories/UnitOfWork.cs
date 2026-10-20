/*
 * File: UnitOfWork.cs
 * Project: Repositories
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

using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Models;

namespace PromoFilterBot.Infrastructure.Repositories;

/// <summary>
/// Unit of work over the PromoFilterBotDbContext of the scope (an update of the bot or an offer of the matcher).
/// </summary>
public class UnitOfWork : IUnitOfWork {
    private readonly PromoFilterBotDbContext _dbContext;
    private readonly ILogger<UnitOfWork> _logger;
    private readonly List<Func<Task>> _beforeCommitActions = [];
    private readonly List<Func<bool, Task>> _afterCompletionActions = [];
    private IDbContextTransaction? _transaction;

    public UnitOfWork(PromoFilterBotDbContext dbContext, ILogger<UnitOfWork> logger) {
        _dbContext = dbContext;
        _logger = logger;
    }

    public bool HasActiveTransaction => _transaction != null;

    public async Task BeginTransactionAsync() {
        if (_transaction != null) return;

        _transaction = await _dbContext.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync() {
        if (_transaction == null) return;

        try {
            await RunBeforeCommitActionsAsync();
        }
        catch {
            // A write that must exist along with the commit failed, so nothing is saved
            await RollbackAsync();
            throw;
        }

        bool committed = false;

        try {
            await _transaction.CommitAsync();
            committed = true;
        }
        finally {
            // A failed commit leaves nothing saved, so the end actions run as for a rollback
            await EndTransactionAsync(committed);
        }
    }

    public async Task RollbackAsync() {
        if (_transaction == null) return;

        try {
            await _transaction.RollbackAsync();
        }
        finally {
            await EndTransactionAsync(committed: false);
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, Func<T, bool> isSuccessful) {
        if (HasActiveTransaction) {
            return await operation();
        }

        await BeginTransactionAsync();

        T result;

        try {
            result = await operation();
        }
        catch {
            await RollbackAsync();
            throw;
        }

        if (isSuccessful(result)) {
            await CommitAsync();
        }
        else {
            await RollbackAsync();
        }

        return result;
    }

    public void BeforeCommit(Func<Task> action) {
        _beforeCommitActions.Add(action);
    }

    public void AfterCompletion(Func<bool, Task> action) {
        _afterCompletionActions.Add(action);
    }

    private async Task RunBeforeCommitActionsAsync() {
        List<Func<Task>> actions = [.. _beforeCommitActions];
        _beforeCommitActions.Clear();

        foreach (Func<Task> action in actions) {
            await action();
        }
    }

    private async Task EndTransactionAsync(bool committed) {
        await _transaction!.DisposeAsync();
        _transaction = null;

        // On a rollback, the actions for before the commit never run
        _beforeCommitActions.Clear();

        List<Func<bool, Task>> actions = [.. _afterCompletionActions];
        _afterCompletionActions.Clear();

        // The writes are already settled, so a failing action is logged instead of failing the operation
        foreach (Func<bool, Task> action in actions) {
            try {
                await action(committed);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Action registered for the end of a transaction failed (committed: {Committed}).", committed);
            }
        }
    }
}