using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Channels;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Net.WebSockets;
using CryptoArbitrage.Domain.Exceptions;

namespace CryptoArbitrage.Infrastructure.Exchanges
{
    public class KrakenExchangeClient
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<TradingPair, Channel<OrderBook>> _orderBookChannels = new();
        private readonly Dictionary<string, TradingPair> _subscribedPairs = new();
        private readonly Dictionary<int, string> _channelIdToSymbol = new();
        
        private string? _apiKey;
        private string? _apiSecret;
            private readonly string _baseUrl = "https://api.kraken.com";
    private readonly string _wsUrl = "wss://ws.kraken.com";  // v1 WebSocket API
    private readonly long _nonce;

        public KrakenExchangeClient()
        {
            _httpClient = new HttpClient();
            _nonce = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        // ... rest of the existing code ...
    }
} 