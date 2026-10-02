using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AulaPedidos.CourseTools;

public static class SmokeCommand
{
    public static async Task<int> ExecuteAsync(string root, string[] args)
    {
        var baseUrl = "http://localhost:5080";
        if (args.Length != 0)
        {
            if (args.Length != 2 || args[0] != "--base-url")
                throw new ArgumentException("Uso: smoke [--base-url http://localhost:5080]");
            baseUrl = args[1];
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var address)
            || address.Scheme is not ("http" or "https")
            || string.IsNullOrWhiteSpace(address.Host)
            || address.UserInfo.Length != 0 || address.Query.Length != 0 || address.Fragment.Length != 0)
            throw new ArgumentException("--base-url debe ser una URL HTTP(S) absoluta, sin credenciales, query ni fragmento.");

        // A redirect must never forward a token to a different host or hide a wrong endpoint.
        using var handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(address.AbsoluteUri.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        var admin = TokenService.Create(root, "Admin", "instructor", 30);
        var student = TokenService.Create(root, "Student", "alumno", 30);
        var other = TokenService.Create(root, "Student", "otro", 30);
        var caseVariant = TokenService.Create(root, "Student", "ALUMNO", 30);

        await ExpectAsync(client, HttpMethod.Get, "health/ready", HttpStatusCode.OK);
        await ExpectAsync(client, HttpMethod.Get, "api/v1/products", HttpStatusCode.Unauthorized);
        var productInput = new
        {
            sku = "CURSO-" + Guid.NewGuid().ToString("N")[..8],
            name = "Curso de arquitectura",
            price = 125.50m
        };
        await ExpectAsync(client, HttpMethod.Post, "api/v1/products", HttpStatusCode.Forbidden, student, productInput);
        var product = await ReadAsync(client, HttpMethod.Post, "api/v1/products", HttpStatusCode.Created, admin, productInput);
        var productId = ReadGuid(product, "id");
        var orderInput = new { items = new[] { new { productId, quantity = 2 } } };
        var order = await ReadAsync(client, HttpMethod.Post, "api/v1/orders", HttpStatusCode.Created, student, orderInput);
        var orderId = ReadGuid(order, "id");
        var version = ReadGuid(order, "version");
        RequireOrder(order, orderId, "Submitted");
        var orderRoute = $"api/v1/orders/{orderId}";
        var cancelRoute = orderRoute + "/cancel";

        RequireOrder(await ReadAsync(client, HttpMethod.Get, orderRoute, HttpStatusCode.OK, student), orderId, "Submitted");
        await ExpectAsync(client, HttpMethod.Get, orderRoute, HttpStatusCode.Forbidden, other);
        await ExpectAsync(client, HttpMethod.Get, orderRoute, HttpStatusCode.Forbidden, caseVariant);
        await RequireListAsync(client, student, orderId, shouldContain: true);
        await RequireListAsync(client, other, orderId, shouldContain: false);
        await RequireListAsync(client, caseVariant, orderId, shouldContain: false);

        await ExpectAsync(client, HttpMethod.Post, cancelRoute, HttpStatusCode.Forbidden, other, new { version });
        await ExpectAsync(client, HttpMethod.Post, cancelRoute, HttpStatusCode.Forbidden, caseVariant, new { version });
        var unchanged = await ReadAsync(client, HttpMethod.Get, orderRoute, HttpStatusCode.OK, student);
        RequireOrder(unchanged, orderId, "Submitted");
        if (ReadGuid(unchanged, "version") != version)
            throw new InvalidOperationException("Smoke: un intento de cancelación no autorizado cambió la versión del pedido.");
        await ExpectAsync(client, HttpMethod.Post, cancelRoute, HttpStatusCode.Conflict, student, new { version = Guid.NewGuid() });

        var cancelled = await ReadAsync(client, HttpMethod.Post, cancelRoute, HttpStatusCode.OK, student, new { version });
        RequireOrder(cancelled, orderId, "Cancelled");
        if (ReadGuid(cancelled, "version") == version)
            throw new InvalidOperationException("Smoke: cancelar debe cambiar la versión del pedido.");

        Console.WriteLine($"Smoke correcto: 401, altas 201, total 251, acceso y cancelación 403, identidad ordinal, conflicto 409 y cancelación. OrderId={orderId}");
        return 0;
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string route,
        HttpStatusCode expected, string? token, object? body)
    {
        using var request = new HttpRequestMessage(method, route);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ProcessRunner.CancellationToken);
        if (response.StatusCode == expected) return response;
        var received = (int)response.StatusCode;
        response.Dispose();
        // Neither the response payload nor authentication material belongs in diagnostic output.
        throw new InvalidOperationException($"Smoke: {method} {route} esperaba {(int)expected} y recibió {received}.");
    }

    private static async Task ExpectAsync(HttpClient client, HttpMethod method, string route,
        HttpStatusCode expected, string? token = null, object? body = null)
    {
        using var response = await SendAsync(client, method, route, expected, token, body);
    }

    private static async Task<JsonElement> ReadAsync(HttpClient client, HttpMethod method, string route,
        HttpStatusCode expected, string? token = null, object? body = null)
    {
        using var response = await SendAsync(client, method, route, expected, token, body);
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ProcessRunner.CancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: deadline.Token);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException($"Smoke: {route} no devolvió un objeto JSON.");
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            throw new InvalidOperationException($"Smoke: {route} no devolvió JSON válido.");
        }
    }

    private static Guid ReadGuid(JsonElement value, string property)
    {
        if (value.TryGetProperty(property, out var element) && element.ValueKind == JsonValueKind.String
            && element.TryGetGuid(out var result) && result != Guid.Empty)
            return result;
        throw new InvalidOperationException($"Smoke: la respuesta no contiene un {property} válido.");
    }

    private static void RequireOrder(JsonElement order, Guid expectedId, string expectedStatus)
    {
        if (ReadGuid(order, "id") != expectedId
            || !order.TryGetProperty("total", out var total) || total.ValueKind != JsonValueKind.Number
            || !total.TryGetDecimal(out var amount) || amount != 251m
            || !order.TryGetProperty("customerId", out var customer) || customer.ValueKind != JsonValueKind.String
            || !string.Equals(customer.GetString(), "alumno", StringComparison.Ordinal)
            || !order.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.String
            || !string.Equals(status.GetString(), expectedStatus, StringComparison.Ordinal))
            throw new InvalidOperationException("Smoke: el pedido no conserva identificador, propietario, total 251 o estado esperado.");
    }

    private static async Task RequireListAsync(HttpClient client, string token, Guid orderId, bool shouldContain)
    {
        var page = await ReadAsync(client, HttpMethod.Get, "api/v1/orders?pageSize=100", HttpStatusCode.OK, token);
        if (!page.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Smoke: el listado no contiene un arreglo items.");
        var contains = items.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.Object && ReadGuid(item, "id") == orderId);
        if (contains != shouldContain)
            throw new InvalidOperationException("Smoke: falló el aislamiento de propietario o la comparación ordinal en el listado de pedidos.");
    }
}
