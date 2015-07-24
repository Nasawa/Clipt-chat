using Quobject.SocketIoClientDotNet.Client;
using System;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Clipt_Chat
{
    public partial class ChatWindow : Form
    {
        private string[] arr = { "Connect", "Disconnect", "http://", "Chat", "GLOBAL", "has joined the chat on", "id", "message", "Disconnected from server" };
        private string id, user;

        private Socket socket = null;

        [DllImport("user32.dll")]
        static extern bool FlashWindow(IntPtr hwnd, bool bInvert);
        public ChatWindow()
        {
            InitializeComponent();
            FillData();
        }

        private delegate void WriteLogCallback(string log);

        private enum strings : int
        {
            Connect, Disconnect, http, chat, global, joined, id, msg, left
        };
        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (socket != null)
            {
                socket.Disconnect();
                socket = null;
                btnConnect.Text = E(strings.Connect);
                WriteLog(E(strings.left));
            }
            else
            {
                if (btnConnect.Text == E(strings.Disconnect))
                    return;
                btnConnect.Text = E(strings.Disconnect);
                chatOutput.Clear();
                id = textRoom.Text;
                user = textUser.Text;
                if (textServer.Text.Substring(0, 7) != E(strings.http))
                    textServer.Text = String.Format("{0}{1}", E(strings.http), textServer.Text);
                SaveData();
                this.Text = String.Format("{0} - #{1}", E(strings.chat), id);
                socket = IO.Socket(textServer.Text);
                socket.Once(Socket.EVENT_CONNECT, () =>
                {
                    socket.Emit(E(strings.msg), id, string.Format("{0}: {1} {2} #{3}", E(strings.global), user, E(strings.joined), id));
                });
                socket.Once(E(strings.id), () =>
                {
                    socket.On(id + "", (msg) =>
                    {
                        WriteLog(msg.ToString());
                    });

                    chatInput.Enter += chatInput_Enter;
                    chatInput.Leave += chatInput_Leave;
                });
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (socket != null && chatInput.TextLength > 0)
            {
                socket.Emit(E(strings.msg), id, string.Format("{0}: {1}", user, chatInput.Text));
                chatInput.ResetText();
            }
        }

        private void chatInput_Enter(object sender, EventArgs e)
        {
            ActiveForm.AcceptButton = btnSend;
        }

        private void chatInput_Leave(object sender, EventArgs e)
        {
            ActiveForm.AcceptButton = null;
        }

        private string E(strings s)
        {
            return arr[(int)s];
        }
        private void FillData()
        {
            if (string.IsNullOrEmpty(textServer.Text))
                textServer.Text = Properties.Settings.Default.Server;
            if (string.IsNullOrEmpty(textRoom.Text))
                textRoom.Text = Properties.Settings.Default.Room;
            if (string.IsNullOrEmpty(textUser.Text))
                textUser.Text = Properties.Settings.Default.User;
        }

        private void SaveData()
        {
            Properties.Settings.Default.Server = textServer.Text;
            Properties.Settings.Default.Room = textRoom.Text;
            Properties.Settings.Default.User = textUser.Text;
            Properties.Settings.Default.Save();
        }

        private void WriteLog(string log)
        {
            if (log.Split(':')[0] != user)
                SystemSounds.Beep.Play();
            if (this.chatOutput.InvokeRequired)
            {
                WriteLogCallback d = new WriteLogCallback(WriteLog);
                this.Invoke(d, new object[] { log });
            }
            else
            {
                this.chatOutput.AppendText(log + "\n");
                this.chatOutput.ScrollToCaret();
                FlashWindow(this.Handle, true);
            }
        }
    }
}