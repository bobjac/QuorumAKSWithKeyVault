using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Nethereum.Web3.Accounts;

using Bobjac.QuorumService.Models;
using Bobjac.QuorumService.Utilities;

namespace Bobjac.QuorumService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuorumContractServiceController : ControllerBase
    {
        private ILogger<QuorumContractServiceController> logger;

        public QuorumContractServiceController(ILogger<QuorumContractServiceController> logger)
        {
            this.logger = logger;
        }

        // POST api/values
        [HttpPost()]
        public async Task<ActionResult<TransactionReturnInfo>> Post([FromBody] QuorumTransactionInput input)
        {
            Console.WriteLine("Received request to create new contract");

            var contractInfo = await GetContractInfo();

            var keyVaultURI = Environment.GetEnvironmentVariable("KEYVAULT_PRIVATEKEY_URI", EnvironmentVariableTarget.Process);
            var RPC = Environment.GetEnvironmentVariable("RPC", EnvironmentVariableTarget.Process);
            Console.WriteLine("Retrieved the keyvault uri and RPC endpoint from environment variables - {0}, {1}", keyVaultURI, RPC);

            QuorumContractHelper.Instance.SetWeb3Handler(RPC);

            var appID = Environment.GetEnvironmentVariable("APP_ID", EnvironmentVariableTarget.Process);
            var appSecret = Environment.GetEnvironmentVariable("APP_SECRET", EnvironmentVariableTarget.Process);

            Console.WriteLine("Retrieved the APP_ID and APP_SECRET from environment variables - {0}, {1}", appID, appSecret);

            var externalAccount = await AccountHelper.BuildExternalSigner(this.logger, keyVaultURI);
            var transactionReturnInfo = await QuorumContractHelper.Instance.CreateContractWithExternalAccountAsync(contractInfo, externalAccount, input.inputParams, input.privateFor);
            return CreatedAtAction(nameof(Get), new { hashCode = transactionReturnInfo.TransactionHash },  transactionReturnInfo); 
        }

        // GET api/values/5
        [HttpGet("{hashCode}")]
        public ActionResult<string> Get(string hashCode)
        {
            return "value";
        }

        private async Task<ContractInfo> GetContractInfo()
        {
            var contractInfo = new ContractInfo();
            var client = new HttpClient();

            Console.WriteLine("Before getting JSON blob");

            Console.WriteLine("Prior to calling Environment.GetEnvironmentVariable for the CONTRACT_JSON_BLOB_URL");
            var contractJsonBlobUrl = Environment.GetEnvironmentVariable("CONTRACT_JSON_BLOB_URL", EnvironmentVariableTarget.Process);
            if (contractJsonBlobUrl == null)
            {
                Console.WriteLine("Environment variable was retrieved and is null");
                contractJsonBlobUrl = string.Empty;
            }
            Console.WriteLine("Pulled contractJsonBlobUrl from the environment varibale with a value of " + contractJsonBlobUrl);

            var filejson = await client.GetStringAsync(contractJsonBlobUrl);

            if (filejson.Length > 0)
            {
                Console.WriteLine("The string returned from client.GetStringAsync had a length greater than zero");
            }
            else 
            {
                Console.WriteLine("The string returned from client.GetStringAsync had a length of zero");
            }

            Console.WriteLine("Pulled fileJson from blob storage witha  value of " + filejson);
            dynamic _file = JsonConvert.DeserializeObject(filejson);

            var abi = _file?.abi;
            Console.WriteLine("The abi is " + abi);
            var byteCode = _file?.bytecode?.Value;
            Console.WriteLine("The byteCode is " + byteCode);

            contractInfo.ContractABI = JsonConvert.SerializeObject(abi);
            contractInfo.ContractByteCode = byteCode;

            return contractInfo;
        }
    }
}
