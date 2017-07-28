using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using Newtonsoft.Json;

using Microsoft.Web.WebSockets;

namespace TradingNEATServer
{
    public class TrainingWebSocketHandler : WebSocketHandler
    {
        private static TrainingSession session = TrainingSession.Instance;
        private static WebSocketCollection clients = new WebSocketCollection();

        private WebSocketCollection ownClient;
        //private string name;

        public TrainingWebSocketHandler()
        {
            this.ownClient = new WebSocketCollection();
        }

        public override void OnOpen()
        {
            //this.name = this.WebSocketContext.QueryString["name"];
            this.ownClient.Add(this);
            clients.Add(this);
        }

        public override void OnMessage(string message)
        {
            RequestBody reqBody = JsonConvert.DeserializeObject<RequestBody>(message);
            string response;
            lock (session)
            {
                try
                {
                    switch (reqBody.Type)
                    {
                        case "status":
                            response = "";
                            break;
                        case "load":
                            response = session.loadPopulationFromFile("");
                            break;
                        case "start":
                            response = session.startTraining();
                            break;
                        case "pause":
                            response = session.pause();
                            break;
                        case "export":
                            response = session.export();
                            break;
                        case "reset":
                            response = session.reset();
                            break;
                        default:
                            throw new Exception($"Unknown request type \"{reqBody.Type}\".");
                    }
                }
                catch(Exception e)
                {
                    response = e.Message;
                }
            }
            this.ownClient.Broadcast(response);
        }

        public override void OnClose()
        {
            clients.Remove(this);
            clients.Broadcast(string.Format("{0} has gone away.", name));
        }

        public static void HandleGenerationCompletion()
        {

        }

        # region WebSocketRequest Structures

        private class RequestBody
        {
            public string Type { get; set; }
            public string Params { get; set; }
        }

        private class StartRequestParams
        {
        }

        private class PauseRequestParams
        {

        }

        private class ExportRequestParams
        {

        }

        private class ResetRequestParams
        {

        }

        # endregion

    }

}