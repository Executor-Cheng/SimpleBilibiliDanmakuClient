using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
#if NETSTANDARD2_0
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using System.IO;
using System.Text;
using System.Text.Json;
#if NET5_0_OR_GREATER
using System.Net.Http.Json;
#endif
#endif

namespace SimpleBilibiliDanmakuClient.Extensions
{
    /// <summary>
    /// Contains the extensions methods for easily performing request or handling response in HttpClient.
    /// </summary>
    public static partial class HttpClientExtensions
    {
        private static Version DefaultHttpVersion { get; } = new Version(2, 0);

        private static readonly MediaTypeHeaderValue DefaultJsonMediaType = new("application/json") { CharSet = "utf-8" };

        /// <param name="client">The <see cref="HttpClient"/>.</param>
        /// <param name="method">The HTTP method.</param>
        /// <param name="uri">The Uri to request.</param>
        /// <param name="content">The contents of the HTTP message.</param>
        /// <param name="token">A <see cref="CancellationToken"/> which may be used to cancel the request operation.</param>
        /// <inheritdoc cref="HttpClient.SendAsync(HttpRequestMessage, CancellationToken)"/>
        public static async Task<HttpResponseMessage> SendAsync(this HttpClient client, HttpMethod method, Uri uri, HttpContent? content, CancellationToken token = default)
        {
            using HttpRequestMessage request = new HttpRequestMessage(method, uri)
            {
                Content = content,
                Version = DefaultHttpVersion
            };
            return await client.SendAsync(request, token).ConfigureAwait(false);
        }

#if NET5_0_OR_GREATER
        /// <param name="responseTask">An asynchronous operation that represents the HTTP response.</param>
        /// <param name="token">A <see cref="CancellationToken"/> which may be used to cancel the serialize operation.</param>
        /// <inheritdoc cref="HttpContent.ReadAsByteArrayAsync()"/>
        public static async Task<byte[]> GetBytesAsync(this Task<HttpResponseMessage> responseTask, CancellationToken token = default)
        {
            using HttpResponseMessage response = await responseTask.ConfigureAwait(false);
            return await response.Content.ReadAsByteArrayAsync(token);
        }

        /// <param name="responseTask">An asynchronous operation that represents the HTTP response.</param>
        /// <param name="token">A <see cref="CancellationToken"/> which may be used to cancel the serialize operation.</param>
        /// <inheritdoc cref="HttpContent.ReadAsStringAsync()"/>
        public static async Task<string> GetStringAsync(this Task<HttpResponseMessage> responseTask, CancellationToken token = default)
        {
            using HttpResponseMessage response = await responseTask.ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync(token);
        }
#else
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0060 // Remove unused parameter
        /// <param name="responseTask">An asynchronous operation that represents the HTTP response.</param>
        /// <param name="token">The <paramref name="token"/> is ignored since <see cref="HttpContent.ReadAsByteArrayAsync()"/> has no reload that uses <see cref="CancellationToken"/>.</param>
        /// <inheritdoc cref="HttpContent.ReadAsByteArrayAsync()"/>
        public static async Task<byte[]> GetBytesAsync(this Task<HttpResponseMessage> responseTask, CancellationToken token = default)
        {
            using HttpResponseMessage response = await responseTask.ConfigureAwait(false);
            return await response.Content.ReadAsByteArrayAsync();
        }

        /// <param name="responseTask">An asynchronous operation that represents the HTTP response.</param>
        /// <param name="token">The <paramref name="token"/> is ignored since <see cref="HttpContent.ReadAsStringAsync()"/> has no reload that uses <see cref="CancellationToken"/>.</param>
        /// <inheritdoc cref="HttpContent.ReadAsStringAsync()"/>
        public static async Task<string> GetStringAsync(this Task<HttpResponseMessage> responseTask, CancellationToken token = default)
        {
            using HttpResponseMessage response = await responseTask.ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync();
        }
#pragma warning restore IDE0060
#pragma warning restore IDE0079
#endif
#if NET5_0_OR_GREATER
        private static Encoding? GetEncoding(string? charset)
        {
            Encoding? encoding = null;

            if (charset != null)
            {
                try
                {
                    // Remove at most a single set of quotes.
                    if (charset.Length > 2 && charset[0] == '\"' && charset[^1] == '\"')
                    {
                        encoding = Encoding.GetEncoding(charset[1..^1]);
                    }
                    else
                    {
                        encoding = Encoding.GetEncoding(charset);
                    }
                }
                catch (ArgumentException e)
                {
                    throw new InvalidOperationException("The character set provided in ContentType is invalid.", e);
                }
            }

            return encoding;
        }

        /// <inheritdoc cref="GetObjectAsync{T}(Task{HttpResponseMessage}, JsonSerializerOptions?, CancellationToken)"/>
        public static Task<T?> GetObjectAsync<T>(this Task<HttpResponseMessage> responseTask, CancellationToken token = default)
            => responseTask.GetObjectAsync<T?>(null, token);

        /// <summary>
        /// Deserializes the HTTP content to an instance of <typeparamref name="T"/> as an asynchronous operation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="responseTask">An asynchronous operation that represents the HTTP response.</param>
        /// <param name="options">A <see cref="JsonSerializerOptions"/> to be used while deserializing the HTTP content.</param>
        /// <param name="token">A <see cref="CancellationToken"/> which may be used to cancel the deserialize operation.</param>
        /// <returns>A task that represents the asynchronous deserialize operation.</returns>
        public static async Task<T?> GetObjectAsync<T>(this Task<HttpResponseMessage> responseTask, JsonSerializerOptions? options, CancellationToken token = default)
        {
            using HttpResponseMessage response = await responseTask.ConfigureAwait(false);
            return await response.Content.ReadFromJsonAsync<T?>(options, token);
        }

        /// <inheritdoc cref="GetObjectAsync(Task{HttpResponseMessage}, Type, JsonSerializerOptions?, CancellationToken)"/>
        public static Task<object?> GetObjectAsync(this Task<HttpResponseMessage> responseTask, Type returnType, CancellationToken token = default)
            => responseTask.GetObjectAsync(returnType, null, token);

        /// <summary>
        /// Deserializes the HTTP content to an instance of <paramref name="returnType"/> as an asynchronous operation.
        /// </summary>
        /// <param name="returnType">The type of the HTTP content to convert to and return.</param>
        /// <inheritdoc cref="GetObjectAsync{T}(Task{HttpResponseMessage}, JsonSerializerOptions?, CancellationToken)"/>
        public static async Task<object?> GetObjectAsync(this Task<HttpResponseMessage> responseTask, Type returnType, JsonSerializerOptions? options, CancellationToken token = default)
        {
            using HttpResponseMessage response = await responseTask.ConfigureAwait(false);
            return await response.Content.ReadFromJsonAsync(returnType, options, token);
        }

        /// <inheritdoc cref="GetJsonAsync(Task{HttpResponseMessage}, JsonDocumentOptions, CancellationToken)"/>
        public static Task<JsonDocument> GetJsonAsync(this Task<HttpResponseMessage> responseTask, CancellationToken token = default)
        {
            return responseTask.GetJsonAsync(default, token);
        }

        /// <summary>
        /// Deserializes the HTTP content to an instance of <see cref="JsonDocument"/> as an asynchronous operation.
        /// </summary>
        /// <param name="responseTask">An asynchronous operation that represents the HTTP response.</param>
        /// <param name="options">A <see cref="JsonSerializerOptions"/> to be used while deserializing the HTTP content.</param>
        /// <param name="token">A <see cref="CancellationToken"/> which may be used to cancel the deserialize operation.</param>
        /// <returns>A task that represents the asynchronous deserialize operation.</returns>
        public static async Task<JsonDocument> GetJsonAsync(this Task<HttpResponseMessage> responseTask, JsonDocumentOptions options, CancellationToken token = default)
        {
            using HttpResponseMessage response = await responseTask.ConfigureAwait(false);
            Stream stream = response.Content.ReadAsStream(token); // Since Content.ReadAsStreamAsync is returned synchronously.
            Encoding? encoding = GetEncoding(response.Content.Headers.ContentType?.CharSet);
            if (encoding != null && encoding != Encoding.UTF8)
            {
                stream = Encoding.CreateTranscodingStream(stream, encoding, Encoding.UTF8);
            }
            using (stream)
            {
                return await JsonDocument.ParseAsync(stream, options, token);
            }
        }
#elif !NETSTANDARD2_0
        /// <inheritdoc cref="GetObjectAsync{T}(Task{HttpResponseMessage}, JsonSerializerOptions?, CancellationToken)"/>
        public static Task<T> GetObjectAsync<T>(this Task<HttpResponseMessage> responseTask, CancellationToken token = default)
            => responseTask.GetObjectAsync<T>(null, token);

        /// <summary>
        /// Deserializes the HTTP content to an instance of <typeparamref name="T"/> as an asynchronous operation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="responseTask">An asynchronous operation that represents the HTTP response.</param>
        /// <param name="options">A <see cref="JsonSerializerOptions"/> to be used while deserializing the HTTP content.</param>
        /// <param name="token">A <see cref="CancellationToken"/> which may be used to cancel the deserialize operation.</param>
        /// <remarks>
        /// The encoding of the HTTP response content must be UTF-8.
        /// </remarks>
        /// <returns>A task that represents the asynchronous deserialize operation.</returns>
        public static async Task<T> GetObjectAsync<T>(this Task<HttpResponseMessage> responseTask, JsonSerializerOptions? options, CancellationToken token = default)
        {
            using HttpResponseMessage response = await responseTask.ConfigureAwait(false);
            using Stream stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(stream, options, token);
        }

        /// <inheritdoc cref="GetObjectAsync(Task{HttpResponseMessage}, Type, JsonSerializerOptions?, CancellationToken)"/>
        public static Task<object?> GetObjectAsync(this Task<HttpResponseMessage> responseTask, Type returnType, CancellationToken token = default)
            => responseTask.GetObjectAsync(returnType, null, token);

        /// <summary>
        /// Deserializes the HTTP content to an instance of <paramref name="returnType"/> as an asynchronous operation.
        /// </summary>
        /// <param name="returnType">The type of the HTTP content to convert to and return.</param>
        /// <inheritdoc cref="GetObjectAsync{T}(Task{HttpResponseMessage}, JsonSerializerOptions?, CancellationToken)"/>
        public static async Task<object?> GetObjectAsync(this Task<HttpResponseMessage> responseTask, Type returnType, JsonSerializerOptions? options, CancellationToken token = default)
        {
            using HttpResponseMessage response = await responseTask.ConfigureAwait(false);
            using var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync(stream, returnType, options, token);
        }

        /// <inheritdoc cref="GetJsonAsync(Task{HttpResponseMessage}, JsonDocumentOptions, CancellationToken)"/>
        public static Task<JsonDocument> GetJsonAsync(this Task<HttpResponseMessage> responseTask, CancellationToken token = default)
        {
            return responseTask.GetJsonAsync(default, token);
        }

        /// <summary>
        /// Deserializes the HTTP content to an instance of <see cref="JsonDocument"/> as an asynchronous operation.
        /// </summary>
        /// <param name="responseTask">An asynchronous operation that represents the HTTP response.</param>
        /// <param name="options">A <see cref="JsonSerializerOptions"/> to be used while deserializing the HTTP content.</param>
        /// <param name="token">A <see cref="CancellationToken"/> which may be used to cancel the deserialize operation.</param>
        /// <remarks>
        /// The encoding of the HTTP response content must be UTF-8.
        /// </remarks>
        /// <returns>A task that represents the asynchronous deserialize operation.</returns>
        public static async Task<JsonDocument> GetJsonAsync(this Task<HttpResponseMessage> responseTask, JsonDocumentOptions options, CancellationToken token = default)
        {
            using HttpResponseMessage response = await responseTask.ConfigureAwait(false);
            using Stream stream = await response.Content.ReadAsStreamAsync();
            return await JsonDocument.ParseAsync(stream, options, token);
        }
#endif
        /// <summary>
        /// Sets Content-Type in response to application/json.
        /// </summary>
        /// <param name="responseTask">An asynchronous operation that represents the HTTP response.</param>
        public static Task<HttpResponseMessage> ForceJson(this Task<HttpResponseMessage> responseTask)
        {
            return responseTask.ContinueWith(p =>
            {
#if !NETSTANDARD2_0
                if (p.IsCompletedSuccessfully) // treats response as json
#else
                if (p.Status == TaskStatus.RanToCompletion)
#endif
                {
                    HttpContentHeaders headers = p.Result.Content.Headers;
                    if (headers.ContentType == null)
                    {
                        headers.ContentType = DefaultJsonMediaType;
                    }
                    else if (headers.ContentType.MediaType != "application/json")
                    {
                        headers.ContentType.MediaType = "application/json";
                    }
                }
                return p;
            }, TaskContinuationOptions.ExecuteSynchronously).Unwrap();
        }

        public static Task<HttpResponseMessage> EnsureSuccessStatusCode(this Task<HttpResponseMessage> responseTask)
        {
            return responseTask.ContinueWith(p =>
            {
#if !NETSTANDARD2_0
                if (p.IsCompletedSuccessfully) // treats response as json
#else
                if (p.Status == TaskStatus.RanToCompletion)
#endif
                {
                    p.Result.EnsureSuccessStatusCode();
                }
                return p;
            }, TaskContinuationOptions.ExecuteSynchronously).Unwrap();
        }
#if NETSTANDARD2_0
        /// <inheritdoc cref="GetObjectAsync{T}(Task{HttpResponseMessage}, JsonSerializerSettings?, CancellationToken)"/>
        public static Task<T> GetObjectAsync<T>(this Task<HttpResponseMessage> responseTask, CancellationToken token = default)
        {
            return responseTask.GetObjectAsync<T>(null, token);
        }

        /// <summary>
        /// Deserializes the HTTP content to an instance of <typeparamref name="T"/> as an asynchronous operation.
        /// </summary>
        /// <param name="responseTask">An asynchronous operation that represents the HTTP response.</param>
        /// <param name="options">A <see cref="JsonSerializerSettings"/> to be used while deserializing the HTTP content.</param>
        /// <param name="token">The <paramref name="token"/> is ignored since <see cref="HttpContent.ReadAsStringAsync"/> has no reload that uses <see cref="CancellationToken"/>.</param>
        /// <returns>A task that represents the asynchronous deserialize operation.</returns>
        public static async Task<T> GetObjectAsync<T>(this Task<HttpResponseMessage> responseTask, JsonSerializerSettings? options, CancellationToken token = default)
        {
            string content = await responseTask.GetStringAsync(token);
            return JsonConvert.DeserializeObject<T>(content, options)!;
        }

        /// <inheritdoc cref="GetObjectAsync(Task{HttpResponseMessage}, Type, JsonSerializerSettings?, CancellationToken)"/>
        public static Task<object?> GetObjectAsync(this Task<HttpResponseMessage> responseTask, Type returnType, CancellationToken token = default)
        {
            return responseTask.GetObjectAsync(returnType, null, token);
        }

        /// <summary>
        /// Deserializes the HTTP content to an instance of <paramref name="returnType"/> as an asynchronous operation.
        /// </summary>
        /// <param name="returnType">The type of the HTTP content to convert to and return.</param>
        /// <inheritdoc cref="GetObjectAsync{T}(Task{HttpResponseMessage}, JsonSerializerSettings?, CancellationToken)"/>
        public static async Task<object?> GetObjectAsync(this Task<HttpResponseMessage> responseTask, Type returnType, JsonSerializerSettings? options, CancellationToken token = default)
        {
            string content = await responseTask.GetStringAsync(token);
            return JsonConvert.DeserializeObject(content, returnType, options);
        }

        /// <inheritdoc cref="GetJsonAsync(Task{HttpResponseMessage}, JsonSerializerSettings, CancellationToken)"/>
        public static Task<JToken> GetJsonAsync(this Task<HttpResponseMessage> responseTask, CancellationToken token = default)
        {
            return responseTask.GetJsonAsync(null, token);
        }

        /// <summary>
        /// Deserializes the HTTP content to an instance of <see cref="JToken"/> as an asynchronous operation.
        /// </summary>
        /// <inheritdoc cref="GetObjectAsync(Task{HttpResponseMessage}, JsonSerializerSettings, CancellationToken)"/>
        public static Task<JToken> GetJsonAsync(this Task<HttpResponseMessage> responseTask, JsonSerializerSettings? options, CancellationToken token = default)
        {
            return responseTask.GetObjectAsync<JToken>(options, token);
        }
#endif
    }
}
