using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using WebServer.Models;
using HttpGetAttribute = System.Web.Http.HttpGetAttribute;
using HttpPostAttribute = System.Web.Http.HttpPostAttribute;
using RouteAttribute = System.Web.Http.RouteAttribute;

namespace WebServer.Controllers
{
    public class ClientController : ApiController
    {
        Log logger = Log.GetInstance();

        [Route("api/client/registerclient/")]
        [HttpPost]
        public void RegisterClient([FromBody]Client client)
        {
            try
            {
                ListClients.addClient(client);
            }
            catch (Exception)
            {
                HttpResponseMessage resMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent($"Failed add the client with the port num {client.port}")
                };
                logger.LogFunc($"ERROR: Failed to remove the client with the port {client.port}");
                throw new HttpResponseException(resMessage);
            }
        }


        [Route("api/client/getclientlist")]
        [HttpGet]
        public List<Client> GetClientList()
        {
            try
            {
                return ListClients.clients;
            }
            catch (Exception)
            {
                HttpResponseMessage resMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Failed to return the client list")
                };
                logger.LogFunc("ERROR: Failed to return the client list");
                throw new HttpResponseException(resMessage);
            }
        }

        [Route("api/client/unregisterclient/")]
        [HttpPost]
        public void UnRegisterClient([FromBody]Client client)
        {
            try
            {
                ListClients.removeClient(client);
            }
            catch (Exception)
            {
                HttpResponseMessage resMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent($"Failed unregister this client with the port num {client.port}")
                };
                logger.LogFunc($"ERROR: Failed to remove the client with the port {client.port}");
                throw new HttpResponseException(resMessage);
            }
        }

        [Route("api/client/updatescores/")]
        [HttpPost]
        public void UpdateScores([FromBody]Client client)
        {
            try
            {
                foreach (Client c in ListClients.clients)
                {
                    if (c.port == client.port)
                    {
                        c.updateJobCount(client.jobcount);
                    }
                }
            }
            catch (Exception e)
            {
                HttpResponseMessage resMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Failed to update the scores")
                };
                logger.LogFunc("ERROR: Failed to update the scores " + e.Message);
                throw new HttpResponseException(resMessage);
            }
        }
    }
}