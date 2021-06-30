using System;
using System.Collections.Generic;
using Leaf.xNet;
using System.Threading;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;


namespace GiveMeLib
{
    /// <summary>
    /// Twitch/InstantGaming operations class
    /// </summary>
    public class InstantGamingGiveMe
    {
        #region Twitch-Instantgaming
        /// <summary>
        /// Search new streamers
        /// </summary>
        /// <returns>StramerInfo list with all new streamers founded</returns>
        static public List<StreamerInfo> FindStreamer()
        {
            List<StreamerInfo> streamers = new List<StreamerInfo>();
            using (HttpRequest richiesta = new HttpRequest())
            {
                richiesta.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:86.0) Gecko/20100101 Firefox/86.0";
                richiesta.IgnoreProtocolErrors = true;
                short count = 1;
                while (true)
                {
                    try
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(2));
                        HttpResponse risposta = richiesta.Get($"https://twitchtracker.com/channels/live?page={count}");
                        if (risposta.ToString().Contains("503 Service Temporarily Unavailable"))
                        {
                            Thread.Sleep(TimeSpan.FromMinutes(1));
                            continue;
                        }
                        else if (!risposta.ToString().Contains("<td>#"))
                        {
                            break;
                        }
                        else
                        {
                            foreach (Match user in Regex.Matches(risposta.ToString(), @"style=""color:inherit"" href=""/\S+"">"))
                            {
                                string userRaw = user.Value.Replace(@"style=""color:inherit"" href=""/", "").Replace(@""">", "").ToUpper();
                                if (!streamers.ContainsStreamer(userRaw))
                                {
                                    streamers.Add(new StreamerInfo { StreamerName = userRaw });
                                }
                            }
                            count++;
                        }
                    }
                    catch(Exception err)
                    {
                        if (err.Message == "Failed to receive the response from the HTTP-server 'twitchtracker.com'.")
                        {
                            Thread.Sleep(TimeSpan.FromMinutes(1));
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            return (streamers.Count != 0) ? streamers : null;
        }

        /// <summary>
        /// Search new streamers without found duplicates
        /// </summary>
        /// <param name="streamerKnow">List with streamers already found</param>
        /// <returns>StramerInfo list with all new streamers founded</returns>
        static public List<StreamerInfo> FindStreamer(in List<StreamerInfo> streamerKnow)
        {
            List<StreamerInfo> streamers = new List<StreamerInfo>();
            using (HttpRequest richiesta = new HttpRequest())
            {
                richiesta.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:86.0) Gecko/20100101 Firefox/86.0";
                richiesta.IgnoreProtocolErrors = true;
                short count = 1;
                while (true)
                {
                    try
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(2));
                        HttpResponse risposta = richiesta.Get($"https://twitchtracker.com/channels/live?page={count}");
                        if (risposta.ToString().Contains("503 Service Temporarily Unavailable"))
                        {
                            Thread.Sleep(TimeSpan.FromMinutes(1));
                            continue;
                        }
                        else if (!risposta.ToString().Contains("<td>#"))
                        {
                            break;
                        }
                        else
                        {
                            foreach (Match user in Regex.Matches(risposta.ToString(), @"style=""color:inherit"" href=""/\S+"">"))
                            {
                                string userRaw = user.Value.Replace(@"style=""color:inherit"" href=""/", "").Replace(@""">", "").ToUpper();
                                if (!streamerKnow.ContainsStreamer(userRaw))
                                {
                                    if (!streamers.ContainsStreamer(userRaw))
                                    {
                                        streamers.Add(new StreamerInfo { StreamerName = userRaw });
                                    }
                                }
                            }
                            count++;
                        }
                    }
                    catch (Exception err)
                    {
                        if (err.Message == "Failed to receive the response from the HTTP-server 'twitchtracker.com'.")
                        {
                            Thread.Sleep(TimeSpan.FromMinutes(1));
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            return (streamers.Count != 0) ? streamers : null;
        }

        /// <summary>
        /// Test streamers for new links
        /// </summary>
        /// <param name="streamers">List with streamers</param>
        static public void TestStreamer(ref List<StreamerInfo> streamers)
        {
            if(streamers.Count == 0)
            {
                throw new Exception("No streamers in the list.");
            }

            foreach (StreamerInfo streamer in streamers)
            {
                using (HttpRequest richiestaControllo = new HttpRequest())
                {
                    richiestaControllo.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:85.0) Gecko/20100101 Firefox/85.0";
                    richiestaControllo.IgnoreProtocolErrors = true;
                    Thread.Sleep(TimeSpan.FromMilliseconds(30));
                    string richiestaRisposta = richiestaControllo.Get($"https://www.instant-gaming.com/en/giveaway/{streamer.StreamerName.ToUpper()}").ToString();
                    if (richiestaRisposta.Contains("404 Not Found"))
                    {
                        continue;
                    }
                    else
                    {
                        if (richiestaRisposta.Contains("has won"))
                        {
                            Match vincitore = Regex.Match(richiestaRisposta, @"<span class=""winner-nickname"">\S+</span>");

                            streamers.RetriveStreamer(streamer.StreamerName).StreamerLink = $"https://www.instant-gaming.com/en/giveaway/{streamer.StreamerName.ToUpper()}";
                            streamers.RetriveStreamer(streamer.StreamerName).StreamerStatus = $"Winner {vincitore.Value.Replace(@"<span class=""winner-nickname"">", string.Empty).Replace("</span>", string.Empty)}";
                        }
                        else if (richiestaRisposta.Contains("Win the game of your choice with"))
                        {
                            streamers.RetriveStreamer(streamer.StreamerName).StreamerLink = $"https://www.instant-gaming.com/en/giveaway/{streamer.StreamerName.ToUpper()}";
                            streamers.RetriveStreamer(streamer.StreamerName).StreamerStatus = "Started";
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check the link status
        /// </summary>
        /// <param name="streamers">List with streamers</param>
        static public void CheckStatus(ref List<StreamerInfo> streamers)
        {
            if (streamers.Count == 0)
            {
                throw new Exception("No streamers in the list.");
            }

            foreach (StreamerInfo streamer in streamers)
            {
                if(streamer.StreamerLink == null)
                {
                    continue;
                }

                using (HttpRequest richiestaControllo = new HttpRequest())
                {
                    richiestaControllo.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:85.0) Gecko/20100101 Firefox/85.0";
                    richiestaControllo.IgnoreProtocolErrors = true;
                    Thread.Sleep(TimeSpan.FromMilliseconds(30));
                    string richiestaRisposta = richiestaControllo.Get(streamer.StreamerLink).ToString();
                    if (richiestaRisposta.Contains("404 Not Found"))
                    {
                        continue;
                    }
                    else
                    {
                        if (richiestaRisposta.Contains("has won"))
                        {
                            Match vincitore = Regex.Match(richiestaRisposta, @"<span class=""winner-nickname"">\S+</span>");

                            streamers.RetriveStreamer(streamer.StreamerName).StreamerStatus = $"Winner {vincitore.Value.Replace(@"<span class=""winner-nickname"">", string.Empty).Replace("</span>", string.Empty)}";
                        }
                        else if (richiestaRisposta.Contains("Win the game of your choice with"))
                        {
                            streamers.RetriveStreamer(streamer.StreamerName).StreamerStatus = $"Started";
                        }
                    }
                }
            }
        }
        #endregion

        #region Database
        /// <summary>
        /// Recover streamers from online MySql database
        /// </summary>
        /// <param name="ipServer">Server Ip</param>
        /// <param name="portServer">Server Port</param>
        /// <param name="dbNome">Db name</param>
        /// <param name="dbUser">Db username</param>
        /// <param name="dbPass">Db password</param>
        /// <returns>StramerInfo list with all database streamers</returns>
        static public List<StreamerInfo> DbOnlineRecover(string ipServer, string portServer, string dbNome, string dbUser, string dbPass)
        {
            List<StreamerInfo> streamers = new List<StreamerInfo>();

            try
            {
                MySqlConnection connection = new MySqlConnection("server=" + ipServer + "; port=" + portServer + "; database=" + dbNome + "; user id=" + dbUser + "; password=" + dbPass);

                connection.Open();

                using (MySqlCommand sqlCmd = new MySqlCommand("SELECT * FROM `Streamers`", connection))
                {
                    using (MySqlDataReader dataRead = sqlCmd.ExecuteReader())
                    {
                        while (dataRead.Read())
                        {
                            streamers.Add(new StreamerInfo { StreamerName = dataRead.GetString(0), StreamerLink = dataRead.GetString(1), StreamerStatus = dataRead.GetString(2) });
                        }
                    }
                }

                connection.Close();
            }
            catch (Exception errore)
            {
                throw new Exception(errore.Message);
            }

            return (streamers.Count != 0) ? streamers : null;
        }

        /// <summary>
        /// Save streamers into database
        /// </summary>
        /// <param name="ipServer">Server Ip</param>
        /// <param name="portServer">Server Port</param>
        /// <param name="dbNome">Db name</param>
        /// <param name="dbUser">Db username</param>
        /// <param name="dbPass">Db password</param>
        /// <param name="streamers">List of streamers to save</param>
        /// <param name="maxthreads">Maximum parallel db connections</param>
        static public void DbOnlineSave(string ipServer, string portServer, string dbNome, string dbUser, string dbPass, in List<StreamerInfo> streamers, byte maxthreads = 5)
        {
            if (streamers.Count == 0)
            {
                throw new Exception("No streamers in the list.");
            }

            Parallel.ForEach(streamers, new ParallelOptions { MaxDegreeOfParallelism = maxthreads }, streamer =>
            {
                try
                {
                    MySqlConnection connection = new MySqlConnection("server=" + ipServer + "; port=" + portServer + "; database=" + dbNome + "; user id=" + dbUser + "; password=" + dbPass);

                    connection.Open();

                    using (MySqlCommand sqlCmd = new MySqlCommand($"SELECT * FROM `Streamers` WHERE streamer ='{streamer.StreamerName}'", connection))
                    {
                        object result = sqlCmd.ExecuteScalar();
                        if (result == null)
                        {
                            sqlCmd.CommandText = $"INSERT INTO `Streamers` (`Streamer`,`Link`,`Status`) VALUES('{streamer.StreamerName}', '{streamer.StreamerLink}', '{streamer.StreamerStatus}')";
                            sqlCmd.ExecuteNonQuery();
                        }
                        else
                        {
                            if (streamer.StreamerLink != null || streamer.StreamerStatus != null)
                            {
                                sqlCmd.CommandText = $"UPDATE `Streamers` SET `Streamer` = '{streamer.StreamerName}', `Link` = '{streamer.StreamerLink}', `Status` = '{streamer.StreamerStatus}' WHERE `Streamer` = '{streamer.StreamerName}'";
                                sqlCmd.ExecuteNonQuery();
                            }
                        }
                    }
                    connection.Close();
                }catch(Exception errore)
                {
                    throw new Exception(errore.Message);
                }
            });
        }
        #endregion
    }
}