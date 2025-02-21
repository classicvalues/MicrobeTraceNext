﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPaaSDTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MicrobeTraceNext.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ILogger<SearchController> _logger;

        public SearchController(ILogger<SearchController> logger)
        {
            _logger = logger;
        }

        //public AddBlockController(IConfiguration iConfig)
        //{
        //    configuration = iConfig;
        //}

        [HttpPost]
        public async Task<DownloadFilterBlockResponse> PostAsync(DownloadFilterBlock input)
        {
            DownloadFilterBlockResponse resp = new DownloadFilterBlockResponse();
            
            string token = AppSettings.Settings.Token;
            string baseUrl = AppSettings.Settings.BaseUrl;
            string result;
            string hash = await GetBlockChainHashCodeController.GetBlockchainHash();

            try
            {
               string filters = BuildFilters(input.BlockFilter);

                var client = new RestClient(baseUrl);
                var request = new RestRequest("downloadfilteredblock", Method.POST);
                
                request.AddParameter("TenantID", "0");
                request.AddParameter("UserID", "0");
                request.AddParameter("RequestingUserID", input.UserID);                               
                request.AddParameter("LedgerName", input.LedgerName);
                request.AddParameter("BlockProofHash", input.BlockProofHash);

                if(hash != null)
                    request.AddParameter("BlockchainProofHash", hash);
                else
                    request.AddParameter("BlockchainProofHash", input.BlockchainProofHash);

              
                request.AddParameter("BlockFilter", filters);
                request.AddParameter("BPAASToken", token, ParameterType.HttpHeader);
              

                IRestResponse response = await client.ExecuteAsync(request);
                result = response.Content;

                if (response != null)
                {
                    var tempresult = JsonConvert.DeserializeObject<dynamic>(response.Content);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {

                        try
                        {

                            JArray a = JArray.Parse(tempresult.ToString());

                            resp.searchResult = new List<BlockData>();

                            foreach (JObject o in a.Children<JObject>())
                            {
                                string blockdata = string.Empty;
                                string blockid = string.Empty;
                                string blockname = string.Empty;

                                foreach (JProperty p in o.Properties())
                                {
                                    string name = p.Name;
                                    //string value = (string)p.Value;
                                    if (name == "BpaaSPayload")
                                    {
                                        // result.Add(p.Value.ToString());
                                        var temp  = JsonConvert.DeserializeObject<dynamic>(p.Value.ToString());

                                        blockdata = temp.ToString();
//                                        patient = JsonConvert.DeserializeObject<PatientDto>(res.ToString());
                                    }

                                    if(name== "BlockName")
                                    {
                                        blockname = p.Value.ToString();
                                    }

                                    if(name == "BlockHashCode")
                                    {

                                        blockid = p.Value.ToString();

                                    }


                                    //Console.WriteLine(name + " -- " + value);
                                }

                                BlockData res = new BlockData()
                                {
                                    blockID = blockid,
                                    blockName = blockname,
                                    data = blockdata
                                };

                                resp.searchResult.Add(res);

                            }
                            resp.success = true;


                        }
                        catch (Exception ex)
                        {
                            resp.message = "Fail to deserilaized the response";
                            resp.success = false;

                        }



                    }
                    else
                    {

                        resp.success = false;
                        resp.message = CleanUpErrroMessage(tempresult);

                    }
                }
                else
                {
                    resp.success = false;
                    resp.message = "BPaaS Service retunred null response";
                }


                //result.microbeTraceData.Add("{"Section1":{"Number":"1","Saved":false,"PatientInformation":{"IsNewCase":true,"Section":"1","CDCInformation":{"CaseState":"000001","CDC2019nCovID":"12345"},"Firstname":"Test","DateOfBirth":"2020-07-01T05:00:00.000Z","FormDateOfBirth":"2020-07-01T05:00:00.000Z","Lastname":"One"},"InterviewerInformation":{"Section":"1","UserProfile":{"IsLoggedIn":true,"BPaasInfo":{"TenantID":"0","UserID":"0","RequestingUserID":"4","LedgerName":"ContactTrace","BlockchainProofHash":"1bb3e7e7edd8ba0ef4aabb9c5c0227439032937ad162d1f6eda798c895f128e9"},"Firstname":"Test","Lastname":"Login","Email":"nuansric@icloud.com”}}}}");





            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                resp.success = false;
                resp.message = e.Message;

            }
            return resp;

        }

        private string CleanUpErrroMessage(string result)
        {
            if (result == null)
                return string.Empty;

            if (result.StartsWith("The requested block has been archived and is not able to be downloaded."))
            {
                result = "The requested block has been archived and is not able to be downloaded.";
            }

            if (result.StartsWith("This blockchain is locked/private by the owner.  The owner has not granted permission to you on this private blockchain to perform desired action."))
            {
                result = "This blockchain is locked/private by the owner.  The owner has not granted permission to you on this private blockchain to perform desired action.";
            }

            if (result.StartsWith("Your role(s) don't allow access to this ledger.  See your administrator to grant you roles to access this ledger."))
            {
                result = "Your role(s) don't allow access to this ledger.  See your administrator to grant you roles to access this ledger. ";
            }

            if (result.StartsWith("Block data not found."))
            {
                result = "Case Report could not be located.";
            }
            return result;
            
        }

        private string BuildFilters(List<Filters> filters)
        {
            StringBuilder reqFilter = new StringBuilder();

            foreach (Filters filter in filters)
            {
                if (reqFilter.Length > 0)
                    reqFilter.Append(",");

                reqFilter.Append(string.Format("{0}{1}:{2}{3}","{", filter.Key, filter.Value, "}"));
            }


            return reqFilter.ToString();
        }

    }
}
