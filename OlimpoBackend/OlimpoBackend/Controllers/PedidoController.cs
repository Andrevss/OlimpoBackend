namespace OlimpoBackend.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using OlimpoBackend.Models;
    using OlimpoBackend.Services;
    using System.Text.Json;

    // Adicione este modelo para receber os dados do frete
    public class CalcularFreteRequest
    {
        public string Cep { get; set; }
        public List<ItemPedidoRequest> Itens { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class PedidosController : ControllerBase
    {
        private readonly IEstoqueService _estoqueService;
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly ICorreiosService _correiosService;
        private readonly IEmailService _emailService;

        public PedidosController(
            IEstoqueService estoqueService,
            IMercadoPagoService mercadoPagoService,
            ICorreiosService correiosService,
            IEmailService emailService)
        {
            _estoqueService = estoqueService;
            _mercadoPagoService = mercadoPagoService;
            _correiosService = correiosService;
            _emailService = emailService;
        }

        [HttpPost("calcular-frete")]
        public async Task<IActionResult> CalcularFrete([FromBody] CalcularFreteRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Cep) || request.Itens == null)
                {
                    return BadRequest(new { erro = "Dados inválidos" });
                }

                var valorFrete = await _correiosService.CalcularFrete(request.Cep, request.Itens);
                return Ok(new { valorFrete });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CriarPedido([FromBody] PedidoRequest request)
        {
            var sessionId = HttpContext.Session.Id;

            try
            {
                var valorFrete = await _correiosService.CalcularFrete(request.Cep, request.Itens);
                var valorProdutos = request.Itens.Sum(i => i.PrecoUnitario * i.Quantidade);
                var valorTotal = valorProdutos + valorFrete;

                var urlPagamento = await _mercadoPagoService.CriarPagamento(request, valorTotal, sessionId);

                return Ok(new
                {
                    urlPagamento,
                    valorTotal,
                    valorFrete,
                    sessionId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        [HttpPost("webhook/mercadopago")]
        public async Task<IActionResult> WebhookMercadoPago([FromBody] dynamic webhookData)
        {
            try
            {
                var tipo = webhookData.type?.ToString();
                var acao = webhookData.action?.ToString();

                if (tipo == "payment" && acao == "payment.updated")
                {
                    var pagamentoId = webhookData.data?.id?.ToString();
                    var pagamento = await _mercadoPagoService.ConsultarPagamento(pagamentoId);

                    var status = pagamento.GetProperty("status").GetString();
                    var sessionId = pagamento.GetProperty("external_reference").GetString();
                    var metadata = pagamento.GetProperty("metadata");

                    if (status == "approved")
                    {
                        var itens = JsonSerializer.Deserialize<List<ItemPedidoRequest>>(
                            metadata.GetProperty("itens").GetString());

                        foreach (var item in itens)
                        {
                            await _estoqueService.ConfirmarReserva(
                                item.ProdutoId, item.Tamanho, item.Quantidade, sessionId);
                        }

                        var pedidoRequest = new PedidoRequest
                        {
                            ClienteNome = metadata.GetProperty("cliente_nome").GetString(),
                            ClienteEmail = metadata.GetProperty("cliente_email").GetString(),
                            ClienteTelefone = metadata.GetProperty("cliente_telefone").GetString(),
                            Endereco = metadata.GetProperty("endereco_completo").GetString(),
                            Itens = itens
                        };

                        var valorTotal = pagamento.GetProperty("transaction_amount").GetDecimal();

                        await _emailService.EnviarEmailPedidoParaLoja(pedidoRequest, valorTotal, pagamentoId);
                        await _emailService.EnviarEmailConfirmacaoParaCliente(
                            pedidoRequest.ClienteEmail,
                            pedidoRequest.ClienteNome,
                            pagamentoId);
                    }
                    else if (status == "cancelled" || status == "rejected")
                    {
                        var itens = JsonSerializer.Deserialize<List<ItemPedidoRequest>>(
                            metadata.GetProperty("itens").GetString());

                        foreach (var item in itens)
                        {
                            await _estoqueService.CancelarReserva(
                                item.ProdutoId, item.Tamanho, item.Quantidade, sessionId);
                        }
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }
    }
}
