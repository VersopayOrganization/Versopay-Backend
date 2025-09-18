namespace VersopayBackend.Dtos
{
    public class PedidosResponseDto
    {
        public int TotalRegistros { get; set; }

        public IEnumerable<PedidoDto> Pedidos { get; set; }
    }
}
