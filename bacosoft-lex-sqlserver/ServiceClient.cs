using System;
using System.Net;
using System.IO;
using System.Text;
using System.Security;
using System.Xml;

public partial class StoredProcedures
{
    private const string IMPORT = "/lex/api/import";

    /// <summary>
    /// Procedimiento para realizar consultas a la web API de Bacosoft LEX.
    /// </summary>
    /// <param name="baseUrl">La URL del servidor con el cual trabajar.</param>
    /// <param name="tenant">El código de la base de datos.</param>
    /// <param name="userName">Nombre de usuario.</param>
    /// <param name="password">Contraseña.</param>
    /// <param name="resource">El recurso que estamos consultando. Ejemplo: <code>/lex/api/pais/query</code>.</param>
    /// <param name="parameters">Texto con los parámetros de la consulta codificados en un XML. Ejemplo: <code>&lt;FindParameters&gt;&lt;projection&gt;detail&lt;/projection&gt;&lt;/FindParameters&gt;</code>.</param>
    /// <param name="error">Código de error. El valor 0 significa que la petición ha sido existosa.</param>
    /// <param name="response">Texto con el resultado codificado en un XML.</param>
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void BacolexQuery(string baseUrl, string tenant, string userName, string password, string resource, string parameters, out int error, out string response)
    {
        GetResponse(baseUrl + resource, tenant, userName, password, parameters, out error, out response);
    }

    /// <summary>
    /// Procedimiento para importar datos dentro de Bacosoft LEX utilizando la web API.
    /// </summary>
    /// <param name="baseUrl">La URL del servidor con el cual trabajar.</param>
    /// <param name="tenant">El código de la base de datos.</param>
    /// <param name="userName">Nombre de usuario.</param>
    /// <param name="password">Contraseña.</param>
    /// <param name="idEmpresa">Identificador de la empresa.</param>
    /// <param name="idEstablecimiento">Identificador del establecimiento</param>
    /// <param name="data">Texto con los datos a importar codificados en un XML.</param>
    /// <param name="validate">true para solo validar los datos a importar; false para importarlos.</param>
    /// <param name="error">Código de error. El valor 0 significa que la petición ha sido existosa pero
    /// es necesario analizar la respuesta para saber si todos los datos fueron importados.</param>
    /// <param name="response">Texto con el resultado de la validación o importación codificado en un XML.</param>
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void BacolexImport(string baseUrl, string tenant, string userName, string password, long? idEmpresa, long? idEstablecimiento, string data, bool validate, out int error, out string response)
    {
        string elemEmpresa = string.Empty;
        if (idEmpresa.HasValue)
        {
            elemEmpresa = $@"<bodega>{idEmpresa.Value}</bodega>";
        }
        string elemEstablecimiento = string.Empty;
        if (idEstablecimiento.HasValue)
        {
            elemEstablecimiento = $@"<establecimiento>{idEstablecimiento.Value}</establecimiento>";
        }
        data = SecurityElement.Escape(data);
        string body = $@"<ImportDataParameters>{elemEmpresa}{elemEstablecimiento}<data>{data}</data><validate>{validate}</validate></ImportDataParameters>";
        GetResponse(baseUrl + IMPORT, tenant, userName, password, body, out error, out response);
    }

    private static void GetResponse(string url, string tenant, string userName, string password, string body, out int error, out string respuesta)
    {
        try
        {
            HttpWebRequest req = CreateRequest(url, tenant, userName, password, body);
            respuesta = GetResponse(req);
            error = 0;
        }
        catch (Exception e)
        {
            GetError(e, out error, out respuesta);
        }
    }

    private static string GetResponse(HttpWebRequest request)
    {
        using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        using Stream receiveStream = response.GetResponseStream();
        using StreamReader readStream = new(receiveStream, Encoding.UTF8);
        return readStream.ReadToEnd();
    }

    private static void GetError(Exception e, out int error, out string respuesta)
    {
        // caso genérico
        error = -100;
        respuesta = e.Message;

        if (e is WebException webEx)
        {
            // si no encuentro una mejor razón, es un error de red
            error = -200;

            using HttpWebResponse resp = (HttpWebResponse)webEx.Response;
            if (resp != null)
            {
                error = (int)resp.StatusCode;
                string mensaje = GetMessage(resp);
                if (mensaje != null)
                {
                    respuesta = mensaje;
                }
            }
        }
    }

    private static string GetMessage(HttpWebResponse resp)
    {
        string res = null;
        try
        {
            using Stream receiveStream = resp.GetResponseStream();
            using StreamReader readStream = new(receiveStream, Encoding.UTF8);
            XmlDocument xdoc = new();
            xdoc.LoadXml(readStream.ReadToEnd());
            foreach (XmlNode node in xdoc.DocumentElement.ChildNodes)
            {
                if ("message".Equals(node.LocalName))
                {
                    res = node.InnerText;
                    break;
                }
            }
        }
        catch (Exception)
        {
            // Ignoramos no he sido capaz de obtener un mensaje de error
        }
        return res;
    }

    private static string GetTenantString(string tenant)
    {
        return "?tenant=" + tenant;
    }

    private static string GetCredentials(string userName, string password)
    {
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + password));
        return "Basic " + encoded;
    }

    private static HttpWebRequest CreateRequest(string url, string tenant, string userName, string password, string body)
    {
        // necesitamos TLS 1.2
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url + GetTenantString(tenant));
        request.Method = "POST";
        request.Headers.Add(HttpRequestHeader.Authorization, GetCredentials(userName, password));
        request.Accept = "application/xml";
        request.ContentType = "application/xml";

        using (var streamWriter = new StreamWriter(request.GetRequestStream()))
        {
            streamWriter.Write(body);
            streamWriter.Flush();
            streamWriter.Close();
        }
        return request;
    }
}
