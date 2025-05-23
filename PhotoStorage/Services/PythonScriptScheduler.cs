using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace PhotoStorage.Services
{
    public class PythonScriptScheduler : IHostedService, IDisposable
    {
        private Timer? _timer;
        private Process? _flaskProcess; // Add a field to track the Flask process

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Start the Flask app as a background process
            _flaskProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "Services/UserActivity/admin_dashboard.py", // Path to the Flask app
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                _flaskProcess.Start();
                Console.WriteLine("Flask app started successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start Flask app: {ex.Message}");
            }

            // Schedule the script to run every 10 seconds
            _timer = new Timer(RunPythonScript, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
            return Task.CompletedTask;
        }

        private void RunPythonScript(object? state)
        {
            try
            {
                var pythonProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = "Services/UserActivity/user_activity_logger.py", // Updated path to the Python script
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                pythonProcess.Start();
                string output = pythonProcess.StandardOutput.ReadToEnd();
                string error = pythonProcess.StandardError.ReadToEnd();
                pythonProcess.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"Python Error: {error}");
                }
                else
                {
                    Console.WriteLine($"Python Output: {output}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to execute Python script: {ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop the Flask app process
            if (_flaskProcess != null && !_flaskProcess.HasExited)
            {
                _flaskProcess.Kill();
                _flaskProcess.Dispose();
                Console.WriteLine("Flask app stopped successfully.");
            }

            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}