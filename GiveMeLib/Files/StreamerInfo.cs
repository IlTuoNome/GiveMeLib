using System;
using System.Collections.Generic;
using System.Linq;

namespace GiveMeLib
{
    /// <summary>
    /// Streamer entity
    /// </summary>
    public class StreamerInfo
    {
        /// <summary>
        /// Streamer name
        /// </summary>
        public string StreamerName { get; set; }

        /// <summary>
        /// Instantgaming streamer giveaway
        /// </summary>
        public string StreamerLink { get; set; }

        /// <summary>
        /// Instantgaming streamer giveaway status
        /// </summary>
        public string StreamerStatus { get; set; }

        public StreamerInfo(string StreamerName = null, string StreamerLink = null, string StreamerStatus = null)
        {
            this.StreamerName = StreamerName;
            this.StreamerLink = StreamerLink;
            this.StreamerStatus = StreamerStatus;
        }

    }

    /// <summary>
    /// Operations list extender for StreamerInfo list
    /// </summary>
    public static class ListStreamExtender
    {
        /// <summary>
        /// Check if the list contains the streamer
        /// </summary>
        /// <param name="streamer">Streamer name to search</param>
        /// <returns>True if the list contains the streamer</returns>
        public static bool ContainsStreamer(this List<StreamerInfo> streamerInfos, string streamer)
        {
            return streamerInfos.Any(check => check.StreamerName == streamer);
        }

        /// <summary>
        /// Check if the list contains the link
        /// </summary>
        /// <param name="link">Link to search</param>
        /// <returns>True if the list contains the link</returns>
        public static bool ContainsLink(this List<StreamerInfo> streamerInfos, string link)
        {
            return streamerInfos.Any(check => check.StreamerLink == link);
        }

        /// <summary>
        /// Retrive streamer istance from the list by streamer name
        /// </summary>
        /// <param name="streamer">Streamer name</param>
        /// <returns>Istance if the list contains the streamer </returns>
        public static StreamerInfo RetriveStreamer(this List<StreamerInfo> streamerInfos, string streamer)
        {
            return streamerInfos.Where(check => check.StreamerName == streamer).FirstOrDefault();
        }

    }
}
