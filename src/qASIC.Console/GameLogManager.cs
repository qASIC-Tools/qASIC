using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace qASIC.Console
{
    public class GameLogManager : LogManager, IEnumerable<qLog>
    {
        public GameLogManager() : this(new List<qLog>()) { }
        public GameLogManager(IEnumerable<qLog> logs) : base()
        {
            Logs = new List<qLog>(logs);
        }

        #region Logging
        public List<qLog> Logs { get; private set; }

        public event Action<qLog> OnUpdateLog;

        public void Clear()
        {
            if (Closed)
                throw new Exception("Can't clear logs, log manager closed!");

            Logs?.Clear();
        }

        protected void InvokeOnUpdateLog(qLog log) =>
            OnUpdateLog?.Invoke(log);

        public override void Log(qLog log)
        {
            if (Closed)
                throw new Exception("Can't log, log manager closed!");

            if (Logs.Contains(log))
            {
                InvokeOnUpdateLog(log);
                return;
            }

            Logs.Add(log);
            InvokeOnLog(log);
            FileWrite(log);
        }
        #endregion

        #region Registering
        public override LogManager RegisterManager(LogManager other)
        {
            if (other != null)
                other.OnLog += Log;

            if (other is GameLogManager gameOther)
                gameOther.OnUpdateLog += Log;

            return this;
        }

        public override LogManager UnregisterManager(LogManager other)
        {
            if (other != null)
                other.OnLog -= Log;

            if (other is GameLogManager gameOther)
                gameOther.OnUpdateLog -= Log;

            return this;
        }
        #endregion

        #region Writing To Disk
        /// <summary>Path of the log file.</summary>
        public string FilePath { get; set; }

        private Task _fileWriteTask = null;
        private Queue<qLog> _fileWriteQueue = new Queue<qLog>();

        /// <summary>Changes the value of <see cref="FilePath"/>.</summary>
        /// <param name="newPath">New file path.</param>
        /// <returns>Returns itself.</returns>
        public GameLogManager FileChangePath(string newPath)
        {
            FilePath = newPath;
            return this;
        }

        /// <summary>Moves a previous version of the log file to a new location.</summary>
        /// <param name="path">Path to move the old log file to.</param>
        /// <returns>Returns itself.</returns>
        public GameLogManager FileMoveOld(string path)
        {
            if (File.Exists(path))
                File.Delete(path);

            if (File.Exists(FilePath))
                File.Move(FilePath, path);

            return this;
        }

        /// <summary>Changes the name of a previous version of the log file.</summary>
        /// <param name="newName">New name for the old log file.</param>
        /// <returns>Returns itself.</returns>
        public GameLogManager FileRenameOld(string newName) =>
            FileMoveOld($"{Path.GetDirectoryName(FilePath)}/{newName}");

        /// <summary>Clears the log file.</summary>
        /// <returns>Returns itself.</returns>
        public GameLogManager FileClear()
        {
            FileWrite(null);
            return this;
        }

        /// <summary>Writes all logs in <see cref="Logs"/> to the file.</summary>
        /// <returns>Return itself.</returns>
        public GameLogManager FileWriteExisting()
        {
            foreach (var item in Logs)
                _fileWriteQueue.Enqueue(item);

            FileEnsureWritingTask();
            return this;
        }

        private void FileWrite(qLog log)
        {
            _fileWriteQueue.Enqueue(log);
            FileEnsureWritingTask();
        }

        private void FileEnsureWritingTask()
        {
            if (_fileWriteQueue.Count == 0)
                return;

            if (_fileWriteTask != null && !_fileWriteTask.IsCompleted)
                return;

            _fileWriteTask = FileWriteTask();
            Task.Run(() => _fileWriteTask);
        }

        private async Task FileWriteTask()
        {
            if (FilePath == null)
            {
                _fileWriteQueue.Clear();
                return;
            }

            var dir = Path.GetDirectoryName(FilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var writer = new StreamWriter(FilePath, true))
            {
                while (_fileWriteQueue.Count > 0)
                {
                    var log = _fileWriteQueue.Dequeue();

                    if (log == null)
                    {
                        await writer.BaseStream.WriteAsync(new byte[0]);
                        continue;
                    }

                    //TODO: make this customizable
                    var txt = $"[{log.time:yyyy.MM.dd HH:mm:ss.fff}] [{log.logType}] {log.message}";

                    await writer.WriteLineAsync(txt);
                }
            }
        }
        #endregion

        public IEnumerator<qLog> GetEnumerator() =>
            Logs.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}