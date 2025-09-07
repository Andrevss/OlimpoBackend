namespace OlimpoBackend.Services
{
    using OlimpoBackend.Models;
    using System.Text.Json;

    public class CorreiosService : ICorreiosService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public CorreiosService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<decimal> CalcularFrete(string cepDestino, List<ItemPedidoRequest> itens)
        {
            var cepOrigem = _configuration["Correios:CepOrigem"];
            var peso = CalcularPeso(itens);

            var cepValido = await ValidarCep(cepDestino);
            if (!cepValido) throw new Exception("CEP inválido");

            return await CalcularFretePorRegiao(cepOrigem, cepDestino, peso);
        }

        private async Task<bool> ValidarCep(string cep)
        {
            try
            {
                cep = cep.Replace("-", "").Replace(".", "");
                var response = await _httpClient.GetAsync($"https://viacep.com.br/ws/{cep}/json/");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(content);
                    return !result.TryGetProperty("erro", out _);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private decimal CalcularPeso(List<ItemPedidoRequest> itens)
        {
            return itens.Sum(i => i.Quantidade) * 0.2m; // 200g por produto
        }

        private async Task<decimal> CalcularFretePorRegiao(string cepOrigem, string cepDestino, decimal peso)
        {
            var estadoOrigem = ObterEstadoPorCep(cepOrigem);
            var estadoDestino = ObterEstadoPorCep(cepDestino);

            if (estadoOrigem == estadoDestino)
                return 15.00m; // Mesmo estado
            else if (EstadosNordeste.Contains(estadoOrigem) && EstadosNordeste.Contains(estadoDestino))
                return 25.00m; // Nordeste
            else
                return 35.00m; // Outras regiões
        }

        private string ObterEstadoPorCep(string cep)
        {
            var prefixo = int.Parse(cep.Substring(0, 2));

            return prefixo switch
            {
                >= 50 and <= 59 => "PE",
                >= 40 and <= 49 => "BA",
                >= 60 and <= 63 => "CE",
                _ => "OUTROS"
            };
        }

        private readonly string[] EstadosNordeste = { "PE", "BA", "CE", "RN", "PB", "AL", "SE", "MA", "PI" };
    }
}
