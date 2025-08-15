using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.WebJobs;

namespace VersopayFunctions
{
    public class KycBlobHandler
    {
        private readonly string _sqlConn;
        private readonly ILogger _logger;

        public KycBlobHandler(IConfiguration cfg, ILoggerFactory loggerFactory)
        {
            _sqlConn = cfg.GetConnectionString("SqlConnection")!;
            _logger = loggerFactory.CreateLogger<KycBlobHandler>();
        }

        [Function("KycBlobHandler")]
        public async Task Run([BlobTrigger("kyc-docs/{*blobName}", Connection = "BlobConnection")] Stream stream, string blobName)
        {
            // Ex.: "usuarios/123/frente-<guid>.jpg"
            try
            {
                var segs = blobName.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segs.Length < 3 || !int.TryParse(segs[1], out var usuarioId))
                {
                    _logger.LogWarning("BlobName inesperado: {blobName}", blobName);
                    return;
                }

                var file = segs[^1]; // "frente-<guid>.jpg"
                var dash = file.IndexOf('-');
                var dot = file.LastIndexOf('.');
                if (dash <= 0 || dot <= dash) { _logger.LogWarning("Nome inesperado: {file}", file); return; }

                var parte = file[..dash].ToLowerInvariant(); // "frente" | "verso" | "selfie" | "cnpj"
                var ext = file[dot..].ToLowerInvariant();  // ".jpg" | ".pdf"

                // --- calcula SHA-256 streaming ---
                stream.Position = 0;
                using var sha = SHA256.Create();
                var buffer = new byte[81920];
                int read;
                while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                    sha.TransformBlock(buffer, 0, read, null, 0);
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                var hashHex = Convert.ToHexString(sha.Hash!); // 64 chars

                // --- valida assinatura mágica ---
                stream.Position = 0;
                var head = new byte[8];
                await stream.ReadAsync(head);

                bool assinaturaOk = parte switch
                {
                    "frente" or "verso" or "selfie" => IsJpeg(head, stream),
                    "cnpj" => IsPdf(head),
                    _ => false
                };

                var novoStatus = assinaturaOk ? 2 /*Verificado*/ : 3 /*Rejeitado*/;

                // --- atualiza SQL apenas na coluna correspondente ---
                await AtualizarAsync(usuarioId, parte, blobName, hashHex, novoStatus);
                _logger.LogInformation("Atualizado {UsuarioId}/{Parte}: {Status}", usuarioId, parte, novoStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar blob {blob}", blobName);
                throw; // opcional: deixar fila de poison
            }
        }

        private static bool IsJpeg(byte[] head, Stream stream)
        {
            // JPEG: começa com FF D8 e termina com FF D9
            if (head.Length < 2) return false;
            bool start = head[0] == 0xFF && head[1] == 0xD8;
            if (!start) return false;
            if (!stream.CanSeek) return true; // não dá pra checar final
            long pos = stream.Position;
            try
            {
                stream.Seek(-2, SeekOrigin.End);
                var tail = new byte[2];
                stream.Read(tail, 0, 2);
                return tail[0] == 0xFF && tail[1] == 0xD9;
            }
            catch { return true; } // se não conseguir, aceitamos só o head
            finally { stream.Position = pos; }
        }

        private static bool IsPdf(byte[] head)
        {
            // PDF: "%PDF-" (25 50 44 46 2D)
            return head.Length >= 5 &&
                   head[0] == 0x25 && head[1] == 0x50 && head[2] == 0x44 &&
                   head[3] == 0x46 && head[4] == 0x2D;
        }

        private async Task AtualizarAsync(int usuarioId, string parte, string blobName, string hashHex, int novoStatus)
        {
            // mapeia colunas conforme a parte
            string caminhoCol, statusCol, hashCol;
            switch (parte)
            {
                case "frente":
                    caminhoCol = "FrenteRgCaminho";
                    statusCol = "FrenteRgStatus";
                    hashCol = "FrenteRgAssinaturaSha256";
                    break;
                case "verso":
                    caminhoCol = "VersoRgCaminho";
                    statusCol = "VersoRgStatus";
                    hashCol = "VersoRgAssinaturaSha256";
                    break;
                case "selfie":
                    caminhoCol = "SelfieDocCaminho";
                    statusCol = "SelfieDocStatus";
                    hashCol = "SelfieDocAssinaturaSha256";
                    break;
                case "cnpj":
                    caminhoCol = "CartaoCnpjCaminho";
                    statusCol = "CartaoCnpjStatus";
                    hashCol = "CartaoCnpjAssinaturaSha256";
                    break;
                default: return;
            }

            var sql = $@"
                    IF EXISTS (SELECT 1 FROM Documentos WHERE UsuarioId = @UsuarioId)
                    BEGIN
                        UPDATE Documentos
                            SET {statusCol} = @Status,
                                {hashCol} = @Hash,
                                DataAtualizacao = SYSUTCDATETIME()
                        WHERE UsuarioId = @UsuarioId AND {caminhoCol} = @BlobName
                    END
                    ELSE
                    BEGIN
                        -- cria registro caso alguém tenha subido direto sem confirm (opcional)
                        INSERT INTO Documentos (UsuarioId, {caminhoCol}, {statusCol}, {hashCol}, DataAtualizacao)
                        VALUES (@UsuarioId, @BlobName, @Status, @Hash, SYSUTCDATETIME())
                    END";

            using var cn = new SqlConnection(_sqlConn);
            await cn.OpenAsync();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add(new SqlParameter("@UsuarioId", SqlDbType.Int) { Value = usuarioId });
            cmd.Parameters.Add(new SqlParameter("@BlobName", SqlDbType.NVarChar, 260) { Value = blobName });
            cmd.Parameters.Add(new SqlParameter("@Status", SqlDbType.Int) { Value = novoStatus });
            cmd.Parameters.Add(new SqlParameter("@Hash", SqlDbType.NVarChar, 64) { Value = hashHex });
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
