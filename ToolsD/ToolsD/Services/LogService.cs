using CommunityToolkit.Maui.Storage;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using ToolsD.Models;

namespace ToolsD.Services
{
    public class LogService
    {
        private readonly FileService Tools;
        private readonly IConfiguration _configuration;
        IFileSaver _fileSaver;

        public LogService(FileService _tools, IConfiguration configuration, IFileSaver fileSaver)
        {
            Tools = _tools;
            _configuration = configuration;
            _fileSaver = fileSaver;
        }
        public async Task<ResponseData> BuscarLogs(RequestLogs request)
        {
            ResponseData responseData = new();

            // Divide un rango en un listado de fechas porque la api no banca rangos de fecha amplios (rip api)
            // Ejecuta ese list de fechas
            await EjecutarRequestRangoFecha(
                                            await GetFechasPendientes(false, (DateTime)request.FechaDesde, (DateTime)request.FechaHasta),
                                            responseData);
         

            if (request.BuscarPendientes)
            {
                var pendientes = await GetFechasPendientes(true);
                if (pendientes.Any())
                {
                    await EjecutarRequestRangoFecha(pendientes,responseData);

                    //Parallel.ForEach(pendientes, f =>
                    //{
                    //    var resp = RealizarRequest(f, f, responseData).Result;
                    //    // Actualizar la respuesta
                    //    if (resp != null)
                    //        responseData = resp;
                    //});
                }
            }
       
            responseData.ErroresAgrupado = responseData.Errores.GroupBy(x => x.ContieneErrorComun).Select(x => new ErrorData(
                x.Count(),
                x.First().TransportName,
                x.First().RequestMessage,
                x.First().ResponseMessage,
                x.Key
            ))
            .Where(X=>X.ContieneErrorComun!= null)
            .ToList();
            
            return responseData;
        }
        private async Task<ResponseData> EjecutarRequestRangoFecha(List<DateTime> fechas, ResponseData responseData)
        {
            foreach (var pendiente in fechas)
            {
                var resp = RealizarRequest(pendiente, pendiente, responseData);
                // Actualizar la respuesta
                if (resp != null)
                    responseData = await resp;
            }
            return responseData;
        }
        private async Task<List<DateTime>> GetFechasPendientes(bool historico = false, DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            if (fechaInicio == null)
                fechaInicio = DateTime.Now.AddDays(-15);

            if (fechaFin == null)
                fechaFin = DateTime.Now;
            // Obtener todas las fechas que no fueron consultadas
            List<DateTime> fechasNoConsultadas = new();
            List<DateTime> fechasConsultadas = new();

            if (historico)
                fechasConsultadas = await Tools.ReadDates();



            for (DateTime f = (DateTime)fechaInicio; f <= fechaFin; f = f.AddDays(1))
            {
                if (historico)
                {
                    // Verificar si la fecha fue consultada
                    bool fechaConsultada = fechasConsultadas.Contains(f.Date);
                    // Si la fecha no fue consultada, agregarla a la lista
                    if (!fechaConsultada)
                        fechasNoConsultadas.Add(f);
                }
                else
                {
                    // No valida si se consulto o no antes! 
                    fechasNoConsultadas.Add(f);
                }

              

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
                            //Tools.PrintColor($"Rango consultado desde:", $" {fecha} - hasta:{fechah}", ConsoleColor.Green);

                        }
                        else
                        {
                            AddFechaError(responseData, fecha, fechah);
                            //Tools.PrintColor($"Rango consultado desde:", $" {fecha} - hasta:{fechah}");
                            //Tools.PrintColor("La respuesta del servidor está vacía.", null);
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
                    x.responseMessage ?? "Sin response Message",
                    erroresComunes.FirstOrDefault(e => x.responseMessage?.Contains(e) ?? false) ?? x.responseMessage?.Substring(0, Math.Min(200, x.responseMessage?.Length ?? 0)) ?? x.errorTipo
                )).ToList();
            // erroresComunes.FirstOrDefault(e => x.responseMessage?.Contains(e) ?? false) ?? x.responseMessage?.Substring(0, Math.Min(200, x.responseMessage?.Length ?? 0)) ?? "Sin error message"

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
        public async Task ExportTXT(ResponseData responseData)
        {
            CancellationTokenSource cancellationTokenSource = new();

            string rangoFecha = responseData.FechasConsultadas.ToString();

            StringBuilder errorContent = new();
            errorContent.AppendLine($"Fechas consultadas: {rangoFecha}");
            foreach (var errorTipoCountPair in responseData.ErroresAgrupado)
            {
                errorContent.AppendLine($"Cantidad de este error: {errorTipoCountPair.Count}");
                errorContent.AppendLine($"ErrorTipo: {errorTipoCountPair.ContieneErrorComun}, Cantidad: {errorTipoCountPair.Count}");
                errorContent.AppendLine($"TransportName: {errorTipoCountPair.TransportName}");
                errorContent.AppendLine($"RequestMessage: {errorTipoCountPair.RequestMessage}");
                errorContent.AppendLine($"ResponseMessage: {errorTipoCountPair.ResponseMessage}");
                errorContent.AppendLine();
            }

            StringBuilder messageContent = new();
            messageContent.AppendLine($"Fechas consultadas: {rangoFecha}");
            foreach (var data in responseData.MensajeData)
            {
                messageContent.AppendLine($"ErrorTipo: {data.errorTipo}");
                messageContent.AppendLine($"RequestId: {data.requestid}");
                messageContent.AppendLine($"TransportName: {data.transportname}");
                messageContent.AppendLine($"RequestMessage: {data.requestmessage}");
                messageContent.AppendLine($"ResponseMessage: {data.responseMessage}");
                messageContent.AppendLine();
            }

            await SaveFileAsync($"salidaResumida_{DateTime.Today.ToString("ddMMyyyy")}_Errores.txt", errorContent.ToString(), cancellationTokenSource.Token);
            await SaveFileAsync($"salidaCompleta_{DateTime.Today.ToString("ddMMyyyy")}_Mensajes.txt", messageContent.ToString(), cancellationTokenSource.Token);


        }

        private async Task SaveFileAsync(string fileName, string content, CancellationToken cancellationToken)
        {
            try
            {
                fileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fileName);

                var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

                await _fileSaver.SaveAsync(fileName, stream, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                Console.Error.WriteLine("Error saving file: " + ex.Message); // Example of logging
            }
        }


    }
}
