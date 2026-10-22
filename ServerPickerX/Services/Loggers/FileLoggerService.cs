using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ServerPickerX.Services.Loggers
{
    public class FileLoggerService : ILoggerService
    {
        private readonly string _logFilePath = AppDomain.CurrentDomain.BaseDirectory + "deadlock_server_picker_log.txt";

        // The log is kept to this size, with one older file of the same size
        // beside it, so it can never take more than about two megabytes
        // however long the app is used. Without a limit a fault that repeats
        // -- a ping sweep failing on every auto refresh, say -- writes to the
        // drive for as long as the app is left open, and leaves behind a file
        // that grows for as many years as the app is installed
        private const long MaxBytes = 1024 * 1024;

        // One writer at a time. Two appends at once can interleave halfway
        // through a line, and two rotations at once can lose the file
        private static readonly SemaphoreSlim WriteLock = new(1, 1);

        public Task LogErrorAsync(string message, string? details = null)
        {
            string line = $"ERROR: {message}";

            if (!string.IsNullOrEmpty(details))
            {
                line += $" | Details: {details}";
            }

            return AppendAsync(line);
        }

        public Task LogInfoAsync(string message)
        {
            return AppendAsync($"INFO: {message}");
        }

        public Task LogWarningAsync(string message)
        {
            return AppendAsync($"WARNING: {message}");
        }

        private async Task AppendAsync(string line)
        {
            string stamped = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {line}";

            await WriteLock.WaitAsync();

            try
            {
                try
                {
                    RotateIfFull();
                }
                catch (Exception)
                {
                    // Another program holding either file open stops the rotation
                    // for now, not the logging. The next line tries again
                }

                await File.AppendAllTextAsync(_logFilePath, stamped + Environment.NewLine);
            }
            catch (Exception)
            {
                // A log that cannot be written is not worth taking the app
                // down for, and there is nowhere left to report it to
            }
            finally
            {
                WriteLock.Release();
            }
        }

        // Once the log is full it becomes the .old file, replacing whatever
        // was there before, and a fresh one is started. Two files, a known
        // size, and the recent history is always the one you still have
        private void RotateIfFull()
        {
            FileInfo log = new(_logFilePath);

            if (!log.Exists || log.Length < MaxBytes)
            {
                return;
            }

            File.Move(_logFilePath, _logFilePath + ".old", overwrite: true);
        }
    }
}
