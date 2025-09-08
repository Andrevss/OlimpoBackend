namespace OlimpoBackend.Services
{
    using OlimpoBackend.Models;
    using System.Text;
    using System.Text.Json;

    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessToken;
        private readonly IConfiguration _configuration;

        public MercadoPagoService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _accessToken = configuration["MercadoPago:AccessToken"];

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            _httpClient.DefaultRequestHeaders.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        }

        public async Task<string> CriarPagamento(PedidoRequest pedido, decimal valorTotal, string sessionId)
        {
            var preference = new
            {
                items = new[]
                {
                new
                {
                    title = "Pedido Olimpo 081",
                    quantity = 1,
                    currency_id = "BRL",
                    unit_price = valorTotal
                }
            },
                payer = new
                {
                    name = pedido.ClienteNome,
                    email = pedido.ClienteEmail,
                    phone = new
                    {
                        number = pedido.ClienteTelefone
                    },
                    address = new
                    {
                        zip_code = pedido.Cep,
                        street_name = pedido.Endereco,
                        city_name = pedido.Cidade,
                        federal_unit = pedido.Estado
                    }
                },
                back_urls = new
                {
                    success = _configuration["MercadoPago:SuccessUrl"],
                    failure = _configuration["MercadoPago:FailureUrl"],
                    pending = _configuration["MercadoPago:PendingUrl"]
                },
                auto_return = "approved",
                notification_url = _configuration["MercadoPago:WebhookUrl"],
                external_reference = sessionId,
                metadata = new
                {
                    session_id = sessionId,
                    cliente_nome = pedido.ClienteNome,
                    cliente_email = pedido.ClienteEmail,
                    cliente_telefone = pedido.ClienteTelefone,
                    endereco_completo = $"{pedido.Endereco}, {pedido.Cidade}, {pedido.Estado}",
                    itens = JsonSerializer.Serialize(pedido.Itens)
                }
            };

            var json = JsonSerializer.Serialize(preference);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.mercadopago.com/checkout/preferences", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                return result.GetProperty("init_point").GetString();
            }

            throw new Exception("Erro ao criar pagamento no Mercado Pago");
        }

        public async Task<dynamic> ConsultarPagamento(string pagamentoId)
        {
            var response = await _httpClient.GetAsync($"https://api.mercadopago.com/v1/payments/{pagamentoId}");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<JsonElement>(responseContent);
            }

            throw new Exception("Erro ao consultar pagamento");
        }
    }
}
