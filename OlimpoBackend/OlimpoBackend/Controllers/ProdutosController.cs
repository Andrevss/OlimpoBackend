namespace OlimpoBackend.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using OlimpoBackend.Models;
    using OlimpoBackend.Services;

    [ApiController]
    [Route("api/[controller]")]
    public class ProdutosController : ControllerBase
    {
        private readonly IEstoqueService _estoqueService;

        public ProdutosController(IEstoqueService estoqueService)
        {
            _estoqueService = estoqueService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Produto>>> ObterTodos()
        {
            Console.WriteLine("Endpoint /api/produtos foi chamado");  // LOG
            var produtos = await _estoqueService.ObterProdutos();

            // Adicionar estoque disponível em tempo real
            foreach (var produto in produtos)
            {
                produto.Estoque = await _estoqueService.ObterEstoqueDisponivel(produto.Id);
            }

            return Ok(produtos.Where(p => p.Ativo));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Produto>> ObterPorId(int id)
        {
            var produto = await _estoqueService.ObterProdutoPorId(id);
            if (produto == null) return NotFound();

            produto.Estoque = await _estoqueService.ObterEstoqueDisponivel(id);
            return Ok(produto);
        }

        [HttpPost("reservar")]
        public async Task<IActionResult> ReservarProduto([FromBody] ReservaRequest request)
        {
            var sessionId = HttpContext.Session.Id;

            var sucesso = await _estoqueService.ReservarEstoque(
                request.ProdutoId,
                request.Tamanho,
                request.Quantidade,
                sessionId
            );

            if (sucesso)
            {
                return Ok(new
                {
                    reservado = true,
                    tempoExpiracao = DateTime.UtcNow.AddMinutes(15),
                    sessionId = sessionId
                });
            }

            return BadRequest(new { erro = "Produto indisponível" });
        }
    }
}
