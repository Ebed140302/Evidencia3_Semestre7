using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ProyectoLogin.Models;
using System.Security.Claims;
using System.IO;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ProyectoLogin.Controllers
{
    [Authorize]
    public class UsuarioController : Controller
    {
        private readonly IConfiguration _configuration;

        public UsuarioController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> Perfil()
        {
            var NombreUsuario = User.FindFirstValue(ClaimTypes.Name); // Obtener el nombre de usuario
            var usuario = await ObtenerUsuarioPorNombreUsuario(NombreUsuario);
            var archivos = await ObtenerArchivosPorUsuarioId(usuario.IdUsuario); // Puedes usar el ID del usuario obtenido

            var viewModel = new UsuarioConArchivosViewModel
            {
                Usuario = usuario,
                Archivos = archivos
            };

            return View(viewModel);
        }

        private async Task<Usuario> ObtenerUsuarioPorNombreUsuario(string NombreUsuario)
        {
            Usuario usuario = null;
            string connectionString = _configuration.GetConnectionString("cadenaSQL");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string sqlQuery = "SELECT IdUsuario, NombreUsuario, Correo FROM dbo.USUARIO WHERE NombreUsuario = @NombreUsuario";

                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.AddWithValue("@NombreUsuario", NombreUsuario);

                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            usuario = new Usuario
                            {
                                IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                                NombreUsuario = reader.GetString(reader.GetOrdinal("NombreUsuario")),
                                Correo = reader.GetString(reader.GetOrdinal("Correo")),
                                // No incluimos la contraseña en la asignación
                            };
                        }
                    }
                }
            }

            return usuario;
        }

        private async Task<List<Archivo>> ObtenerArchivosPorUsuarioId(int usuarioId)
        {
            List<Archivo> archivos = new List<Archivo>();
            string connectionString = _configuration.GetConnectionString("cadenaSQL");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string sqlQuery = "SELECT Id, Nombre, Tipo, FechaCarga FROM dbo.Archivos WHERE IdUsuario = @UsuarioId";
                using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);

                    await con.OpenAsync();
                    using (SqlDataReader dataReader = await cmd.ExecuteReaderAsync())
                    {
                        while (dataReader.Read())
                        {
                            var archivo = new Archivo()
                            {
                                Id = dataReader.GetInt32(dataReader.GetOrdinal("Id")),
                                Nombre = dataReader.GetString(dataReader.GetOrdinal("Nombre")),
                                Tipo = dataReader.GetString(dataReader.GetOrdinal("Tipo")),
                                FechaCarga = dataReader.GetDateTime(dataReader.GetOrdinal("FechaCarga"))
                                // El contenido no se incluye para evitar la carga de datos pesados
                            };
                            archivos.Add(archivo);
                        }
                    }
                }
            }

            return archivos;
        }

        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> CargarArchivo(IFormFile archivo)
        {
            // Obtener el nombre de usuario del usuario autenticado
            var nombreUsuario = User.FindFirstValue(ClaimTypes.Name);

            if (archivo != null && archivo.Length > 0)
            {
                byte[] contenido;
                using (var ms = new MemoryStream())
                {
                    await archivo.CopyToAsync(ms);
                    contenido = ms.ToArray();
                }

                string connectionString = _configuration.GetConnectionString("cadenaSQL");
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    // Obtener el IdUsuario correspondiente al nombre de usuario
                    string obtenerIdUsuarioQuery = "SELECT IdUsuario FROM dbo.USUARIO WHERE NombreUsuario = @NombreUsuario";
                    using (SqlCommand idUsuarioCommand = new SqlCommand(obtenerIdUsuarioQuery, con))
                    {
                        idUsuarioCommand.Parameters.AddWithValue("@NombreUsuario", nombreUsuario);
                        await con.OpenAsync();
                        var idUsuario = (int)await idUsuarioCommand.ExecuteScalarAsync();

                        // Insertar el archivo asociado al usuario
                        string sqlQuery = "INSERT INTO dbo.Archivos (Nombre, Tipo, Contenido, FechaCarga, IdUsuario) VALUES (@Nombre, @Tipo, @Contenido, @FechaCarga, @IdUsuario)";
                        using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@Nombre", archivo.FileName);
                            cmd.Parameters.AddWithValue("@Tipo", archivo.ContentType);
                            cmd.Parameters.AddWithValue("@Contenido", contenido);
                            cmd.Parameters.AddWithValue("@FechaCarga", DateTime.UtcNow);
                            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario); // Usar el IdUsuario obtenido

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                return RedirectToAction("Perfil");
            }

            ModelState.AddModelError("", "Debes seleccionar un archivo para cargar.");
            return View("Perfil");
        }
        public IActionResult Descargar(int id)
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("cadenaSQL")))
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand("SELECT Nombre, Tipo, Contenido FROM Archivos WHERE Id = @Id", con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader dataReader = cmd.ExecuteReader())
                    {
                        if (dataReader.Read())
                        {
                            string nombre = dataReader.GetString(0);
                            string tipo = dataReader.GetString(1);
                            byte[] contenido = (byte[])dataReader["Contenido"];

                            return File(contenido, tipo, nombre);
                        }
                    }
                }
            }

            return NotFound(); // Archivo no encontrado
        }




    }
}
