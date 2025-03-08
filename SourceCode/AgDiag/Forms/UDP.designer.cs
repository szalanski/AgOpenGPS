using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace AgDiag
{
    public partial class FormLoop
    {
        // Server socket
        private Socket recvFromLoopBackSocket;

        private EndPoint epSender = new IPEndPoint(IPAddress.Any, 0);

        // Data stream
        private byte[] buffer = new byte[1024];

        private int cntr;

        private void LoadLoopback()
        { 
            try //loopback
            {
                // Initialise the socket
                recvFromLoopBackSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // AgDiag listens on this port
                recvFromLoopBackSocket.Bind(new IPEndPoint(IPAddress.Any, 17777)); //old version is 15555

                // Initialise the IPEndPoint for the client
                EndPoint client = new IPEndPoint(IPAddress.Any, 0);

                // Start listening for incoming data
                recvFromLoopBackSocket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref client, new AsyncCallback(ReceiveDataLoopAsync), recvFromLoopBackSocket);

            }
            catch (Exception ex)
            {
                //lblStatus.Text = "Error";
                MessageBox.Show("Load Error: " + ex.Message, "UDP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //loopback functions
        private void ReceiveFromLoopBack(int port, byte[] data)
        {
            if (data[0] == 0x80 && data[1] == 0x81)
            {
                switch (data[3])
                {
                    case 253:
                        {
                            for (int i = 5; i < data.Length; i++)
                            {
                                asModule.pgn[i] = data[i];
                            }

                            break;
                        }
                    case 254:
                        {

                            for (int i = 5; i < data.Length; i++)
                            {
                                asData.pgn[i] = data[i];
                            }

                            break;
                        }
                    case 252:
                        {

                            for (int i = 5; i < data.Length; i++)
                            {
                                asSet.pgn[i] = data[i];
                            }

                            break;
                        }
                    case 251:
                        {

                            for (int i = 5; i < data.Length; i++)
                            {
                                asConfig.pgn[i] = data[i];
                            }

                            break;
                        }
                    case 239:
                        {

                            for (int i = 5; i < data.Length; i++)
                            {
                                maData.pgn[i] = data[i];
                            }

                            break;
                        }

                    default:
                        {
                            lblDefaultSends.Text = data.Length.ToString();
                            break;
                        }
                }
            }
            else
            {
                cntr += data.Length;
                lblDefaultSends.Text = cntr.ToString();
            }
        }

        private void ReceiveDataLoopAsync(IAsyncResult asyncResult)
        {
            try
            {
                // Initialise the IPEndPoint for the clients

                // Receive all data
                int msgLen = recvFromLoopBackSocket.EndReceiveFrom(asyncResult, ref epSender);

                byte[] localMsg = new byte[msgLen];
                Array.Copy(buffer, localMsg, msgLen);

                // Listen for more connections again...
                recvFromLoopBackSocket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref epSender, new AsyncCallback(ReceiveDataLoopAsync), recvFromLoopBackSocket);

                //string text = Encoding.ASCII.GetString(localMsg);

                // Update status through a delegate
                int port = ((IPEndPoint)epSender).Port;
                BeginInvoke((MethodInvoker)(() => ReceiveFromLoopBack(port, localMsg)));
            }
            catch (Exception)
            {
                //MessageBox.Show("ReceiveData Error: " + ex.Message, "UDP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
