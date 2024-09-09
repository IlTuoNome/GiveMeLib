using System;
using System.Collections.Generic;
using Leaf.xNet;
using System.Threading;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GiveMeLib
{
    /// <summary>
    /// Twitch/InstantGaming operations class
    /// </summary>
    public static class InstantGamingGiveMe
    {
        #region Twitch-Instantgaming
        /// <summary>
        /// Search new streamers
        /// </summary>
        /// <param name="maxPage">Maximum number of pages to retrieve</param>
        /// <returns>StramerInfo list with all new streamers founded</returns>
        static public List<StreamerInfo> FindStreamer(short maxPage = 10)
        {
            List<StreamerInfo> streamers = new List<StreamerInfo>();
            using (HttpRequest richiesta = new HttpRequest())
            {
                richiesta.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:86.0) Gecko/20100101 Firefox/86.0";
                richiesta.IgnoreProtocolErrors = true;
                for(short count = 1; count <= maxPage; count++)
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
                                    streamers.Add(new StreamerInfo(userRaw));
                                }
                            }
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
        /// <param name="maxPage">Maximum number of pages to retrieve</param>
        /// <param name="streamerKnow">List with streamers already found</param>
        /// <returns>StramerInfo list with all new streamers founded</returns>
        static public List<StreamerInfo> FindStreamer(in List<StreamerInfo> streamerKnow, short maxPage = 10)
        {
            if(streamerKnow == null)
            {
                throw new Exception("StreamerKnow is null.");
            }
            else if(streamerKnow.Count == 0)
            {
                throw new Exception("No streamers in the streamerKnow list, use first FindStreamer.");
            }

            List<StreamerInfo> streamers = new List<StreamerInfo>();
            using (HttpRequest richiesta = new HttpRequest())
            {
                richiesta.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:86.0) Gecko/20100101 Firefox/86.0";
                richiesta.IgnoreProtocolErrors = true;
                for(short count = 1; count <= maxPage; count++)
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
                                        streamers.Add(new StreamerInfo(userRaw));
                                    }
                                }
                            }
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
            if(streamers == null)
            {
                throw new Exception("Streamers is null.");
            }
            else if (streamers.Count == 0)
            {
                throw new Exception("No streamers in the list.");
            }

            foreach (StreamerInfo streamer in streamers)
            {
                using (HttpRequest richiestaControllo = new HttpRequest())
                {
                    richiestaControllo.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:85.0) Gecko/20100101 Firefox/85.0";
                    richiestaControllo.IgnoreProtocolErrors = true;
                    try
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(30));
                        string richiestaRisposta = richiestaControllo.Get($"https://www.instant-gaming.com/en/giveaway/{streamer.StreamerName.ToUpper()}").ToString();
                        if (richiestaRisposta.Contains("404 Not Found"))
                        {
                            continue;
                        }
                        else
                        {
                            if (richiestaRisposta.Contains("This giveaway is over"))
                            {
                                Match vincitore = Regex.Match(richiestaRisposta, @"""nickname"":""([^""]+)""");

                                streamers.RetriveStreamer(streamer.StreamerName).StreamerLink = $"https://www.instant-gaming.com/en/giveaway/{streamer.StreamerName.ToUpper()}";
                                streamers.RetriveStreamer(streamer.StreamerName).StreamerStatus = $"Winner {vincitore.Value.Replace(@"""nickname"":""", string.Empty).Replace('"', ' ')}";
                            }
                            else if (richiestaRisposta.Contains("Win the game of your choice"))
                            {
                                streamers.RetriveStreamer(streamer.StreamerName).StreamerLink = $"https://www.instant-gaming.com/en/giveaway/{streamer.StreamerName.ToUpper()}";
                                streamers.RetriveStreamer(streamer.StreamerName).StreamerStatus = "Started";
                            }
                        }
                    }catch(Exception err)
                    {
                        if (err.Message == "Failed to receive the response from the HTTP-server 'instant-gaming.com'.")
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
        }

        /// <summary>
        /// Check the link status
        /// </summary>
        /// <param name="streamers">List with streamers</param>
        static public void CheckStatus(ref List<StreamerInfo> streamers)
        {
            if (streamers == null)
            {
                throw new Exception("Streamers is null.");
            }
            else if (streamers.Count == 0)
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
                    try
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(30));
                        string richiestaRisposta = richiestaControllo.Get(streamer.StreamerLink).ToString();
                        if (richiestaRisposta.Contains("404 Not Found"))
                        {
                            continue;
                        }
                        else
                        {
                            if (richiestaRisposta.Contains("This giveaway is over"))
                            {
                                Match vincitore = Regex.Match(richiestaRisposta, @"""nickname"":""([^""]+)""");
                                streamers.RetriveStreamer(streamer.StreamerName).StreamerStatus = $"Winner {vincitore.Value.Replace(@"""nickname"":""", string.Empty).Replace('"', ' ')}";
                            }
                            else if (richiestaRisposta.Contains("Win the game of your choice"))
                            {
                                streamers.RetriveStreamer(streamer.StreamerName).StreamerStatus = $"Started";
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        if (err.Message == "Failed to receive the response from the HTTP-server 'instant-gaming.com'.")
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
        }
        #endregion

        #region Database
        /// <summary>
        /// Recover streamers from online MySql database
        /// </summary>
        /// <param name="ipServer">Server Ip</param>
        /// <param name="portServer">Server Port</param>
        /// <param name="dbName">Db name</param>
        /// <param name="dbUser">Db username</param>
        /// <param name="dbPass">Db password</param>
        /// <returns>StramerInfo list with all database streamers</returns>
        static public List<StreamerInfo> DbOnlineRecover(string ipServer, string portServer, string dbName, string dbUser, string dbPass)
        {
            List<StreamerInfo> streamers = new List<StreamerInfo>();

            try
            {
                MySqlConnection connection = new MySqlConnection("server=" + ipServer + "; port=" + portServer + "; database=" + dbName + "; user id=" + dbUser + "; password=" + dbPass);

                connection.Open();

                using (MySqlCommand sqlCmd = new MySqlCommand("SELECT * FROM `Streamers`", connection))
                {
                    using (MySqlDataReader dataRead = sqlCmd.ExecuteReader())
                    {
                        while (dataRead.Read())
                        {
                            StreamerInfo streamer = new StreamerInfo(dataRead.GetString(0));
                            streamer.StreamerLink = (!string.IsNullOrEmpty(dataRead.GetString(1))) ? dataRead.GetString(1) : null;
                            streamer.StreamerStatus = (!string.IsNullOrEmpty(dataRead.GetString(2))) ? dataRead.GetString(2) : null;
                            streamers.Add(streamer);
                        }
                    }
                }

                connection.Close();
            }
            catch (Exception err)
            {
                throw new Exception(err.Message);
            }

            return (streamers.Count != 0) ? streamers : null;
        }

        /// <summary>
        /// Save streamers into database
        /// </summary>
        /// <param name="ipServer">Server Ip</param>
        /// <param name="portServer">Server Port</param>
        /// <param name="dbName">Db name</param>
        /// <param name="dbUser">Db username</param>
        /// <param name="dbPass">Db password</param>
        /// <param name="streamers">List of streamers to save</param>
        /// <param name="maxThreads">Maximum parallel db connections</param>
        static public void DbOnlineSave(string ipServer, string portServer, string dbName, string dbUser, string dbPass, in List<StreamerInfo> streamers, byte maxThreads = 5)
        {
            if(streamers == null)
            {
                throw new Exception("Streamers is null.");
            }
            else if (streamers.Count == 0)
            {
                throw new Exception("No streamers in the list.");
            }

            Parallel.ForEach(streamers, new ParallelOptions { MaxDegreeOfParallelism = maxThreads }, streamer =>
            {
                try
                {
                    MySqlConnection connection = new MySqlConnection("server=" + ipServer + "; port=" + portServer + "; database=" + dbName + "; user id=" + dbUser + "; password=" + dbPass);

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
                }catch(Exception err)
                {
                    throw new Exception(err.Message);
                }
            });
        }
        #endregion
    }
}