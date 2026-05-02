using System;
using System.IO;
using System.Threading;
using Xunit;
using controller_mcp.Features.Tools;

namespace controller_mcp.Tests
{
    [Collection("AuditLogger")]
    public class AuditLoggerTests : IDisposable
    {
        private string _tempLogDir;

        public AuditLoggerTests()
        {
            _tempLogDir = Path.Combine(Path.GetTempPath(), "AuditLogger_Tests_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempLogDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempLogDir))
            {
                Directory.Delete(_tempLogDir, true);
            }
        }

        [Fact]
        public void AuditLogger_Reconfigure_CreatesDirectory()
        {
            string newDir = Path.Combine(_tempLogDir, "SubLogs");
            AuditLogger.Reconfigure(newDir);

            Assert.True(Directory.Exists(newDir));
        }

        [Fact]
        public void AuditLogger_Log_WritesToDisk()
        {
            AuditLogger.Initialize(_tempLogDir);
            
            AuditLogger.Log(LogLevel.INFO, "TEST_CATEGORY", "Test Message");
            
            // Allow background thread to write
            System.Threading.SpinWait.SpinUntil(() => 
            {
                string[] currentFiles = Directory.GetFiles(_tempLogDir, "*.log");
                if (currentFiles.Length == 0) return false;
                try
                {
                    foreach(var file in currentFiles)
                    {
                        if (File.ReadAllText(file).Contains("TEST_CATEGORY")) return true;
                    }
                    return false;
                }
                catch { return false; }
            }, 5000);

            string[] files = Directory.GetFiles(_tempLogDir, "*.log");
            Assert.Single(files);

            string content = File.ReadAllText(files[0]);
            Assert.Contains("TEST_CATEGORY", content);
            Assert.Contains("Test Message", content);
        }
    }
}
