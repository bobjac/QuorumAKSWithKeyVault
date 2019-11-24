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
    public class QuorumTransactionServiceController : ControllerBase
    {
        private ILogger<QuorumContractServiceController> logger;

        public QuorumTransactionServiceController(ILogger<QuorumContractServiceController> logger)
        {
            this.logger = logger;
        }

        // POST api/values
        [HttpPost()]
        public async Task<ActionResult<TransactionReturnInfo>> Post([FromBody] QuorumTransactionInput input)
        {
            if (input.functionName == null || String.IsNullOrEmpty(input.contractAddress))
            {
                return new BadRequestObjectResult("You must supply a contract address and function name");
            }

            var contractInfo = new ContractInfo();
            var client = new HttpClient();

            var filejson = await client.GetStringAsync(Environment.GetEnvironmentVariable("CONTRACT_JSON_BLOB_URL", EnvironmentVariableTarget.Process));
            dynamic _file = JsonConvert.DeserializeObject(filejson);

            var abi = _file?.abi;
            var byteCode = _file?.bytecode?.Value;

            contractInfo.ContractABI = JsonConvert.SerializeObject(abi);
            contractInfo.ContractByteCode = byteCode;

            var keyVaultURI = Environment.GetEnvironmentVariable("KEYVAULT_PRIVATEKEY_URI", EnvironmentVariableTarget.Process);
            var RPC = Environment.GetEnvironmentVariable("RPC", EnvironmentVariableTarget.Process);

            QuorumContractHelper.Instance.SetWeb3Handler(RPC);

            var appID = Environment.GetEnvironmentVariable("APP_ID", EnvironmentVariableTarget.Process);
            var appSecret = Environment.GetEnvironmentVariable("APP_SECRET", EnvironmentVariableTarget.Process);

            var externalAccount = AccountHelper.BuildExternalSigner(this.logger,keyVaultURI); 
            
            //var externalAccount = AccountHelper.BuildExternalSignerWithToken(log,keyVaultURI,appID,appSecret); 
            var res = await QuorumContractHelper.Instance.CreateTransactionWithExternalAccountAsync(input.contractAddress, contractInfo, input.functionName, externalAccount, input.inputParams, input.privateFor);
            return CreatedAtAction(nameof(Get), new { id = res.TransactionHash },  res); 
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }
    }
}