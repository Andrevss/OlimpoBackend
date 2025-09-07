namespace OlimpoBackend.Services
{
    using OlimpoBackend.Models;
    using System.Net;
    using System.Net.Mail;

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarEmailPedidoParaLoja(PedidoRequest pedido, decimal valorTotal, string pagamentoId)
        {
            var smtpClient = new SmtpClient(_configuration["Email:SmtpHost"])
            {
                Port = int.Parse(_configuration["Email:SmtpPort"]),
                Credentials = new NetworkCredential(
                    _configuration["Email:Username"],
                    _configuration["Email:Password"]
                ),
                EnableSsl = true,
            };

            var corpoEmail = $@"
            <h2>NOVO PEDIDO - OLIMPO 081</h2>
            <h3>Dados do Cliente:</h3>
            <p><strong>Nome:</strong> {pedido.ClienteNome}</p>
            <p><strong>Email:</strong> {pedido.ClienteEmail}</p>
            <p><strong>Telefone:</strong> {pedido.ClienteTelefone}</p>
            
            <h3>Endereço de Entrega:</h3>
            <p>{pedido.Endereco}</p>
            <p>{pedido.Cidade}, {pedido.Estado}</p>
            <p><strong>CEP:</strong> {pedido.Cep}</p>
            
            <h3>Produtos:</h3>
            <ul>
            {string.Join("", pedido.Itens.Select(item =>
                    $"<li>Produto ID: {item.ProdutoId} - Tamanho: {item.Tamanho} - Qtd: {item.Quantidade} - Preço: R$ {item.PrecoUnitario:F2}</li>"
                ))}
            </ul>
            
            <h3><strong>Total: R$ {valorTotal:F2}</strong></h3>
            <p><strong>ID do Pagamento:</strong> {pagamentoId}</p>
        ";

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["Email:FromAddress"], "Olimpo 081"),
                Subject = $"Novo Pedido - {pedido.ClienteNome}",
                Body = corpoEmail,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(_configuration["Email:ToAddress"]);
            await smtpClient.SendMailAsync(mailMessage);
        }

        public async Task EnviarEmailConfirmacaoParaCliente(string emailCliente, string nomeCliente, string numeroPedido)
        {
            var smtpClient = new SmtpClient(_configuration["Email:SmtpHost"])
            {
                Port = int.Parse(_configuration["Email:SmtpPort"]),
                Credentials = new NetworkCredential(
                    _configuration["Email:Username"],
                    _configuration["Email:Password"]
                ),
                EnableSsl = true,
            };

            var corpoEmail = $@"
            <h2>Pedido Confirmado - Olimpo 081</h2>
            <p>Olá {nomeCliente},</p>
            <p>Seu pedido foi confirmado com sucesso!</p>
            <p><strong>Número do pedido:</strong> {numeroPedido}</p>
            <p>Entraremos em contato em breve com informações sobre o envio.</p>
            <p>Obrigado por escolher a Olimpo 081!</p>
        ";

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["Email:FromAddress"], "Olimpo 081"),
                Subject = "Pedido Confirmado - Olimpo 081",
                Body = corpoEmail,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(emailCliente);
            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
