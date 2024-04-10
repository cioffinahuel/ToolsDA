namespace ToolsD.Models
{
    public class MensajeData
    {
        public string requestid { get; set; }
        public string transportname { get; set; }
        public DateTime requesttime { get; set; }
        public string errorTipo { get; set; }
        public string requestmessage { get; set; }
        public string responseMessage { get; set; }
    }
    public class Mensajes
    {
        public List<MensajeData> MensajeData { get; set; }
    }
    public class Response
    {
        public Mensajes Mensajes { get; set; }
    }
    public class ResponseData
    {
        public List<MensajeData> MensajeData { get; set; } = new List<MensajeData>();
        public Dictionary<string, ErrorData> ErrorTipoCount { get; set; } = new Dictionary<string, ErrorData>();
        public string FechasConsultadas { get; set; } = "";
        public string FechasError { get; set; } = "";

        public List<ErrorData> Errores = new();
        public List<ErrorData> ErroresAgrupado = new();


    }
    public class ErrorData
    {
        public int Count { get; set; }
        public string TransportName { get; set; }
        public string RequestMessage { get; set; }
        public string ResponseMessage { get; set; }
        public string ContieneErrorComun { get; set; }
        public ErrorData(int count, string transportName, string requestMessage, string responseMessage, string contieneErrorComun)
        {
            this.Count = count;
            this.TransportName = transportName;
            this.RequestMessage = requestMessage;
            this.ResponseMessage = responseMessage;
            this.ContieneErrorComun = contieneErrorComun;
        }
    }
    public class RequestLogs
    {
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public bool BuscarPendientes { get; set; }
        public bool Resumido { get; set; }
    }
}
