namespace OlimpoBackend.Services
{
    using OlimpoBackend.Models;
    using System.Text.Json;

    public class EstoqueService : IEstoqueService
    {
        private readonly IConfiguration _configuration;
        private readonly string _arquivoEstoque;
        private static readonly Dictionary<string, DateTime> _reservasTemporarias = new();
        private static readonly object _lock = new();
        private readonly TimeSpan _tempoReserva = TimeSpan.FromMinutes(15);

        public EstoqueService(IConfiguration configuration)
        {
            _configuration = configuration;
            _arquivoEstoque = Path.Combine(Directory.GetCurrentDirectory(), "Data", "estoque.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_arquivoEstoque));
        }

        private async Task SalvarEstoque(List<Produto> produtos)
        {
            var json = JsonSerializer.Serialize(produtos, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await File.WriteAllTextAsync(_arquivoEstoque, json);
        }

        private async Task<List<Produto>> CarregarEstoque()
        {
            if (!File.Exists(_arquivoEstoque))
            {
                var produtosIniciais = _configuration.GetSection("Produtos").Get<List<Produto>>();
                await SalvarEstoque(produtosIniciais);
                return produtosIniciais;
            }
            var json = await File.ReadAllTextAsync(_arquivoEstoque);
            return JsonSerializer.Deserialize<List<Produto>>(json) ?? new List<Produto>();
        }

        private int ContarReservas(int produtoId, string tamanho)
        {
            var prefixo = $"{produtoId}-{tamanho}-";
            return _reservasTemporarias.Count(r => r.Key.StartsWith(prefixo));
        }

        private void LimparReservasExpiradas()
        {
            var agora = DateTime.UtcNow;
            var expiradas = _reservasTemporarias
                .Where(r => r.Value < agora)
                .Select(r => r.Key)
                .ToList();

            foreach (var chave in expiradas)
            {
                _reservasTemporarias.Remove(chave);
            }
        }

        public async Task<List<Produto>> ObterProdutos()
        {
            return await CarregarEstoque();
        }

        public async Task<Produto?> ObterProdutoPorId(int id)
        {
            var produtos = await CarregarEstoque();
            return produtos.FirstOrDefault(p => p.Id == id);
        }

        public async Task<bool> ReservarEstoque(int produtoId, string tamanho, int quantidade, string sessionId)
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    LimparReservasExpiradas();

                    var chaveReserva = $"{produtoId}-{tamanho}-{sessionId}";

                    if (_reservasTemporarias.ContainsKey(chaveReserva))
                    {
                        _reservasTemporarias[chaveReserva] = DateTime.UtcNow.Add(_tempoReserva);
                        return true;
                    }

                    var produtos = CarregarEstoque().GetAwaiter().GetResult();
                    var produto = produtos.FirstOrDefault(p => p.Id == produtoId);

                    if (produto?.Estoque.ContainsKey(tamanho) == true)
                    {
                        var disponivel = produto.Estoque[tamanho];
                        var reservado = ContarReservas(produtoId, tamanho);

                        if (disponivel - reservado >= quantidade)
                        {
                            _reservasTemporarias[chaveReserva] = DateTime.UtcNow.Add(_tempoReserva);
                            return true;
                        }
                    }
                    return false;
                }
            });
        }

        public async Task ConfirmarReserva(int produtoId, string tamanho, int quantidade, string sessionId)
        {
            await Task.Run(async () =>
            {
                lock (_lock)
                {
                    var chaveReserva = $"{produtoId}-{tamanho}-{sessionId}";

                    if (_reservasTemporarias.ContainsKey(chaveReserva))
                    {
                        _reservasTemporarias.Remove(chaveReserva);

                        var produtos = CarregarEstoque().GetAwaiter().GetResult();
                        var produto = produtos.FirstOrDefault(p => p.Id == produtoId);

                        if (produto?.Estoque.ContainsKey(tamanho) == true)
                        {
                            produto.Estoque[tamanho] -= quantidade;
                            SalvarEstoque(produtos).GetAwaiter().GetResult();
                        }
                    }
                }
            });
        }

        public async Task CancelarReserva(int produtoId, string tamanho, int quantidade, string sessionId)
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    var chaveReserva = $"{produtoId}-{tamanho}-{sessionId}";
                    _reservasTemporarias.Remove(chaveReserva);
                }
            });
        }

        public async Task<Dictionary<string, int>> ObterEstoqueDisponivel(int produtoId)
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    LimparReservasExpiradas();

                    var produtos = CarregarEstoque().GetAwaiter().GetResult();
                    var produto = produtos.FirstOrDefault(p => p.Id == produtoId);

                    if (produto == null) return new Dictionary<string, int>();

                    var estoqueDisponivel = new Dictionary<string, int>();

                    foreach (var item in produto.Estoque)
                    {
                        var reservado = ContarReservas(produtoId, item.Key);
                        estoqueDisponivel[item.Key] = Math.Max(0, item.Value - reservado);
                    }

                    return estoqueDisponivel;
                }
            });
        }
    }
}
