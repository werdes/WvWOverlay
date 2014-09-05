using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TS3QueryLib.Core;
using TS3QueryLib.Core.Client;
using TS3QueryLib.Core.Client.Entities;
using TS3QueryLib.Core.Client.Notification.EventArgs;
using TS3QueryLib.Core.Client.Responses;
using TS3QueryLib.Core.Common;
using TS3QueryLib.Core.Common.Responses;
using TS3QueryLib.Core.Communication;

namespace WvWOverlay.Teamspeak
{
    public class ClientWrapper
    {
        public LogWriter m_oLogWriter;
        public ConnectionState Status;
        public TalkingState TalkingStatus = TalkingState.Quiet;
        private WhoAmIResponse m_oWhoAmiI;

        public ClientWrapper(LogWriter oLogWriter)
        {
            m_oLogWriter = oLogWriter;
        }

        #region Events

        /// <summary>
        /// Thrown at change of Talk-Status
        /// </summary>
        public event EventHandler TalkStatusChanged;


        #endregion

        #region ClientQueryLib

        private AsyncTcpDispatcher QueryDispatcher
        {
            get { return AppContext.Instance.Dispatcher; }
            set { AppContext.Instance.Dispatcher = value; }
        }

        private QueryRunner QueryRunner
        {
            get { return AppContext.Instance.QueryRunner; }
            set { AppContext.Instance.QueryRunner = value; }
        }


        public void Connect()
        {
            try
            {
                // do not connect when already connected or during connection establishing
                if (QueryDispatcher != null)
                    return;

                Status = ConnectionState.Connecting;
                QueryDispatcher = new AsyncTcpDispatcher("localhost", 25639);
                QueryDispatcher.ReadyForSendingCommands += QueryDispatcher_ReadyForSendingCommands;
                QueryDispatcher.ServerClosedConnection += QueryDispatcher_ServerClosedConnection;
                QueryDispatcher.SocketError += QueryDispatcher_SocketError;
                QueryDispatcher.NotificationReceived += QueryDispatcher_NotificationReceived;
                QueryDispatcher.Connect();
            }
            catch (Exception oEx)
            {
                Status = ConnectionState.NoClientDetected;
                m_oLogWriter.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        private void QueryDispatcher_ReadyForSendingCommands(object sender, System.EventArgs e)
        {
            try
            {
                Status = ConnectionState.Connected;
                // you can only run commands on the queryrunner when this event has been raised first!
                QueryRunner = new QueryRunner(QueryDispatcher);
                QueryRunner.Notifications.ChannelTalkStatusChanged += Notifications_ChannelTalkStatusChanged;
                QueryRunner.RegisterForNotifications(ClientNotifyRegisterEvent.Any);

                m_oLogWriter.WriteMessage("TS Connection established", LogWriter.MESSAGE_TYPE.Info);

                ReloadIdentity();

                if (ClientIsMuted())
                {
                    TalkingStatus = TalkingState.MicrophoneMuted;

                    EventExtensions.RaiseEvent(TalkStatusChanged, this, null);
                }

                EventExtensions.RaiseEvent(TalkStatusChanged, this, null);
            }
            catch (Exception oEx)
            {
                Status = ConnectionState.NoClientDetected;
                m_oLogWriter.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        /// <summary>
        /// Liefert, ob der Client gemuted ist
        /// </summary>
        /// <returns></returns>
        private bool ClientIsMuted()
        {
            bool bRetVal = false;
            string cQuery;
            try
            {
                cQuery = QueryRunner.SendRaw("clientvariable clid=" + m_oWhoAmiI.ClientId + " client_input_muted");

                if (cQuery.Contains("client_input_muted=1"))
                {
                    bRetVal = true;
                }
            }
            catch (Exception oEx)
            {
                m_oLogWriter.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
            return bRetVal;
        }

        public void ReloadIdentity()
        {
            try
            {
                m_oWhoAmiI = QueryRunner.SendWhoAmI();
            }
            catch (Exception oEx)
            {
                m_oLogWriter.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        private void QueryDispatcher_ServerClosedConnection(object sender, System.EventArgs e)
        {
            try
            {
                // this event is raised when the connection to the server is lost.
                m_oLogWriter.WriteMessage("Connection to server closed/lost.", LogWriter.MESSAGE_TYPE.Warning);

                // dispose

                this.Disconnect();
            }
            catch (Exception oEx)
            {
                Status = ConnectionState.NoClientDetected;
                m_oLogWriter.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        private void QueryDispatcher_BanDetected(object sender, EventArgs<SimpleResponse> e)
        {
            try
            {

                // force disconnect
                Disconnect();
                EventExtensions.RaiseEvent(TalkStatusChanged, this, null);
            }
            catch (Exception oEx)
            {
                Status = ConnectionState.NoClientDetected;
                m_oLogWriter.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        private void QueryDispatcher_SocketError(object sender, SocketErrorEventArgs e)
        {
            try
            {
                // do not handle connection lost errors because they are already handled by QueryDispatcher_ServerClosedConnection
                if (e.SocketError == SocketError.ConnectionReset)
                    return;
                if (e.SocketError == SocketError.ConnectionRefused)
                {
                    //Kein Client
                    Status = ConnectionState.NoClientDetected;
                    m_oLogWriter.WriteMessage("Kein TS-Client gefunden", LogWriter.MESSAGE_TYPE.Info);
                    EventExtensions.RaiseEvent(TalkStatusChanged, this, null);
                    Disconnect();
                    return;
                }

                // this event is raised when a socket exception has occured
                m_oLogWriter.WriteMessage("Socket error!! Error Code: " + e.SocketError, LogWriter.MESSAGE_TYPE.Error);

                EventExtensions.RaiseEvent(TalkStatusChanged, this, null);
                // force disconnect
                Disconnect();
            }
            catch (Exception oEx)
            {
                Status = ConnectionState.NoClientDetected;
                m_oLogWriter.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        private void QueryDispatcher_NotificationReceived(object sender, EventArgs<string> e)
        {
            try
            {
                uint nClientID = 0;

                if (e.Value.Contains("clid"))
                    nClientID = uint.Parse(e.Value.Split(' ').Single(x => x.Contains("clid")).Split('=')[1]);

                if (nClientID > 0)
                {
                    //ich?
                    if (m_oWhoAmiI.ClientId == nClientID)
                    {
                        if (e.Value.Contains("client_input_muted=0"))
                        {
                            //entmute
                            TalkingStatus = TalkingState.Quiet;
                            EventExtensions.RaiseEvent(TalkStatusChanged, this, null);
                        }
                        else if (e.Value.Contains("client_input_muted=1"))
                        {
                            //mute
                            TalkingStatus = TalkingState.MicrophoneMuted;
                            EventExtensions.RaiseEvent(TalkStatusChanged, this, null);
                        }
                    }
                }
            }
            catch (Exception oEx)
            {
                Status = ConnectionState.NoClientDetected;
                m_oLogWriter.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        private void Notifications_ChannelTalkStatusChanged(object sender, TalkStatusEventArgsBase e)
        {
            try
            {
                //ich?
                if (m_oWhoAmiI.ClientId == e.ClientId)
                {
                    if (TalkingStatus != TalkingState.MicrophoneMuted)
                    {
                        if (e.TalkStatus == TS3QueryLib.Core.Client.Notification.Enums.TalkStatus.TalkStarted)
                        {
                            TalkingStatus = TalkingState.Talking;
                        }
                        else
                        {
                            TalkingStatus = TalkingState.Quiet;
                        }
                        EventExtensions.RaiseEvent(TalkStatusChanged, this, null);
                    }
                }
            }
            catch (Exception oEx)
            {
                Status = ConnectionState.NoClientDetected;
                m_oLogWriter.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        public void Disconnect()
        {
            try
            {
                if (QueryDispatcher != null)
                    QueryDispatcher.DetachAllEventListeners();
                // QueryRunner disposes the Dispatcher too
                if (QueryRunner != null)
                    QueryRunner.Dispose();

                QueryDispatcher = null;
                QueryRunner = null;
                Status = ConnectionState.Disconnected;
            }
            catch (Exception oEx)
            {
                Status = ConnectionState.NoClientDetected;
                m_oLogWriter.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        #endregion
    }


    public class AppContext
    {
        private static AppContext _instance;

        public AsyncTcpDispatcher Dispatcher { get; set; }
        public QueryRunner QueryRunner { get; set; }

        public static AppContext Instance
        {
            get { return _instance ?? (_instance = new AppContext()); }
        }
    }

    public enum ConnectionState
    {
        Connecting,
        Connected,
        Disconnected,
        NoClientDetected
    }

    public enum TalkingState
    {
        Quiet,
        Talking,
        MicrophoneMuted
    }

}
