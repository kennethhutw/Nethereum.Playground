﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.Signer;
using Nethereum.Web3;

namespace NetDapps.MessageSigning.Components
{
    public class SignMessageBase : ComponentBase
    {
        public string PrivateKey { get; set; }
        public string Message { get; set; }
        public string AccountAddress { get; set; }
        public string SignedMessage { get; set; }

        protected override void OnInit()
        {
            Message = "wee test message 18/09/2017 02:55PM";
            PrivateKey = "0xb5b1870957d373ef0eeffecc6e4812c0fd08f554b37b233526acc331bf1544f7";
            base.OnInit();
        }

        protected async Task SignMessage()
        {
            var signer1 = new EthereumMessageSigner();
            var key = new EthECKey(PrivateKey);
            var signature1 = signer1.EncodeUTF8AndSign(Message, key);
            SignedMessage = signature1;
            AccountAddress = key.GetPublicAddress();
        }

    }
}