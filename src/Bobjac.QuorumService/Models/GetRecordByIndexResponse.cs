using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts.CQS;
using Nethereum.Util;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Contracts;
using Nethereum.Contracts.Extensions;
using Nethereum.RPC.Eth.DTOs;
using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Bobjac.QuorumService.Models
{
    [FunctionOutput]
    public class GetRecordByIndexResponse : IFunctionOutputDTO
    {
        [Parameter("string", "", 1)]
        public string SensorId { get; set;}

        [Parameter("int256", "", 2)]
        public int Temperature { get; set;}

        [Parameter("int256", "", 3)]
        public int Humidity { get; set;}

        [Parameter("string", "", 4)]
        public string Timestamp { get; set;}
    }
}