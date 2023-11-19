namespace ProyectoLogin.Models
{
    public class Archivo
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; }
        public byte[] Contenido { get; set; }
        public DateTime FechaCarga { get; set; }
        public int UsuarioId { get; set; }
        public int IdUsuario { get; internal set; }
    }
}

