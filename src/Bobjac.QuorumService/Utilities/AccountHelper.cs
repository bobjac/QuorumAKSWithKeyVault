using System;
using System.Threading.Tasks;
using Nethereum.Quorum;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3.Accounts;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Nethereum.KeyStore;
using Nethereum.Signer.AzureKeyVault;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Extensions.Logging;

using Bobjac.QuorumService.Models;

namespace Bobjac.QuorumService.Utilities
{
    public static class AccountHelper
    {
        static string APP_ID;
        static string APP_SECRET;

        public static Account DecryptAccount(string accountJsonFile, string passWord)
        {
            //using the simple key store service
            var service = new KeyStoreService();
            //decrypt the private key
            var key = service.DecryptKeyStoreFromJson(passWord, accountJsonFile);

            return new Account(key);
        }

        public static async Task<Account> NewAccount(string KeyVaultURI){

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

            SecretBundle secretBundle;
            try
            {
                secretBundle = await keyVaultClient.GetSecretAsync(KeyVaultURI);
            }
            catch (KeyVaultErrorException kex)
            {
                throw new Exception(kex.Message);
            }
            
            return new Account(secretBundle.Value);
        }

         public static async Task<ExternalAccount> BuildExternalSigner(ILogger log, string KeyVaultURI){

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

            try
            {
                var signer = new AzureKeyVaultExternalSigner(keyVaultClient,KeyVaultURI);
                
                var address = await signer.GetAddressAsync();

                Console.WriteLine("Signer Address: "+ address);

                return new ExternalAccount(signer);
            }
            catch(Exception e){
                log.LogInformation(e.Message);
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public static async Task<ExternalAccount> BuildExternalSignerWithToken(ILogger log, string KeyVaultURI, string appID, string appSecret){
            
            APP_ID = appID;
            APP_SECRET = appSecret;
            
            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetToken));
            
            try{

                var signer = new AzureKeyVaultExternalSigner(keyVaultClient,KeyVaultURI);

                var address = await signer.GetAddressAsync();

                Console.WriteLine("Signer Address: "+ address);

                return new ExternalAccount(signer);
            }
            catch(Exception e){
                log.LogInformation(e.Message);
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public static async Task<string> GetToken(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            var clientCred = new ClientCredential(APP_ID, APP_SECRET);
            var result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
                throw new InvalidOperationException("Failed to obtain the JWT token");

            return result.AccessToken;
        }
    }
}
