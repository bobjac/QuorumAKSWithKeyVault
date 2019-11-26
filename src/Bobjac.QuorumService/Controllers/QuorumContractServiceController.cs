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
            logger.LogInformation("Received request to create new contract");

            var contractInfo = await GetContractInfo();

            var keyVaultURI = Environment.GetEnvironmentVariable("KEYVAULT_PRIVATEKEY_URI", EnvironmentVariableTarget.Process);
            var RPC = Environment.GetEnvironmentVariable("RPC", EnvironmentVariableTarget.Process);

            logger.LogInformation("Retrieved the keyvault uri and RPC endpoint from environment variables - {0}, {1}", keyVaultURI, RPC);

            QuorumContractHelper.Instance.SetWeb3Handler(RPC);

            var appID = Environment.GetEnvironmentVariable("APP_ID", EnvironmentVariableTarget.Process);
            var appSecret = Environment.GetEnvironmentVariable("APP_SECRET", EnvironmentVariableTarget.Process);

            logger.LogInformation("Retrieved the APP_ID and APP_SECRET from environment variables - {0}, {1}", appID, appSecret);

            var externalAccount = AccountHelper.BuildExternalSigner(this.logger, keyVaultURI);
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

            this.logger.LogInformation("Before getting JSON blob");

            var filejson = await client.GetStringAsync(Environment.GetEnvironmentVariable("CONTRACT_JSON_BLOB_URL", EnvironmentVariableTarget.Process));
            dynamic _file = JsonConvert.DeserializeObject(filejson);

            var abi = _file?.abi;
            var byteCode = _file?.bytecode?.Value;

            contractInfo.ContractABI = JsonConvert.SerializeObject(abi);
            contractInfo.ContractByteCode = byteCode;

            return contractInfo;
        }
    }
}
