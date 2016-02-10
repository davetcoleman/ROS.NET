// File: XmlRpcUtil.cs
// Project: XmlRpc_Wrapper
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 11/18/2015
// Updated: 02/10/2016

#region USINGZ

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

#endregion

namespace XmlRpc_Wrapper
{
    public class XmlRpcException : Exception
    {
        private int errorCode = -1;

        public XmlRpcException(string msg, int errCode = -1)
            : base(msg)
        {
            errorCode = errCode;
        }

        public int getCode()
        {
            return errorCode;
        }
    }

    public enum HTTPHeaderField
    {
        Accept = 0,
        Accept_Charset = 1,
        Accept_Encoding = 2,
        Accept_Language = 3,
        Accept_Ranges = 4,
        Authorization = 5,
        Cache_Control = 6,
        Connection = 7,
        Cookie = 8,
        Content_Length = 9,
        Content_Type = 10,
        Date = 11,
        Expect = 12,
        From = 13,
        Host = 14,
        If_Match = 15,
        If_Modified_Since = 16,
        If_None_Match = 17,
        If_Range = 18,
        If_Unmodified_Since = 19,
        Max_Forwards = 20,
        Pragma = 21,
        Proxy_Authorization = 22,
        Range = 23,
        Referer = 24,
        TE = 25,
        Upgrade = 26,
        User_Agent = 27,
        Via = 28,
        Warn = 29,
        Age = 30,
        Allow = 31,
        Content_Encoding = 32,
        Content_Language = 33,
        Content_Location = 34,
        Content_Disposition = 35,
        Content_MD5 = 36,
        Content_Range = 37,
        ETag = 38,
        Expires = 39,
        Last_Modified = 40,
        Location = 41,
        Proxy_Authenticate = 42,
        Refresh = 43,
        Retry_After = 44,
        Server = 45,
        Set_Cookie = 46,
        Trailer = 47,
        Transfer_Encoding = 48,
        Vary = 49,
        Warning = 50,
        WWW_Authenticate = 51,
        HEADER_VALUE_MAX_PLUS_ONE = 52
    };

    /// <summary>
    ///     Does HTTP header parsing
    ///     Taken from ... somewhere.
    /// </summary>
    internal class HTTPHeader
    {
        #region PROPERTIES

        private string[] m_StrHTTPField = new string[52];
        private byte[] m_byteData = new byte[4096];

        public string[] HTTPField
        {
            get { return m_StrHTTPField; }
            set { m_StrHTTPField = value; }
        }

        public byte[] Data
        {
            get { return m_byteData; }
            set { m_byteData = value; }
        }

        #endregion

        // convertion
        public int IndexHeaderEnd = 0;
        public int LastIndex = 0;
        private ASCIIEncoding encoding = new ASCIIEncoding();

        #region CONSTRUCTEUR

        /// <summary>
        ///     Constructeur par d?faut - non utilis?
        /// </summary>
        private HTTPHeader()
        {
        }

        public HTTPHeader(string HTTPRequest)
        {
            try
            {
                IndexHeaderEnd = 0;
                string Header;

                // Si la taille de requ?te est sup?rieur ou ?gale ? 1460, alors toutes la chaine est l'ent?te http
                if (HTTPRequest.Length >= 1460)
                {
                    Header = HTTPRequest;
                }
                else
                {
                    IndexHeaderEnd = HTTPRequest.IndexOf("\r\n\r\n");
                    Header = HTTPRequest.Substring(0, IndexHeaderEnd);
                    Data = encoding.GetBytes(HTTPRequest.Substring(IndexHeaderEnd + 4));
                }

                HTTPHeaderParse(Header);
            }
            catch (Exception)
            {
            }
        }

        public HTTPHeader(byte[] ByteHTTPRequest)
        {
            string HTTPRequest = encoding.GetString(ByteHTTPRequest);
            try
            {
                //int IndexHeaderEnd;
                string Header;

                // Si la taille de requ?te est sup?rieur ou ?gale ? 1460, alors toutes la chaine est l'ent?te http
                if (HTTPRequest.Length >= 1460)
                    Header = HTTPRequest;
                else
                {
                    IndexHeaderEnd = HTTPRequest.IndexOf("\r\n\r\n");
                    Header = HTTPRequest.Substring(0, IndexHeaderEnd);
                    Data = encoding.GetBytes(HTTPRequest.Substring(IndexHeaderEnd + 4));
                }

                HTTPHeaderParse(Header);
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region METHODES

        private ConcurrentDictionary<HTTPHeaderField,string> HeaderFieldToStrings = new ConcurrentDictionary<HTTPHeaderField,string>();

        private void HTTPHeaderParse(string Header)
        {
            #region HTTP HEADER REQUEST & RESPONSE

            for (int f=(int)HTTPHeaderField.Accept; f<(int)HTTPHeaderField.HEADER_VALUE_MAX_PLUS_ONE; f++)
            {
                HTTPHeaderField HHField = (HTTPHeaderField) f;
                string HTTPfield = null;
                if (!HeaderFieldToStrings.TryGetValue(HHField, out HTTPfield))
                {
                    HTTPfield = "\n" + HHField.ToString().Replace('_', '-') + ": ";
                    HeaderFieldToStrings.TryAdd(HHField, HTTPfield);
                }

                // Si le champ n'est pas pr?sent dans la requ?te, on passe au champ suivant
                int Index = Header.IndexOf(HTTPfield, StringComparison.OrdinalIgnoreCase);
                if (Index == -1)
                    continue;

                string buffer = Header.Substring(Index + HTTPfield.Length);
                Index = buffer.IndexOf("\r\n", StringComparison.OrdinalIgnoreCase);
                if (Index == -1)
                    m_StrHTTPField[f] = buffer.Trim();
                else
                    m_StrHTTPField[f] = buffer.Substring(0, Index).Trim();

                if (m_StrHTTPField[f].Length == 0)
                {
                    XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.WARNING, "HTTP HEADER: field \"{0}\" has a length of 0", HHField.ToString());
                }
                XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.DEBUG, "HTTP HEADER: Index={0} | champ={1} = {2}", f, HTTPfield.Substring(1), m_StrHTTPField[HHField]);
            }

            #endregion
        }

        #endregion
    }

    [DebuggerStepThrough]
    public static class XmlRpcUtil
    {
        public enum XMLRPC_LOG_LEVEL
        {
            CRITICAL = 0,
            ERROR = 1,
            WARNING = 2,
            INFO = 3,
            DEBUG = 4,
            SPEW = 5
        }

        public static string XMLRPC_VERSION = "XMLRPC++ 0.7";
        private static XMLRPC_LOG_LEVEL MINIMUM_LOG_LEVEL = XMLRPC_LOG_LEVEL.ERROR;

        public static void SetLogLevel(XMLRPC_LOG_LEVEL level)
        {
            MINIMUM_LOG_LEVEL = level;
        }

        public static void SetLogLevel(int level)
        {
            SetLogLevel((XMLRPC_LOG_LEVEL) level);
        }

        public static void error(string format, params object[] list)
        {
            Debug.WriteLine(String.Format(format, list));
        }

        public static void log(int level, string format, params object[] list)
        {
            log((XMLRPC_LOG_LEVEL) level, format, list);
        }

        public static void log(XMLRPC_LOG_LEVEL level, string format, params object[] list)
        {
            if (level <= MINIMUM_LOG_LEVEL)
                Debug.WriteLine(String.Format(format, list));
        }
    }
}