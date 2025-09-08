using OlimpoBackend.Models;

namespace OlimpoBackend.Services
{
    public interface IEstoqueService
    {
        Task<List<Produto>> ObterProdutos();
        Task<Produto?> ObterProdutoPorId(int id);
        Task<bool> ReservarEstoque(int produtoId, string tamanho, int quantidade, string sessionId);
        Task ConfirmarReserva(int produtoId, string tamanho, int quantidade, string sessionId);
        Task CancelarReserva(int produtoId, string tamanho, int quantidade, string sessionId);
        Task<Dictionary<string, int>> ObterEstoqueDisponivel(int produtoId);
    }
}
