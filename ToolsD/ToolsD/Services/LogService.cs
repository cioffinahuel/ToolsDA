using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using ToolsD.Models;

namespace ToolsD.Services
{
    public class LogService
    {
        private readonly FileService Tools;
        private readonly IConfiguration _configuration;
        public LogService(FileService _tools, IConfiguration configuration)
        {
            Tools = _tools;
            _configuration = configuration;
        }


        public async Task<ResponseData> BuscarLogs(RequestLogs request)
        {
            ResponseData responseData = await RealizarRequest((DateTime)request.FechaDesde, (DateTime)request.FechaHasta, new ResponseData());

            if (request.BuscarPendientes)
            {
                var pendientes = await GetFechasPendientes();
                if (pendientes.Any())
                {
                    foreach (var pendiente in pendientes) 
                    {
                        var resp =  RealizarRequest(pendiente, pendiente, responseData);
                        // Actualizar la respuesta
                        if (resp != null)
                            responseData =  await resp;
                    }
                    //Parallel.ForEach(pendientes, f =>
                    //{
                    //    var resp = RealizarRequest(f, f, responseData).Result;
                    //    // Actualizar la respuesta
                    //    if (resp != null)
                    //        responseData = resp;
                    //});
                }
            }
            if (request.Resumido ) { 
                responseData.Errores = responseData.Errores.GroupBy(x => x.ContieneErrorComun).Select(x => new ErrorData(
                    x.Count(),
                    x.First().TransportName,
                    x.First().RequestMessage,
                    x.First().ResponseMessage,
                    x.Key
                ))
                .Where(X=>X.ContieneErrorComun!= null)
                .ToList();
            }

            return responseData;
        }

        private async Task<List<DateTime>> GetFechasPendientes()
        {
            DateTime fechaInicio = DateTime.Now.AddDays(-15);
            DateTime fechaFin = DateTime.Now;

            // Obtener todas las fechas que no fueron consultadas
            List<DateTime> fechasNoConsultadas = new();
            List<DateTime> fechasConsultadas = await Tools.ReadDates();

            for (DateTime f = fechaInicio; f <= fechaFin; f = f.AddDays(1))
            {
                // Verificar si la fecha fue consultada
                bool fechaConsultada = fechasConsultadas.Contains(f.Date);
                // Si la fecha no fue consultada, agregarla a la lista
                if (!fechaConsultada)
                    fechasNoConsultadas.Add(f);

            }

            return fechasNoConsultadas;
        }

        public async Task<ResponseData> RealizarRequest(DateTime fecha, DateTime fechah, ResponseData responseData)
        {
            HttpResponseMessage response = new();
            try
            {
                string fechaDesde = fecha.ToString("yyyy-MM-dd") + "T00:01:00";
                string fechaHasta = fechah.ToString("yyyy-MM-dd") + "T23:59:59";

                using (HttpClient httpClient = new HttpClient())
                {
                    string apiUrl = _configuration["ApiUrl"];
                    var requestBody = new
                    {
                        credentials = new
                        {
                            username = _configuration["User"],
                            password = _configuration["Password"]
                        },
                        fechaDesde,
                        fechaHasta
                    };

                    response = await httpClient.PostAsJsonAsync(apiUrl, requestBody);
                    responseData = await ProcessResponse(response, responseData, fecha, fechah);

                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(response.StatusCode);
                //Console.WriteLine($"Error al realizar request: {ex.Message}");
            }

            return responseData;
        }
        public async Task<ResponseData> ProcessResponse(HttpResponseMessage response, ResponseData responseData, DateTime fecha, DateTime fechah)
        {

            try
            {
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    if (!string.IsNullOrEmpty(responseContent))

                        if (!string.IsNullOrEmpty(responseContent) && responseContent.Length > 100)
                        {
                            var responseObj = Tools.DeserializeResponse(responseContent);
                            var erroresComunes = await Tools.ReadErrors();

                            var errores = GetErrores(responseObj, erroresComunes);
                            responseData.Errores.AddRange(errores);

                            await Tools.SaveDates(fecha, fechah);

                            string rangoFecha = GetRangoFecha(fecha, fechah);
                            responseData.FechasConsultadas += rangoFecha;

                            responseData.MensajeData.AddRange(responseObj.Mensajes.MensajeData);
                            Tools.PrintColor($"Rango consultado desde:", $" {fecha} - hasta:{fechah}", ConsoleColor.Green);

                        }
                        else
                        {
                            Tools.PrintColor($"Rango consultado desde:", $" {fecha} - hasta:{fechah}");
                            Tools.PrintColor("La respuesta del servidor está vacía.", null);
                        }
                }
                else
                {
                    AddFechaError(responseData, fecha, fechah); 
                    throw new Exception($"Error en la solicitud. Código de estado: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
            return responseData;
        }

        public List<ErrorData> GetErrores(Response responseObj, string[] erroresComunes)
        {
            return responseObj.Mensajes.MensajeData
               .Select(x => new ErrorData(
                   1,
                   x.transportname,
                   x.requestmessage,
                   x.responseMessage,
                   erroresComunes.FirstOrDefault(e => x.responseMessage.Contains(e)) ?? x.responseMessage.Substring(0, Math.Min(200, x.responseMessage.Length)))
               ).ToList();
        }

        public string GetRangoFecha(DateTime fecha, DateTime fechah)
        {
            string rangoFecha = fecha.ToString("dd-MM-yyyy") + ";";
            if (fecha != fechah)
                rangoFecha = $"[{fecha.ToString("dd-MM-yyyy")} - {fechah.ToString("dd-MM-yyyy")}];";

            return rangoFecha;
        }

        public void AddFechaError(ResponseData responseData, DateTime fecha, DateTime fechah)
        {
            string rangoFecha = GetRangoFecha(fecha, fechah);
            responseData.FechasError += rangoFecha;
        }
    }
}
