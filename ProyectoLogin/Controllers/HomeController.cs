﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using ProyectoLogin.Models; // Asegúrate de que tienes un modelo 'Usuario' adecuado
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace ProyectoLogin.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            // Obtener el correo electrónico del usuario autenticado
            string userEmail = User.FindFirstValue(ClaimTypes.Email);

            // Redirigir al perfil del usuario si no es el correo específico
            if (userEmail != "ebed.ml32@gmail.com")
            {
                return RedirectToAction("Perfil", "Usuario");
            }

            // Si es el correo específico, continúa con la lógica para cargar los usuarios
            List<Usuario> usuarios = new List<Usuario>();
            string connectionString = _configuration.GetConnectionString("cadenaSQL");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string sqlQuery = "SELECT * FROM dbo.USUARIO";
                using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                {
                    con.Open();
                    using (SqlDataReader dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            var usuario = new Usuario()
                            {
                                IdUsuario = Convert.ToInt32(dataReader["IdUsuario"]),
                                NombreUsuario = Convert.ToString(dataReader["NombreUsuario"]),
                                // Suponiendo que tu modelo Usuario tiene estas propiedades
                            };
                            usuarios.Add(usuario);
                        }
                    }
                }
            }
            return View(usuarios);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(Usuario usuario)
        {
            string connectionString = _configuration.GetConnectionString("cadenaSQL");
            int rowsAffected = 0;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string sqlQuery = "UPDATE dbo.USUARIO SET NombreUsuario = @nombre WHERE IdUsuario = @id";
                using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                {
                    cmd.Parameters.Add("@nombre", SqlDbType.NVarChar, 50).Value = usuario.NombreUsuario;
                    cmd.Parameters.AddWithValue("@id", usuario.IdUsuario);

                    con.Open();
                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }
            }

            if (rowsAffected > 0)
            {
                // Si la actualización fue exitosa, devuelve el ID y el nombre del usuario.
                return Json(new { success = true, message = "Usuario actualizado correctamente.", id = usuario.IdUsuario, nombre = usuario.NombreUsuario });
            }
            else
            {
                // Si no hubo filas afectadas, devuelve un mensaje de error.
                return Json(new { success = false, message = "Error al actualizar el usuario." });
            }
        }
        [HttpPost]
        public async Task<IActionResult> Eliminar([FromForm] int id)
        {
            string connectionString = _configuration.GetConnectionString("cadenaSQL");
            int rowsAffected = 0;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string sqlQuery = "DELETE FROM dbo.USUARIO WHERE IdUsuario = @id";
                using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    con.Open();
                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }
            }

            if (rowsAffected > 0)
            {
                // Si la eliminación fue exitosa, devuelve el ID del usuario y un mensaje de éxito.
                return Json(new { success = true, message = "Usuario eliminado correctamente.", id = id });
            }
            else
            {
                // Si no se afectaron filas, es probable que el usuario no existiera.
                return Json(new { success = false, message = "Error al eliminar el usuario. Es posible que no exista." });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> CerrarSesion()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("IniciarSesion", "Inicio");
        }
        [HttpGet]
        public IActionResult GetUserFiles(int IdUsuario)
        {
            List<Archivo> archivos = new List<Archivo>();
            string connectionString = _configuration.GetConnectionString("cadenaSQL");

            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT Id, Nombre, Tipo, Contenido, FechaCarga FROM dbo.Archivos WHERE IdUsuario = @IdUsuario";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdUsuario", IdUsuario);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var archivo = new Archivo
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                                Tipo = reader.GetString(reader.GetOrdinal("Tipo")),
                                Contenido = reader["Contenido"] as byte[], // Suponiendo que Contenido es un array de bytes
                                FechaCarga = reader.GetDateTime(reader.GetOrdinal("FechaCarga")),
                                IdUsuario = IdUsuario // Suponiendo que tienes esta propiedad en tu modelo
                            };
                            archivos.Add(archivo);
                        }
                    }
                }
            }

            return Json(archivos);
        }
   
    }


}

