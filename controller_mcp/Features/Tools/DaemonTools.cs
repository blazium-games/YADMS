using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace controller_mcp.Features.Tools
{
    public static class DaemonTools
    {
        [McpServerTool, Description("Installs the current MCP Server application as a background Daemon that runs automatically when the user logs in, hiding completely in the system tray.")]
        public static CallToolResult InstallAsService()
        {
            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    rk.SetValue("ControllerMcpDaemon", "\"" + Application.ExecutablePath + "\" --daemon");
                }
                
                return new CallToolResult
                {
                    Content = new List<ContentBlock> { new TextContentBlock { Text = "Successfully registered Controller MCP to start as a background daemon on logon." } }
                };
            }
            catch (Exception ex)
            {
                return new CallToolResult { IsError = true, Content = new List<ContentBlock> { new TextContentBlock { Text = $"Failed to install daemon: {ex.Message}" } } };
            }
        }
    }
}
