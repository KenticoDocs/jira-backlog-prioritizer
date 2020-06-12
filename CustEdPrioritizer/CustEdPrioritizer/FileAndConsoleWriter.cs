using System;
using System.IO;

namespace CustEdPrioritizer
{
    /// <summary>
    /// Class writing into both a log file and the console output.
    /// </summary>
    class FileAndConsoleWriter : IDisposable
    {
        public FileStream Stream { get; private set; }

        public StreamWriter Writer { get; private set; }

        private TextWriter OldOut { get; set; }

        /// <summary>
        /// Constructor. Initializes a log file at a specified path.
        /// </summary>
        /// <param name="path">Path of the log file.</param>
        public FileAndConsoleWriter(string path)
        {
            OldOut = Console.Out;

            try
            {
                Stream = new FileStream(path, FileMode.Create, FileAccess.Write);
                Writer = new StreamWriter(Stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine("The log file could not be initialized. Press a key to terminate the application.");
                Console.WriteLine(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Writes a string followed by a line terminator to both a text stream and the console output.
        /// </summary>
        /// <param name="value">The string value that is written to the outputs.</param>
        public void WriteLine(string value)
        {
            Console.SetOut(Writer);
            Console.WriteLine(value);
            Console.SetOut(OldOut);
            Console.WriteLine(value);
            Writer.Flush();
        }

        /// <summary>
        /// Writes a line terminator to both a text stream and the console output.
        /// </summary>
        public void WriteLine()
        {
            WriteLine("");
        }

        /// <summary>
        /// Writes a string to both a text stream and the console output.
        /// </summary>
        /// <param name="value">The string value that is written to the outputs.</param>
        public void Write(string value)
        {
            Console.SetOut(Writer);
            Console.Write(value);
            Console.SetOut(OldOut);
            Console.Write(value);
            Writer.Flush();
        }

        /// <summary>
        /// Formats and writes a string followed by a line terminator to both a text stream and the console output.
        /// </summary>
        /// <param name="value">The string value that is formatted and written to the outputs.</param>
        /// <param name="args">Strings replacing the value at a specified places.</param>
        public void WriteLine(string value, params object[] args)
        {
            string message = String.Format(value, args);
            WriteLine(message);
        }

        /// <summary>
        /// Formats and writes a string to both a text stream and the console output.
        /// </summary>
        /// /// <param name="value">The string value that is formatted and written to the outputs.</param>
        /// <param name="args">Strings replacing the value at a specified places.</param>
        public void Write(string value, params object[] args)
        {
            string message = String.Format(value, args);
            Write(message);
        }

        Boolean isDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            ReleaseResources(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose resources used by the class.
        /// </summary>
        /// <param name="isFromDispose">Sets whether the method is called from the Dispose method.</param>
        protected void ReleaseResources(bool isFromDispose)
        {
            if (!isDisposed)
            {
                if (isFromDispose)
                {
                    Writer.Close();
                    Stream.Close();

                    Writer.Dispose();
                    Stream.Dispose();
                }
            }

            isDisposed = true;
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~FileAndConsoleWriter()
        {
            ReleaseResources(false);
        }
    }
}