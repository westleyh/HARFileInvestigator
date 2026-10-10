using System.Globalization;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace HARFileInvestigator
{
    internal static class HarTraceLoader
    {
        public static async Task<List<HarTraceEntry>> LoadAsync(string filePath)
        {
            await using var stream = File.OpenRead(filePath);
            using var document = await JsonDocument.ParseAsync(stream);

            if (!document.RootElement.TryGetProperty("log", out var logElement) ||
                !logElement.TryGetProperty("entries", out var entriesElement) ||
                entriesElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException("Invalid HAR format. Expected `log.entries` array.");
            }

            var results = new List<HarTraceEntry>();

            var entryIndex = 0;
            foreach (var entry in entriesElement.EnumerateArray())
            {
                results.Add(ParseEntry(entry, entryIndex));
                entryIndex++;
            }

            return results;
        }

        private static HarTraceEntry ParseEntry(JsonElement entry, int entryIndex)
        {
            var startedDateTimeText = GetString(entry, "startedDateTime");
            var startedDateTime = DateTimeOffset.TryParse(startedDateTimeText, out var parsedDateTime)
                ? parsedDateTime
                : DateTimeOffset.MinValue;

            var durationMs = GetDouble(entry, "time");

            var request = entry.TryGetProperty("request", out var requestElement) ? requestElement : default;
            var response = entry.TryGetProperty("response", out var responseElement) ? responseElement : default;
            var serverIpAddress = GetString(entry, "serverIPAddress");

            var method = GetString(request, "method");
            var url = GetString(request, "url");
            var host = TryGetHost(url);
            var requestHttpVersion = GetString(request, "httpVersion");

            var status = GetInt(response, "status");
            var statusText = GetString(response, "statusText");
            var mimeType = response.TryGetProperty("content", out var contentElement)
                ? GetString(contentElement, "mimeType")
                : string.Empty;
            var responseHttpVersion = GetString(response, "httpVersion");
            var bodySize = GetInt(response, "bodySize");

            var requestHeaders = ReadNameValueArray(request, "headers");
            var requestQueryString = ReadNameValueArray(request, "queryString");
            var responseHeaders = ReadNameValueArray(response, "headers");
            var additionalFields = BuildAdditionalFields(entry, requestHeaders, responseHeaders);
            var javaScriptStack = ReadJavaScriptStack(entry);

            var requestText = BuildRequestText(
                method,
                url,
                requestHttpVersion,
                requestHeaders,
                requestQueryString,
                request);

            var responseText = BuildResponseText(
                status,
                statusText,
                responseHttpVersion,
                mimeType,
                bodySize,
                responseHeaders,
                response);

            var searchText = string.Join(
                ' ',
                startedDateTime.ToString("O", CultureInfo.InvariantCulture),
                method,
                url,
                host,
                status.ToString(CultureInfo.InvariantCulture),
                statusText,
                mimeType,
                serverIpAddress,
                requestText,
                responseText);

            return new HarTraceEntry
            {
                EntryIndex = entryIndex,
                StartedDateTime = startedDateTime,
                Method = method,
                Url = url,
                Host = host,
                Status = status,
                StatusText = statusText,
                MimeType = mimeType,
                DurationMs = durationMs,
                IpAddress = serverIpAddress,
                AdditionalFields = additionalFields,
                JavaScriptStack = javaScriptStack,
                RequestText = requestText,
                ResponseText = responseText,
                SearchText = searchText
            };
        }

        private static string ReadJavaScriptStack(JsonElement entry)
        {
            if (!TryGetProperty(entry, "_initiator", out var initiator) &&
                !TryGetProperty(entry, "initiator", out initiator))
            {
                return string.Empty;
            }

            if (!TryGetProperty(initiator, "stack", out var stack))
            {
                return string.Empty;
            }

            var lines = new List<string>();
            AppendStackFrames(stack, lines);
            return string.Join(Environment.NewLine, lines);
        }

        private static void AppendStackFrames(JsonElement stack, List<string> lines)
        {
            if (stack.ValueKind == JsonValueKind.String)
            {
                var text = stack.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    lines.Add(text.TrimEnd());
                }
                return;
            }

            if (TryGetProperty(stack, "description", out var description) && description.ValueKind == JsonValueKind.String)
            {
                var text = description.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    lines.Add(text.TrimEnd());
                }
            }

            if (TryGetProperty(stack, "callFrames", out var frames) && frames.ValueKind == JsonValueKind.Array)
            {
                foreach (var frame in frames.EnumerateArray())
                {
                    var formattedFrame = FormatStackFrame(frame);
                    if (!string.IsNullOrWhiteSpace(formattedFrame))
                    {
                        lines.Add(formattedFrame);
                    }
                }
            }
            else if (TryGetProperty(stack, "frames", out var alternateFrames) && alternateFrames.ValueKind == JsonValueKind.Array)
            {
                foreach (var frame in alternateFrames.EnumerateArray())
                {
                    var formattedFrame = FormatStackFrame(frame);
                    if (!string.IsNullOrWhiteSpace(formattedFrame))
                    {
                        lines.Add(formattedFrame);
                    }
                }
            }

            if (TryGetProperty(stack, "parent", out var parent) && parent.ValueKind == JsonValueKind.Object)
            {
                var asyncCause = GetString(stack, "parentDescription");
                if (!string.IsNullOrWhiteSpace(asyncCause))
                {
                    lines.Add($"--- {asyncCause} ---");
                }

                AppendStackFrames(parent, lines);
            }
        }

        private static string FormatStackFrame(JsonElement frame)
        {
            var functionName = GetString(frame, "functionName");
            var url = GetString(frame, "url");
            var hasLineNumber = TryGetIntValue(frame, "lineNumber", out var lineNumber);
            var hasColumnNumber = TryGetIntValue(frame, "columnNumber", out var columnNumber);
            if (string.IsNullOrWhiteSpace(functionName))
            {
                functionName = "(anonymous)";
            }

            var location = url;
            if (hasLineNumber)
            {
                location += $":{lineNumber + 1}";
                if (hasColumnNumber)
                {
                    location += $":{columnNumber + 1}";
                }
            }

            return string.IsNullOrWhiteSpace(location)
                ? $"    at {functionName}"
                : $"    at {functionName} ({location})";
        }

        private static bool TryGetIntValue(JsonElement parent, string propertyName, out int result)
        {
            result = 0;
            if (!TryGetProperty(parent, propertyName, out var value))
            {
                return false;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out result))
            {
                return true;
            }

            return value.ValueKind == JsonValueKind.String &&
                   int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
        {
            value = default;
            if (element.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            return false;
        }

        private static Dictionary<string, string> BuildAdditionalFields(
            JsonElement entry,
            IReadOnlyList<KeyValuePair<string, string>> requestHeaders,
            IReadOnlyList<KeyValuePair<string, string>> responseHeaders)
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in entry.EnumerateObject())
            {
                if (property.NameEquals("request") ||
                    property.NameEquals("response") ||
                    property.NameEquals("cache") ||
                    property.NameEquals("timings"))
                {
                    continue;
                }

                if (TryReadSimpleValue(property.Value, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    fields[$"Session.{property.Name}"] = value;
                }
            }

            foreach (var header in requestHeaders)
            {
                if (!string.IsNullOrWhiteSpace(header.Key))
                {
                    fields[$"ReqHeader.{header.Key}"] = header.Value;
                }
            }

            foreach (var header in responseHeaders)
            {
                if (!string.IsNullOrWhiteSpace(header.Key))
                {
                    fields[$"RespHeader.{header.Key}"] = header.Value;
                }
            }

            return fields;
        }

        private static bool TryReadSimpleValue(JsonElement value, out string text)
        {
            text = string.Empty;
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    text = value.GetString() ?? string.Empty;
                    return true;
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    text = value.GetRawText();
                    return true;
                default:
                    return false;
            }
        }

        private static string BuildRequestText(
            string method,
            string url,
            string requestHttpVersion,
            IReadOnlyList<KeyValuePair<string, string>> requestHeaders,
            IReadOnlyList<KeyValuePair<string, string>> requestQueryString,
            JsonElement request)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"{method} {url} {requestHttpVersion}");
            sb.AppendLine();

            sb.AppendLine();
            sb.AppendLine("Request Headers:");
            AppendNameValueLines(sb, requestHeaders);

            sb.AppendLine();
            sb.AppendLine("Query String:");
            AppendNameValueLines(sb, requestQueryString);

            if (request.ValueKind != JsonValueKind.Undefined &&
                request.TryGetProperty("postData", out var postData))
            {
                var requestMimeType = GetString(postData, "mimeType");
                var requestBody = GetString(postData, "text");

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    requestBody = GetPostDataParamsText(postData);
                }

                sb.AppendLine();
                sb.AppendLine($"Body MIME Type: {requestMimeType}");
                sb.AppendLine("Request Body:");
                sb.AppendLine(string.IsNullOrWhiteSpace(requestBody) ? "<empty>" : requestBody);
            }

            return sb.ToString();
        }

        private static string BuildResponseText(
            int status,
            string statusText,
            string responseHttpVersion,
            string mimeType,
            int bodySize,
            IReadOnlyList<KeyValuePair<string, string>> responseHeaders,
            JsonElement response)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"{responseHttpVersion} {status} {statusText}");
            sb.AppendLine();
            sb.AppendLine($"MIME Type    : {mimeType}");
            sb.AppendLine($"Body Size    : {bodySize}");

            sb.AppendLine();
            sb.AppendLine("Response Headers:");
            AppendNameValueLines(sb, responseHeaders);

            var responseBody = GetResponseBodyText(response);
            sb.AppendLine();
            sb.AppendLine("Response Body:");
            sb.AppendLine(string.IsNullOrWhiteSpace(responseBody) ? "<empty>" : responseBody);

            return sb.ToString();
        }

        private static string GetPostDataParamsText(JsonElement postData)
        {
            if (!postData.TryGetProperty("params", out var paramsElement) ||
                paramsElement.ValueKind != JsonValueKind.Array)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var item in paramsElement.EnumerateArray())
            {
                var name = GetString(item, "name");
                var value = GetString(item, "value");

                if (!string.IsNullOrWhiteSpace(name))
                {
                    sb.AppendLine($"{name}={value}");
                }
            }

            return sb.ToString().TrimEnd();
        }

        private static string GetResponseBodyText(JsonElement response)
        {
            if (response.ValueKind == JsonValueKind.Undefined ||
                !response.TryGetProperty("content", out var content))
            {
                return string.Empty;
            }

            var text = GetString(content, "text");
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var encoding = GetString(content, "encoding");
            if (!string.Equals(encoding, "base64", StringComparison.OrdinalIgnoreCase))
            {
                return text;
            }

            try
            {
                var bytes = Convert.FromBase64String(text);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return text;
            }
        }

        private static void AppendNameValueLines(StringBuilder sb, IReadOnlyList<KeyValuePair<string, string>> values)
        {
            if (values.Count == 0)
            {
                sb.AppendLine("  <none>");
                return;
            }

            foreach (var item in values)
            {
                sb.AppendLine($"  {item.Key}: {item.Value}");
            }
        }

        private static IReadOnlyList<KeyValuePair<string, string>> ReadNameValueArray(JsonElement parent, string propertyName)
        {
            if (parent.ValueKind == JsonValueKind.Undefined ||
                !parent.TryGetProperty(propertyName, out var arrayElement) ||
                arrayElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var items = new List<KeyValuePair<string, string>>();
            foreach (var element in arrayElement.EnumerateArray())
            {
                var name = GetString(element, "name");
                var value = GetString(element, "value");

                if (!string.IsNullOrWhiteSpace(name))
                {
                    items.Add(new KeyValuePair<string, string>(name, value));
                }
            }

            return items;
        }

        private static string TryGetHost(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                ? uri.Host
                : string.Empty;
        }

        private static string GetString(JsonElement parent, string propertyName)
        {
            if (parent.ValueKind == JsonValueKind.Undefined || !parent.TryGetProperty(propertyName, out var value))
            {
                return string.Empty;
            }

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? string.Empty,
                JsonValueKind.Number => value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => string.Empty
            };
        }

        private static int GetInt(JsonElement parent, string propertyName)
        {
            if (parent.ValueKind == JsonValueKind.Undefined || !parent.TryGetProperty(propertyName, out var value))
            {
                return 0;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
            {
                return intValue;
            }

            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return 0;
        }

        private static double GetDouble(JsonElement parent, string propertyName)
        {
            if (parent.ValueKind == JsonValueKind.Undefined || !parent.TryGetProperty(propertyName, out var value))
            {
                return 0D;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var doubleValue))
            {
                return doubleValue;
            }

            if (value.ValueKind == JsonValueKind.String &&
                double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return 0D;
        }
    }
}
