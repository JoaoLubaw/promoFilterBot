/*
 * File: StubHttpHandler.cs
 * Project: Helpers
 * Create Date: Tuesday, 20th October 2026
 * Author: João Lubawaski (joaolubawaski@gmail.com)
 * -----
 * Last Modified: Tuesday, 20th October 2026 7:00:00 pm
 * Modified by: João Lubawaski (joaolubawaski@gmail.com)
 * -----
 * Copyright (c) 2026 João Lubawaski
 * ----
 * HISTORY:
 * Date      	By	Comments
 * ----------	---	---------------------------------------------------------
 */

using System.Net;

namespace PromoFilterBot.Tests.Helpers;

/// <summary>
/// Answers the requests of an HttpClient with the responses given for each address, and records them.
/// </summary>
public sealed class StubHttpHandler : HttpMessageHandler {
    private readonly Dictionary<string, Func<HttpResponseMessage>> _responses = new Dictionary<string, Func<HttpResponseMessage>>(StringComparer.Ordinal);

    public List<string> Requests { get; } = [];

    public StubHttpHandler Redirect(string from, string to, HttpStatusCode status = HttpStatusCode.Found) {
        _responses[from] = () => {
            HttpResponseMessage response = new HttpResponseMessage(status);
            response.Headers.Location = new Uri(to);
            return response;
        };

        return this;
    }

    public StubHttpHandler Page(string url, string html) {
        _responses[url] = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(html) };

        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        string url = request.RequestUri!.AbsoluteUri;
        Requests.Add(url);

        HttpResponseMessage response = _responses.TryGetValue(url, out Func<HttpResponseMessage>? respond)
            ? respond()
            : new HttpResponseMessage(HttpStatusCode.NotFound);

        return Task.FromResult(response);
    }
}