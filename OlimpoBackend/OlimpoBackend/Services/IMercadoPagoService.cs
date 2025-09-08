using OlimpoBackend.Models;

namespace OlimpoBackend.Services
{
    public interface IMercadoPagoService
    {
        Task<string> CriarPagamento(PedidoRequest pedido, decimal valorTotal, string sessionId);
        Task<dynamic> ConsultarPagamento(string pagamentoId);
    }
}
