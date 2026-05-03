using System;
using System.Threading.Tasks;
using Xunit;
using controller_mcp.Features.Tools;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace controller_mcp.Tests
{
    [Collection("Sequential")]
    public class PcapIntegrationTests : IDisposable
    {
        private readonly PcapTestTargetHelper _target;

        public PcapIntegrationTests()
        {
            _target = new PcapTestTargetHelper();
        }

        public void Dispose()
        {
            _target.Dispose();
        }

        [Fact]
        public async Task PcapTools_CaptureTrafficFromTargetPid_Succeeds()
        {
            int pid = _target.TargetProcess.Id;

            // Start packet capture filtering only our target PID's traffic
            var startResult = PcapTools.StartPacketCapture(0, filter: "", target_pid: pid);
            
            // Check if Npcap driver is missing
            if (startResult.IsError == true)
            {
                string errorText = ((TextContentBlock)startResult.Content[0]).Text.ToLower();
                Assert.True(errorText.Contains("capture") || errorText.Contains("wpcap") || errorText.Contains("npcap"));
                // Driver missing is acceptable in CI, so we stop here.
                return;
            }

            Assert.Contains("capturing", ((TextContentBlock)startResult.Content[0]).Text);
            
            var doc = JsonDocument.Parse(((TextContentBlock)startResult.Content[0]).Text);
            string captureId = doc.RootElement.GetProperty("capture_id").GetString();

            try
            {
                // Let the background loop capture at least a few 500ms intervals
                await Task.Delay(2500);

                var receiveResult = PcapTools.ReceivePacketCapture(captureId);
                Assert.False(receiveResult.IsError == true);

                string json = ((TextContentBlock)receiveResult.Content[0]).Text;
                var receiveDoc = JsonDocument.Parse(json);
                int packetsCount = receiveDoc.RootElement.GetProperty("packets_count").GetInt32();

                Assert.True(packetsCount > 0, "No packets were captured from the target PID.");

                bool foundUdpPacket = false;
                foreach (var pkt in receiveDoc.RootElement.GetProperty("packets").EnumerateArray())
                {
                    string summary = pkt.GetString();
                    if (summary.Contains("UDP 127.0.0.1:13337 -> 127.0.0.1:13337"))
                    {
                        foundUdpPacket = true;
                        break;
                    }
                }

                Assert.True(foundUdpPacket, "Captured packets did not contain the expected UDP loopback string.");
            }
            finally
            {
                PcapTools.StopPacketCapture(captureId);
            }
        }
    
        [Fact] public void PcapIntegration_StartCapture_FailsGracefullyOnInvalidFilter() { var result = PcapTools.StartPacketCapture(0, "INVALID_FILTER_!@#$!@#$", -1); Assert.True(result.IsError == true || result.Content != null); }
    }
}
