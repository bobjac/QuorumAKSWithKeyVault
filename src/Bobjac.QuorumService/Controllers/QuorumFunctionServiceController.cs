using System;
using System.Collections.Generic;
using System.IO;
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
    public class QuorumFunctionServiceController : ControllerBase
    {
        private ILogger<QuorumContractServiceController> logger;

        public QuorumFunctionServiceController(ILogger<QuorumContractServiceController> logger)
        {
            this.logger = logger;
        }

        // POST api/values
        [HttpPost()]
        public async Task<ActionResult<GetRecordByIndexResponse>> Post([FromBody] QuorumTransactionInput input)
        {
            Console.WriteLine("C# HTTP trigger function processed a request.");
            
            if(input.functionName == null || (String.IsNullOrEmpty(input.contractAddress)))
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

            var accountJSON = Environment.GetEnvironmentVariable("KEYVAULT_ACCOUNT1_URL", EnvironmentVariableTarget.Process);
            
            var pwd = Environment.GetEnvironmentVariable("KEYVAULT_ETH_PASSWORD", EnvironmentVariableTarget.Process);
            var RPC = Environment.GetEnvironmentVariable("RPC", EnvironmentVariableTarget.Process);

            QuorumContractHelper.Instance.SetWeb3Handler(RPC);
            //var res = await QuorumContractHelper.Instance.CallContractFunctionAsync<int>(address, contractInfo, functionName, AccountHelper.DecryptAccount(accountJSON,pwd),functionParams);

            var keyVaultURI = Environment.GetEnvironmentVariable("KEYVAULT_PRIVATEKEY_URI", EnvironmentVariableTarget.Process);
            var appID = Environment.GetEnvironmentVariable("APP_ID", EnvironmentVariableTarget.Process);
            var appSecret = Environment.GetEnvironmentVariable("APP_SECRET", EnvironmentVariableTarget.Process);

            //var externalAccount = AccountHelper.BuildExternalSignerWithToken(this.logger,keyVaultURI,appID,appSecret); 
            var externalAccount = await AccountHelper.BuildExternalSigner(this.logger, keyVaultURI);
            //var res = await QuorumContractHelper.Instance.CallContractFunctionAsync<int>(input.contractAddress, contractInfo, input.functionName, externalAccount.Address, input.inputParams);
            var res = await QuorumContractHelper.Instance.CallContractFunctionAsync<GetRecordByIndexResponse>(input.contractAddress, contractInfo, input.functionName, externalAccount.Address, input.inputParams);
            //return new GetRecordByIndexResponse { SensorId = res.SensorId}
            return res;
        }

        // GET api/values/5
        [HttpGet("{hashCode}")]
        public ActionResult<string> Get(string hashCode)
        {
            return "value";
        }
    }
}