using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Web.WebSockets;

namespace TradingNEATServer
{
    /// <summary>
    /// Base websocket handler, allowing for clients to connect to /ws.ashx for an open WS channel.
    /// </summary>
    public class WSHttpHandler : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            if (context.IsWebSocketRequest) context.AcceptWebSocketRequest(new TrainingWebSocketHandler());
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}