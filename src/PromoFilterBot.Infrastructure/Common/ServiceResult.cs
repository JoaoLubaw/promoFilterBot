/*
 * File: ServiceResult.cs
 * Project: Common
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

namespace PromoFilterBot.Infrastructure.Common;

public enum ResultCode {
    NO_ERROR = 200,

    // Permission Errors
    UNREGISTERED_USER = 1001,
    NOT_ADMIN = 1002,

    // Validation Errors
    EMPTY_REQUEST = 2001,
    INVALID_ALERT_TEXT = 2002,
    ALERT_TEXT_TOO_LONG = 2003,
    INVALID_MAX_PRICE = 2004,
    ACTIVE_ALERT_LIMIT_REACHED = 2005,
    ALERT_LIMIT_REACHED = 2006,
    INVALID_CHANNEL_REFERENCE = 2008,
    PRIVATE_CHANNEL_LINK = 2009,
    INVALID_PAUSE_DURATION = 2010,
    INVALID_ALERT_POSITION = 2011,
    EMPTY_OFFER = 2012,

    // Entity Errors
    ENTITY_NOT_FOUND = 3000,
    ALERT_NOT_FOUND = 3001,
    CHANNEL_NOT_FOUND = 3002,
    CHANNEL_ALREADY_MONITORED = 3003,
    CHANNEL_NOT_MONITORED = 3006,
    MESSAGE_ALREADY_PROCESSED = 3007,
    NOTIFICATION_NOT_FOUND = 3008,
    NOTIFICATION_ALREADY_HANDLED = 3009,

    // System Errors
    UNHANDLED_ERROR = 8000
}

/// <summary>
/// Outcome of a service call: a result code and the data to show. An expected failure (missing data, no permission,
/// entity not found) is a result code, never an exception.
/// </summary>
public class ServiceResult<T> {
    public ServiceResult(ResultCode code, T? data) {
        Code = code;
        Data = data;
    }

    public ResultCode Code { get; }

    public T? Data { get; }

    public bool IsSuccess => Code == ResultCode.NO_ERROR;
}

/// <summary>
/// Data of a ServiceResult that has nothing to return.
/// </summary>
public class EmptyDto {
    public static EmptyDto Instance { get; } = new EmptyDto();
}