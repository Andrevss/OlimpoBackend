namespace OlimpoBackend.Models
{
    public class Produto
    {
        public int Id {get; set; }
        public string? Nome {get; set; }
        public string? Descricao {get; set; }
        public decimal Preco {get; set; }
        public bool Ativo {get; set; }
        public Dictionary<string, int> Estoque { get; set; } = new();
    }

    public class PedidoRequest
    {
        public string? ClienteNome { get; set; }
        public string? ClienteEmail { get; set; }
        public string? ClienteTelefone { get; set; }
        public string? Cep { get; set; }
        public string? Endereco { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Estado { get; set; }
        public List<ItemPedidoRequest> Itens { get; set; } = new();
    }

    public class ItemPedidoRequest
    {
        public int ProdutoId { get; set; }
        public int Quantidade { get; set; }
        public string? Tamanho { get; set; }
        public decimal PrecoUnitario { get; set; }
    }

    public class ReservaRequest
    {
        public int ProdutoId { get; set; }
        public int Quantidade { get; set; }
        public string? Tamanho { get; set; }
    }

    public class CalcularFreteRequest
    {
        public string Cep { get; set; }
        public List<ItemPedidoRequest> Itens { get; set; }
    }
}
