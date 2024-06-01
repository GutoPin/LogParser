using System;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        //Conecccion de la DB
        string connectionString = "Conexion a la base de datos";
        //Coneccion del txt o .log
        string toSearch = File.ReadAllText("Ruta del archivo");

        string pattern = @"(?<hora>[0-9]{2}\:[0-9]{2}\:[0-9]{2}\.[0-9]{3})\s\[(?<idlog>[0-9]{4,10})\]\sDelivery\sstarted\sfor\sbounce\+(?<rpath>[^\@]+)@(?<emailto>[^\s]*)\s(.*)";
        string pattern2 = @"(?<hora>[0-9]{2}\:[0-9]{2}\:[0-9]{2}\.[0-9]{3})\s\[(?<idlog>[0-9]{4,10})\]\sDelivery\sfor\sbounce\+(?<rpath>[^\@]+)@(?<emaildomain>[^\s]*)\sto\s(?<to>[^\s]*)\shas\s(?:completed|)[\s]*\(*(?<estado>Delivered|Bounced|Deleted)\)*(?<razon>.*)";

        string sqlInsert = @"
                    INSERT INTO SMLOGS
                    (IDSMTP, IDLOG, IDFILE, NroLinea, IDMAILNEWS, TIPO, IDSCHEDULLE, IDUSUARIO,
                    RPATH, EMAILTO, HORA_ENVIO, ESTADO, RAZON, SMLOG, R)
                    VALUES
                    (@IdSMTP, @IdLog, @IdFile, @NroLinea, @IdMailNews, @Tipo, @IdSchedulle, @IdUsuario, 
                    @RPath, @EmailTo, @Hora, @Estado, @Razon, @Smlog, @R)";

        MatchCollection matches = Regex.Matches(toSearch, pattern);
        MatchCollection matches2 = Regex.Matches(toSearch, pattern2);

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            using (SqlCommand command = new SqlCommand(sqlInsert, connection))
            {
                foreach (Match match in matches)
                {
                    int matchIndex = match.Index;
                    string lineNumber = (CountLines(toSearch.Substring(0, matchIndex)) + 1).ToString();

                    command.Parameters.Clear();

                    string idSMTP = match.Groups["IdSMTP"].Value;           //Hecho
                    string idlog = match.Groups["idlog"].Value;             //Hecho
                    string idFile = match.Groups["idFile"].Value;
                    string idMailNews = match.Groups["idMailNews"].Value;
                    string tipo = match.Groups["tipo"].Value;
                    string idSchedulle = match.Groups["idSchedulle"].Value;
                    string idUsuario = match.Groups["idUsuario"].Value;
                    string rpath = match.Groups["rpath"].Value;             //Hecho
                    string emailto = match.Groups["emailto"].Value;         //Hecho
                    string hora = match.Groups["hora"].Value;               //Hecho
                    string smlog = match.Groups["smlog"].Value;
                    string r = match.Groups["r"].Value;

                    command.Parameters.AddWithValue("@Estado", DBNull.Value);
                    command.Parameters.AddWithValue("@Razon", DBNull.Value);


                    foreach (Match match2 in matches2)
                    {
                        string idlog2 = match2.Groups["idlog"].Value;
                        string estado2 = match2.Groups["estado"].Value;
                        string razon2 = match2.Groups["razon"].Value;
                        string charest = null;

                        switch (estado2)
                        {
                            case "Delivered":
                                charest = "1";
                                break;
                            case "Bounced":
                                charest = "2";
                                break;
                            case "Deleted":
                                charest = "3";
                                break;
                            default:
                                break;
                        }

                        if (idlog == idlog2)
                        {
                            command.Parameters["@Estado"].Value = string.IsNullOrEmpty(charest) ? (object)DBNull.Value : charest;
                            command.Parameters["@Razon"].Value = string.IsNullOrEmpty(razon2) ? (object)DBNull.Value : razon2;
                        }
                    }

                    command.Parameters.AddWithValue("@IdSMTP", string.IsNullOrEmpty(idSMTP) ? "0" : idSMTP);
                    command.Parameters.AddWithValue("@IdLog", string.IsNullOrEmpty(idlog) ? "0" : idlog);
                    command.Parameters.AddWithValue("@IdFile", string.IsNullOrEmpty(idFile) ? "0" : idFile);
                    command.Parameters.AddWithValue("@NroLinea", string.IsNullOrEmpty(lineNumber) ? "0" : lineNumber);
                    command.Parameters.AddWithValue("@IdMailNews", string.IsNullOrEmpty(idMailNews) ? "0" : idMailNews);
                    command.Parameters.AddWithValue("@Tipo", string.IsNullOrEmpty(tipo) ? "0" : tipo);
                    command.Parameters.AddWithValue("@IdSchedulle", string.IsNullOrEmpty(idSchedulle) ? "0" : idSchedulle);
                    command.Parameters.AddWithValue("@IdUsuario", string.IsNullOrEmpty(idUsuario) ? "0" : idUsuario);
                    command.Parameters.AddWithValue("@RPath", string.IsNullOrEmpty(rpath) ? "0" : rpath);
                    command.Parameters.AddWithValue("@EmailTo", string.IsNullOrEmpty(emailto) ? "0" : emailto);
                    command.Parameters.AddWithValue("@Hora", string.IsNullOrEmpty(hora) ? "0" : hora);

                    command.Parameters.AddWithValue("@Smlog", string.IsNullOrEmpty(smlog) ? "0" : smlog);
                    command.Parameters.AddWithValue("@R", string.IsNullOrEmpty(r) ? "0" : r);

                    Console.WriteLine($"Linea: {lineNumber}, Hora: {hora}, ID Log: {idlog}, RPath: {rpath}, Email Domain: {emailto}, IDSmtp: {idSMTP}");
                    command.ExecuteNonQuery();
                }
            }
        }
    }

    static int CountLines(string text)
    {
        int count = 0;
        int index = -1;

        while ((index = text.IndexOf('\n', index + 1)) != -1)
        {
            count++;
        }

        return count;
    }

}

/*
 * ^ - Starts with
 * $ - Ends with
 * [] - Range
 * () - Group
 * . - Single character once
 * + - one or more characters in a row
 * ? - optional preceding character match
 * \ - escape character
 * \n - New line
 * \d - Digit
 * \D - Non-digit
 * \s - White space
 * \S - non-white space
 * \w - alphanumeric/underscore character (word chars)
 * \W - non-word characters
 * {x,y} - Repeat low (x) to high (y) (no "y" means at least x, no ",y" means that many)
 * (x|y) - Alternative - x or y
 * 
 * [^x] - Anything but x (where x is whatever character you want)
 */
