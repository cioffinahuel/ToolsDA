using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ToolsD.Models;

namespace ToolsD.Services
{
    public class FileService
    {
        readonly IConfiguration _configuration;
        readonly ILocalStorageService _localStorageService;
  


        public FileService(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
            InicializarErrores();
        }

        public void PrintColor(string Tipo, string Valor, ConsoleColor color = ConsoleColor.Red)
        {
          
        }


        #region Guardar y leer errores comunes
        public async void InicializarErrores()
        {
           await SaveErrors(new string[]
            {
                "Cannot insert the value NULL into column 'FECHA_INGRESO'",
                "Execution Timeout Expired.  The timeout period elapsed prior to completion of the operation or the server is not responding.",
                "Conversion failed when converting the nvarchar value 'Transaction (Process ID 556) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction. - ' to data type int.",
                "Conversion failed when converting the nvarchar value 'Transaction (Process ID 290) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction. - ' to data type int.",
                "Conversion failed when converting the nvarchar value 'Transaction (Process ID 492) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction. - ' to data type int.",
                "Conversion failed when converting the nvarchar value 'Transaction (Process ID 889) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction. - ' to data type int.",
                "Conversion failed when converting the nvarchar value 'Transaction (Process ID 645) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction. - ' to data type int.",
                "Conversion failed when converting the nvarchar value 'Transaction (Process ID 122) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction. - ' to data type int.",
                "Conversion failed when converting the nvarchar value 'Transaction (Process ID 131) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction. - ' to data type int."
            });

        }

        public async Task SaveErrors(string[] errors)
        {
            string serializedErrors = JsonConvert.SerializeObject(errors);
            await _localStorageService.SetItem("errors", serializedErrors);
        }
        private async Task WriteErrors(string[] errors)
        {
            List<string> existingErrors = await _localStorageService.GetItem<List<string>>("errors") ?? new List<string>();

            existingErrors.AddRange(errors);

            await _localStorageService.SetItem("errors", existingErrors);
        }

        public void DeleteErrors()
        {
            _localStorageService.RemoveItem("errors");
        }
        public async Task<string[]> ReadErrors()
        {
            string serializedErrors = await _localStorageService.GetItem<string>("errors");
            if (serializedErrors == null)
                return new string[0];

            return JsonConvert.DeserializeObject<string[]>(serializedErrors);
        }

        #endregion

        #region Guardar y leer fechas consultadas

        public async Task SaveDates(DateTime fd, DateTime fh)
        {
            List<DateTime> fechas = new List<DateTime>();

            for (DateTime f = fd; f <= fh; f = f.AddDays(1))
            {
                fechas.Add(f);
            }

            await WriteDates(fechas);
        }
        private async Task WriteDates(List<DateTime> dates)
        {
            List<DateTime> existingDates = await _localStorageService.GetItem<List<DateTime>>("consultedDates") ?? new List<DateTime>();

            dates.ForEach(date =>
            {
                if (!existingDates.Contains(date))
                {
                    existingDates.Add(date);
                }
            });

            await _localStorageService.SetItem("consultedDates", existingDates);
        }
        public void DeleteDates()
        {
           _localStorageService.RemoveItem("consultedDates");
        }
        public async Task<List<DateTime>> ReadDates()
        {
            List<DateTime> serializedDates = await _localStorageService.GetItem<List<DateTime>>("consultedDates");
            if (serializedDates == null)
                return new List<DateTime>();

            return serializedDates;
        }

        #endregion
        public Response DeserializeResponse(string responseContent)
        {
            try
            {
                JObject jsonObject = JObject.Parse(responseContent);

                Response response = new Response
                {
                    Mensajes = new Mensajes
                    {
                        MensajeData = DeserializeMensajeData(jsonObject)
                    }
                };

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al deserializar: {ex.Message}");
                return null;
            }
            return null;
        }

        private List<MensajeData> DeserializeMensajeData(JObject jsonObject)
        {
            try
            {
                JToken mensajeDataToken = jsonObject.SelectToken("Mensajes.MensajeData");

                if (mensajeDataToken != null)
                {
                    return mensajeDataToken.Type switch
                    {
                        JTokenType.Array => mensajeDataToken.ToObject<List<MensajeData>>(),
                        
                        JTokenType.Object => new List<MensajeData> { mensajeDataToken.ToObject<MensajeData>() },
                        _ => null,
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al deserializar MensajeData: {ex.Message}");
                return null;
            }
            return null;
        }

    }
}
