using System;
using System.Collections.Generic;
using System.Text;

namespace Devcade
{
  namespace SaveData
  {
    using System.IO;
    using Newtonsoft.Json;

    /// <summary>
    /// Singleton class to save and load data through a backend.
    /// </summary>
    public class SaveManager
    {
      /// <summary>
      /// Simple struct to represent a request message.
      /// </summary>
      private struct Request
      {
        private string command;
        private string path;
        private string data;

        public string Command { get => command; }
        public string Path { get => path; }
        public string Data { get => data; }

        /// <summary>
        /// Create a request object with the specified properties.
        /// </summary>
        /// <param name="command">The command to send to the backend.</param>
        /// <param name="path">Path to use for this command.</param>
        /// <param name="data">Any data to be sent with this command.</param>
        public Request(string command, string path, string data)
        {
          this.command = command;
          this.path = path;
          this.data = data;
        }
      }

      /// <summary>
      /// Simple struct to represent a response message.
      /// </summary>
      private struct Response
      {
        private bool success;
        private string error;
        private string data;

        public bool Success { get => success; }
        public string Error { get => error; }
        public string Data { get => data; }

        /// <summary>
        /// Create a request object with the specified properties.
        /// </summary>
        /// <param name="success">Weather the command succeeded.</param>
        /// <param name="error">Any error messages applicable.</param>
        /// <param name="data">Any data returned.</param>
        public Response(bool success, string error, string data)
        {
          this.success = success;
          this.error = error;
          this.data = data;
        }
      }

      private static SaveManager instance;
      private StreamWriter pipeOut;
      private StreamReader pipeIn;

      public static SaveManager Instance
      {
        get
        {
          if (instance == null)
          {
            instance = new SaveManager();
          }
          return instance;
        }
      }

      /// <summary>
      /// Creates a Saves object and initializes the pipes.
      /// </summary>
      private SaveManager()
      {
        // This goes to read_game because the pipes are named from the backends perspective
        pipeOut = new StreamWriter(Environment.GetEnvironmentVariable("DEVCADE_PATH") + "/read_game");

        pipeIn = new StreamReader(Environment.GetEnvironmentVariable("DEVCADE_PATH") + "/write_game");
      }

      /// <summary>
      /// Save arbitrary text data to the cloud.
      /// </summary>
      /// <param name="path">Path of the file to save to in your s3 bucket. NOTE: Overwrites if the file exists already.</param>
      /// <param name="data">Text data to be saved.</param>
      /// <returns>True if successful, false otherwise.</returns>
      public bool SaveText(string path, string data)
      {
        pipeOut.WriteLine(JsonConvert.SerializeObject(new Request("Save Data", path, data)));
        pipeOut.Flush();

        return JsonConvert.DeserializeObject<Response>(pipeIn.ReadLine()).Success;
      }

      /// <summary>
      /// Load arbitrary text data from the cloud.
      /// </summary>
      /// <param name="path">Path of the file in your s3 bucket.</param>
      /// <returns>Data loaded as string.</returns>
      public string LoadText(string path)
      {
        pipeOut.WriteLine(JsonConvert.SerializeObject(new Request("Load Data", path, "")));
        pipeOut.Flush();

        return JsonConvert.DeserializeObject<Response>(pipeIn.ReadLine()).Data;
      }

      /// <summary>
      /// Closes the pipe files as this is destroyed.
      /// </summary>
      ~SaveManager()
      {
        pipeIn.Close();
        pipeOut.Close();
      }
    }
  }
}
