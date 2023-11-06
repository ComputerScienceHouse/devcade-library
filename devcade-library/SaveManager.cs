
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Devcade;
public enum StorageType {
    Local,
    Remote,
}

#nullable enable
public static class Persistence {
    private static Socket _socket;
    private static StreamWriter _writeHalf;
    private static StreamReader _readHalf;
    private static Dictionary<string, Dictionary<string, string>> _data;
    private static StorageType _storage_type;
    private static string _path = "."; // for local storage
    
    private static Thread _thread;
    private static Dictionary<uint, TaskCompletionSource<Response>> _requests = new();
    
    public static bool initalized => _storage_type == StorageType.Local ? _data != null : _writeHalf != null;

    /// <summary>
    /// Initalize the persistence system. This should be called before saving
    /// or loading any data. If you are running on devcade, this will connect
    /// to the backend. If you are running locally, this will save / load to
    /// your filesystem.
    /// </summary>
    public static void Init() {
        if (OnDevcade()) {
            initRemote();
        } else {
            initLocal();
        }
    }

    /// <summary>
    /// Used to force the game to use the onboard backend, even when developing
    /// locally. This is useful for testing the backend and library locally, but
    /// shouldn't be needed in most cases. You can use this if you're having issues
    /// with saving / loading on devcade but not locally, or just ask a devcade
    /// admin (joeneil) to check the logs for you and fix the library.
    /// </summary>
    public static void InitForceRemote() {
        initRemote();
    }

    /// <summary>
    /// Sets the local path to save data to. This should be called before
    /// flusing any data to disk, or loading any data from disk. This has
    /// no effect if you are running on devcade, and only affects the local
    /// storage location for testing games on your own machine.
    /// </summary>
    public static void SetLocalPath(string path) {
        _path = path;
    }

    [DoesNotReturn]
    private static void Run() {
        string devcade_path = Environment.GetEnvironmentVariable("DEVCADE_PATH") ?? "/tmp/devcade";
        string sock_path = $"{devcade_path}/game.sock";
        tryOpenSocket(sock_path);
        while (_socket == null) {
            Console.WriteLine($"[Library.Persistence] Could not connect to {sock_path}, retrying... (are you running on devcade?)");
            Thread.Sleep(1000);
            tryOpenSocket(sock_path);
        }
        
        _data = new Dictionary<string, Dictionary<string, string>>();

        Console.WriteLine("[Library.Persistence] Starting read loop");
        while (true) {
            string message = _readHalf.ReadLine() ?? "";
            if (message == "") {
                Thread.Sleep(100);
            }

            var response = Response.FromJson(message);
            if (_requests.ContainsKey(response.request_id)) {
                _requests[response.request_id].SetResult(response);
                _requests.Remove(response.request_id);
            } else {
                Console.WriteLine($"[Library.Persistence] Got unexpected response: {response}");
            }
        }
    }
    
    /// <summary>
    /// Saves a value to either a local map, local filesystem or the devcade backend. Returns an async task that
    /// will complete when the save is finished. Must be awaited to ensure the save is complete.
    /// Check Save(...).Result.IsOk() to ensure the save was successful.
    /// </summary>
    /// <param name="group">A 'group' to save the data in. Different groups can have the same key without collision
    /// and will be stored separately, so loading a small subset of data can be faster if you store a lot of data</param>
    /// <param name="key">The key to be used, along with the group, to uniquely identify an entry.</param>
    /// <param name="value">The object to serialize and save</param>
    /// <param name="serializerOptions">Optional parameter to customize the serialization process, use this to ensure
    /// serialized data are saved and loaded correctly.</param>
    /// <typeparam name="T">The type of the object to be saved</typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException">If the passed type is not serializeable</exception>
    /// <exception cref="InvalidOperationException">If Save() is called before Init()</exception>
    public static async Task<Response> Save<T>(string group, string key, T value, JsonSerializerOptions? serializerOptions) {
        if (!typeof (T).IsSerializable) throw new ArgumentException("Type is not serializable");
        if (!initalized) throw new InvalidOperationException("Persistence not initalized yet (call Persistence.Init() or wait longer after calling it)");
        return _storage_type switch { 
            StorageType.Local => saveLocal(group, key, value, serializerOptions),
            StorageType.Remote => await saveRemote(group, key, value, serializerOptions),
            _ => throw new Exception("Invalid storage type"),
            };
    }

    /// <summary>
    /// See <see cref="Save{T}"/> For more information. This is a blocking version of Save&lt;T&gt;  that will block until the save is
    /// complete. Check SaveSync(...).IsOk() to ensure the save was successful. This method is not recommended for
    /// multiple saves in a row, as it will block the thread until the save is complete. Instead, use multiple calls to
    /// Save(...) and await the returned tasks all at once.
    /// </summary>
    public static Response SaveSync<T>(string group, string key, T value, JsonSerializerOptions? serializerOptions) {
        var task = Save(group, key, value, serializerOptions);
        task.Wait();
        return task.Result;
    }
    
    // Internal method to save data to the local filesystem (not the devcade backend)
    // Used when developing locally or if you're just running the game on your own machine
    private static Response saveLocal<T>(string group, string key, T value, JsonSerializerOptions? serializerOptions) {
        if (!_data.ContainsKey(group)) {
            var path_parts = group.Split('/');
            // group path part is everything but the last part
            var group_path = string.Join('/', path_parts[..^1]);
            if (!Directory.Exists($"{_path}/{group_path}")) {
                Directory.CreateDirectory($"{_path}/{group_path}");
            }
            if (File.Exists($"{_path}/{group}.save")) {
                _data[group] = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText($"{_path}/{group}.save")) ?? new Dictionary<string, string>();
            } else {
                _data[group] = new Dictionary<string, string>();
            }
        }
        _data[group][key] = JsonSerializer.Serialize(value, serializerOptions);
        return Response.FromOk();
    }

    // Internal method to save data to the devcade backend, used when running on devcade, or when the user wants to be
    // spicy and run the devcade backend locally.
    private static async Task<Response> saveRemote<T>(string group, string key, T value, JsonSerializerOptions? serializerOptions) {
        string json = JsonSerializer.Serialize(value, serializerOptions);
        Request req = Request.SaveRequest(group, key, json);
        TaskCompletionSource<Response> tcs = new TaskCompletionSource<Response>();
        _requests[req.request_id] = tcs;
        write(req.Serialize());
        return await tcs.Task;
    }
    
    /// <summary>
    /// Loads a value from either a local map, local filesystem or the devcade backend. Returns an async task that
    /// will complete when the load is finished. Must be awaited to ensure the load is complete.
    /// The loaded value can be accessed through Load(...).Result.GetObject<T>(serializerOptions)
    /// </summary>
    /// <param name="group">>A 'group' to load the data from. Different groups can have the same key without collision
    /// and will be stored separately, so loading a small subset of data can be faster if you store a lot of data</param>
    /// <param name="key"></param>
    /// <param name="serializerOptions"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static async Task<Response> Load<T>(string group, string key, JsonSerializerOptions? serializerOptions) {
        return _storage_type switch {
            StorageType.Local => Response.FromObject(loadLocal<T>(group, key, serializerOptions)),
            StorageType.Remote => await loadRemote(group, key),
            _ => throw new Exception("Invalid storage type")
        };
    }

    /// <summary>
    /// See <see cref="Load{T}"/> For more information. This is a blocking version of Load&lt;T&gt;  that will block until the load is
    /// complete, and will attempt to unwrap the response into a T. This will discard any errors that occur during the
    /// load, and return null if the load fails. This method is not recommended for multiple loads in a row, as it will
    /// block the thread until the load is complete. Instead, use multiple calls to Load(...) and await the returned
    /// tasks all at once.
    /// </summary>
    /// <param name="group"></param>
    /// <param name="key"></param>
    /// <param name="serializerOptions"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? LoadSync<T>(string group, string key, JsonSerializerOptions? serializerOptions) {
        var task = Load<T>(group, key, serializerOptions);
        task.Wait();
        return task.Result.GetObject<T>(serializerOptions) ?? default;
    }
    
    private static T? loadLocal<T>(string group, string key, JsonSerializerOptions? serializerOptions) {
        if (!_data.ContainsKey(group)) {
            string[] path_parts = group.Split('/');
            // group path part is everything but the last part
            string group_path = string.Join('/', path_parts[..^1]);
            if (!Directory.Exists($"{_path}/{group_path}")) {
                Directory.CreateDirectory($"{_path}/{group_path}");
            }
            if (File.Exists($"{_path}/{group}.save")) {
                _data[group] = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText($"{_path}/{group}.save")) ?? new Dictionary<string, string>();
            } else {
                _data[group] = new Dictionary<string, string>();
            }
        }
        return !_data[group].ContainsKey(key) ? default : JsonSerializer.Deserialize<T>(_data[group][key], serializerOptions);
    }
    
    private static Task<Response> loadRemote(string group, string key) {
        var req = Request.LoadRequest(group, key);
        var tcs = new TaskCompletionSource<Response>();
        _requests[req.request_id] = tcs;
        write(req.Serialize());
        return tcs.Task;
    }
    
    /// <summary>
    /// Flushes all data to disk (if local) otherwise sends a flush request to the backend.
    /// </summary>
    public static async Task<Response> Flush() {
        return _storage_type switch {
            StorageType.Local => await flushLocal(),
            StorageType.Remote => await flushRemote(),
            _ => throw new Exception("Invalid storage type")
        };
    }
    
    private static async Task<Response> flushLocal() {
        foreach ((string group, var data) in _data) {
            string group_path = string.Join('/', group.Split('/')[..^1]);
            if (!Directory.Exists($"{_path}/{group_path}")) {
                Directory.CreateDirectory($"{_path}/{group_path}");
            }
            await File.WriteAllTextAsync($"{_path}/{group}.save", JsonSerializer.Serialize(data));
        }
        return Response.FromOk();
    }
    
    private static async Task<Response> flushRemote() {
        var req = Request.FlushRequest();
        var tcs = new TaskCompletionSource<Response>();
        _requests[req.request_id] = tcs;
        write(req.Serialize());
        return await tcs.Task;
    }

    /// <summary>
    /// Checks whether running on devcade or not.
    /// </summary>
    /// <returns></returns>
    public static bool OnDevcade() {
        // check if /home/devcade exists
        return Directory.Exists("/home/devcade");
    }

    private static void initLocal() {
        Console.WriteLine("[Library.Persistence] Initializing local storage");
        _storage_type = StorageType.Local;
        _data = new Dictionary<string, Dictionary<string, string>>();
    }

    private static void initRemote() {
        Console.WriteLine("[Library.Persistence] Initializing remote storage");
        _storage_type = StorageType.Remote;
        _thread = new Thread(Run) {IsBackground = true};
        _thread.Start();
    }

    private static Socket? tryOpenSocket(string path) {
        Console.WriteLine($"[Library.Persistence] Trying to open socket @ {path}");
        try {
            _socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
            var endpoint = new UnixDomainSocketEndPoint(path);
            _socket.Connect(endpoint);
            
            var stream = new NetworkStream(_socket);
            _readHalf = new StreamReader(stream, new UTF8Encoding(false));
            _writeHalf = new StreamWriter(stream, new UTF8Encoding(false));
            return _socket;
        } catch (Exception e) {
            Console.WriteLine("[Library.Persistence] ERROR: Failed to open socket: " + e.Message);
            _socket = null;
            return null;
        }
    }

    private static void write(string message) {
        _writeHalf.WriteLine(message);
        _writeHalf.Flush();
        _writeHalf.BaseStream.Flush();
    }

    private static string read() {
        try {
            return _readHalf.ReadLine() ?? "";
        } catch (Exception e) {
            Console.WriteLine("[Library.Persistence] ERROR: Failed to read from socket: " + e.Message);
            return "";
        }
    }

    private sealed class Request {
        private enum RequestType {
            Save,
            Load,
            Flush,
        }
        
        public readonly uint request_id;
        private readonly RequestType type;
        private readonly string? group;
        private readonly string? key;
        private readonly string? value;
        
        private static uint _next_request_id;
        
        private Request (uint request_id, RequestType type, string? group, string? key, string? value) {
            this.request_id = request_id;
            this.type = type;
            this.group = group;
            this.key = key;
            this.value = value;
        }
        
        public static Request SaveRequest(string group, string key, string value) {
            return new Request(_next_request_id++, RequestType.Save, group, key, value);
        }
        
        public static Request LoadRequest(string group, string key) {
            return new Request(_next_request_id++, RequestType.Load, group, key, null);
        }
        
        public static Request FlushRequest() {
            return new Request(_next_request_id++, RequestType.Flush, null, null, null);
        }

        public string Serialize() {
            bool quoteValue = value != null && value[0] != '{' && value[0] != '[';
            string v = value?.Replace("\"", "\\\"") ?? "null";
            string s = type switch {
                RequestType.Save => $"{{\"request_id\": {request_id}, \"type\": \"Save\", \"data\": [\"{group}\", \"{key}\", \"{v}\"]}}",
                RequestType.Load => $"{{\"request_id\": {request_id}, \"type\": \"Load\", \"data\": [\"{group}\", \"{key}\"]}}",
                RequestType.Flush => $"{{\"request_id\": {request_id}, \"type\": \"Flush\"}}",
            };
            return s;
        }
    }

    public sealed class Response {
        public enum ResponseType {
            Ok,
            Err,
            Object,
        }

        private Dictionary<string, object> _data;
        private object? _object;
        
        public uint request_id { get; private set; }

        public ResponseType type => (ResponseType) Enum.Parse(typeof(ResponseType), JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(_data["type"])) ?? "");

        public string? error => type == ResponseType.Err && _data.ContainsKey("data") ? JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(_data["data"])) ?? "" : null;


        private Response() {
            _data = new Dictionary<string, object>();
        }

        public T? GetObject<T>(JsonSerializerOptions? serializerOptions) {
            if (_object != null && type == ResponseType.Object) return (T) _object;
            if (type == ResponseType.Err) {
                Console.WriteLine("[Library.Persistence] WARN: Tried to get object from error response" +
                                  (error != null ? $": {error}" : ""));
                return default;
            }
            if (_data == null) return (T?)_object;
            string s_deser;
            try { 
                s_deser = JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(_data["data"]), serializerOptions) ?? "";
            } catch (Exception e) {
                Console.WriteLine("[Library.Persistence] WARN: Failed to deserialize object: " + e.Message);
                return default;
            }

            _object = typeof(T).ToString() switch {
                // C# can't switch on types so we have to use strings
                // C# also can't deserialize to number types so we have to do that manually too
                "System.Int32" => int.Parse(s_deser),
                "System.UInt32" => uint.Parse(s_deser),
                "System.Int64" => long.Parse(s_deser),
                "System.UInt64" => ulong.Parse(s_deser),
                "System.Int16" => short.Parse(s_deser),
                "System.UInt16" => ushort.Parse(s_deser),
                "System.Byte" => byte.Parse(s_deser),
                "System.SByte" => sbyte.Parse(s_deser),
                "System.Single" => float.Parse(s_deser),
                "System.Double" => double.Parse(s_deser),
                "System.Decimal" => decimal.Parse(s_deser),
                _ => this.deserialize<T>() ?? default
            };
            return (T?) _object;
        }

        private T? deserialize<T>() {
            try {
                // Serialized objects are wrapped in a string so that serde can deserialize arbitrary types into a string
                // without having to know the type at compile time (or at all).
                string intermediate = JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(_data["data"])) ?? "";
                return JsonSerializer.Deserialize<T>(intermediate);
            } catch (Exception e) {
                Console.WriteLine("[Library.Persistence] WARN: Failed to deserialize object: " + e.Message);
                return default;
            }
        }

        public bool IsOk() {
            return type is ResponseType.Ok;
        }

        public bool IsErr() {
            return type is ResponseType.Err;
        }
        
        public bool IsObject() {
            return type is ResponseType.Object;
        }

        public static Response FromJson(string json) {
            Response r = new() {
                _data = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>()
            };
            uint id = JsonSerializer.SerializeToDocument(r._data["request_id"]).RootElement.Deserialize<uint>();
            r.request_id = id;
            return r;
        }

        internal static Response FromObject(object? obj) {
            Response r = obj != null ? new Response {
                _object = obj,
                _data = {
                    ["request_id"] = uint.MaxValue,
                    ["type"] = "Object",
                }
            } : new Response {
                _data = {
                    ["request_id"] = uint.MaxValue,
                    ["type"] = "Err",
                    ["data"] = "Locally stored value not found"
                }
            };
            return r;
        }

        internal static Response FromOk() {
            Response r = new Response {
                _data = {
                    ["request_id"] = uint.MaxValue,
                    ["type"] = "Ok",
                }
            };
            return r;
        }
    }
}
