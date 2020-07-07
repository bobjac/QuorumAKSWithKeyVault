using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Nethereum.Quorum;
using Nethereum.Web3.Accounts;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.TransactionReceipts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Hex.HexTypes;

using Bobjac.QuorumService.Models;

namespace Bobjac.QuorumService.Utilities
{
    public sealed class QuorumContractHelper
    {
        static QuorumContractHelper instance = null;
        static readonly object instancelock = new object();
        #region Singleton

        public static QuorumContractHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (instancelock)
                    {
                        instance = new QuorumContractHelper();
                    }

                }

                return instance;
            }
        }

        #endregion

        private Web3Quorum web3 = null;
        private TransactionReceiptPollingService TransactionService;


        public void SetWeb3Handler(string RpcURL)
        {
            web3 = new Web3Quorum(RpcURL);
            TransactionService = new TransactionReceiptPollingService(web3.TransactionManager);
        }

        public async Task<TransactionReturnInfo> CreateContractAsync(ContractInfo contractInfo, Account account, object[] inputParams = null, List<string> PrivateFor = null)
        {
            if (web3 == null)
            {
                throw new Exception("web3 handler has not been set - please call SetWeb3Handler First");
            }

            if (PrivateFor != null && PrivateFor?.Count != 0)
            {
                web3.ClearPrivateForRequestParameters();
                web3.SetPrivateRequestParameters(PrivateFor); 
            }

            //--- get transaction count to set nonce ---// 
            var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address, BlockParameter.CreatePending());

            // -- set signer as the account that is sending the transaction --//
            web3.Client.OverridingRequestInterceptor = new AccountTransactionSigningInterceptor(account.PrivateKey, web3.Client);

            try
            {
/*
                var gasDeploy = await web3.Eth.DeployContract.EstimateGasAsync(
                    abi: contractInfo.ContractABI,
                    contractByteCode: contractInfo.ContractByteCode,
                    from: account.Address,
                    values: inputParams == null ? new object[]{0} : inputParams);
*/
                Console.WriteLine("Creating new contract and waiting for address");

                // COULD ALSO USE TransactionService.DeployContractAndWaitForAddress
                // Gas estimate is usually low - with private quorum we don't need to worry about gas so lets just multiply it by 5.
                var realGas = new HexBigInteger(50000);
                //var realGas = new HexBigInteger(gasDeploy.Value*5);


                var transactionReceipt = await TransactionService.DeployContractAndWaitForReceiptAsync(() =>

                        web3.Eth.DeployContract.SendRequestAsync(
                                contractInfo.ContractABI,
                                contractInfo.ContractByteCode,
                                account.Address, 
                                gas: realGas,
                                gasPrice: new HexBigInteger(0), 
                                value: new HexBigInteger(0), 
                                nonce: txCount,
                                values: inputParams)
                );


                Console.WriteLine(transactionReceipt.ContractAddress);

                return new TransactionReturnInfo
                {
                    TransactionHash = transactionReceipt.TransactionHash,
                    BlockHash = transactionReceipt.BlockHash,
                    BlockNumber = transactionReceipt.BlockNumber.Value,
                    ContractAddress = transactionReceipt.ContractAddress
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public async Task<TransactionReturnInfo> CreateContractWithExternalAccountAsync(ContractInfo contractInfo, ExternalAccount externalAccount, object[] inputParams = null, List<string> PrivateFor = null)
        {
            if (web3 == null)
            {
                throw new Exception("web3 handler has not been set - please call SetWeb3Handler First");
            }

            if (PrivateFor != null && PrivateFor?.Count != 0)
            {
                web3.ClearPrivateForRequestParameters();
                web3.SetPrivateRequestParameters(PrivateFor); 
            }


            await externalAccount.InitialiseAsync();
            Console.WriteLine("externalAccount InitializeAsync() completed");
            externalAccount.InitialiseDefaultTransactionManager(web3.Client);
            Console.WriteLine("externalAccount.InitialiseDefaultTransactionManager() completed");
            //--- get transaction count to set nonce ---// 
            var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(externalAccount.Address, BlockParameter.CreatePending());
            Console.WriteLine("web3.Eth.Transactions.GetTransactionCount.SendRequestAsync() completed");
            try
            {
                /*
                var gasDeploy = await web3.Eth.DeployContract.EstimateGasAsync(
                    abi: contractInfo.ContractABI,
                    contractByteCode: contractInfo.ContractByteCode,
                    from: externalAccount.Address,
                    values: inputParams == null ? new object[]{} : inputParams); */

                Console.WriteLine("Creating new contract and waiting for address");

                // Gas estimate is usually low - with private quorum we don't need to worry about gas so lets just multiply it by 5.
                var realGas = new HexBigInteger(50000000);
                //var realGas = new HexBigInteger(gasDeploy.Value*5);

                Console.WriteLine("About to call web3.Eth.DeployContract.GetData");
                var txData = web3.Eth.DeployContract.GetData(
                    contractInfo.ContractByteCode,
                    contractInfo.ContractABI,
                    inputParams
                );
                Console.WriteLine("Returned from web3.Eth.DeployContract.GetData with the returned data of: {0}", txData);

                var txInput = new TransactionInput(
                    txData,
                    externalAccount.Address,
                    realGas,
                    new HexBigInteger(0)
                );

                Console.WriteLine("About to call externalAccount.TransactionManager.SendTransactionAndWaitForReceiptAsync");
                var transactionReceipt = await externalAccount.TransactionManager.SendTransactionAndWaitForReceiptAsync(txInput,null);
                Console.WriteLine("Returned from externalAccount.TransactionManager.SendTransactionAndWaitForReceiptAsync");
                Console.WriteLine(transactionReceipt.ContractAddress);

                return new TransactionReturnInfo
                {
                    TransactionHash = transactionReceipt.TransactionHash,
                    BlockHash = transactionReceipt.BlockHash,
                    BlockNumber = transactionReceipt.BlockNumber.Value,
                    ContractAddress = transactionReceipt.ContractAddress
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public async Task<TransactionReturnInfo> CreateTransactionAsync(string ContractAddress, ContractInfo contractInfo, string FunctionName, Account account, object[] inputParams = null, List<string> PrivateFor = null)
        {
            if (web3 == null)
            {
                throw new Exception("web3 handler has not been set - please call SetWeb3Handler First");
            }

            if (PrivateFor != null && PrivateFor?.Count != 0)
            {
                web3.ClearPrivateForRequestParameters();
                web3.SetPrivateRequestParameters(PrivateFor);
            }
            
            var contract = web3.Eth.GetContract(contractInfo.ContractABI, ContractAddress);

            if (contract == null)
            {
                throw new Exception("Could not find contract with ABI at specified address");
            }

            var contractFunction = contract.GetFunction(FunctionName);

            if (contractFunction == null)
            {
                throw new Exception("Could not find function with name " + FunctionName);
            }

            try
            {
                // var gasCallFunction = await contractFunction.EstimateGasAsync(
                //     from: account.Address,
                //     gas: null, 
                //     value: null, 
                //     functionInput: inputParams == null ? new object[]{} : inputParams
                // );

                // --- the above call consistently fails - might be because of Web3Quorum --- //

                //var realGas = new HexBigInteger(gasCallFunction.Value + 500000);

                // Seems to be deprecated, using transactionmanager service instead 
                // // -- set signer as the account that is sending the transaction --//
                // web3.Client.OverridingRequestInterceptor = new AccountTransactionSigningInterceptor(account.PrivateKey, web3.Client);


                var realGas = new HexBigInteger(500000);

                var txManager = new AccountSignerTransactionManager(web3.Client, account.PrivateKey);

                var txInput = contractFunction.CreateTransactionInput(account.Address,realGas,new HexBigInteger(0),new HexBigInteger(0),inputParams);

                var txCountNonce = await txManager.GetNonceAsync(txInput);
                
                //--- get transaction count to set nonce ---// 
                var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address, BlockParameter.CreatePending());

                txInput.Nonce = txCountNonce;

                var supposedNextNonce = await account.NonceService.GetNextNonceAsync();
                Console.WriteLine("Nonce txInput Value: " + txCountNonce.Value + "\nNonceManager Value: " + txCount.Value);
                
                var transactionReceipt = await txManager.SendTransactionAndWaitForReceiptAsync(txInput, null);

                // var transactionReceipt = await TransactionService.SendRequestAndWaitForReceiptAsync(() =>
                //         contractFunction.SendTransactionAsync(
                //                account.Address,
                //                gas: realGas,
                //                gasPrice: new HexBigInteger(0),
                //                value: new HexBigInteger(0),
                //                functionInput: inputParams)
                // );

                if (transactionReceipt != null)
                {
                    Console.WriteLine($"Processed Transaction - txHash: {transactionReceipt.TransactionHash}");

                    return new TransactionReturnInfo
                    {
                        TransactionHash = transactionReceipt.TransactionHash,
                        BlockHash = transactionReceipt.BlockHash,
                        BlockNumber = transactionReceipt.BlockNumber.Value,
                        ContractAddress = ContractAddress
                    };

                }

                return null;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public async Task<TransactionReturnInfo> CreateTransactionWithExternalAccountAsync(string ContractAddress, ContractInfo contractInfo, string FunctionName, ExternalAccount externalAccount, object[] inputParams = null, List<string> PrivateFor = null)
        {
            if (web3 == null)
            {
                throw new Exception("web3 handler has not been set - please call SetWeb3Handler First");
            }

            if (PrivateFor != null && PrivateFor?.Count != 0)
            {
                web3.ClearPrivateForRequestParameters();
                web3.SetPrivateRequestParameters(PrivateFor);
            }

            var contract = web3.Eth.GetContract(contractInfo.ContractABI, ContractAddress);

            if (contract == null)
            {
                throw new Exception("Could not find contract with ABI at specified address");
            }

            var contractFunction = contract.GetFunction(FunctionName);

            if (contractFunction == null)
            {
                throw new Exception("Could not find function with name " + FunctionName);
            }

            try
            {
                await externalAccount.InitialiseAsync();
                externalAccount.InitialiseDefaultTransactionManager(web3.Client);
            
                //default RealGas
                var realGas = new HexBigInteger(500000);

                var txInput = contractFunction.CreateTransactionInput(externalAccount.Address,realGas,new HexBigInteger(0),new HexBigInteger(0),inputParams);

                //var txCountNonce = await externalAccount.NonceService.GetNextNonceAsync();
                
                //--- set nonce? ---// 
                //txInput.Nonce = txCountNonce;

                //var supposedNextNonce = await account.NonceService.GetNextNonceAsync();
                //log.LogInformation("Nonce txInput Value: " + txCountNonce.Value );
                //Console.WriteLine("Nonce txInput Value: " + txCountNonce.Value );
                
                var transactionReceipt = await externalAccount.TransactionManager.SendTransactionAndWaitForReceiptAsync(txInput,null);

                if (transactionReceipt != null)
                {
                    Console.WriteLine($"Processed Transaction - txHash: {transactionReceipt.TransactionHash}");

                    return new TransactionReturnInfo
                    {
                        TransactionHash = transactionReceipt.TransactionHash,
                        BlockHash = transactionReceipt.BlockHash,
                        BlockNumber = transactionReceipt.BlockNumber.Value,
                        ContractAddress = ContractAddress
                    };
                }
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public async Task<GetRecordByIndexResponse> CallContractFunctionAsync<T>(string contractAddress, ContractInfo contractInfo, string functionName, string accountAddress, object[] inputParams = null)
        {
            try
            {
                var contract = web3.Eth.GetContract(contractInfo.ContractABI, contractAddress);
                var function = contract.GetFunction(functionName);

               // return await function.CallAsync<T>(accountAddress, null, null, inputParams);
                return await function.CallDeserializingToObjectAsync<GetRecordByIndexResponse>(accountAddress, null, null, inputParams);
            } 
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return new GetRecordByIndexResponse();
            }
        }
    }
}