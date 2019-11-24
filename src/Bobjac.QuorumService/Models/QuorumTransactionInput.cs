using System;
using System.Collections.Generic;

namespace Bobjac.QuorumService.Models
{
    public class QuorumTransactionInput
    {
        public string contractAddress { get; set; }
        public string functionName { get; set; }
        public List<string> privateFor { get; set; }
        public object[] inputParams { get; set; }
    }
}