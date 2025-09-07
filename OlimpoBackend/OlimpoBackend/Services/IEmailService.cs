using OlimpoBackend.Models;

namespace OlimpoBackend.Services
{
    public interface IEmailService
    {
        Task EnviarEmailPedidoParaLoja(PedidoRequest pedido, decimal valorTotal, string pagamentoId);
        Task EnviarEmailConfirmacaoParaCliente(string emailCliente, string nomeCliente, string numeroPedido);
    }
}
