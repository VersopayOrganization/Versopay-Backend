namespace VersopayBackend.Dtos
{
    public class PedidosTotalResponseDto
    {
        public int TotalRegistros { get; set; }

        public IEnumerable<PedidoResponseDto> Pedidos { get; set; }
    }
}
