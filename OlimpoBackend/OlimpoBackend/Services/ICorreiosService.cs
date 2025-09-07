using OlimpoBackend.Models;
namespace OlimpoBackend.Services
{
    public interface ICorreiosService
    {
        Task<decimal> CalcularFrete(string cepDestino, List<ItemPedidoRequest> itens);
    }
}
