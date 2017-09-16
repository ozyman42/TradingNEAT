using System;
using System.Collections.Generic;
using Newtonsoft.Json;

using Microsoft.Web.WebSockets;

namespace TradingNEATServer
{
    public class TrainingWebSocketHandler : WebSocketHandler
    {
        private static TrainingSession session = TrainingSession.Instance;
        private static WebSocketCollection clients = new WebSocketCollection();
        private static StorageLayer storage = StorageLayer.Instance;

        private WebSocketCollection ownClient;
        //private string name;

        public TrainingWebSocketHandler()
        {
            this.ownClient = new WebSocketCollection();
            Dictionary<string, StorageLayer.DBValue> insertData = new Dictionary<string, StorageLayer.DBValue>();
            insertData["base_currency"] = new StorageLayer.DBValue("USD");
            insertData["target_currency"] = new StorageLayer.DBValue("BTC");
            insertData["seconds_between_each_data_point"] = new StorageLayer.DBValue(15);
            storage.insertIntoTable("data_sets", insertData);
        }

        public override void OnOpen()
        {
            //this.name = this.WebSocketContext.QueryString["name"];
            this.ownClient.Add(this);
            clients.Add(this);
            this.ownClient.Broadcast("{\"type\":\"connect\",\"data\":{\"hello\":\"world\"}}");
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
                        case "load-start":
                            session.loadPopulationFromFile("");
                            response = session.startTraining();
                            break;
                        case "pause-log":
                            response = session.pause();
                            response = session.export();
                            break;
                        case "finish":
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
            //clients.Broadcast(string.Format("{0} has gone away.", name));
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